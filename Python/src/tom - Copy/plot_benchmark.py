"""
Created on Aug 18, 2018

@author: Siyuan Qi

Description of the file.

"""

import os
import pickle

import numpy as np
import seaborn as sns
import matplotlib.pyplot as plt

import tom.config


def ticks_and_legend(xlabel, ylabel, legend_loc):
    plt.xlabel(xlabel, fontsize=18)
    plt.ylabel(ylabel, fontsize=18)
    plt.legend(loc=legend_loc, prop={'size': 15})
    plt.xticks(fontsize=18)
    plt.yticks(fontsize=18)


def plot_data(results, xlabel, ylabel, filename=None, loc='lower right', ylim=None):
    sns.set(context="paper")
    fig = plt.figure()
    for i, method in enumerate(results['methods']):
        data = results[ylabel][i]
        sns.lineplot(x=xlabel, y=ylabel, data=data, label=method)

    if ylim:
        plt.ylim(ylim[0], ylim[1])
    ticks_and_legend(xlabel, ylabel, loc)

    if not filename:
        plt.show()
    else:
        plt.savefig(filename, bbox_inches='tight')
        plt.close()


def load_results(method_dir):
    acc_rewards = {'Return': [], 'Episode': []}
    beliefs = {'Belief': [], 'Time step': []}
    success_rate = {'Success rate': [], 'Episode': []}
    estimated_belief = {'Belief': [], 'Time step': []}
    weights = [{'Value': [], 'Episode': []}, {'Value': [], 'Episode': []}, {'Value': [], 'Distance': []}]

    final_episode_num = 100

    for result in sorted(os.listdir(method_dir)):
        if result.split('_')[1].startswith('avg'):
            # Load Returns
            with open(os.path.join(method_dir, result), 'rb') as f:
                data = pickle.load(f)
                acc_rewards['Return'].extend(data)
                acc_rewards['Episode'].extend(np.arange(len(data)))
        elif result.split('_')[1].startswith('benchmark'):
            # Load benchmark data
            with open(os.path.join(method_dir, result), 'rb') as f:
                data = pickle.load(f)
                success_record = list()
                for episode, episode_data in enumerate(data):
                    episode_success = episode_data[-1]
                    success_record.append(1 if episode_success else 0)
                    success_rate['Episode'].append(episode)
                    success_rate['Success rate'].append(np.mean(success_record[-tom.config.compute_avg_length:]))
                    if episode >= len(data) - final_episode_num:
                        episode_beliefs = episode_data[:-1]
                        beliefs['Belief'].extend(episode_beliefs)
                        beliefs['Time step'].extend(np.arange(len(episode_beliefs)))
        elif result.split('_')[1].startswith('estimated'):
            with open(os.path.join(method_dir, result), 'rb') as f:
                data = pickle.load(f)
                for episode, episode_data in enumerate(data[-final_episode_num:]):
                    episode_beliefs = episode_data
                    estimated_belief['Belief'].extend(episode_beliefs)
                    estimated_belief['Time step'].extend(np.arange(len(episode_beliefs)))
        elif result.split('_')[1].startswith('weights'):
            with open(os.path.join(method_dir, result), 'rb') as f:
                data = pickle.load(f)
                for i in range(2):
                    weights[i]['Value'].extend(data[:, i])
                    weights[i]['Episode'].extend(np.arange(len(data[:, i])))

                # Distance value function
                bin_size = 0.2
                weights[2]['Value'].extend(data[-1, 2:])
                weights[2]['Distance'].extend(np.arange(len(data[-1, 2:])) * bin_size)

    return acc_rewards, beliefs, success_rate, estimated_belief, weights


def plot_estimation(paths, exp_name, results):
    if 'ToM' not in results['methods']:
        return
    index = results['methods'].index('ToM')
    estimations = {'methods': [], 'Belief': [], 'Time step': []}
    estimations['methods'].append('Ground truth')
    estimations['Belief'].append(results['Belief'][index])
    estimations['methods'].append('Estimation')
    estimations['Belief'].append(results['Estimated belief'][index])
    plot_data(estimations, 'Time step', 'Belief', os.path.join(paths.tmp_root, 'plots', exp_name, 'estimated_beliefs.pdf'), loc='upper right', ylim=[-0.1, 1.1])


def plot_values(paths, exp_name, method, weights):
    if not weights[0]['Episode'] or method=='chase':
        return
    sns.set(context="paper")

    # Plot beliefs
    fig = plt.figure(0)
    for i, state in enumerate(['Police', 'Not police']):
        sns.lineplot(x='Episode', y='Value', data=weights[i], label=state)
    ticks_and_legend('Episode', 'Value', 'lower right')
    plt.savefig(os.path.join(paths.tmp_root, 'plots', exp_name, 'value_belief_{}.pdf'.format(method)), bbox_inches='tight')
    plt.close(0)

    # Plot distance value function
    fig = plt.figure(1)
    sns.lineplot(x='Distance', y='Value', data=weights[2], label=method)
    ticks_and_legend('Distance', 'Value', 'upper right')
    plt.savefig(os.path.join(paths.tmp_root, 'plots', exp_name, 'value_dist.pdf'), bbox_inches='tight')


def plot_benchmark(paths):
    benchmark_dir = os.path.join(paths.tmp_root, 'benchmark')
    for exp_name in os.listdir(benchmark_dir):
        if exp_name != 'fast_thief':
            continue
        exp_dir = os.path.join(benchmark_dir, exp_name)
        results = {'methods': [], 'Return': [], 'Belief': [], 'Success rate': [], 'Estimated belief': []}
        for method in sorted(os.listdir(exp_dir)):
            method_dir = os.path.join(exp_dir, method)
            acc_rewards, beliefs, success_rate, estimated_belief, weights = load_results(method_dir)
            results['methods'].append(method)
            results['Return'].append(acc_rewards)
            results['Belief'].append(beliefs)
            results['Success rate'].append(success_rate)
            results['Estimated belief'].append(estimated_belief)

            plot_values(paths, exp_name, method, weights)
        plt.close(1)
        plot_data(results, 'Episode', 'Return', os.path.join(paths.tmp_root, 'plots', exp_name, 'return.pdf'), loc='lower right')
        plot_data(results, 'Time step', 'Belief', os.path.join(paths.tmp_root, 'plots', exp_name, 'beliefs.pdf'), loc='upper right', ylim=[-0.1, 1.1])
        plot_data(results, 'Episode', 'Success rate', os.path.join(paths.tmp_root, 'plots', exp_name, 'avg_success.pdf'), loc='lower right', ylim=[-0.1, 1.1])
        plot_estimation(paths, exp_name, results)


def main():
    paths = tom.config.Paths()
    plot_benchmark(paths)


if __name__ == '__main__':
    main()