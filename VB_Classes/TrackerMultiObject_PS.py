import numpy as np
import cv2 as cv
import sys
from PyStream import PyStreamRun
from PyStream import getDrawRect

title_window = "MultiTracker_PS.py"
# https://docs.opencv.org/3.4/d8/d77/classcv_1_1MultiTracker.html

cv.namedWindow(title_window)
tracker = cv.MultiTracker_create()
saveRect = (0, 0, 0, 0)

def OpenCVCode(imgRGB, depth32f, frameCount):
    global saveRect
    drawRect = getDrawRect()
    if saveRect != drawRect :
        ok = tracker.add(cv.TrackerMIL_create(), imgRGB, drawRect)
        saveRect = drawRect

    ok, boxes = tracker.update(imgRGB)

    for newbox in boxes:
        p1 = (int(newbox[0]), int(newbox[1]))
        p2 = (int(newbox[0] + newbox[2]), int(newbox[1] + newbox[3]))
        cv.rectangle(imgRGB, p1, p2, (200,0,0))

    cv.imshow(title_window, imgRGB)
    if saveRect == (0, 0, 0, 0): cv.putText(imgRGB, "Draw here to select an object to track", (40,100), cv.FONT_HERSHEY_SIMPLEX, 0.75, (50,170,50),2)
    return imgRGB

PyStreamRun(OpenCVCode, title_window)