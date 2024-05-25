import cv2 as cv
from z_PyStream import PyStreamRun

alpha_slider_max = 100
titleWindow = 'z_AddWeighted_PS.py'
saveAlpha = 50
    
def on_trackbar(val):
    global saveAlpha 
    saveAlpha = val 

def OpenCVCode(imgRGB, depth32f, frameCount):
    alpha = saveAlpha / alpha_slider_max
    beta = ( 1.0 - alpha )
    depth_colormap = cv.applyColorMap(cv.convertScaleAbs(depth32f, alpha=0.03), cv.COLORMAP_HSV)
    dst = cv.addWeighted(imgRGB, alpha, depth_colormap, beta, 0.0)
    cv.imshow(titleWindow, dst)
    return dst, None

cv.namedWindow(titleWindow)
cv.createTrackbar('Alpha', titleWindow , saveAlpha, alpha_slider_max, on_trackbar)
on_trackbar(saveAlpha)
PyStreamRun(OpenCVCode, titleWindow)
