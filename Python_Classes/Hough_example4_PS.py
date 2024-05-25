import numpy as np
import cv2 as cv
from z_PyStream import PyStreamRun
# https://docs.opencv.org/3.4/d4/d70/tutorial_hough_circle.html
import sys
titleWindow = 'z_Hough_example4_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global src
    if frameCount == 0:
        w = imgRGB.shape[1]
        h = imgRGB.shape[0]
        cv.resize(src,(w, h), src)
    img = cv.cvtColor(src, cv.COLOR_BGR2GRAY)
    img = cv.medianBlur(img, 5)
    cimg = src.copy() # numpy function

    circles = cv.HoughCircles(img, cv.HOUGH_GRADIENT, 1, 10, np.array([]), 100, 30, 1, 30)

    if circles is not None: # Check if circles have been found and only then iterate over these and add them to the image
        a, b, c = circles.shape
        for i in range(b):
            cv.circle(cimg, (circles[0][i][0], circles[0][i][1]), int(circles[0][i][2]), (0, 0, 255), 3, cv.LINE_AA)
            cv.circle(cimg, (circles[0][i][0], circles[0][i][1]), 2, (0, 255, 0), 3, cv.LINE_AA)  # draw center of circle
    return src, cimg

src = cv.imread(cv.samples.findFile('../Data/board.jpg'))
PyStreamRun(OpenCVCode, titleWindow)