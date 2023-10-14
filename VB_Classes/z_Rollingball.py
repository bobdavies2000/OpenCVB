# https://pypi.org/project/opencv-rolling-ball/
import cv2 as cv
titleWindow = 'z_Rollingball.py'
# be sure to add the package opencv-rolling-ball with Tools/Python/Environment/Packages
from cv2_rolling_ball import subtract_background_rolling_ball
img = cv.imread(f'../Data/rolling-ball-input.png', 0)
cv.imshow("Python input", img)
cv.waitKey(100)

print("Working on the image...This can take a while even on small images.")
img, background = subtract_background_rolling_ball(img, 30, light_background=True, use_paraboloid=False, do_presmooth=True)
cv.imshow("background", background)
cv.waitKey()