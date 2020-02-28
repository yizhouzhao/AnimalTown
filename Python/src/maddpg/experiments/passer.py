import os
import argparse
import datetime
import numpy as np
import tensorflow as tf
import time
import pickle
import scipy.misc

import maddpg.common.tf_util as U
from maddpg.trainer.maddpg import MADDPGAgentTrainer
import tensorflow.contrib.layers as layers

import tom.config
import tom.render_utils


def parse_args():
    paths = tom.config.Paths()
    exp_name = tom.config.exp_name
    episodes = tom.config.episodes
    episode_len = tom.config.episode_len
    save_rate = tom.config.save_rate
    method = 'maddpg'

    parser = argparse.ArgumentParser("Reinforcement Learning experiments for multiagent environments")
    # Environment
    parser.add_argument("--scenario", type=str, default="simple_chase", help="name of the scenario script")
    parser.add_argument("--max-episode-len", type=int, default=episode_len, help="maximum episode length")
    parser.add_argument("--num-episodes", type=int, default=episodes, help="number of episodes")
    parser.add_argument("--num-adversaries", type=int, default=0, help="number of adversaries")
    parser.add_argument("--good-policy", type=str, default="maddpg", help="policy for good agents")
    parser.add_argument("--adv-policy", type=str, default="maddpg", help="policy of adversaries")
    # Core training parameters
    parser.add_argument("--lr", type=float, default=1e-3, help="learning rate for Adam optimizer")
    parser.add_argument("--gamma", type=float, default=0.95, help="discount factor")
    parser.add_argument("--batch-size", type=int, default=32, help="number of episodes to optimize at the same time")
    parser.add_argument("--num-units", type=int, default=64, help="number of units in the mlp")
    # Checkpointing
    parser.add_argument("--exp-name", type=str, default=exp_name, help="name of the experiment")
    parser.add_argument("--save-dir", type=str, default=os.path.join(paths.tmp_root, 'checkpoints', exp_name, method), help="directory in which training state and model should be saved")
    parser.add_argument("--save-rate", type=int, default=save_rate, help="save model once every time this many episodes are completed")
    parser.add_argument("--load-dir", type=str, default="", help="directory in which training state and model are loaded")
    # Evaluation
    parser.add_argument("--restore", action="store_true", default=False)
    parser.add_argument("--display", action="store_true", default=False)
    parser.add_argument("--display-mode", type=str, default="normal", help="mode of display: normal, belief, or both")
    parser.add_argument("--save-screen", action="store_true", default=False, help="whether to save screen")
    parser.add_argument("--benchmark", action="store_true", default=True)
    parser.add_argument("--benchmark-iters", type=int, default=episodes, help="number of iterations run for benchmarking")
    parser.add_argument("--benchmark-dir", type=str, default=os.path.join(paths.tmp_root, 'benchmark', exp_name, method), help="directory where benchmark data is saved")
    parser.add_argument("--passer-dir", type=str, default=os.path.join(paths.tmp_root, 'benchmark', exp_name, 'passer'), help="directory where benchmark data is saved")
    parser.add_argument("--plots-dir", type=str, default=os.path.join(paths.tmp_root, 'plots', exp_name, method), help="directory where plot data is saved")

    arglist = parser.parse_args()
    if not os.path.exists(arglist.passer_dir):
        os.makedirs(arglist.passer_dir)

    return arglist


def mlp_model(input, num_outputs, scope, reuse=False, num_units=64, rnn_cell=None):
    # This model takes as input an observation and returns values of all actions
    with tf.variable_scope(scope, reuse=reuse):
        out = input
        out = layers.fully_connected(out, num_outputs=num_units, activation_fn=tf.nn.relu)
        out = layers.fully_connected(out, num_outputs=num_units, activation_fn=tf.nn.relu)
        out = layers.fully_connected(out, num_outputs=num_units, activation_fn=tf.nn.relu)
        out = layers.fully_connected(out, num_outputs=num_outputs, activation_fn=None)
        return out


def make_env(scenario_name, arglist, benchmark=False):
    from multiagent.environment import MultiAgentEnv
    import multiagent.scenarios as scenarios

    # load scenario from script
    scenario = scenarios.load(scenario_name + ".py").Scenario(open_world=True, setting=arglist.exp_name)
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


def get_trainers(env, num_adversaries, obs_shape_n, arglist):
    trainers = []
    model = mlp_model
    trainer = MADDPGAgentTrainer
    for i in range(num_adversaries):
        trainers.append(trainer(
            "agent_%d" % i, model, obs_shape_n, env.action_space, i, arglist,
            local_q_func=(arglist.adv_policy=='ddpg')))
    for i in range(num_adversaries, env.n):
        trainers.append(trainer(
            "agent_%d" % i, model, obs_shape_n, env.action_space, i, arglist,
            local_q_func=(arglist.good_policy=='ddpg')))
    return trainers


def train(arglist):
    # CANNOT display and save results at the same time
    assert(not(arglist.display and arglist.save_screen))

    with U.single_threaded_session():
        # Create environment
        env = make_env(arglist.scenario, arglist, arglist.benchmark)
        obs_n = env.reset()
        thief = env.world.thieves[0]
        passer = env.world.others[0]

        # Initialize
        U.initialize()

        # Load previous results, if necessary
        if arglist.restore:
            print('Loading previous state...')
            U.load_state(os.path.join(arglist.save_dir, '1535265965', 'model'))

        episode_returns = [0.0]  # sum of rewards for all agents
        passer_info = [[]]
        passer_done = False
        episode_step = 0
        train_step = 0
        t_start = time.time()
        timestamp = datetime.datetime.fromtimestamp(t_start).strftime('%Y-%m-%d_%H:%M:%S')

        np.random.seed(int(t_start))
        tf.set_random_seed(int(t_start))

        print('Starting iterations...')
        while True:
            # get action
            action_n = [np.array([1, 0, 0, 0, 0])]
            # environment step
            new_obs_n, rew_n, done_n, info_n = env.step(action_n)
            # done = any(done_n)
            done = env._get_done(passer)
            episode_step += 1
            terminal = (episode_step >= arglist.max_episode_len)

            passer_info[-1].append(env._get_info(passer)[0])
            if env._get_done(passer):
                passer_done = True
            if done or terminal:
                obs_n = env.reset()
                episode_step = 0
                episode_returns.append(0)
                passer_info[-1].append(passer_done)
                passer_info.append([])
                passer_done = False
                if len(episode_returns) > arglist.num_episodes:
                    break

            # increment global step counter
            train_step += 1

        # for benchmarking learned policies
        if arglist.benchmark:
            file_name = os.path.join(arglist.passer_dir, '{}_benchmark.pkl'.format(int(t_start)))
            with open(file_name, 'wb') as fp:
                pickle.dump(passer_info[:-1], fp)


if __name__ == '__main__':
    arglist = parse_args()
    train(arglist)
