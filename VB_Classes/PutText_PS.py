import cv2 as cv

title_window = 'PutText_PS.py'
    
def OpenCVCode(imgRGB, depth32f, frameCount):
    cv.putText(imgRGB, "The Python back end is updating this text", (40,80), cv.FONT_HERSHEY_SIMPLEX, 0.75,(0,0,255),2)
    return imgRGB

from PyStream import PyStreamRun
PyStreamRun(OpenCVCode, title_window)