import numpy as np
from multiagent.core import World, Agent, Landmark, Police, Thief, Other
from multiagent.scenario import BaseScenario


class Scenario(BaseScenario):
    def __init__(self, **kwargs):
        super(Scenario, self).__init__()
        self.colors = {'police': [0.85, 0.25, 0.25], 'thief': [0.25, 0.85, 0.25], 'other': [0, 0, 0]}
        self.open_world = kwargs.get('open_world', True)
        self.setting = kwargs.get('setting', 'known_thief')
        self.permutation = []

        self.tom_thief = kwargs.get('tom_thief', False)

    def make_world(self):
        num_polices = 1
        num_thieves = 1
        num_others = 1
        world = World(num_polices=num_polices, num_thieves=num_thieves, num_others=num_others, open_world=self.open_world)
        world.dim_c = 1

        # Order matters here
        world.agents = [Other() for i in range(world.num_others)] \
                       + [Police() for i in range(world.num_polices)] \
                       + [Thief(world) for i in range(world.num_thieves)]

        for i, agent in enumerate(world.agents):
            agent.name = '{}-agent {}'.format(agent.identity, i)
            agent.index = i
            agent.color = np.array(self.colors[agent.identity])

            agent.collide = True
            agent.silent = True

        # accessor for different agents
        world.others = world.agents[: world.num_others]
        world.polices = world.agents[world.num_others:(world.num_others + world.num_polices)]
        world.thieves = world.agents[-world.num_thieves:]

        # make initial conditions
        self.reset_world(world)

        # action callbacks for scripted agents
        for i, agent in enumerate(world.agents):
            if agent.identity == "other":
                agent.action_callback = agent.act
            if agent.identity == "thief" and not self.tom_thief:
                agent.action_callback = agent.act

        return world

    def reset_world(self, world):
        for agent in world.agents:
            agent.state.p_pos = np.random.uniform(-1, +1, world.dim_p)
            agent.state.p_vel = np.zeros(world.dim_p)
            agent.state.c = np.zeros(world.dim_c)
            agent.state.r = 0.0

            for thief in world.thieves:
                thief.reset_belief()

        if self.setting == 'known_thief':
            self.permutation = np.arange(len(world.agents))
        elif self.setting == 'unknown_thief':
            self.permutation = np.random.permutation(len(world.agents))
        else:
            self.permutation = np.arange(len(world.agents))
            # raise ValueError('Scenario setting "{}" not recognized.'.format(self.setting))

    def reward(self, agent, world):
        reward = 0
        thief = world.thieves[0]
        police = world.polices[0]
        # Assuming one thief and one police

        # Use time as negative rewards
        if agent.identity == "police":
            if not world.is_collision(agent, thief):
                reward -= 0.1
            else:
                reward += 1.0
        else:
            if not world.is_collision(agent, police):
                reward += 0.1
            else:
                reward -= 1.0

        return reward

    def permute(self, obs):
        return obs[self.permutation, :]

    def observation(self, agent, world):
        positions = np.zeros((len(world.agents), 2))
        velocities = np.zeros((len(world.agents), 2))
        forces = np.zeros((len(world.agents), 2))
        for i, other in enumerate(world.agents):
            positions[i, :] = other.state.p_pos
            velocities[i, :] = other.state.p_vel
            if other.action.u is not None:
                forces[i, :] = other.action.u
        return self.permute(velocities), self.permute(positions), self.permute(forces)

    def done(self, agent, world):
        if agent.identity == "police":
            for thief in world.thieves:
                if world.is_collision(agent, thief):
                    return True
            return False
        else:
            for police in world.polices:
                if world.is_collision(agent, police):
                    return True
            return False

    def info(self, agent, world):
        # return the updated belief of thief
        # assuming only one thief
        assert(world.num_thieves == 1)
        belief = world.thieves[0].belief

        return belief[agent.index]

    def benchmark_data(self, agent, world):
        # returns data for benchmarking purposes
        return self.info(agent, world)
