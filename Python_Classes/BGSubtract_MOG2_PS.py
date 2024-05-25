import cv2 as cv
from z_PyStream import PyStreamRun
titleWindow = 'z_BGSubtract_MOG2_PS.py'
fileName = '../opencv/Samples/Data/vtest.avi'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global backSub, capture, fileName
    ret, frame = capture.read()
    if frame is None:
        capture = cv.VideoCapture(cv.samples.findFileOrKeep(fileName))
        ret, frame = capture.read()

    fgMask = backSub.apply(frame)

    cv.rectangle(frame, (40, 100), (90, 120), (255,255,255), -1)
    cv.putText(frame, str(capture.get(cv.CAP_PROP_POS_FRAMES)), (40, 115), cv.FONT_HERSHEY_SIMPLEX, 0.5 , (0,0,0))

    return frame, fgMask

backSub = cv.createBackgroundSubtractorMOG2()
capture = cv.VideoCapture(cv.samples.findFileOrKeep(fileName))
PyStreamRun(OpenCVCode, titleWindow)