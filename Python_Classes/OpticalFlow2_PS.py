import numpy as np
import cv2 as cv
import sys
from PyStream import PyStreamRun
titleWindow = 'OpticalFlow2_PS.py'

def draw_flow(img, flow, step=16):
    height, width = img.shape[:2]
    y, x = np.mgrid[step/2:height:step, step/2:width:step].reshape(2,-1).astype(int)
    fx, fy = flow[y,x].T
    lines = np.vstack([x, y, x+fx, y+fy]).T.reshape(-1, 2, 2)
    lines = np.int32(lines + 0.5)
    vis = cv.cvtColor(img, cv.COLOR_GRAY2BGR)
    cv.polylines(vis, lines, 0, (0, 255, 0))
    for (x1, y1), (_x2, _y2) in lines:
        cv.circle(vis, (x1, y1), 1, (0, 255, 0), -1)
    return vis


def draw_hsv(flow):
    height, width = flow.shape[:2]
    fx, fy = flow[:,:,0], flow[:,:,1]
    ang = np.arctan2(fy, fx) + np.pi
    v = np.sqrt(fx*fx+fy*fy)
    hsv = np.zeros((height, width, 3), np.uint8)
    hsv[...,0] = ang*(180/np.pi/2)
    hsv[...,1] = 255
    hsv[...,2] = np.minimum(v*4, 255)
    bgr = cv.cvtColor(hsv, cv.COLOR_HSV2BGR)
    return bgr


def warp_flow(img, flow):
    height, width = flow.shape[:2]
    flow = -flow
    flow[:,:,0] += np.arange(width)
    flow[:,:,1] += np.arange(height)[:,np.newaxis]
    res = cv.remap(img, flow, None, cv.INTER_LINEAR)
    return res

def OpenCVCode(imgRGB, depth32f, frameCount):
    global prev_imgRGB, prev, show_hsv, show_glitch, cur_glitch
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    if frameCount == 0:
        show_hsv = True
        show_glitch = True
        prev_imgRGB = gray.copy()
        cur_glitch = imgRGB.copy()
        return imgRGB, None
    else:
        flow = cv.calcOpticalFlowFarneback(prev_imgRGB, gray, None, 0.5, 3, 15, 3, 5, 1.2, 0)
        prev_imgRGB = gray.copy()

        cur_glitch = warp_flow(cur_glitch, flow)
        cv.imshow('glitch', cur_glitch)

        if frameCount % 30 == 0: cur_glitch = imgRGB.copy()
    return draw_flow(gray, flow), draw_hsv(flow)

if __name__ == '__main__':
    PyStreamRun(OpenCVCode, titleWindow)
    