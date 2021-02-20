import numpy as np
import cv2 as cv
import sys
from PyStream import PyStreamRun
titleWindow = 'MSER_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global mser
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    vis = imgRGB.copy()

    regions, _ = mser.detectRegions(gray)
    hulls = [cv.convexHull(p.reshape(-1, 1, 2)) for p in regions]
    cv.polylines(vis, hulls, 1, (0, 255, 0))
    cv.imshow('imgRGB', vis)
    return vis, None

if __name__ == '__main__':
    mser = cv.MSER_create()
    PyStreamRun(OpenCVCode, titleWindow)
