import cv2 as cv
from PyStream import PyStreamRun1
titleWindow = 'BGSubtract_PS1.py'
fileName = '../Data/vtest.avi'

def OpenCVCode(frameCount):
    global backSub, capture, fileName
    ret, frame = capture.read()
    if frame is None:
        capture = cv.VideoCapture(cv.samples.findFileOrKeep(fileName))
        ret, frame = capture.read()

    fgMask = backSub.apply(frame)

    cv.rectangle(frame, (10, 2), (100,20), (255,255,255), -1)
    cv.putText(frame, str(capture.get(cv.CAP_PROP_POS_FRAMES)), (15, 15), cv.FONT_HERSHEY_SIMPLEX, 0.5 , (0,0,0))

    return frame, fgMask

#backSub = cv.createBackgroundSubtractorKNN()
backSub = cv.createBackgroundSubtractorMOG2()
capture = cv.VideoCapture(cv.samples.findFileOrKeep(fileName))
PyStreamRun1(OpenCVCode, titleWindow)