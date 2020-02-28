import os

import torch
import numpy as np
import scipy.stats

import tom_both_sides.memory
from tom_both_sides.belief_transition import TransitionKernel, BeliefEstimation

class ToMAgent(object):
    epsilon = 0.05  # Epsilon greedy
    alpha = 5e-2  # Learning rate for RL
    gamma = 0.95  # Discount factor for reward
    lbd = 0.95  # Discount factor for eligibility trace

    lr = 1e-3
    memory_size = 10000
    batch_size = 512

    kappa = 0.5

    use_gt_belief = False
    distance_bin_num = 7
    distance_bin_size = 0.2

    def __init__(self, agent, world, scenario, all_obs, use_gt_belief=False, use_distance=True ):
        self.agent = agent
        # self.opponent = opponent
        self.world = world
        self.scenario = scenario
        self.__class__.use_gt_belief = use_gt_belief
        self.__class__.use_distance = use_distance
        self.device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

        if self.agent.identity == "police":
            self.opponent_index = self.world.thieves[0].index
        elif self.agent.identity == "thief":
            self.opponent_index = self.world.polices[0].index

        self.use_distance = use_distance

        self.num_agents = self.world.num_agents
        self.weight = np.asarray([np.random.rand(self.distance_bin_num + 2)*0.01 for i in range(self.num_agents)]) \
            if self.use_distance else np.zeros((self.num_agents, 2))
        self.memory = tom_both_sides.memory.Memory(self.memory_size)

        self.eligibility = np.zeros_like(self.weight)

        self.prev_obs = self.observe_agent(all_obs, self.get_permuted_index(
            self.opponent_index))

        self.prev_action_choice = None
        self.belief_size = 2
        self.first_level_beliefs = self.belief_mean = self.belief_covar = self.prev_belief = None
        self.initialize()

        self.p_not_opponent = 1 / (2 * np.pi)

        self.epoch = 0

        self.actions = np.zeros((5, 5))
        self.actions[np.arange(5), [0, 1, 2, 3, 4]] = 1
        self.pred_sample_count = self.filter_sample_count = 0
        forces = np.array([[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1]])
        self.forces = torch.Tensor(forces).to(self.device)

        self.kernel_model = TransitionKernel(self.prev_obs.shape[0], self.forces[0, :].shape[0],
                                                       self.belief_size).to(self.device)
        self.kernel_optimizer = torch.optim.Adam(self.kernel_model.parameters(), lr=self.lr)
        self.estimator_model = BeliefEstimation(self.prev_obs.shape[0], self.belief_size).to(self.device)
        self.estimator_optimizer = torch.optim.Adam(self.estimator_model.parameters(), lr=self.lr)
        self.loss_fn = torch.nn.MSELoss()

        self.pred_covar = torch.zeros((self.num_agents, self.belief_size, self.belief_size), dtype=torch.float32,
                                                requires_grad=False).to(self.device)
        self.filter_covar = torch.zeros((self.num_agents, self.belief_size, self.belief_size), dtype=torch.float32,
                                                  requires_grad=False).to(self.device)

    def initialize(self):
        self.eligibility = np.zeros_like(self.weight)

        self.belief_mean = torch.ones((self.num_agents, 1, self.belief_size), dtype=torch.float32).to(
            self.device)  # batch_size x self.belief_size

        self.belief_mean /= self.belief_size  # Uniform belief
        self.belief_covar = torch.zeros((self.num_agents, self.belief_size, self.belief_size), dtype=torch.float32).to(self.device)

        self.first_level_beliefs = torch.ones((self.num_agents, self.belief_size), dtype=torch.float32).to(
            self.device)
        self.first_level_beliefs /= self.belief_size
        self.first_level_beliefs[self.agent.index][0] = 0.0
        self.first_level_beliefs[self.agent.index][1] = 1.0

    # ================================ Observe environment ================================
    # obs, force, belief are always torch tensors
    def observe_agent(self, all_obs, permuted_index):
        other_obs = [torch.Tensor(quantities[permuted_index]) for quantities in all_obs]
        own_obs = [torch.Tensor(quantities[self.get_permuted_index(self.agent.index)]) for quantities in all_obs]
        return torch.cat(own_obs + other_obs).to(self.device)

    def get_gt_belief(self, opponent):
        belief = opponent.first_level_beliefs[self.agent.index]
        return belief

    def get_permuted_index(self, index):
        permuted_index = self.scenario.permutation[index]
        return permuted_index

    # ================================ Planning ================================
    def compute_value(self):
        values = list()
        for i, suspect in enumerate(self.world.agents):
            if suspect is not self.agent:
                values.append(np.dot(self.get_state_feature(suspect), self.weight[i]))
            else:
                values.append(-float('inf'))
        return values

    def action_value(self, all_obs, action, suspect):
        force = self.forces[np.argmax(action), :]
        return np.dot(self.get_next_state_feature(all_obs, force, suspect), self.weight[suspect.index])

    def select_action(self, all_obs):
        suspect = self.world.agents[np.argmax(self.first_level_beliefs[:, 0])]
        # select action using epsilon greedy.
        if np.random.random() < self.epsilon:
            self.prev_action_choice = np.random.randint(5)
        else:
            action_values = [self.action_value(all_obs, action, suspect) for action in self.actions]
            self.prev_action_choice = np.argmax(action_values)
        return self.actions[self.prev_action_choice]

    def get_distance_feature(self, distance):
        distance_feature = np.zeros(self.distance_bin_num)

        # Piecewise linear value function for distance
        bin = int(distance / self.distance_bin_size)
        k = ((bin + 1) * self.distance_bin_size - distance) / self.distance_bin_size
        distance_feature[min(self.distance_bin_num - 1, bin)] = k
        distance_feature[min(self.distance_bin_num - 1, bin + 1)] = 1 - k

        return distance_feature

    def get_state_feature(self, suspect):
        belief = self.belief_mean[suspect.index].squeeze().detach().cpu().numpy()
        if self.use_distance:
            distance = self.world.distance(self.agent, suspect)
            return np.concatenate((belief, self.get_distance_feature(distance)))
        else:
            return belief

    def get_next_state_feature(self, all_obs, force, suspect):

        _, belief = self.belief_prediction(self.belief_mean[suspect.index], self.observe_agent(all_obs, suspect.index).unsqueeze(-2), force.unsqueeze(-2))  # TODO
        belief = belief.squeeze().detach().cpu().numpy()

        if self.use_distance:
            next_vel, next_pos = self.world.compute_next_pos(self.agent, force)
            distance = self.world.pos_distance(next_pos, suspect.state.p_pos)
            return np.concatenate((belief, self.get_distance_feature(distance)))
        else:
            return belief

    def update_first_level_beliefs(self):
        for suspect in self.world.agents:
            velocity = suspect.action.u
            direction = self.agent.state.p_pos - suspect.state.p_pos
            if self.agent.identity == "police":
                direction = -direction

            direction_norm = np.linalg.norm(direction)
            vel_agent_norm = np.linalg.norm(velocity)

            if vel_agent_norm != 0 and direction_norm != 0:
                dot = (np.dot(velocity, direction) / (vel_agent_norm * direction_norm))
                # To account for some random value, e.g. 1.0000000002.
                dot = np.clip(dot, -1, 1)
                theta = np.arccos(dot)

                new_belief = torch.zeros(2)
                new_belief[0] = self.first_level_beliefs[suspect.index][0] * scipy.stats.vonmises.pdf(theta, self.kappa)
                new_belief[1] = self.first_level_beliefs[suspect.index][1] * self.p_not_opponent
                new_belief = new_belief / new_belief.sum()

                self.first_level_beliefs[suspect.index] = new_belief



    # ================================ Belief update ================================
    def belief_prediction(self, prev_belief, obs, force):
        # prev_belief size: batch_size x belief_size
        kernel = self.kernel_model.forward(obs, force)
        belief = torch.bmm(prev_belief.unsqueeze(-2), kernel)  # batch_size x 1 x belief_size
        return kernel, belief.squeeze(-2)  # batch_size x belief_size

    def bayes_update(self, all_obs, force):
        for i, suspect in enumerate(self.world.agents):
            # Prediction
            suspect_index = self.get_permuted_index(i)
            obs = self.observe_agent(all_obs, suspect_index).unsqueeze(-2)
            kernel, pred_mean = self.belief_prediction(self.belief_mean[i], obs, force.unsqueeze(-2))
            kernel = kernel.squeeze()
            pred_covar = self.pred_covar[i] / self.pred_sample_count + torch.mm(torch.mm(kernel, self.belief_covar[i]), kernel)

            # Correction
            sample_mean = self.estimator_model.forward(obs)
            sum_inv = np.linalg.pinv((pred_covar + self.filter_covar[i] / self.filter_sample_count).detach().cpu().numpy())
            sum_inv = torch.Tensor(sum_inv).to(self.device)
            filter_mean = pred_mean + torch.mm(torch.mm(pred_covar, sum_inv), (sample_mean - pred_mean).t()).t()
            filter_covar = pred_covar - torch.mm(torch.mm(pred_covar, sum_inv), pred_covar)
            self.belief_mean[i] = filter_mean
            self.belief_covar[i] = filter_covar

            # Renormalize to ensure it is a valid probability
            if torch.min(self.belief_mean[i]) < 0:
                self.belief_mean[i] = self.belief_mean[i].clone() - torch.min(self.belief_mean[i])
            self.belief_mean[i] = self.belief_mean[i].clone() / torch.sum(self.belief_mean[i])

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

    def train(self, opponent, reward, done, prev_values, curr_values, next_values, all_obs):
        if self.prev_belief is None:
            self.prev_belief = self.get_gt_belief(opponent)
        self.update_first_level_beliefs()
        # Update memory
        cur_obs, cur_belief = self.observe_agent(all_obs, self.get_permuted_index(self.opponent_index)), \
                              self.get_gt_belief(opponent)
        force = self.forces[self.prev_action_choice]

        self.memory.append_memory([self.prev_obs, self.prev_belief, force, cur_obs, cur_belief])
        self.prev_obs, self.prev_belief = cur_obs, cur_belief
        self.train_belief_update()
        self.bayes_update(all_obs, force)

        # Update feature weights
        for i, suspect in enumerate(self.world.agents):
            if suspect is not self.agent:
                # get td error
                if done:
                    td_error = reward - curr_values[i]
                else:
                    td_error = reward + self.gamma * next_values[i] - curr_values[i]

                # get current belief
                feature = self.get_state_feature(suspect)

                # update eligibility trace
                self.eligibility[i] = self.gamma * self.lbd * self.eligibility[i] + \
                                   (1.0 - self.alpha * self.gamma * self.lbd * np.dot(self.eligibility[i], feature)) * feature

                # Update weights
                self.weight[i] += self.alpha * (td_error + curr_values[i] - prev_values[i]) * self.eligibility[i] - \
                          self.alpha * (curr_values[i] - prev_values[i]) * feature

        # Learning rate decay
        self.alpha *= 0.9999

    def save_model(self, path):
        torch.save([self.kernel_model.state_dict(), self.kernel_optimizer.state_dict()], os.path.join(path, 'kernel.pth'))
        torch.save([self.estimator_model.state_dict(), self.estimator_optimizer.state_dict()], os.path.join(path, 'estimator.pth'))
        torch.save([self.weight, self.pred_covar, self.pred_sample_count, self.filter_covar, self.filter_sample_count], os.path.join(path, 'params.pkl'))

    def load_model(self, path):
        state_dict, optimizer = torch.load(os.path.join(path, 'kernel.pth'))
        self.kernel_optimizer.load_state_dict(optimizer)
        self.kernel_model.load_state_dict(state_dict)

        state_dict, optimizer = torch.load(os.path.join(path, 'estimator.pth'))
        self.estimator_model.load_state_dict(state_dict)
        self.estimator_optimizer.load_state_dict(optimizer)

        self.weight, self.pred_covar, self.pred_sample_count, self.filter_covar, self.filter_sample_count = torch.load(os.path.join(path, 'params.pkl'))
        self.pred_covar.requires_grad_(False)
        self.filter_covar.requires_grad_(False)


def main():
    pass

if __name__ == '__main__':
    main()