import cv2 as cv
from PyStream import PyStreamRun

alpha_slider_max = 100
title_window = 'AddWeighted_PS.py'
saveAlpha = 50
    
def on_trackbar(val):
    global saveAlpha 
    saveAlpha = val 

def OpenCVCode(imgRGB, depth32f, frameCount):
    alpha = saveAlpha / alpha_slider_max
    beta = ( 1.0 - alpha )
    depth_colormap = cv.applyColorMap(cv.convertScaleAbs(depth32f, alpha=0.03), cv.COLORMAP_HSV)
    dst = cv.addWeighted(imgRGB, alpha, depth_colormap, beta, 0.0)
    cv.imshow(title_window, dst)
    return dst

cv.namedWindow(title_window)
cv.createTrackbar('Alpha', title_window , saveAlpha, alpha_slider_max, on_trackbar)
on_trackbar(saveAlpha)
PyStreamRun(OpenCVCode, title_window)
