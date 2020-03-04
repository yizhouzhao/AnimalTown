import os
import time

import numpy as np
import scipy.misc


def get_save_dirs(display_modes, save_dir, episode_index):
    save_screen_dirs = []
    for i, d in enumerate(display_modes):
        save_screen_dir = os.path.join(save_dir, "epoch" + str(episode_index), d)
        if not os.path.exists(save_screen_dir):
            os.makedirs(save_screen_dir)
        save_screen_dirs.append(save_screen_dir)
    return save_screen_dirs


def get_display_modes(display_mode):
    if display_mode == "all":
        display_modes = ["normal", "belief", "uniform"]
    else:
        display_modes = [display_mode]
    return display_modes


def render_image(env, display_mode, belief=None, visible=True):
    if display_mode == "normal" or display_mode == "all":
        image = env.render(mode='rgb_array', visible=visible)
    elif display_mode == "belief":
        image = env.render(mode='rgb_array', belief=belief, visible=visible)
    elif display_mode == "uniform":
        image = env.render(mode='rgb_array', belief=np.ones_like(belief)*0.5, visible=visible)
    return image


def save_screen(env, display_mode, filename, belief=None):
    image = render_image(env, display_mode, belief, False)
    scipy.misc.imsave(filename, image)

