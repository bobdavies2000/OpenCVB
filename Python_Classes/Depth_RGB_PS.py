import cv2 as cv
titleWindow = 'z_Depth_RGB_PS.py'
import numpy as np
from z_PyStream import PyStreamRun

def OpenCVCode(imgRGB, depth32f, frameCount):
    depth_colormap = cv.applyColorMap(cv.convertScaleAbs(depth32f, alpha=0.03), cv.COLORMAP_HSV)
    return imgRGB, depth_colormap

PyStreamRun(OpenCVCode, titleWindow)
