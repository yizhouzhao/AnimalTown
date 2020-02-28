import os

# Local imports
import tom.config
import tom.render_utils

paths = tom.config.Paths()
exp_name = tom.config.exp_name
# parameters
method = 'chase'  # ['ToM', 'maddpg', 'chase]
display_mode = "all"  # ["belief", "uniform", "all"]
time_str = ""
if method == "maddpg":
    time_str = "1535704787"
epochs = [1]


display_modes = tom.render_utils.get_display_modes(display_mode)

for epoch in epochs:
    plots_dir = os.path.join(paths.tmp_root, 'plots', exp_name, method)
    if method == "maddpg":
        plots_dir = os.path.join(paths.tmp_root, 'plots', exp_name, method, time_str)
    image_dirs = tom.render_utils.get_save_dirs(display_modes, plots_dir, str(epoch))
    for image_dir in image_dirs:
        os.chdir(image_dir)
        os.system(
            'ffmpeg -f image2 -r 7 -s 1920x1080 -i step%d.png -vcodec mpeg4 -crf 25 -pix_fmt yuv420p -y ./video.mp4')
