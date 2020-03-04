import os
import argparse
import time
import pickle

# Libraries
import torch
import numpy as np
import seaborn as sns
import matplotlib.pyplot as plt

# Local imports
import tom.config
import tom.render_utils
from tom.tom_agents import ToMAgent


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


def make_env(scenario_name, arglist):
    from multiagent.environment import MultiAgentEnv
    import multiagent.scenarios as scenarios

    # load scenario from script
    scenario = scenarios.load(scenario_name + ".py").Scenario(open_world=True, setting=arglist.exp_name)

    # create world
    world = scenario.make_world()

    # create multi-agent environment
    try:
        done_callback = scenario.done
        info_callback = scenario.info
    except AttributeError:
        done_callback = None
        info_callback = None

    env = MultiAgentEnv(world, scenario.reset_world, scenario.reward, scenario.observation, done_callback=done_callback,
                        info_callback=info_callback)
    return env, scenario


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


def train(arglist):
    # CANNOT display and save results at the same time
    if arglist.save_screen:
        arglist.display = False

    t_start = time.time()
    np.random.seed(int(t_start))
    torch.manual_seed(int(t_start))

    # Create environment
    env, scenario = make_env(arglist.scenario, arglist)
    new_obs_n = env.reset()
    # Policy agents
    polices = list()
    for i, policy_agent in enumerate(env.world.policy_agents):
        polices.append(ToMAgent(policy_agent, env.world, scenario, new_obs_n[i], arglist.exp_name,
                                use_gt_belief=arglist.use_gt_belief, use_distance=arglist.use_distance,
                                direct_chase=arglist.direct_chase))
    if arglist.restore:
        for police in polices:
            police.load_model(os.path.join(arglist.save_dir, sorted(os.listdir(arglist.save_dir))[-1]))
    if not arglist.debug:
        os.makedirs(os.path.join(arglist.save_dir, str(int(t_start))))

    weights = np.zeros((0, polices[0].__class__.weight.shape[0]))

    # Record data
    episode_returns = []
    episode_info = []
    episode_estimatio_belief = []

    # for different rendering modes
    display_modes = tom.render_utils.get_display_modes(arglist.display_mode)

    # Initialize saving directories
    arglist.plots_dir = os.path.join(arglist.plots_dir, str(int(t_start)))
    os.makedirs(os.path.join(arglist.plots_dir, 'belief_value'))
    os.makedirs(os.path.join(arglist.plots_dir, 'dist_value'))

    # training loop
    print("Starting training")
    sns.set(style="white", context="paper", palette="muted", color_codes=True)
    while weights.shape[0] <= arglist.num_episodes:
        weights = np.vstack((weights, polices[0].__class__.weight))
        # print("weights", weights, "stack", polices[0].__class__.weight)
        if weights.shape[0] % arglist.save_rate == 0:
            plot_weights(arglist, weights, os.path.join(arglist.plots_dir, 'belief_value', "belief_value_{}.png".format(weights.shape[0])),
                         os.path.join(arglist.plots_dir, 'dist_value', "dist_value_{}.png".format(weights.shape[0])))
            plot_avg_return(arglist, episode_returns, os.path.join(arglist.plots_dir, "avg_return.png"))

        # Initialization
        env.reset()
        episode_returns.append(0)
        episode_info.append([])
        episode_estimatio_belief.append([])
        action_n = np.zeros((len(polices), 5))
        episode_step = 0
        prev_values = np.zeros(len(polices))
        for police in polices:
            police.initialize()

        if arglist.save_screen:
            save_screen_dirs = tom.render_utils.get_save_dirs(display_modes, arglist.plots_dir, str(weights.shape[0]))

        # training each episode
        while True:
            # get current value
            curr_values = [police.compute_value() for police in polices]

            # choose action based on current approximation
            for i, police in enumerate(polices):
                action_n[i] = police.select_action(new_obs_n[i])

            # update the environment
            new_obs_n, reward_n, done_n, info_n = env.step(action_n)
            done = any(done_n)

            # FIXME: only record the first agent's history
            episode_returns[-1] += reward_n[0]
            episode_info[-1].append(info_n['n'][0][0])
            episode_estimatio_belief[-1].append(polices[0].belief_mean.detach().squeeze().cpu().numpy()[0])

            # get next value
            next_values = [police.compute_value() for police in polices]
            if not arglist.restore:
                for i, police in enumerate(polices):
                    police.train(reward_n[i], done, prev_values[i], curr_values[i], next_values[i], new_obs_n[i])
            prev_values = next_values

            thief = env.world.thieves[0]
            # for displaying learned policies
            if arglist.display:
                # time.sleep(0.1)
                tom.render_utils.render_image(env, arglist.display_mode, thief.belief, True)

            # Save screen
            if arglist.save_screen:
                for i, d in enumerate(display_modes):
                    filename = os.path.join(save_screen_dirs[i], "step{}.png".format(episode_step))
                    tom.render_utils.save_screen(env, d, filename, belief=thief.belief)

            episode_step += 1
            if done or episode_step >= arglist.max_episode_len:
                episode_info[-1].append(done)
                break

            # End episode loop

        if not arglist.debug:
            # Save trained model every few training steps
            if weights.shape[0] % arglist.save_rate == 0:
                polices[0].save_model(os.path.join(arglist.save_dir, str(int(t_start))))

        # End training loop

    # Save episode accumulated rewards
    ret_file_name = os.path.join(arglist.benchmark_dir, '{}_returns.pkl'.format(int(t_start)))
    with open(ret_file_name, 'wb') as fp:
        pickle.dump(episode_returns[:-1], fp)
    avg_episode_returns = [np.mean(episode_returns[max(0, i-tom.config.compute_avg_length):min(i+tom.config.compute_avg_length+1, len(episode_returns))]) for i in range(1, len(episode_returns)+1)]
    avg_ret_file_name = os.path.join(arglist.benchmark_dir, '{}_avg_returns.pkl'.format(int(t_start)))
    with open(avg_ret_file_name, 'wb') as fp:
        pickle.dump(avg_episode_returns[:-1], fp)

    avg_ret_file_name = os.path.join(arglist.benchmark_dir, '{}_weights.pkl'.format(int(t_start)))
    with open(avg_ret_file_name, 'wb') as fp:
        pickle.dump(weights, fp)

    # for benchmarking learned policies
    if arglist.benchmark:
        file_name = os.path.join(arglist.benchmark_dir, '{}_benchmark.pkl'.format(int(t_start)))
        with open(file_name, 'wb') as fp:
            pickle.dump(episode_info[:-1], fp)
        file_name = os.path.join(arglist.benchmark_dir, '{}_estimated_belief.pkl'.format(int(t_start)))
        with open(file_name, 'wb') as fp:
            pickle.dump(episode_estimatio_belief[:-1], fp)


def main():
    arglist = parse_args()
    train(arglist)


if __name__ == '__main__':
    main()