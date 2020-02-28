import os
import time
import random
import argparse
import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F
import pickle
from torch.autograd import Variable
import math
import matplotlib.pyplot as plt

from collections import namedtuple

import tom.config

use_cuda = torch.cuda.is_available()
FloatTensor = torch.cuda.FloatTensor if use_cuda else torch.FloatTensor
LongTensor = torch.cuda.LongTensor if use_cuda else torch.LongTensor
ByteTensor = torch.cuda.ByteTensor if use_cuda else torch.ByteTensor
Tensor = FloatTensor

# action space
actions = np.zeros((5, 5))
actions[np.arange(5), [0, 1, 2, 3, 4]] = 1


def parse_args():
    paths = tom.config.Paths()
    exp_name = tom.config.exp_name
    episodes = tom.config.episodes
    episode_len = tom.config.episode_len
    save_rate = tom.config.save_rate
    method = 'mtom'

    parser = argparse.ArgumentParser("Training payoff net for baseline multi-tom")

    parser.add_argument("--scenario", type=str, default="simple_chase", help="name of the scenario script")
    parser.add_argument("--max-episode-len", type=int, default=episode_len, help="maximum episode length")
    parser.add_argument("--num-episodes", type=int, default=episodes, help="number of episodes")
    parser.add_argument("--save-dir", type=str, default=os.path.join(paths.tmp_root, 'checkpoints', exp_name, method), help="directory in which training state and model should be saved")

    arglist = parser.parse_args()
    if not os.path.exists(arglist.save_dir):
        os.makedirs(arglist.save_dir)
    return arglist


def make_env(scenario_name, benchmark=False):
    from multiagent.environment import MultiAgentEnv
    import multiagent.scenarios as scenarios

    # load scenario from script
    scenario = scenarios.load(scenario_name + ".py").Scenario()
    # create world
    world = scenario.make_world()
    # create multiagent environment
    try:
        done_callback = scenario.done
        info_callback = scenario.info
    except AttributeError:
        done_callback = None
        info_callback = None
    if benchmark:
        env = MultiAgentEnv(world, scenario.reset_world, scenario.reward, scenario.observation, scenario.benchmark_data, done_callback=done_callback)
    else:
        env = MultiAgentEnv(world, scenario.reset_world, scenario.reward, scenario.observation, done_callback=done_callback)
    return env


class DQN(nn.Module):

    def __init__(self):
        super(DQN, self).__init__()
        self.linear1 = nn.Linear(4, 64)
        self.linear2 = nn.Linear(64, 64)
        self.linear3 = nn.Linear(64, 64)
        self.linear4 = nn.Linear(64, 5)

    def forward(self, x):
        x = F.relu(self.linear1(x))
        x = F.relu(self.linear2(x))
        x = F.relu(self.linear3(x))
        x = self.linear4(x)
        return x


Transition = namedtuple('Transition',
                        ('state', 'action', 'next_state', 'reward'))


class ReplayMemory(object):

    def __init__(self, capacity):
        self.capacity = capacity
        self.memory = []
        self.position = 0

    def push(self, *args):
        """Saves a transition."""
        if len(self.memory) < self.capacity:
            self.memory.append(None)
        self.memory[self.position] = Transition(*args)
        self.position = (self.position + 1) % self.capacity

    def sample(self, batch_size):
        return random.sample(self.memory, batch_size)

    def __len__(self):
        return len(self.memory)


# pre-define the number of agents.
num_agents = 3

BATCH_SIZE = 128
GAMMA = 0.8
EPS_START = 0.9
EPS_END = 0.05
EPS_DECAY = 2000
TARGET_UPDATE = 100
policy_net = DQN()
target_net = DQN()
target_net.load_state_dict(policy_net.state_dict())
target_net.eval()

if use_cuda:
    policy_net.cuda()
    target_net.cuda()

learning_rate = 1e-4
optimizer = torch.optim.Adam(policy_net.parameters(), lr=learning_rate)
memory = ReplayMemory(10000)

steps_done = 0


