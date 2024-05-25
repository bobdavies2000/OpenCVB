import cv2 as cv
from z_PyStream import PyStreamRun

titleWindow = 'z_Python_Stream_PS.py'
    
def OpenCVCode(imgRGB, depth32f, frameCount):
    cv.putText(imgRGB, "Python backend: frameCount = " + str(int(frameCount)), (40,80), cv.FONT_HERSHEY_SIMPLEX, 0.75,(0,0,255),2)
    return imgRGB, None

PyStreamRun(OpenCVCode, titleWindow)