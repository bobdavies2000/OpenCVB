import numpy as np
import cv2 as cv
import sys, getopt

# local modules
from common import clock, draw_str
from PyStream import PyStreamRun
titleWindow = 'Facedetect_PS.py'

def detect(img, cascade):
    rects = cascade.detectMultiScale(img, scaleFactor=1.1, minNeighbors=10, minSize=(20, 20),
                                     flags=cv.CASCADE_SCALE_IMAGE)
    if len(rects) == 0:
        return []
    rects[:,2:] += rects[:,:2]
    return rects

def draw_rects(img, rects, color):
    for x1, y1, x2, y2 in rects:
        cv.rectangle(img, (x1, y1), (x2, y2), color, 2)

def OpenCVCode(imgRGB, depth32f, frameCount):
    global cascade, nested
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    gray = cv.equalizeHist(gray)

    t = clock()
    rects = detect(gray, cascade)
    vis = imgRGB.copy()
    draw_rects(vis, rects, (0, 255, 0))
    if not nested.empty():
        for x1, y1, x2, y2 in rects:
            roi = gray[y1:y2, x1:x2]
            vis_roi = vis[y1:y2, x1:x2]
            subrects = detect(roi.copy(), nested)
            draw_rects(vis_roi, subrects, (255, 0, 0))
    dt = clock() - t

    draw_str(vis, (20, 20), 'time: %.1f ms' % (dt*1000))
    cv.imshow('facedetect', vis)
    return vis, None

if __name__ == '__main__':
    print('This example works only occasionally!  Same face model in C# works ok when face is vertical.')
    cascade_fn = "../opencv/data/haarcascades/haarcascade_frontalface_default.xml"
    nested_fn  = "../opencv/data/haarcascades/haarcascade_eye.xml"

    cascade = cv.CascadeClassifier(cv.samples.findFile(cascade_fn))
    nested = cv.CascadeClassifier(cv.samples.findFile(nested_fn))

    PyStreamRun(OpenCVCode, titleWindow)
