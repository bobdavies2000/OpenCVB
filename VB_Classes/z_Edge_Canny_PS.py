import cv2 as cv
import numpy as np
import sys
from z_PyStream import PyStreamRun
titleWindow = 'z_Edge_Canny_PS.py'

def nothing(*arg):
    pass
def OpenCVCode(imgRGB, depth32f, frameCount):
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    thrs1 = cv.getTrackbarPos('thrs1', 'edge')
    thrs2 = cv.getTrackbarPos('thrs2', 'edge')
    edge = cv.Canny(gray, thrs1, thrs2, apertureSize=5)
    vis = imgRGB.copy()
    vis = np.uint8(vis/2.)
    vis[edge != 0] = (0, 255, 0)
    cv.imshow('edge', vis)
    return vis, None

print(__doc__)
cv.namedWindow('edge')
cv.createTrackbar('thrs1', 'edge', 2000, 5000, nothing)
cv.createTrackbar('thrs2', 'edge', 4000, 5000, nothing)
PyStreamRun(OpenCVCode, titleWindow)
