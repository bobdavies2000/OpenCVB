# kalman_mousetracker.py - OpenCV mouse-tracking demo using TinyEKF
# Adapted from
#   http://www.morethantechnical.com/2011/06/17/simple-kalman-filter-for-tracking-using-opencv-2-2-w-code/
# Copyright (C) 2016 Simon D. Levy
# MIT License
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

# This delay will affect the Kalman update rate
DELAY_MSEC = 1

# Arbitrary display params
WINDOW_NAME = 'Kalman Mousetracker [ESC to quit]'
WINDOW_SIZE = 500

import cv2 as cv
import numpy as np
import sys
from TinyEKF import EKF
titleWindow = 'z_Kalman_TinyEKF.py'

class TrackerEKF(EKF):
    # An EKF for mouse tracking
    def __init__(self):
        # Two state values (mouse coordinates), two measurement values (mouse coordinates)
        EKF.__init__(self, 2, 2)

    def f(self, x):
        # State-transition function is identity
        return np.copy(x), np.eye(2)

    def h(self, x):
        # Observation function is identity
        return x, np.eye(2)

class MouseInfo(object):
    # A class to store X,Y points
    def __init__(self):
        self.x, self.y = -1, -1

    def __str__(self):
        return '%4d %4d' % (self.x, self.y)

def mouseCallback(event, x, y, flags, mouse_info):
    # Callback to update a MouseInfo object with new X,Y coordinates
    mouse_info.x = x
    mouse_info.y = y


def drawCross(img, center, r, g, b):
    #Draws a cross a the specified X,Y coordinates with color RGB
    d = 5
    t = 2
    color = (r, g, b)
    ctrx = center[0]
    ctry = center[1]
    cv.line(img, (ctrx - d, ctry - d), (ctrx + d, ctry + d), color, t, cv.LINE_AA)
    cv.line(img, (ctrx + d, ctry - d), (ctrx - d, ctry + d), color, t, cv.LINE_AA)


def drawLines(img, points, r, g, b):
    cv.polylines(img, [np.int32(points)], isClosed=False, color=(r, g, b))


def newImage():
    return np.zeros((WINDOW_SIZE,WINDOW_SIZE,3), np.uint8) 


try:
    if __name__ == '__main__':
        # Create a new image in a named window
        img = newImage()
        cv.namedWindow(WINDOW_NAME)

        # Create an X,Y mouse info object and set the window's mouse callback to modify it
        mouse_info = MouseInfo()
        cv.setMouseCallback(WINDOW_NAME, mouseCallback, mouse_info)

        # Loop until mouse inside window
        while True:

            if mouse_info.x > 0 and mouse_info.y > 0:
                break

            cv.imshow(WINDOW_NAME, img)
            if cv.waitKey(1) == 27:
                exit(0)

        # These will get the trajectories for mouse location and Kalman estiamte
        measured_points = []
        kalman_points = []

        # Create a new Kalman filter for mouse tracking
        kalfilt = TrackerEKF()

        # Loop till user hits escape
        count = 0
        while True:
            # Serve up a fresh image
            img = newImage()
            if count % 1000 == 0: 
                print(count)
                measured_points = []
                kalman_points = []
            count += 1

            # Grab current mouse position and add it to the trajectory
            measured = (mouse_info.x, mouse_info.y)
            measured_points.append(measured)

            # Update the Kalman filter with the mouse point, getting the estimate.
            estimate = kalfilt.step((mouse_info.x, mouse_info.y))

            # Add the estimate to the trajectory
            estimated = [int (c) for c in estimate]
            kalman_points.append(estimated)

            # Display the trajectories and current points
            drawLines(img, kalman_points,   0,   255, 0)
            drawCross(img, estimated,       255, 255, 255)
            drawLines(img, measured_points, 255, 255, 0)
            drawCross(img, measured, 0,   0,   255)

            # Delay for specified interval, quitting on ESC
            cv.imshow(WINDOW_NAME, img)
            if cv.waitKey(DELAY_MSEC) & 0xFF == 27:
                break
except Exception as exception:
    print(exception)
    Mbox('OpenCVB.py', 'Failure - see print output', 1)    
