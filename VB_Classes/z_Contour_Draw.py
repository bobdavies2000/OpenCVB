# USAGE - How to run this code ?
# python find_shapes.py --image shapes.png

import numpy as np
import argparse
import cv2 as cv
titleWindow = 'z_Contour_Draw.py'

# construct the argument parse and parse the arguments
ap = argparse.ArgumentParser()
ap.add_argument("-i", "--image", help = "path to the image file")
args = vars(ap.parse_args())

# load the image
image = cv.imread('../opencv/Samples/Data/lena.jpg') # args["image"])
lower = np.array([20,0,155])
upper = np.array([255,120,250])
shapeMask = cv.inRange(image, lower, upper)
size = image.shape[0], image.shape[1], 1
mask = np.array(np.frombuffer(shapeMask, np.uint8).reshape(size))
mask = cv.cvtColor(mask, cv.COLOR_GRAY2BGR)

# find the contours in the mask
(cnts, _) = cv.findContours(shapeMask.copy(), cv.RETR_EXTERNAL, cv.CHAIN_APPROX_SIMPLE)

# loop over the contours
for c in cnts:
    cv.drawContours(image, [c], -1, (0, 255, 0), 2)

size = image.shape[0], image.shape[1], 3
both = np.empty((image.shape[0], image.shape[1]*2, image.shape[2]), image.dtype)
both = cv.hconcat([image, mask])
cv.imshow("Contours found using Mask/FindContours", both)
cv.waitKey(0)
