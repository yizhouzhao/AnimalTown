import os
import numpy as np
from itertools import product
import torch

import tom.config

device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
FloatTensor = torch.cuda.FloatTensor
DoubleTensor = torch.cuda.DoubleTensor

paths = tom.config.Paths()
exp_name = tom.config.exp_name
episodes = tom.config.episodes
episode_len = tom.config.episode_len
save_rate = tom.config.save_rate
method = 'mtom'

model_dir = os.path.join(paths.tmp_root, 'checkpoints', exp_name, method, '0model/model.pt')


class MToMAgent:
    lbd = 0.7
    action_space = [0, 1, 2, 3, 4]

    def __init__(self, num_agents=3, num_actions=5):
        self.num_agents = num_agents
        # self.num_other_agents = num_agents - 1
        self.num_agents_of_interests = 1
        self.num_other_agents = num_agents - 1

        # Use discrete actions
        self.num_actions = num_actions

        self.zero_order_beliefs = self.first_order_beliefs = None
        self.confidence = None
        self.init_belief()
        self.init_confidence()

        self.first_order_actions = np.zeros(self.num_agents_of_interests)

        all_actions = [[i for i in range(self.num_actions)] for j in range(self.num_agents_of_interests)]
        self.trajectories = list(product(*all_actions))

        # self.payoff_net = DQN(self.num_agents).to(device)
        self.payoff_net = torch.load(model_dir)

        self.action_selected = 0

    def init_belief(self):
        # Initialize zero order belief
        self.zero_order_beliefs = np.random.rand(self.num_agents_of_interests, self.num_actions)
        self.zero_order_beliefs /= self.zero_order_beliefs.sum(axis=1)[:, np.newaxis]

        # Initialize first order belief
        self.first_order_beliefs = np.random.rand(self.num_agents_of_interests, self.num_agents_of_interests, self.num_actions)
        self.first_order_beliefs /= self.first_order_beliefs.sum(axis=2)[:, :, np.newaxis]

    def init_confidence(self):
        # Initialize confidence level for all other agents
        self.confidence = np.random.rand(self.num_agents_of_interests)

    def select_action(self, state):
        value_list = [self.compute_integrated_belief_value(action, state) for action in self.action_space]
        self.action_selected = np.argmax(value_list)
        return self.action_selected

    def compute_integrated_belief_value(self, action, state):
        integrated_beliefs = np.zeros_like(self.zero_order_beliefs)

        for agent_index in range(self.num_agents_of_interests):
            first_order_action = self.select_first_order_action(agent_index, state)
            self.first_order_actions[agent_index] = first_order_action
            # Assuming only cares about one agent
            for action_index, zero_order_action in enumerate(self.action_space):
                integrated_belief = self.compute_integrated_belief(zero_order_action, first_order_action,
                                                                   self.zero_order_beliefs[0][action_index],
                                                                   self.confidence[agent_index])
                integrated_beliefs[agent_index][action_index] = integrated_belief

            # update first_order_belief
            self.update_first_order_belief()
            # update confidence
            self.update_confidence(agent_index)

        # update zero_order_belief
        self.zero_order_beliefs = integrated_beliefs
        return self.compute_value(action, state, integrated_beliefs[0])

    def compute_integrated_belief(self, zero_order_action, first_order_action, zero_order_belief, confidence):
        # return integrated zero_order_belief of a single agent
        return self.compute_integrated_function_value(zero_order_action, first_order_action, confidence, zero_order_belief)

    def compute_zero_order_value(self, agent_index, action, state):
        belief = self.zero_order_beliefs[0]
        return self.compute_value(action, state, belief, enumerate_all=False)

    def select_zero_order_action(self, agent_index, state):
        return np.argmax([self.compute_zero_order_value(agent_index, action, state) for action in self.action_space])

    def select_first_order_action(self, agent_index, state):
        return np.argmax([self.compute_first_order_value(agent_index, action, state) for action in self.action_space])

    def compute_first_order_value(self, agent_index, action, state):
        belief = self.first_order_beliefs[0][0]
        return self.compute_value(action, state, belief, enumerate_all=False)

    def update_first_order_belief(self):
        gt_action = self.action_selected
        for action_index, first_order_action in enumerate(self.action_space):
            first_order_belief = self.first_order_beliefs[0][0][action_index]
            self.first_order_beliefs[0][0][action_index] = self.compute_integrated_function_value(first_order_action,
                                                                                                  gt_action,
                                                                                                  self.lbd,
                                                                                                  first_order_belief)

    def update_confidence(self, agent_index):
        first_order_action = self.first_order_actions[agent_index]
        gt_action = self.action_selected
        prev_confidence = self.confidence[agent_index]
        self.confidence[agent_index] = \
            self.compute_integrated_function_value(gt_action, first_order_action, self.lbd, prev_confidence)

    # Utility functions
    # a generic computation for function in the form of integration function
    def compute_integrated_function_value(self, lhs_action, rhs_action, rate, prev_value):
        temp_value = (1 - rate) * prev_value
        # print("integrated function value computed")
        if lhs_action == rhs_action:
            return temp_value + rate
        else:
            return temp_value

    def compute_value(self, action, state, belief, enumerate_all=False):
        value = 0.0
        if enumerate_all:
            for action_n in self.trajectories:
                curr_belief = 1.0
                for agent_index in range(self.num_agents_of_interests):
                    curr_belief *= belief[agent_index][action_n[agent_index]]

                state = state.flatten()
                # print(type(state))
                state = torch.from_numpy(state).type(FloatTensor)
                payoff = self.payoff_net(state)
                # print(payoff[action])
                value += payoff[action].item() * curr_belief
                state = state.cpu().numpy()
        else:
            for action_index in range(self.num_actions):
                curr_belief = 1.0
                curr_belief *= belief[action_index]

                state = state.flatten()
                state = torch.from_numpy(state).type(FloatTensor)
                payoff = self.payoff_net(state)
                value += payoff[action].item() * curr_belief
                state = state.cpu().numpy()

        return value

    # Benchmarking
    def save_model(self, path):
        torch.save([self.zero_order_beliefs, self.first_order_beliefs, self.confidence], os.path.join(path, 'params.pkl'))

    def load_model(self, path):
        self.zero_order_beliefs, self.first_order_beliefs, self.confidence = torch.load(os.path.join(path, 'params.pkl'))






