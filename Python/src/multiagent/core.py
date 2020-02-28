import numpy as np
import scipy
import scipy.stats
import torch


# physical/external base state of all entities
class EntityState(object):
    def __init__(self):
        # physical position
        self.p_pos = None
        # physical velocity
        self.p_vel = None


# state of agents (including communication and internal/mental state)
class AgentState(EntityState):
    def __init__(self):
        super(AgentState, self).__init__()
        # communication utterance
        self.c = None


# action of the agent
class Action(object):
    def __init__(self):
        # physical action
        self.u = None
        # communication action
        self.c = None

        # rotational action--5 degrees clock-wise or counterclock-wise
        self.r = 0.0


# properties and state of physical world entity
class Entity(object):
    def __init__(self):
        # name 
        self.name = ''
        # index
        self.index = -1
        # properties:
        self.size = 0.050
        # entity can move / be pushed
        self.movable = False
        # entity collides with others
        self.collide = True
        # material density (affects mass)
        self.density = 25.0
        # color
        self.color = None
        # max speed and accel
        self.max_speed = None
        self.accel = None
        # state
        self.state = EntityState()
        # mass
        self.initial_mass = 1.0

    @property
    def mass(self):
        return self.initial_mass


# properties of landmark entities
class Landmark(Entity):
     def __init__(self):
        super(Landmark, self).__init__()


# properties of agent entities
class Agent(Entity):
    def __init__(self):
        super(Agent, self).__init__()
        # agents are movable by default
        self.movable = True
        # agents are not rotatable by default
        self.rotatable = False
        # cannot send communication signals
        self.silent = False
        # cannot observe the world
        self.blind = False
        # physical motor noise amount
        self.u_noise = None
        # communication noise amount
        self.c_noise = None
        # control range
        self.u_range = 1.0
        # state
        self.state = AgentState()
        # action
        self.action = Action()
        # script behavior to execute
        self.action_callback = None
        # identity: police, thief, or other
        self.identity = None


