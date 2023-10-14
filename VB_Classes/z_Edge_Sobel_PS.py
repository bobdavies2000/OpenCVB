import sys
import cv2 as cv
from z_PyStream import PyStreamRun
titleWindow = 'z_Edge_Sobel_PS.py'

def OpenCVCode(src, depth32f, frameCount):
    scale = 1
    delta = 0
    ddepth = cv.CV_16S
    # Remove noise by blurring with a Gaussian filter ( kernel size = 3 )
    src = cv.GaussianBlur(src, (3, 3), 0)

    gray = cv.cvtColor(src, cv.COLOR_BGR2GRAY)
    grad_x = cv.Sobel(gray, ddepth, 1, 0, ksize=3, scale=scale, delta=delta, borderType=cv.BORDER_DEFAULT)
    grad_y = cv.Sobel(gray, ddepth, 0, 1, ksize=3, scale=scale, delta=delta, borderType=cv.BORDER_DEFAULT)

    abs_grad_x = cv.convertScaleAbs(grad_x)
    abs_grad_y = cv.convertScaleAbs(grad_y)
    grad = cv.addWeighted(abs_grad_x, 0.5, abs_grad_y, 0.5, 0)
    return grad, None

PyStreamRun(OpenCVCode, titleWindow)