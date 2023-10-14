import sys
import cv2 as cv
import numpy as np
from z_PyStream import PyStreamRun
titleWindow = 'z_Filter_2D_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    ddepth = -1
    kernel_size = 3 + 2 * (int(frameCount) % 5)
    kernel = np.ones((kernel_size, kernel_size), dtype=np.float32)
    kernel /= (kernel_size * kernel_size)
    dst2 = cv.filter2D(imgRGB, ddepth, kernel)
    dst3 = cv.filter2D(depth32f, ddepth, kernel)
    return dst2, np.asarray(dst3, dtype=np.uint8)

PyStreamRun(OpenCVCode, titleWindow)