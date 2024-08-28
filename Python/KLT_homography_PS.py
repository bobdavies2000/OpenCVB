'''
Lucas-Kanade homography tracker
===============================

Lucas-Kanade sparse optical flow demo. Uses goodFeaturesToTrack
for track initialization and back-tracking for match verification
between frames. Finds homography between reference and current views.

https://github.com/opencv/opencv/blob/master/samples/python/lk_homography.py

Usage
-----
lk_homography_PS.py 


Keys
----
SPACE - start tracking
r     - toggle RANSAC
'''

import numpy as np
import cv2 as cv
import sys
from PyStream import PyStreamRun
titleWindow = 'KLT_homography_PS.py'

from common import draw_str

lk_params = dict( winSize  = (19, 19),
                  maxLevel = 2,
                  criteria = (cv.TERM_CRITERIA_EPS | cv.TERM_CRITERIA_COUNT, 10, 0.03))

feature_params = dict( maxCorners = 1000,
                       qualityLevel = 0.01,
                       minDistance = 8,
                       blockSize = 19 )

def checkedTrace(img0, img1, p0, back_threshold = 1.0):
    p1, _st, _err = cv.calcOpticalFlowPyrLK(img0, img1, p0, None, **lk_params)
    p0r, _st, _err = cv.calcOpticalFlowPyrLK(img1, img0, p1, None, **lk_params)
    d = abs(p0-p0r).reshape(-1, 2).max(-1)
    status = d < back_threshold
    return p1, status

green = (0, 255, 0)
red = (0, 0, 255)

class App:
    def Open(self):
        self.p0 = None
        self.use_ransac = True
        print(__doc__)
        PyStreamRun(self.OpenCVCode, titleWindow)

    def OpenCVCode(self, imgRGB, depth32f, frameCount):
        frame_gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)
        vis = imgRGB.copy()
        if self.p0 is not None:
            p2, trace_status = checkedTrace(self.gray1, frame_gray, self.p1)

            self.p1 = p2[trace_status].copy()
            self.p0 = self.p0[trace_status].copy()
            self.gray1 = frame_gray

            if len(self.p0) < 4:
                self.p0 = None
                exit
            H, status = cv.findHomography(self.p0, self.p1, (0, cv.RANSAC)[self.use_ransac], 10.0)
            h, w = imgRGB.shape[:2]
            overlay = cv.warpPerspective(self.frame0, H, (w, h))
            vis = cv.addWeighted(vis, 0.5, overlay, 0.5, 0.0)

            for (x0, y0), (x1, y1), good in zip(self.p0[:,0], self.p1[:,0], status[:,0]):
                if good:
                    cv.line(vis, (x0, y0), (x1, y1), (0, 128, 0))
                cv.circle(vis, (x1, y1), 2, (red, green)[good], -1)
            draw_str(vis, (20, 20), 'track count: %d' % len(self.p1))
            if self.use_ransac:
                draw_str(vis, (20, 40), 'RANSAC')
        else:
            p = cv.goodFeaturesToTrack(frame_gray, **feature_params)
            if p is not None:
                for x, y in p[:,0]:
                    cv.circle(vis, (x, y), 2, green, -1)
                draw_str(vis, (20, 20), 'feature count: %d' % len(p))

        cv.imshow('lk_homography', vis)
        ch = cv.waitKey(1)
        if ch == ord(' '):
            self.frame0 = imgRGB.copy()
            self.p0 = cv.goodFeaturesToTrack(frame_gray, **feature_params)
            if self.p0 is not None:
                self.p1 = self.p0
                self.gray0 = frame_gray
                self.gray1 = frame_gray
        if ch == ord('r'):
            self.use_ransac = not self.use_ransac
        return vis, None

App().Open()
