"""
Created on Aug 20, 2018

@author: Siyuan Qi

Description of the file.

"""

import random
import collections


class Memory(object):
    def __init__(self, memory_size):
        self.memory_size = memory_size
        self.memory = collections.deque(maxlen=self.memory_size)

    def append_memory(self, experience):
        self.memory.append(experience)

    def uniform_sample(self, batch_size):
        return random.sample(self.memory, min(len(self.memory), batch_size))


def main():
    pass


if __name__ == '__main__':
    main()