def select_action(state):
    global steps_done
    sample = random.random()
    eps_threshold = EPS_END + (EPS_START - EPS_END) * \
        math.exp(-1. * steps_done / EPS_DECAY)
    steps_done += 1
    if sample > eps_threshold:
        output = policy_net(Variable(state).type(FloatTensor))
        return policy_net(Variable(state).type(FloatTensor)).data.max(1)[1].view(1, 1)

    else:
        return LongTensor([[random.randrange(5)]])


def optimize_model():
    if len(memory) < BATCH_SIZE:
        return

    transitions = memory.sample(BATCH_SIZE)
    batch = Transition(*zip(*transitions))
    print("state", batch.state)

    state_batch = Variable(torch.cat(batch.state, dim=0))
    next_state_batch = Variable(torch.cat(batch.next_state, dim=0))
    reward_batch = Variable(torch.cat(batch.reward, dim=0))
    action_batch = Variable(torch.cat(batch.action, dim=0))

    state_action_values = policy_net(state_batch).gather(1, action_batch)

    next_state_values = target_net(next_state_batch).max(1)[0]

    expected_state_action_values = (torch.unsqueeze(next_state_values, 1) * GAMMA) + reward_batch

    expected_state_action_values = Variable(expected_state_action_values.data)

    loss = F.mse_loss(state_action_values, expected_state_action_values)

    # Optimize the model
    optimizer.zero_grad()
    loss.backward()

    # for param in policy_net.parameters():
    #     param.grad.data.clamp_(-1, 1)
    optimizer.step()

    return loss.data


episode_durations = []
loss = []
returns = []


def plot_durations():
    avg_return = np.array([np.mean(returns[max(0, i-99):i+1]) for i in range(len(returns))])

    avg_duration = np.array([np.mean(episode_durations[max(0, i-99):i+1]) for i in range(len(episode_durations))])

    plt.figure(2, figsize=(8, 4))
    plt.clf()
    plt.title('Training...')
    plt.subplot(121)
    plt.xlabel('Episode')
    plt.ylabel('Duration')
    plt.plot(avg_duration, 'g')
    plt.subplot(122)
    plt.xlabel('Episode')
    plt.ylabel('Return')
    plt.plot(avg_return, 'r')
    plt.savefig('./learning_curve/plot.png')

    plt.pause(0.001)


def train(arglist):

    from itertools import count

    num_episodes = arglist.num_episodes

    # Construct environment
    env = make_env(arglist.scenario)
    num_policies = env.world.num_polices

    policy_net.train()
    target_net.train()

    for i_episode in range(num_episodes):
        print("Training step", i_episode)
        # Initialize the environment and state
        env.reset()
        returns.append(0)

        state = np.random.uniform(low=-1.0, high=1.0, size=(2, 2))
        state = state.flatten()
        state = torch.from_numpy(state).type(Tensor)
        state = torch.unsqueeze(state, 0)

        for t in count():

            terminal = (t > arglist.max_episode_len)

            action_n = np.zeros((num_policies, len(actions)))
            action_batch = []
            for i in range(num_policies):
                action = select_action(state)
                action_batch.append(action)
                action_n[i] = actions[action.item()]

            new_obs_n, reward_n, done_n, info_n = env.step(action_n)
            returns[-1] += reward_n[0]

            done = any(done_n)
            reward = Tensor([reward_n])
            action = LongTensor([action_batch])

            next_state = new_obs_n[0][1][-2:]
            police_position = env.world.polices[0].state.p_pos
            thief_position = env.world.thieves[0].state.p_pos

            next_state[0] = police_position
            next_state[1] = thief_position

            next_state = next_state.flatten()
            next_state = torch.from_numpy(next_state).type(Tensor)
            next_state = torch.unsqueeze(next_state, 0)

            # Store the transition in memory
            memory.push(state, action, next_state, reward)

            # Move to the next state
            state = next_state

            # Perform one step of the optimization (on the target network)
            loss_data = optimize_model()

            if done or terminal:

                episode_durations.append(t + 1)

                loss.append(loss_data)
                plot_durations()
                break

            # Update the target network
            if i_episode % 10 == 0:
                target_net.load_state_dict(policy_net.state_dict())

    torch.save(policy_net, os.path.join(arglist.save_dir, "0model/model.pt"))
    print('Complete')


if __name__ == '__main__':
    arglist = parse_args()
    train(arglist)
