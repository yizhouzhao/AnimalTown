import numpy as np
from multiagent.core import World, Agent, Landmark, Police, Thief, Other


class Scenario:
    def __init__(self, **kwargs):
        self.colors = {'police': [0.85, 0.25, 0.25], 'thief': [0.25, 0.85, 0.25], 'other': [0, 0, 0]}
        self.open_world = kwargs.get('open_world', True)
        self.setting = kwargs.get('setting', 'known_thief')
        self.permutation = []

        self.tom_thief = kwargs.get('tom_thief', False)

