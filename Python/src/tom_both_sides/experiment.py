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
# from tom_both_sides import config
import tom_both_sides.config
import tom_both_sides.render_utils
from tom_both_sides.tom_agents import ToMAgent


def parse_args():
    paths = tom_both_sides.config.Paths()
    exp_name = tom_both_sides.config.exp_name
    episodes = tom_both_sides.config.episodes
    episode_len = tom_both_sides.config.episode_len
    save_rate = tom_both_sides.config.save_rate
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
    parser.add_argument("--debug", action="store_true", default=True)
    parser.add_argument("--restore", action="store_true", default=debug)
    parser.add_argument("--display", action="store_true", default=True)
    parser.add_argument("--display-mode", type=str, default="normal", help="mode of display: normal, belief, uniform, or all")
    parser.add_argument("--save-screen", action="store_true", default=False, help="whether to save screen")
    parser.add_argument("--benchmark", action="store_true", default=True)
    parser.add_argument("--benchmark-iters", type=int, default=episodes, help="number of iterations run for benchmarking")
    parser.add_argument("--benchmark-dir", type=str, default=os.path.join(paths.tmp_root, 'benchmark', exp_name, arglist.method), help="directory where benchmark data is saved")
    parser.add_argument("--plots-dir", type=str, default=os.path.join(paths.tmp_root, 'plots', exp_name, arglist.method), help="directory where plot data is saved")

    arglist = parser.parse_args()
    # for identity in ["thief", "police"]:
    if not os.path.exists(os.path.join(arglist.save_dir)):
        os.makedirs(os.path.join(arglist.save_dir))
    if not os.path.exists(os.path.join(arglist.benchmark_dir)):
        os.makedirs(os.path.join(arglist.benchmark_dir))
    if not os.path.exists(os.path.join(arglist.plots_dir)):
        os.makedirs(os.path.join(arglist.plots_dir))

    return arglist


def make_env(scenario_name, arglist):
    from multiagent.environment import MultiAgentEnv
    import multiagent.scenarios as scenarios

    # load scenario from script
    scenario = scenarios.load(scenario_name + ".py").Scenario(open_world=True, setting=arglist.exp_name, tom_thief=True)

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
    thieves = list()
    for i, policy_agent in enumerate(env.world.policy_agents):
        if policy_agent.identity == 'thief':
            thieves.append(ToMAgent(policy_agent, env.world, scenario, new_obs_n[i], arglist.exp_name,  use_distance=arglist.use_distance))
        elif policy_agent.identity == 'police':
            polices.append(ToMAgent(policy_agent, env.world, scenario, new_obs_n[i], arglist.exp_name, use_distance=arglist.use_distance))

    if arglist.restore:
        for police in polices:
            police.load_model(os.path.join(arglist.save_dir, sorted(os.listdir(arglist.save_dir))[-1], "police"))
        for thief in thieves:
            thief.load_model(os.path.join(arglist.save_dir, sorted(os.listdir(arglist.save_dir))[-1], "thief"))

    if not arglist.debug:
        os.makedirs(os.path.join(arglist.save_dir, str(int(t_start)), 'police'))
        os.makedirs(os.path.join(arglist.save_dir, str(int(t_start)), 'thief'))

    weights = np.zeros((0, polices[0].weight[0].shape[0]))


    # Record data
    episode_returns = []
    episode_info = []
    episode_estimatio_belief = []

    # for different rendering modes
    display_modes = tom_both_sides.render_utils.get_display_modes(arglist.display_mode)

    # Initialize saving directories
    arglist.plots_dir = os.path.join(arglist.plots_dir, str(int(t_start)))
    os.makedirs(os.path.join(arglist.plots_dir, 'belief_value'))
    os.makedirs(os.path.join(arglist.plots_dir, 'dist_value'))

    # training loop
    print("Starting training")
    sns.set(style="white", context="paper", palette="muted", color_codes=True)
    while weights.shape[0] <= arglist.num_episodes:
        if weights.shape[0] % 5 == 0:
            print("Training episode: ", weights.shape[0])
        # TODO: change plot weight.
        weights = np.vstack((weights, polices[0].weight[0]))

        # Initialization
        env.reset()

        episode_returns.append(0)
        episode_info.append([])
        episode_estimatio_belief.append([])

        action_n = np.zeros((2, 5))
        episode_step = 0
        police_prev_values = np.zeros((len(polices), env.world.num_agents))
        thief_prev_values = np.zeros((len(thieves), env.world.num_agents))

        for police in polices:
            police.initialize()
        for thief in thieves:
            thief.initialize()

        # if arglist.save_screen:
        #     save_screen_dirs = tom.render_utils.get_save_dirs(display_modes, arglist.plots_dir,
        #                                                       str(weights.shape[0]))

        # training each episode
        while True:
            # print("episode_step", episode_step)

            # compute current values
            # shape: num_police (or num_thief) x num_agents
            police_curr_values = [police.compute_value() for police in polices]
            thief_curr_values = [thief.compute_value() for thief in thieves]

            # select action based on current approximation
            for i, police in enumerate(polices):
                action_n[i] = police.select_action(new_obs_n[i])
            for i, thief in enumerate(thieves):
                action_n[i + len(polices)] = thief.select_action(new_obs_n[i + len(polices)])

            # update the environment
            new_obs_n, reward_n, done_n, info_n = env.step(action_n)
            done = any(done_n)

            # FIXME: only record the first agent's history
            episode_returns[-1] += reward_n[0]
            episode_info[-1].append(info_n['n'][0][0])
            episode_estimatio_belief[-1].append(polices[0].belief_mean.detach().squeeze().cpu().numpy()[0])

            # get next values
            police_next_values = [police.compute_value() for police in polices]
            thief_next_values = [thief.compute_value() for thief in thieves]

            if not arglist.restore:
                for i, police in enumerate(polices):
                    police.train(thieves[0], reward_n[i], done, police_prev_values[i],
                                 police_curr_values[i], police_next_values[i], new_obs_n[i])
                for i, thief in enumerate(thieves):
                    thief.train(polices[0], reward_n[env.world.num_polices + i], done, thief_prev_values[i],
                                thief_curr_values[i], thief_next_values[i], new_obs_n[env.world.num_polices + i])

            thief_prev_values = thief_next_values
            police_prev_values = police_next_values

            if arglist.display:
                # time.sleep(0.1)
                tom_both_sides.render_utils.render_image(env, arglist.display_mode)

            # Save screen
            # if arglist.save_screen:
            #     for i, d in enumerate(display_modes):
            #         filename = os.path.join(save_screen_dirs[i], "step{}.png".format(episode_step))
            #         tom.render_utils.save_screen(env, d, filename, belief=thief.belief)

            episode_step += 1
            if done or episode_step >= arglist.max_episode_len:
                episode_info[-1].append(done)
                break
            # End episode loop

        if not arglist.debug:
            # Save trained model every few training steps
            if weights.shape[0] % arglist.save_rate == 0:
                polices[0].save_model(os.path.join(arglist.save_dir, str(int(t_start)), 'police'))
                thieves[0].save_model(os.path.join(arglist.save_dir, str(int(t_start)), 'thief'))



def main():
    arglist = parse_args()
    train(arglist)


if __name__ == '__main__':
    main()