import cv2 as cv
import sys
from PyStream import PyStreamRun
from PyStream import getDrawRect

# https://learnopencv.com/object-tracking-using-opencv-cpp-python/
titleWindow = 'Tracker_PS.py'
trackerName = ""
currentIndex = 2
tracker = cv.TrackerKCF_create()
algorithmList = ['BOOSTING', 'MIL','KCF', 'TLD', 'MEDIANFLOW','MOSSE', 'CSRT'] #  'GOTURN',  (not working!)
saveIndex = 2
sr = (0, 0, 0, 0)

def on_trackbar(val):
    global trackerName, tracker
    currentIndex = val
    trackerName = algorithmList[currentIndex]
    if trackerName == 'BOOSTING':   tracker = cv.TrackerBoosting_create()
    if trackerName == 'MIL':        tracker = cv.TrackerMIL_create()
    if trackerName == 'KCF':        tracker = cv.TrackerKCF_create()
    if trackerName == 'TLD':        tracker = cv.TrackerTLD_create() # not very good!
    if trackerName == 'MEDIANFLOW': tracker = cv.TrackerMedianFlow_create()
    #if trackerName == 'GOTURN':     tracker = cv.TrackerGOTURN_create() # not working!
    if trackerName == 'MOSSE':      tracker = cv.TrackerMOSSE_create()
    if trackerName == "CSRT":       tracker = cv.TrackerCSRT_create()    

def OpenCVCode(imgRGB, depth32f, frameCount):
    global saveIndex, sr, algorithmList

    drawRect = getDrawRect()
    if saveIndex != currentIndex: # when the tracker is changed, use must redraw the rectangle.
        if sr == drawRect: drawRect = (0, 0, 0, 0)

    cv.imshow(titleWindow, imgRGB)
    cv.waitKey(1)

    # when the width of the drawRect is nonzero, then there is something to track
    if drawRect[3]:
        if sr != drawRect :
            on_trackbar(currentIndex) # this will reinitialize the tracker if just the drawRect changed
            sr = drawRect
            saveIndex = currentIndex
            # Initialize tracker with first imgRGB and bounding box
            ok = tracker.init(imgRGB, drawRect)

        # Update tracker
        ok, rectNew = tracker.update(imgRGB)
 
        # Draw bounding box
        if ok:
            p1 = (int(rectNew[0]), int(rectNew[1]))
            p2 = (int(rectNew[0] + rectNew[2]), int(rectNew[1] + rectNew[3]))
            cv.rectangle(imgRGB, p1, p2, (255,0,0), 2, 1)
            cv.putText(imgRGB, algorithmList[saveIndex] + " Tracker", (40,100), cv.FONT_HERSHEY_SIMPLEX, 0.75, (50,170,50),2)
        else :
            cv.putText(imgRGB, "Tracking failure detected", (40, 100), cv.FONT_HERSHEY_SIMPLEX, 0.75,(0,0,255),2)
    else :
        cv.putText(imgRGB, "Draw anywhere to start tracking", (40, 100), cv.FONT_HERSHEY_SIMPLEX, 0.75,(255,255,255),2)
        cv.putText(imgRGB, "Click to clear rect and start again", (40, 200), cv.FONT_HERSHEY_SIMPLEX, 0.75,(255,255,255),2)
    return imgRGB, None

cv.namedWindow(titleWindow)
cv.createTrackbar('Methods', titleWindow , currentIndex, len(algorithmList) - 1, on_trackbar)
on_trackbar(currentIndex)
PyStreamRun(OpenCVCode, titleWindow)