import os
import time
import argparse
import numpy as np
import torch.nn as nn
import torch.nn.functional as F
import pickle

import tom.config
import tom.render_utils
from mtom import MToMAgent

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

    parser = argparse.ArgumentParser("Training baseline multi-tom")

    parser.add_argument("--scenario", type=str, default="simple_chase", help="name of the scenario script")
    parser.add_argument("--max-episode-len", type=int, default=episode_len, help="maximum episode length")
    parser.add_argument("--num-episodes", type=int, default=episodes, help="number of episodes")

    # Checkpoint
    parser.add_argument("--exp-name", type=str, default=exp_name, help="name of the experiment")
    parser.add_argument("--save-dir", type=str, default=os.path.join(paths.tmp_root, 'checkpoints', exp_name, method), help="directory in which training state and model should be saved")
    parser.add_argument("--save-rate", type=int, default=save_rate, help="save model once every time this many episodes are completed")

    # Evaluation
    parser.add_argument("--restore", action="store_true", default=False)
    parser.add_argument("--display", action="store_true", default=False)
    parser.add_argument("--display-mode", type=str, default="normal", help="mode of display: normal, belief, or both")
    parser.add_argument("--save-screen", action="store_true", default=False, help="whether to save screen")
    parser.add_argument("--benchmark", action="store_true", default=True)
    parser.add_argument("--benchmark-iters", type=int, default=episodes, help="number of iterations run for benchmarking")
    parser.add_argument("--benchmark-dir", type=str, default=os.path.join(paths.tmp_root, 'benchmark', exp_name, method), help="directory where benchmark data is saved")
    parser.add_argument("--plots-dir", type=str, default=os.path.join(paths.tmp_root, 'plots', exp_name, method), help="directory where plot data is saved")

    arglist = parser.parse_args()
    if not os.path.exists(arglist.save_dir):
        os.makedirs(arglist.save_dir)
    if not os.path.exists(arglist.benchmark_dir):
        os.makedirs(arglist.benchmark_dir)
    if not os.path.exists(arglist.plots_dir):
        os.makedirs(arglist.plots_dir)

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
        env = MultiAgentEnv(world, scenario.reset_world, scenario.reward, scenario.observation, scenario.benchmark_data,
                            done_callback=done_callback, info_callback=info_callback)
    else:
        env = MultiAgentEnv(world, scenario.reset_world, scenario.reward, scenario.observation,
                            done_callback=done_callback, info_callback=info_callback)
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


def train(arglist):
    # CANNOT display and save results at the same time
    assert(not(arglist.display and arglist.save_screen))

    t_start = time.time()

    env = make_env(arglist.scenario)

    agents = env.world.agents
    num_agents = len(agents)
    policy_agents = env.world.policy_agents
    num_policies = len(policy_agents)

    num_actions = 5
    polices = [MToMAgent(num_agents, num_actions) for i in range(num_policies)]

    # Record data
    episode_returns = []
    episode_info = []

    # for different rendering modes
    display_modes = tom.render_utils.get_display_modes(arglist.display_mode)

    # Initialize saving directories
    save_dir = None
    if arglist.save_screen:
        save_dir = os.path.join(arglist.plots_dir, str(int(t_start)))
        if not os.path.exists(save_dir):
            os.makedirs(save_dir)

    if not os.path.exists(os.path.join(arglist.save_dir, str(int(t_start)))):
        os.makedirs(os.path.join(arglist.save_dir, str(int(t_start))))

    # Training loop
    print("Starting training")
    training_step = 0
    while training_step < arglist.num_episodes:
        training_step += 1

        # Initialization
        env.reset()
        episode_returns.append(0)
        episode_info.append([])
        episode_step = 0
        action_n = np.zeros((num_policies, num_actions))
        obs_n = np.random.uniform(low=-1.0, high=1.0, size=(2, 2))

        if arglist.restore:
            for police in polices:
                # police.load_model(os.path.join(arglist.save_dir, sorted(os.listdir(arglist.save_dir))[-1]))
                police.load_model(os.path.join(arglist.save_dir, "result"))

        save_screen_dirs = []
        if arglist.save_screen:
            for i, d in enumerate(display_modes):
                save_screen_dir = os.path.join(save_dir, "epoch" + str(training_step), d)
                if not os.path.exists(save_screen_dir):
                    os.makedirs(save_screen_dir)
                save_screen_dirs.append(save_screen_dir)

        # Training each episode
        while True:
            # Select actions
            for i, police in enumerate(polices):
                action_n[i] = actions[police.select_action(obs_n)]

            # Advance the environment
            new_obs_n, reward_n, done_n, info_n = env.step(action_n)

            done = any(done_n)

            obs_n = new_obs_n[0][1][-2:]
            police_position = env.world.polices[0].state.p_pos
            thief_position = env.world.thieves[0].state.p_pos

            obs_n[0] = police_position
            obs_n[1] = thief_position

            episode_returns[-1] += reward_n[0]
            episode_info[-1].append(info_n['n'][0][0])

            episode_step += 1
            if done or episode_step >= arglist.max_episode_len:
                episode_info[-1].append(done)
                break

            thief = env.world.thieves[0]
            # Display learned policies
            if arglist.display:
                time.sleep(0.1)
                if arglist.display_mode == 'normal':
                    env.render()
                else:
                    env.render(belief=thief.belief)

            # Save screen
            if arglist.save_screen:
                for i, d in enumerate(display_modes):
                    filename = os.path.join(save_screen_dirs[i], "step{}.png".format(episode_step))
                    tom.render_utils.save_screen(env, d, filename, belief=thief.belief)

        # save trained model every few training steps:
        if training_step % arglist.save_rate == 0:
            polices[0].save_model(os.path.join(arglist.save_dir, str(int(t_start))))
        # End training of current episode
        print("Training step {}, episode reward {}".format(training_step, episode_returns[-1]))

    # End training loop

    # Save episode accumulated rewards
    ret_file_name = os.path.join(arglist.benchmark_dir, '{}_returns.pkl'.format(int(t_start)))
    with open(ret_file_name, 'wb') as fp:
        pickle.dump(episode_returns[:-1], fp)
    avg_episode_returns = [np.mean(episode_returns[max(0, i-tom.config.compute_avg_length):i]) for i in range(1, len(episode_returns)+1)]
    avg_ret_file_name = os.path.join(arglist.benchmark_dir, '{}_avg_returns.pkl'.format(int(t_start)))
    with open(avg_ret_file_name, 'wb') as fp:
        pickle.dump(avg_episode_returns[:-1], fp)

    # for benchmarking learned policies
    if arglist.benchmark:
        file_name = os.path.join(arglist.benchmark_dir, '{}_benchmark.pkl'.format(int(t_start)))
        with open(file_name, 'wb') as fp:
            pickle.dump(episode_info[:-1], fp)

    # Save training statistics


if __name__ == '__main__':
    arglist = parse_args()
    train(arglist)
