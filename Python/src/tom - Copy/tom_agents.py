"""
Created on Aug 19, 2018

@author: Siyuan Qi

Description of the file.

"""

import os

import torch
import numpy as np

import tom.memory
from tom.belief_transition import TransitionKernel, BeliefEstimation


class ToMAgent(object):
    epsilon = 0.00  # Epsilon greedy
    alpha = 5e-2  # Learning rate for RL
    gamma = 0.95  # Discount factor for reward
    lbd = 0.95  # Discount factor for eligibility trace

    lr = 1e-3
    memory_size = 10000
    batch_size = 512

    # action space
    actions = np.zeros((5, 5))
    actions[np.arange(5), [0, 1, 2, 3, 4]] = 1
    forces = np.array([[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1]])

    use_gt_belief = False
    direct_chase = False
    use_distance = True
    distance_bin_num = 7
    distance_bin_size = 0.2
    weight = np.array([0.0, 0.0, 0.0]) if use_distance else np.array([0.0, 0.0])
    # weight = np.array([7.0, 8.75, -10.0])

    model_initialized = False
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    kernel_model = kernel_optimizer = estimator_model = estimator_optimizer = None
    loss_fn = torch.nn.MSELoss()
    forces = torch.Tensor(forces).to(device)
    pred_covar = filter_covar = None
    pred_sample_count = filter_sample_count = 0

    def __init__(self, agent, world, scenario, all_obs, setting='known_thief', use_gt_belief=False, direct_chase=False, use_distance=True):
        self.agent = agent
        self.world = world
        self.scenario = scenario
        self.thief = self.world.thieves[0]  # assume the thief is known?
        self.setting = setting
        self.__class__.use_gt_belief = use_gt_belief
        self.__class__.use_distance = use_distance
        self.__class__.direct_chase = direct_chase

        if use_distance:
            if direct_chase:
                self.__class__.weight = np.array([0.0, 0.0] + [self.distance_bin_num - i for i in range(self.distance_bin_num)])
            else:
                # Randomize initial weights
                self.__class__.weight = np.random.rand(self.distance_bin_num + 2)*0.01
        else:
            self.__class__.weight = np.array([0.0, 0.0])

        self.eligibility = np.zeros_like(self.weight)

        self.memory = tom.memory.Memory(self.memory_size)
        self.prev_obs, self.prev_belief = self.observe_agent(all_obs, self.get_permuted_index(self.thief.index)), self.get_gt_belief()
        self.prev_action_choice = None
        self.belief_size = self.prev_belief.shape[0]
        self.belief_mean = self.belief_covar = None
        self.initialize()

        self.epoch = 0
        if not self.model_initialized:
            self.__class__.kernel_model = TransitionKernel(self.prev_obs.shape[0], self.forces[0, :].shape[0], self.belief_size).to(self.device)
            self.__class__.kernel_optimizer = torch.optim.Adam(self.kernel_model.parameters(), lr=self.lr)
            self.__class__.estimator_model = BeliefEstimation(self.prev_obs.shape[0], self.belief_size).to(self.device)
            self.__class__.estimator_optimizer = torch.optim.Adam(self.estimator_model.parameters(), lr=self.lr)
            self.__class__.loss_fn = torch.nn.MSELoss()

            self.__class__.pred_covar = torch.zeros((self.belief_size, self.belief_size), dtype=torch.float32, requires_grad=False).to(self.device)
            self.__class__.filter_covar = torch.zeros((self.belief_size, self.belief_size), dtype=torch.float32, requires_grad=False).to(self.device)

    def initialize(self):
        self.eligibility = np.zeros_like(self.weight)

        self.belief_mean = torch.ones((1, self.belief_size), dtype=torch.float32).to(self.device)  # batch_size x self.belief_size
        self.belief_mean /= self.belief_size  # Uniform belief
        self.belief_covar = torch.zeros((self.belief_size, self.belief_size), dtype=torch.float32).to(self.device)

    # ================================ Observe environment ================================
    # obs, force, belief are always torch tensors
    def observe_agent(self, all_obs, permuted_index):
        other_obs = [torch.Tensor(quantities[permuted_index]) for quantities in all_obs]
        own_obs = [torch.Tensor(quantities[self.get_permuted_index(self.agent.index)]) for quantities in all_obs]
        return torch.cat(own_obs+other_obs).to(self.device)

    def get_gt_belief(self):
        belief = torch.Tensor(self.thief.belief[self.agent.index]).to(self.device)
        return belief

    def get_permuted_index(self, index):
        permuted_index = self.scenario.permutation[index]
        return permuted_index

    # ================================ Planning ================================
    def compute_value(self):
        if self.use_gt_belief:
            return np.dot(self.get_gt_state_feature(self.thief), self.weight)
        else:
            return np.dot(self.get_state_feature(self.thief), self.weight)

    def action_value(self, all_obs, action, suspect):
        force = self.forces[np.argmax(action), :]
        return np.dot(self.get_next_state_feature(all_obs, force, suspect), self.weight)

    def select_action(self, all_obs):
        # select action using epsilon greedy.
        if np.random.random() < self.epsilon:
            self.prev_action_choice = np.random.randint(5)
        else:
            action_values = [self.action_value(all_obs, action, self.thief) for action in self.actions]
            self.prev_action_choice = np.argmax(action_values)
        return self.actions[self.prev_action_choice]

    def get_distance_feature(self, distance):
        distance_feature = np.zeros(self.distance_bin_num)

        # Piecewise linear value function for distance
        bin = int(distance/self.distance_bin_size)
        k = ((bin+1) * self.distance_bin_size - distance) / self.distance_bin_size
        distance_feature[min(self.distance_bin_num-1, bin)] = k
        distance_feature[min(self.distance_bin_num-1, bin + 1)] = 1 - k

        return distance_feature

    def get_state_feature(self, thief):
        belief = self.belief_mean.squeeze().detach().cpu().numpy()
        # print("belief", belief)
        if self.use_distance:
            distance = self.world.distance(self.agent, thief)
            return np.concatenate((belief, self.get_distance_feature(distance)))
        else:
            return belief

    def get_gt_state_feature(self, thief):
        if self.use_distance:
            distance = self.world.distance(self.agent, thief)
            return np.concatenate((thief.belief[self.agent.index], self.get_distance_feature(distance)))
        else:
            return thief.belief[self.agent.index]

    def get_next_state_feature(self, all_obs, force, thief):
        if self.use_gt_belief:
            belief = thief.compute_new_belief(self.agent, force)  # Use ground truth belief transition
        else:
            thief_index = self.get_permuted_index(thief.index)
            _, belief = self.belief_prediction(self.belief_mean, self.observe_agent(all_obs, thief_index).unsqueeze(-2), force.unsqueeze(-2))  # TODO
            belief = belief.squeeze().detach().cpu().numpy()

        if self.use_distance:
            next_vel, next_pos = self.world.compute_next_pos(self.agent, force)
            distance = self.world.pos_distance(next_pos, thief.state.p_pos)
            return np.concatenate((belief, self.get_distance_feature(distance)))
        else:
            return belief

    # ================================ Belief update ================================
    def belief_prediction(self, prev_belief, obs, force):
        # prev_belief size: batch_size x belief_size
        kernel = self.kernel_model.forward(obs, force)
        belief = torch.bmm(prev_belief.unsqueeze(-2), kernel)  # batch_size x 1 x belief_size
        return kernel, belief.squeeze(-2)  # batch_size x belief_size

    def bayes_update(self, all_obs, force):
        # Prediction
        thief_index = self.get_permuted_index(self.thief.index)
        obs = self.observe_agent(all_obs, thief_index).unsqueeze(-2)
        kernel, pred_mean = self.belief_prediction(self.belief_mean, obs, force.unsqueeze(-2))
        kernel = kernel.squeeze()
        pred_covar = self.pred_covar / self.pred_sample_count + torch.mm(torch.mm(kernel, self.belief_covar), kernel)

        # Correction
        sample_mean = self.estimator_model.forward(obs)
        sum_inv = np.linalg.pinv((pred_covar + self.filter_covar / self.filter_sample_count).detach().cpu().numpy())
        sum_inv = torch.Tensor(sum_inv).to(self.device)
        filter_mean = pred_mean + torch.mm(torch.mm(pred_covar, sum_inv), (sample_mean - pred_mean).t()).t()
        filter_covar = pred_covar - torch.mm(torch.mm(pred_covar, sum_inv), pred_covar)
        self.belief_mean = filter_mean
        self.belief_covar = filter_covar

        # Renormalize to enure it is a valid probability
        if torch.min(self.belief_mean) < 0:
            self.belief_mean -= torch.min(self.belief_mean)
        self.belief_mean = self.belief_mean / torch.sum(self.belief_mean)

    # ================================ Training ================================
    def train_belief_update(self):
        experiences = self.memory.uniform_sample(self.batch_size)
        prev_obs, prev_belief, force, cur_obs, cur_belief = map(torch.stack, zip(*experiences))

        _, cur_belief_kernel_pred = self.belief_prediction(prev_belief, prev_obs, force)
        kernel_loss = self.loss_fn(cur_belief_kernel_pred, cur_belief)

        self.kernel_optimizer.zero_grad()
        kernel_loss.backward()
        self.kernel_optimizer.step()

        cur_belief_estimator_pred = self.estimator_model.forward(cur_obs)
        estimator_loss = self.loss_fn(cur_belief_estimator_pred, cur_belief)
        self.estimator_optimizer.zero_grad()
        estimator_loss.backward()
        self.estimator_optimizer.step()

        # Compute covariance matrix
        diff = cur_belief_kernel_pred.unsqueeze(-2) - cur_belief.unsqueeze(-2)
        self.pred_covar += torch.sum(torch.bmm(diff.permute(0, 2, 1), diff), dim=0)
        diff = (cur_belief_estimator_pred - cur_belief).unsqueeze(-2)
        self.filter_covar += torch.sum(torch.bmm(diff.permute(0, 2, 1), diff), dim=0)

        self.pred_sample_count += len(experiences)
        self.filter_sample_count += len(experiences)

        self.epoch += 1
        if self.epoch % self.memory.memory_size == 1000:
            self.lr *= 0.95
            for param_group in self.kernel_optimizer.param_groups:
                param_group['lr'] = self.lr
            for param_group in self.estimator_optimizer.param_groups:
                param_group['lr'] = self.lr

    def train(self, reward, done, prev_value, curr_value, next_value, all_obs):
        if not self.use_gt_belief and not self.direct_chase:
            # Update memory
            cur_obs, cur_belief = self.observe_agent(all_obs, self.get_permuted_index(self.thief.index)), self.get_gt_belief()
            force = self.forces[self.prev_action_choice]
            self.memory.append_memory([self.prev_obs, self.prev_belief, force, cur_obs, cur_belief])
            # print("prev belief", self.prev_belief, "curr", cur_belief)
            self.prev_obs, self.prev_belief = cur_obs, cur_belief
            self.train_belief_update()
            self.bayes_update(all_obs, force)

        if not self.direct_chase:
            # Update feature weights
            # get td error
            if done:
                td_error = reward - curr_value
            else:
                td_error = reward + self.gamma * next_value - curr_value

            # get current belief
            if self.use_gt_belief:
                feature = self.get_gt_state_feature(self.thief)
            else:
                feature = self.get_state_feature(self.thief)

            # update eligibility trace
            self.eligibility = self.gamma * self.lbd * self.eligibility + \
                               (1.0 - self.alpha * self.gamma * self.lbd * np.dot(self.eligibility, feature)) * feature
            # Update weights
            self.weight += self.alpha * (td_error + curr_value - prev_value) * self.eligibility - \
                      self.alpha * (curr_value - prev_value) * feature

            # Learning rate decay
            self.alpha *= 0.9999

    def save_model(self, path):
        torch.save([self.kernel_model.state_dict(), self.kernel_optimizer.state_dict()], os.path.join(path, 'kernel.pth'))
        torch.save([self.estimator_model.state_dict(), self.estimator_optimizer.state_dict()], os.path.join(path, 'estimator.pth'))
        torch.save([self.weight, self.pred_covar, self.pred_sample_count, self.filter_covar, self.filter_sample_count], os.path.join(path, 'params.pkl'))

    def load_model(self, path):
        state_dict, optimizer = torch.load(os.path.join(path, 'kernel.pth'))
        self.__class__.kernel_optimizer.load_state_dict(optimizer)
        self.__class__.kernel_model.load_state_dict(state_dict)

        state_dict, optimizer = torch.load(os.path.join(path, 'estimator.pth'))
        self.__class__.estimator_model.load_state_dict(state_dict)
        self.__class__.estimator_optimizer.load_state_dict(optimizer)

        self.__class__.weight, self.__class__.pred_covar, self.__class__.pred_sample_count, self.__class__.filter_covar, self.__class__.filter_sample_count = torch.load(os.path.join(path, 'params.pkl'))
        self.__class__.pred_covar.requires_grad_(False)
        self.__class__.filter_covar.requires_grad_(False)


def main():
    pass


if __name__ == '__main__':
    main()