'''
Camshift tracker
================

This is a demo that shows mean-shift based tracking
You select a color objects such as your face and it tracks it.
This reads the pipe connected to OpenCVB

http://www.robinhewitt.com/research/track/camshift.html

Usage:
------
    camshift.py 

    To initialize tracking, select the object with mouse

Keys:
-----
    ESC   - exit
    b     - toggle back-projected probability visualization
'''

import sys
import mmap
import array
import argparse
import numpy as np
import cv2 as cv
import os, time
from time import sleep
from z_PyStream import PyStreamRun
from z_PyStream import getDrawRect
titleWindow = 'z_Camshift1_PS.py'

class App(object):
    def show_hist(self, img):
        bin_count = self.hist.shape[0]
        bin_w = int(img.shape[1] / bin_count)
        for i in range(bin_count):
            h = int(self.hist[i])
            c = int(180.0*i/bin_count)
            cv.rectangle(img, (i*bin_w+2, 255), ((i+1)*bin_w-2, 255-h), (c, 255, 255), -1)
        img = cv.cvtColor(img, cv.COLOR_HSV2BGR)
        return img

    def Open(self):
        self.selectWindow = (0, 0, 0, 0)
        self.track_window = None
        self.drag_start = None
        self.initialized = False
        self.show_backproj = False
        self.hist = None
        PyStreamRun(self.OpenCVCode, titleWindow)

    def OpenCVCode(self, imgRGB, depth32f, frameCount):
        hsv = cv.cvtColor(imgRGB, cv.COLOR_BGR2HSV)
        mask = cv.inRange(hsv, np.array((0., 60., 32.)), np.array((180., 255., 255.)))
        self.img = np.copy(imgRGB)
        self.img[mask == 0] = 0

        rect = getDrawRect()
        if rect != self.selectWindow:
            self.track_window = rect
            self.selectWindow = rect
            x1, y1, x0, y0 = rect
            hsv_roi = hsv[y0:y1, x0:x1]
            mask_roi = mask[y0:y1, x0:x1]

            hist = cv.calcHist( [hsv_roi], [0], mask_roi, [32], [0, 180] )
            cv.normalize(hist, hist, 0, 255, cv.NORM_MINMAX)
            self.hist = hist.reshape(-1)
            imgRGB[mask == 0] = 0

        if np.any(self.hist != None):
            prob = cv.calcBackProject([hsv], [0], self.hist, [0, 180], 1)
            prob &= mask
            term_crit = ( cv.TERM_CRITERIA_EPS | cv.TERM_CRITERIA_COUNT, 10, 1 )
            track_box, self.track_window = cv.CamShift(prob, self.track_window, term_crit)
            graph = cv.cvtColor(imgRGB, cv.COLOR_HSV2BGR)
            self.img = self.show_hist(np.zeros(imgRGB.shape, np.uint8))

            if self.show_backproj:
                imgRGB[:] = prob[...,np.newaxis]
            try:
                cv.ellipse(imgRGB, track_box, (0, 0, 255), 2)
            except:
                print(track_box)

        return imgRGB, self.img

App().Open()