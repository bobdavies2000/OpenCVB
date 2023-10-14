#!/usr/bin/env python
import numpy as np
import cv2 as cv
import sys
from z_PyStream import PyStreamRun
titleWindow = "MotionTemplate_PS.py"

MHI_DURATION = 0.5
DEFAULT_THRESHOLD = 32
MAX_TIME_DELTA = 0.25
MIN_TIME_DELTA = 0.05

# (empty) trackbar callback
def nothing(dummy):
    pass

def draw_motion_comp(vis, rect, angle, color):
    cv.rectangle(vis, (rect[0], rect[1]), (rect[0]+rect[2], rect[1]+rect[3]), (0, 255, 0))
    r = min(rect[2]/2, rect[3]/2)
    cx, cy = rect[0]+rect[2]/2, rect[1]+rect[3]/2
    angle = angle*np.pi/180
    cv.circle(vis, (int(cx), int(cy)), int(r), color, 3)
    cv.line(vis, (int(cx), int(cy)), (int(cx+np.cos(angle)*r), int(cy+np.sin(angle)*r)), color, 3)

def OpenCVCode(imgRGB, depth32f, frameCount):
    global myFrameCount, prev_imgRGB
    height, width = imgRGB.shape[:2]
    motion_history = np.zeros((height, width), np.float32)
    hsv = np.zeros((height, width, 3), np.uint8)
    hsv[:,:,1] = 255
     
    if myFrameCount > 0:
        frame_diff = cv.absdiff(imgRGB, prev_imgRGB)
        gray_diff = cv.cvtColor(frame_diff, cv.COLOR_BGR2GRAY)
        thrs = cv.getTrackbarPos('threshold', titleWindow)
        ret, motion_mask = cv.threshold(gray_diff, thrs, 1, cv.THRESH_BINARY)
        timestamp = cv.getTickCount() / cv.getTickFrequency()
        cv.motempl.updateMotionHistory(motion_mask, motion_history, timestamp, MHI_DURATION)
        mg_mask, mg_orient = cv.motempl.calcMotionGradient( motion_history, MAX_TIME_DELTA, MIN_TIME_DELTA, apertureSize=5 )
        seg_mask, seg_bounds = cv.motempl.segmentMotion(motion_history, timestamp, MAX_TIME_DELTA)

        visual_name = visuals[cv.getTrackbarPos('visual', titleWindow)]
        if visual_name == 'input':
            vis = imgRGB.copy()
        elif visual_name == 'frame_diff':
            vis = frame_diff.copy()
        elif visual_name == 'motion_hist':
            vis = np.uint8(np.clip((motion_history-(timestamp-MHI_DURATION)) / MHI_DURATION, 0, 1)*255)
            vis = cv.cvtColor(vis, cv.COLOR_GRAY2BGR)
        elif visual_name == 'grad_orient':
            hsv[:,:,0] = mg_orient/2
            hsv[:,:,2] = mg_mask*255
            vis = cv.cvtColor(hsv, cv.COLOR_HSV2BGR)

        for i, rect in enumerate([(0, 0, width, height)] + list(seg_bounds)):
            x, y, rw, rh = rect
            area = rw*rh
            if area < 64**2:
                continue
            silh_roi   = motion_mask   [y:y+rh,x:x+rw]
            orient_roi = mg_orient     [y:y+rh,x:x+rw]
            mask_roi   = mg_mask       [y:y+rh,x:x+rw]
            mhi_roi    = motion_history[y:y+rh,x:x+rw]
            if cv.norm(silh_roi, cv.NORM_L1) < area*0.05:
                continue
            angle = cv.motempl.calcGlobalOrientation(orient_roi, mask_roi, mhi_roi, timestamp, MHI_DURATION)
            color = ((255, 0, 0), (0, 0, 255))[i == 0]
            draw_motion_comp(vis, rect, angle, color)

        cv.putText(vis, visual_name, (20, 20), cv.FONT_HERSHEY_PLAIN, 1.0, (200,0,0))
        cv.imshow(titleWindow, vis)
        return vis, None
    else :
        return imgRGB, None

    prev_imgRGB = imgRGB.copy()
    myFrameCount += 1

myFrameCount = 0
cv.namedWindow(titleWindow)
visuals = ['input', 'frame_diff', 'motion_hist', 'grad_orient']
cv.createTrackbar('visual', titleWindow, 2, len(visuals)-1, nothing)
cv.createTrackbar('threshold', titleWindow, DEFAULT_THRESHOLD, 255, nothing)
PyStreamRun(OpenCVCode, titleWindow)
