import random
import numpy as np
from mlagents_envs.base_env import StepResult


class Thief:
    def __init__(self):
        pass

    def act(self, states: StepResult) -> np.ndarray:
        return np.random.rand(1, 2) * 2 - 1


def main():
    pass


if __name__ == '__main__':
    main()
