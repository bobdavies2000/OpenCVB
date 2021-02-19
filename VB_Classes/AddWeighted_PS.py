import cv2 as cv

alpha_slider_max = 100
title_window = 'AddWeighted_PS.py'
saveAlpha = 50
    
def on_trackbar(val):
    global saveAlpha 
    saveAlpha = val 

def OpenCVCode(imgRGB, depth_colormap, frameCount):
    alpha = saveAlpha / alpha_slider_max
    beta = ( 1.0 - alpha )
    dst1 = cv.addWeighted(imgRGB, alpha, depth_colormap, beta, 0.0)
    cv.imshow(title_window, dst1)

cv.namedWindow(title_window)
cv.createTrackbar('Alpha', title_window , saveAlpha, alpha_slider_max, on_trackbar)
on_trackbar(saveAlpha)
from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, title_window)
