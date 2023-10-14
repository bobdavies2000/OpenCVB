# https://www.pyimagesearch.com/2017/01/02/rotate-images-correctly-with-opencv-and-python/
import numpy as np
import imutils
import cv2
 
# load the image from disk
image = cv2.imread('../Data/pic1.png')
 
# loop over the rotation angles
#for angle in np.arange(0, 360, 15):
#	rotated = imutils.rotate(image, angle)
#	cv2.imshow("Rotated (Problematic)", rotated)
#	cv2.waitKey(1000)
 
# loop over the rotation angles again, this time ensuring
# no part of the image is cut off
for i in range(100):
    for angle in np.arange(0, 360, 15):
        rotated = imutils.rotate_bound(image, angle)
        cv2.imshow("Rotated (Correct)", rotated)
        cv2.waitKey(1000)