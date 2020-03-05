import random
import numpy as np
from mlagents_envs.base_env import StepResult
from tom.agent import Agent

class Thief:
    def __init__(self):
        pass

    def act(self, states: StepResult) -> np.ndarray:
        return np.random.rand(1, 2) * 2 - 1

class Thief(Agent):
    def __init__(self, world):
        super(Thief, self).__init__()
        self.identity = 'thief'
        self.enemy = None
        # self.actions = [[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1]]
        self.actions = np.array([[1, 0], [0, 1], [-1, 0], [0, -1]], dtype=np.float32) * 5.0
        self.offsets = np.array([[1, 0], [0, 1], [-1, 0], [0, -1]], dtype=np.float32) * 2.0

        self.kappa = 0.5

        # initialize belief uniformly
        self.num_choices = world.num_others + world.num_polices
        self.initial_prob = 0.5

        self.belief = np.zeros((world.num_agents, 2))
        self.reset_belief()

        # random movement if not police
        self.p_not_police = 1 / (2 * np.pi)

    def reset_belief(self):
        for i in range(self.num_choices):
            self.belief[i, 0] = self.initial_prob
        self.belief[:, 1] = 1 - self.belief[:, 0]

    def compute_new_belief(self, agent, velocity):
        direction = self.state.p_pos - agent.state.p_pos
        direction_norm = np.linalg.norm(direction)

        vel_agent_norm = np.linalg.norm(velocity)

        # No update if agent didn't move
        if vel_agent_norm != 0 and direction_norm != 0:
            dot = (np.dot(velocity, direction) / (vel_agent_norm * direction_norm))
            # To account for some random value, e.g. 1.0000000002.
            dot = np.clip(dot, -1, 1)
            theta = np.arccos(dot)

            new_belief = np.zeros(2)
            new_belief[0] = self.belief[agent.index, 0] * scipy.stats.vonmises.pdf(theta, self.kappa)
            new_belief[1] = self.belief[agent.index, 1] * self.p_not_police
            new_belief = new_belief / new_belief.sum()

            return new_belief
        else:
            return self.belief[agent.index, :]

    def update_belief(self, world):
        for agent in world.agents:
            if agent.identity != "thief":
                # velocity = agent.state.p_vel
                velocity = agent.action.u
                self.belief[agent.index, :] = self.compute_new_belief(agent, velocity)

    def act(self, thief, world):
        # TODO: change the threshold. Currently using 0.5.
        if np.max(self.belief[:, 0]) <= 0.0:
            thief.action.u = np.array(self.actions[np.random.randint(5)])

        else:
            # there is one agent with high probability of being a police.
            self.enemy = world.agents[np.argmax(self.belief[:, 0])]

            max_dist = 0.0
            best_action = -1
            for i in range(self.actions.shape[0]):
                p_vel, p_pos = world.compute_next_pos(thief, self.actions[i, :])
                dist = world.pos_distance(p_pos, self.enemy.state.p_pos)
                if dist > max_dist:
                    max_dist = dist
                    best_action = i
            action = self.actions[best_action, :]

            thief.action.u = action.astype(float)

        return thief.action

def main():
    pass


if __name__ == '__main__':
    main()
