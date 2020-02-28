import os
import argparse
import datetime
import numpy as np
import tensorflow as tf
import time
import pickle

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

    debug = False

    parser = argparse.ArgumentParser("Reinforcement Learning experiments for multiagent environments")
    # Environment
    parser.add_argument("--scenario", type=str, default="simple_chase", help="name of the scenario script")
    parser.add_argument("--max-episode-len", type=int, default=episode_len, help="maximum episode length")
    parser.add_argument("--num-episodes", type=int, default=episodes, help="number of episodes")
    parser.add_argument("--num-adversaries", type=int, default=0, help="number of adversaries")
    parser.add_argument("--good-policy", type=str, default="maddpg", help="policy for good agents")
    parser.add_argument("--adv-policy", type=str, default="maddpg", help="policy of adversaries")
    # Core training parameters
    parser.add_argument("--lr", type=float, default=1e-2, help="learning rate for Adam optimizer")
    parser.add_argument("--gamma", type=float, default=0.95, help="discount factor")
    parser.add_argument("--batch-size", type=int, default=32, help="number of episodes to optimize at the same time")
    parser.add_argument("--num-units", type=int, default=64, help="number of units in the mlp")
    # Checkpointing
    parser.add_argument("--exp-name", type=str, default=exp_name, help="name of the experiment")
    parser.add_argument("--save-dir", type=str, default=os.path.join(paths.tmp_root, 'checkpoints', exp_name, method), help="directory in which training state and model should be saved")
    parser.add_argument("--save-rate", type=int, default=save_rate, help="save model once every time this many episodes are completed")
    parser.add_argument("--load-dir", type=str, default="", help="directory in which training state and model are loaded")
    # Evaluation
    parser.add_argument("--debug", action="store_true", default=debug)
    parser.add_argument("--restore", action="store_true", default=False)
    parser.add_argument("--display", action="store_true", default=True)
    parser.add_argument("--display-mode", type=str, default="all", help="mode of display: normal, belief, uniform, or all")
    parser.add_argument("--save-screen", action="store_true", default=False, help="whether to save screen")
    parser.add_argument("--benchmark", action="store_true", default=True)
    parser.add_argument("--benchmark-iters", type=int, default=episodes, help="number of iterations run for benchmarking")
    parser.add_argument("--benchmark-dir", type=str, default=os.path.join(paths.tmp_root, 'benchmark', exp_name, method), help="directory where benchmark data is saved")
    parser.add_argument("--plots-dir", type=str, default=os.path.join(paths.tmp_root, 'plots', exp_name, method), help="directory where plot data is saved")

    arglist = parser.parse_args()
    if not os.path.exists(arglist.benchmark_dir):
        os.makedirs(arglist.benchmark_dir)
    if not os.path.exists(arglist.plots_dir):
        os.makedirs(arglist.plots_dir)

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
    if arglist.save_screen:
        arglist.display = False

    with U.single_threaded_session():
        # Create environment
        env = make_env(arglist.scenario, arglist, arglist.benchmark)
        obs_n = env.reset()
        # Create agent trainers
        obs_shape_n = [obs_n[i][1][-2:, :].flatten().shape for i in range(env.n)]
        num_adversaries = min(env.n, arglist.num_adversaries)
        trainers = get_trainers(env, num_adversaries, obs_shape_n, arglist)
        print('Using good policy {} and adv policy {}'.format(arglist.good_policy, arglist.adv_policy))

        # Initialize
        U.initialize()

        # Load previous results, if necessary
        if arglist.restore:
            print('Loading previous state...')
            U.load_state(os.path.join(arglist.save_dir, sorted(os.listdir(arglist.save_dir))[-1], 'model'))

        episode_returns = [0.0]  # sum of rewards for all agents
        agent_rewards = [[0.0] for _ in range(env.n)]  # individual agent reward
        agent_info = [[]]  # placeholder for benchmarking info
        saver = tf.train.Saver()
        episode_step = 0
        train_step = 0
        t_start = time.time()
        timestamp = datetime.datetime.fromtimestamp(t_start).strftime('%Y-%m-%d_%H:%M:%S')
        if not arglist.debug:
            os.makedirs(os.path.join(arglist.save_dir, str(int(t_start))))

        np.random.seed(int(t_start))
        tf.set_random_seed(int(t_start))

        # for different rendering modes
        display_modes = tom.render_utils.get_display_modes(arglist.display_mode)

        # Initialize saving directories
        save_dir = None
        if arglist.save_screen:
            save_dir = os.path.join(arglist.plots_dir, str(int(t_start)))
            if not os.path.exists(save_dir):
                os.makedirs(save_dir)

        print('Starting iterations...')
        while True:
            # get action
            action_n = [agent.action(obs[1][-2:, :].flatten()) for agent, obs in zip(trainers, obs_n)]
            print("action", action_n)
            # environment step
            new_obs_n, rew_n, done_n, info_n = env.step(action_n)
            done = any(done_n)
            episode_step += 1
            terminal = (episode_step >= arglist.max_episode_len)
            # collect experience
            for i, agent in enumerate(trainers):
                agent.experience(obs_n[i][1][-2:, :].flatten(), action_n[i], rew_n[i], new_obs_n[i][1][-2:, :].flatten(), done_n[i], terminal)
            obs_n = new_obs_n

            for i, rew in enumerate(rew_n):
                episode_returns[-1] += rew
                agent_rewards[i][-1] += rew

            thief = env.world.thieves[0]
            if arglist.save_screen:
                save_screen_dirs = tom.render_utils.get_save_dirs(display_modes, save_dir, len(episode_returns))
                for i, d in enumerate(display_modes):
                    filename = os.path.join(save_screen_dirs[i], "step{}.png".format(episode_step))
                    tom.render_utils.save_screen(env, d, filename, belief=thief.belief)

            agent_info[-1].append(info_n['n'][0][0])
            if done or terminal:
                obs_n = env.reset()
                episode_step = 0
                episode_returns.append(0)
                for a in agent_rewards:
                    a.append(0)
                agent_info[-1].append(done)
                agent_info.append([])
                if len(episode_returns) > arglist.num_episodes:
                    break

            # increment global step counter
            train_step += 1

            # for displaying learned policies
            if arglist.display:
                time.sleep(0.1)
                tom.render_utils.render_image(env, arglist.display_mode, thief.belief, True)
                continue

            # update all trainers, if not in display or benchmark mode
            loss = None
            for agent in trainers:
                agent.preupdate()
            for agent in trainers:
                loss = agent.update(trainers, train_step)

            # save model, display training output
            if (done or terminal) and (len(episode_returns) % arglist.save_rate == 0) and not arglist.debug:
                U.save_state(os.path.join(arglist.save_dir, str(int(t_start)), 'model'), saver=saver)

        # saves final episode reward for plotting training curve later
        if len(episode_returns) > arglist.num_episodes:
            ret_file_name = os.path.join(arglist.benchmark_dir, '{}_returns.pkl'.format(int(t_start)))
            with open(ret_file_name, 'wb') as fp:
                pickle.dump(episode_returns[:-1], fp)
            print('...Finished total of {} episodes.'.format(len(episode_returns)-1))
        avg_episode_returns = [np.mean(episode_returns[max(0, i - tom.config.compute_avg_length):min(
            i + tom.config.compute_avg_length + 1, len(episode_returns))]) for i in range(1, len(episode_returns) + 1)]
        avg_ret_file_name = os.path.join(arglist.benchmark_dir, '{}_avg_returns.pkl'.format(int(t_start)))
        with open(avg_ret_file_name, 'wb') as fp:
            pickle.dump(avg_episode_returns[:-1], fp)

        # for benchmarking learned policies
        if arglist.benchmark:
            file_name = os.path.join(arglist.benchmark_dir, '{}_benchmark.pkl'.format(int(t_start)))
            print('Finished benchmarking, now saving...')
            with open(file_name, 'wb') as fp:
                pickle.dump(agent_info[:-1], fp)


if __name__ == '__main__':
    arglist = parse_args()
    train(arglist)
