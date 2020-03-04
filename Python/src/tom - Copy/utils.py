import argparse
import os
import matplotlib.pyplot as plt
import numpy as np
import seaborn as sns

import tom.config

def parse_args():
    paths = tom.config.Paths()
    exp_name = tom.config.exp_name
    episodes = tom.config.episodes
    episode_len = tom.config.episode_len
    save_rate = tom.config.save_rate
    method = 'ToM'  # ['chase', 'ToM gt', 'ToM']

    debug = False

    parser = argparse.ArgumentParser("Testing theory of mind agent")

    parser.add_argument("--scenario", type=str, default="simple_chase", help="name of the scenario script")
    parser.add_argument("--max-episode-len", type=int, default=episode_len, help="maximum episode length")
    parser.add_argument("--num-episodes", type=int, default=episodes, help="number of episodes")

    # Agent setting
    parser.add_argument("--method", type=str, default=method, help="select method")
    arglist = parser.parse_args()  # Get the method first

    parser.add_argument("--use-gt-belief", action="store_true", default=True if arglist.method == 'ToM gt' else False)
    parser.add_argument("--direct-chase", action="store_true", default=True if arglist.method == 'chase' else False)
    parser.add_argument("--use-distance", action="store_true", default=True)

    # Checkpoint
    parser.add_argument("--exp-name", type=str, default=exp_name, help="name of the experiment")
    parser.add_argument("--save-dir", type=str, default=os.path.join(paths.tmp_root, 'checkpoints', exp_name, arglist.method), help="directory in which training state and model should be saved")
    parser.add_argument("--save-rate", type=int, default=save_rate, help="save model once every time this many episodes are completed")

    # Evaluation
    parser.add_argument("--debug", action="store_true", default=debug)
    parser.add_argument("--restore", action="store_true", default=False)
    parser.add_argument("--display", action="store_true", default=True)
    parser.add_argument("--display-mode", type=str, default="all", help="mode of display: normal, belief, uniform, or all")
    parser.add_argument("--save-screen", action="store_true", default=False, help="whether to save screen")
    parser.add_argument("--benchmark", action="store_true", default=True)
    parser.add_argument("--benchmark-iters", type=int, default=episodes, help="number of iterations run for benchmarking")
    parser.add_argument("--benchmark-dir", type=str, default=os.path.join(paths.tmp_root, 'benchmark', exp_name, arglist.method), help="directory where benchmark data is saved")
    parser.add_argument("--plots-dir", type=str, default=os.path.join(paths.tmp_root, 'plots', exp_name, arglist.method), help="directory where plot data is saved")

    arglist = parser.parse_args()
    # if not os.path.exists(arglist.save_dir):
    #     os.makedirs(arglist.save_dir)
    if not os.path.exists(os.path.join(arglist.save_dir, "thief")):
        os.makedirs(os.path.join(arglist.save_dir, "thief"))
    if not os.path.exists(arglist.benchmark_dir):
        os.makedirs(arglist.benchmark_dir)
    if not os.path.exists(arglist.plots_dir):
        os.makedirs(arglist.plots_dir)

    return arglist

def plot_weights(arglist, weights, filename1=None, filename2=None):
    plt.figure(0)
    plt.clf()
    sns.barplot(x=np.arange(2), y=weights[-1, :2], color='c')
    # plt.ylim(-1.0, 1.0)
    if filename1 and not arglist.debug:
        plt.savefig(filename1)
        plt.close()
    else:
        plt.pause(0.001)

    plt.figure(1)
    plt.clf()
    plt.plot(np.arange(weights.shape[1]-3), weights[-1, 3:], color='m')
    # plt.ylim(-1.0, 1.0)
    if filename2 and not arglist.debug:
        plt.savefig(filename2)
        plt.close()
    else:
        plt.pause(0.001)

    # =============================== DEBUG ===============================
    # plt.figure(0)
    # plt.clf()
    # plt.plot(np.arange(2), weights[-1, :2], color='c')
    # plt.plot(np.arange(2, weights.shape[1]), weights[-1, 2:], color='m')
    # # plt.ylim(-1.0, 1.0)
    # plt.savefig(filename1)
    # plt.pause(0.001)
    pass


def plot_avg_return(arglist, episode_returns, filename=None):
    plt.figure(3)
    # plt.plot(np.arange(len(episode_returns)), episode_returns, 'k')
    plt.plot(np.arange(len(episode_returns)), [np.mean(episode_returns[max(0, i-99):i+1]) for i in range(len(episode_returns))], 'r')
    plt.xlabel('Number of episodes')
    plt.ylabel('Avg return')

    if filename:
        plt.savefig(filename)
        plt.close()
    else:
        plt.pause(0.001)