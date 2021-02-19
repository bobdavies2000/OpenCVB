import cv2 as cv
title_window = 'Depth_RGB_PS.py'
import numpy as np
def OpenCVCode(imgRGB, depth32f, frameCount):
    depth_colormap = cv.applyColorMap(cv.convertScaleAbs(depth32f, alpha=0.03), cv.COLORMAP_HSV)
    images = np.vstack((imgRGB, depth_colormap))
    cv.imshow("RGB and Depth Images", images)
    return depth_colormap

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, 'Depth_RGB_PS.py')
