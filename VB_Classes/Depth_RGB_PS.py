import cv2 as cv
titleWindow = 'Depth_RGB_PS.py'
import numpy as np
from PyStream import PyStreamRun

def OpenCVCode(imgRGB, depth32f, frameCount):
    depth_colormap = cv.applyColorMap(cv.convertScaleAbs(depth32f, alpha=0.03), cv.COLORMAP_HSV)
    images = np.vstack((imgRGB, depth_colormap))
    cv.imshow("RGB and Depth Images", images)
    return depth_colormap

PyStreamRun(OpenCVCode, 'Depth_RGB_PS.py')
