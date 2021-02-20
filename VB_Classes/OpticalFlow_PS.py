import numpy as np
import cv2 as cv
import sys
from PyStream import PyStreamRun
titleWindow = 'OpticalFlow_PS.py'

def set_ShowHSV(val):
    global show_hsv 
    show_hsv = val 
    print('HSV flow visualization is', ['off', 'on'][show_hsv == 0])
def set_ShowGlitch(val):
    global show_glitch 
    show_glitch = val 
    print('glitch is', ['off', 'on'][show_glitch])
def set_SpatialPropagation(val):
    global inst, use_spatial_propagation 
    use_spatial_propagation = val
    inst.setUseSpatialPropagation(use_spatial_propagation == 1)
    print('spatial propagation is', ['off', 'on'][val])
def set_TemporalPropagation(val): # this doesn't do anything yet
    global use_temporal_propagation 
    use_temporal_propagation = val 
    print('temporal propagation is', ['off', 'on'][use_temporal_propagation])


def draw_flow(img, flow, step=16):
    height, width = flow.shape[:2]
    y, x = np.mgrid[step/2:height:step, step/2:width:step].reshape(2,-1).astype(int)
    fx, fy = flow[y,x].T
    lines = np.vstack([x, y, x+fx, y+fy]).T.reshape(-1, 2, 2)
    lines = np.int32(lines + 0.5)
    vis = cv.cvtColor(img, cv.COLOR_GRAY2BGR)
    cv.polylines(vis, lines, 0, (0, 255, 0))
    for (x1, y1), (x2, y2) in lines:
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
    global show_hsv, show_glitch, use_spatial_propagation, use_temporal_propagation, cur_glitch, prevgray, inst, flow, initialized 
    if initialized == False:
        initialized = True
        prevgray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
        cur_glitch = imgRGB.copy()
        cv.namedWindow('flow', cv.WINDOW_AUTOSIZE)
        cv.createTrackbar('HSV Flow', 'flow', show_hsv, 1, set_ShowHSV)
        cv.createTrackbar('Glitch Window', 'flow', show_glitch, 1, set_ShowGlitch)
        cv.createTrackbar('Spatial Prop.', 'flow', use_spatial_propagation, 1, set_SpatialPropagation)
        cv.createTrackbar('Temporal Prop.', 'flow', use_temporal_propagation, 1, set_TemporalPropagation)

    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
    if flow is not None and use_temporal_propagation:
        #warp previous flow to get an initial approximation for the current flow:
        flow = inst.calc(prevgray, gray, warp_flow(flow,flow))
    else:
        flow = inst.calc(prevgray, gray, None)
    prevgray = gray

    flowRGB = draw_flow(gray, flow)
    cv.imshow('flow', flowRGB)
    if show_hsv:
        cv.imshow('flow HSV', draw_hsv(flow))
    if show_glitch:
        cur_glitch = warp_flow(cur_glitch, flow)
        cv.imshow('glitch', cur_glitch)
    return flowRGB, None

if __name__ == '__main__':
    print(__doc__)
    initialized = False
    prevgray = None
    show_hsv = 0
    show_glitch = False
    use_spatial_propagation = False
    use_temporal_propagation = True
    cur_glitch = None
    inst = cv.DISOpticalFlow.create(cv.DISOPTICAL_FLOW_PRESET_MEDIUM)
    inst.setUseSpatialPropagation(use_spatial_propagation)
    flow = None

PyStreamRun(OpenCVCode, titleWindow)

