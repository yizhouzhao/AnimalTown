from mlagents_envs.environment import UnityEnvironment
import numpy as np

from tom.police import Police
from tom.thief import Thief
# todo: read config.json


def make_env2() -> UnityEnvironment:
    from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig, EngineConfigurationChannel

    engine_configuration_channel = EngineConfigurationChannel()
    env = UnityEnvironment(base_port=5004, file_name=None, side_channels=[engine_configuration_channel])

    # Reset the environment
    # env.reset()
    #
    # group_name = env.get_agent_groups()
    # print(group_name)
    #
    # # Set the default brain to work with
    # group_name = env.get_agent_groups()[0]
    # group_spec = env.get_agent_group_spec(group_name)
    #
    # # Set the time scale of the engine
    # engine_configuration_channel.set_configuration_parameters(time_scale=5.0)
    return env


def main():
    env = make_env2()
    env.reset()

    # todo: load agents dynamically
    # todo: there could be multiple agents per group
    police = Police()
    thief = Thief()

    group_names = env.get_agent_groups()
    is_done = False
    for i in range(1000):
        for group_name in group_names:
            step_result = env.get_step_result(group_name)
            if np.any(step_result.done):  # if any agent is done, the episode is restarted
                is_done = True

            for agent_id in step_result.agent_id:
                next_states = step_result.get_agent_step_result(agent_id=agent_id)

                group_name_parts = group_name.split('?')
                if group_name_parts[0] == "Police":
                    actions = police.act(next_states)
                elif group_name_parts[0] == "Thief":
                    actions = thief.act(next_states)
                else:
                    raise NotImplementedError
                env.set_actions(agent_group=group_name, action=actions)
        if is_done:
            break
        env.step()
        print(i)

    env.close()
    print("Exit")


if __name__ == '__main__':
    main()