# multi-agent world
class World(object):
    def __init__(self, **kwargs):
        # list of agents and entities (can change at execution-time!)
        self.agents = []

        self.thieves = []
        self.polices = []
        self.others = []

        self.num_polices = kwargs.get('num_polices', 0)
        self.num_thieves = kwargs.get('num_thieves', 0)
        self.num_others = kwargs.get('num_others', 0)
        self.num_agents = self.num_polices + self.num_thieves + self.num_others
        self.open_world = kwargs.get('open_world', False)

        self.landmarks = []
        # communication channel dimensionality
        self.dim_c = 0
        # position dimensionality
        self.dim_p = 2

        # rotation dimensionality
        self.dim_r = 1

        # color dimensionality
        self.dim_color = 3
        # simulation timestep
        self.dt = 0.1
        # physical damping
        self.damping = 0.25
        # contact response parameters
        self.contact_force = 1e+2
        self.contact_margin = 1e-3

        # force discrete action
        # self.discrete_action = True

    # return all entities in the world
    @property
    def entities(self):
        return self.agents + self.landmarks

    # return all agents controllable by external policies
    @property
    def policy_agents(self):
        return [agent for agent in self.agents if agent.action_callback is None]

    # return all agents controlled by world scripts
    @property
    def scripted_agents(self):
        return [agent for agent in self.agents if agent.action_callback is not None]

    # update state of the world
    def step(self):
        # set actions for scripted agents
        # print("scripted agents;", self.scripted_agents)
        for agent in self.scripted_agents:
            agent.action = agent.action_callback(agent, self)
        # gather forces applied to entities
        p_force = [None] * len(self.entities)
        # apply agent physical controls
        p_force = self.apply_action_force(p_force)
        # apply environment forces
        p_force = self.apply_environment_force(p_force)
        # integrate physical state
        self.integrate_state(p_force)
        # update agent state
        for agent in self.agents:
            self.update_agent_state(agent)

    # gather agent action forces
    def apply_action_force(self, p_force):
        # set applied forces
        for i,agent in enumerate(self.agents):
            if agent.movable:
                noise = np.random.randn(*agent.action.u.shape) * agent.u_noise if agent.u_noise else 0.0
                p_force[i] = agent.action.u + noise                
        return p_force

    # gather physical forces acting on entities
    def apply_environment_force(self, p_force):
        # simple (but inefficient) collision response
        for a, entity_a in enumerate(self.entities):
            for b, entity_b in enumerate(self.entities):
                if b <= a:
                    continue
                [f_a, f_b] = self.get_collision_force(entity_a, entity_b)
                if f_a is not None:
                    if p_force[a] is None:
                        p_force[a] = 0.0
                    p_force[a] = f_a + p_force[a] 
                if f_b is not None:
                    if p_force[b] is None:
                        p_force[b] = 0.0
                    p_force[b] = f_b + p_force[b]        
        return p_force

    def compute_next_pos(self, entity, p_force):
        p_vel = entity.state.p_vel * (1 - self.damping)
        if p_force is not None:
            if type(p_force) == torch.Tensor:
                p_force = p_force.cpu().data.numpy()
            p_vel += ( p_force / entity.mass) * self.dt
        if entity.max_speed is not None:
            speed = np.linalg.norm(p_vel)
            if speed > entity.max_speed:
                p_vel = p_vel / speed * entity.max_speed
        p_pos = entity.state.p_pos + p_vel * self.dt

        if self.open_world:
            offset = np.array([1.0, 1.0])
            p_pos = (p_pos + offset) / 2.0
            p_pos -= np.floor(p_pos)
            p_pos = p_pos * 2.0 - offset
        else:
            p_pos = np.clip(p_pos, -1.0, 1.0)

        return p_vel, p_pos

    # integrate physical state
    def integrate_state(self, p_force):
        for i, entity in enumerate(self.entities):
            if not entity.movable: continue
            entity.state.p_vel, entity.state.p_pos = self.compute_next_pos(entity, p_force[i])

    def update_agent_state(self, agent):
        # set communication state (directly for now)
        if agent.silent:
            agent.state.c = np.zeros(self.dim_c)
        else:
            noise = np.random.randn(*agent.action.c.shape) * agent.c_noise if agent.c_noise else 0.0
            agent.state.c = agent.action.c + noise      

    # get collision forces for any contact between two entities
    def get_collision_force(self, entity_a, entity_b):
        if (not entity_a.collide) or (not entity_b.collide):
            return [None, None] # not a collider
        if (entity_a is entity_b):
            return [None, None] # don't collide against itself
        # compute actual distance between entities
        delta_pos = entity_a.state.p_pos - entity_b.state.p_pos
        dist = np.sqrt(np.sum(np.square(delta_pos)))
        # minimum allowable distance
        dist_min = entity_a.size + entity_b.size
        # softmax penetration
        k = self.contact_margin
        if dist != 0.0:
            penetration = np.logaddexp(0, -(dist - dist_min)/k)*k
            force = self.contact_force * delta_pos / dist * penetration
            force_a = +force if entity_a.movable else None
            force_b = -force if entity_b.movable else None
            return [force_a, force_b]
        else:
            return [0.0, 0.0]
        
    def pos_distance(self, pos_a, pos_b):
        if self.open_world:
            offset = np.array([[0, 0], [1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [1, -1], [-1, -1], [-1, 1]], dtype=np.float32) * 2.0
            distances = np.sqrt(np.sum(np.square(offset + pos_a - pos_b), axis=1))
            return np.min(distances)
        else:
            return np.sqrt(np.sum(np.square(pos_a - pos_b)))

    def distance(self, entity_a, entity_b):
        return self.pos_distance(entity_a.state.p_pos, entity_b.state.p_pos)

    def is_collision(self, entity_a, entity_b):
        dist = self.distance(entity_a, entity_b)
        dist_min = entity_a.size + entity_b.size

        return dist <= dist_min


class Police(Agent):
    def __init__(self):
        super(Police, self).__init__()
        self.identity = "police"


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


class Other(Agent):
    def __init__(self):
        super(Other, self).__init__()
        self.identity = 'other'
        self.actions = np.array([[1, 0], [0, 1], [-1, 0], [0, -1]]) * 5.0
        self.last_action = np.random.randint(5)

    # callback function for scripted others
    def act(self, other, world):
        offset = np.random.choice([-1, 0, 1], p=[0.3, 0.4, 0.3])
        self.last_action = (self.last_action + offset) % len(self.actions)

        other.action.u = self.actions[self.last_action, :]

        return other.action
