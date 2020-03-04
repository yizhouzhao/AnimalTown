"""
Created on Jan 11, 2018

@author: Siyuan Qi

Description of the file.

"""

import errno
import logging
import os
import pathlib

PROJECT_ROOT = pathlib.Path().absolute().parent
print(PROJECT_ROOT)
exp_name = 'fast_thief'  # fast_thief / fast_thief
episodes = 1000
episode_len = 100
compute_avg_length = min(100, episodes)
save_rate = min(50, episodes)


class Paths(object):
    def __init__(self):
        """
        Configuration of data paths
        member variables:
            data_root: The root folder of all the recorded data of events
            metadata_root: The root folder where the processed information (Skeleton and object features) is stored.
        """
        self.project_root = PROJECT_ROOT
        self.src_root = os.path.join(self.project_root, 'src')
        self.tmp_root = os.path.join(self.project_root, 'tmp')
        self.log_root = os.path.join(self.project_root, 'log')


def set_logger(name='learner.log'):
    if not os.path.exists(os.path.dirname(name)):
        try:
            os.makedirs(os.path.dirname(name))
        except OSError as exc:  # Guard against race condition
            if exc.errno != errno.EEXIST:
                raise

    logger = logging.getLogger(name)
    file_handler = logging.FileHandler(name, mode='w')
    file_handler.setFormatter(logging.Formatter('%(asctime)s %(levelname)s: %(message)s',
                                                "%Y-%m-%d %H:%M:%S"))
    logger.addHandler(file_handler)
    logger.setLevel(logging.DEBUG)
    return logger


def main():
    pass


if __name__ == '__main__':
    main()
