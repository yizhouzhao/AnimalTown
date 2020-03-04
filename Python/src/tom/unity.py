import os
import time
import pickle

# Libraries
import torch
import numpy as np
import seaborn as sns

# Local imports
import tom.render_utils
from tom.tom_agents import ToMAgent
from tom.utils import parse_args, plot_avg_return, plot_weights


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


def make_env2():
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig, EngineConfigurationChannel

    engine_configuration_channel = EngineConfigurationChannel()
    env = UnityEnvironment(base_port=5004, file_name=None, side_channels=[engine_configuration_channel])

    # Reset the environment
    # env.reset()

    # Set the default brain to work with
    # group_name = env.get_agent_groups()[0]
    # group_spec = env.get_agent_group_spec(group_name)

    # Set the time scale of the engine
    # engine_configuration_channel.set_configuration_parameters(time_scale=5.0)


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