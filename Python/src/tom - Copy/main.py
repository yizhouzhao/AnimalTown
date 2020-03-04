# todo: read config.json


def make_env2():
    from mlagents_envs.environment import UnityEnvironment
    from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig, EngineConfigurationChannel

    engine_configuration_channel = EngineConfigurationChannel()
    env = UnityEnvironment(base_port=5004, file_name=None, side_channels=[engine_configuration_channel])

    # Reset the environment
    env.reset()

    group_name = env.get_agent_groups()
    print(group_name)

    # Set the default brain to work with
    group_name = env.get_agent_groups()[0]
    group_spec = env.get_agent_group_spec(group_name)

    # Set the time scale of the engine
    engine_configuration_channel.set_configuration_parameters(time_scale=5.0)
    return env

def main():
    env = make_env2()
    env.reset()
    env.get_agent_groups()



if __name__ == '__main__':
    main()