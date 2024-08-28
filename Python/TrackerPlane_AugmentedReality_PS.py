'''
Planar augmented reality
==================

This sample shows an example of augmented reality overlay over a planar object
tracked by PlaneTracker from plane_tracker.py. solvePnP function is used to
estimate the tracked object location in 3d space.

video: http://www.youtube.com/watch?v=pzVbhxx6aog

Usage
-----
TrackerPlane_AugmentedReality_PS.py 

Keys:
   SPACE  -  pause video
   c      -  clear targets

Select a textured planar object to track by drawing a box with a mouse.
Use 'focal' slider to adjust to camera focal length for proper video augmentation.
'''
import numpy as np
import cv2 as cv
import common
from TrackerPlane_PS import PlaneTracker
from PyStream import PyStreamRun
import sys
titleWindow = "TrackerPlane_AugmentedReality_PS.py"

# Simple model of a house - cube with a triangular prism "roof"
ar_verts = np.float32([[0, 0, 0], [0, 1, 0], [1, 1, 0], [1, 0, 0],
                       [0, 0, 1], [0, 1, 1], [1, 1, 1], [1, 0, 1],
                       [0, 0.5, 2], [1, 0.5, 2]])
ar_edges = [(0, 1), (1, 2), (2, 3), (3, 0),
            (4, 5), (5, 6), (6, 7), (7, 4),
            (0, 4), (1, 5), (2, 6), (3, 7),
            (4, 8), (5, 8), (6, 9), (7, 9), (8, 9)]

class App:
    def Open(self):
        self.imgRGB = None
        self.paused = False
        self.tracker = PlaneTracker()

        cv.namedWindow(titleWindow)
        cv.createTrackbar('focal', titleWindow, 25, 50, common.nothing)
        self.rect_sel = common.RectSelector(titleWindow, self.on_rect)
        PyStreamRun(self.OpenCVCode,  titleWindow)

    def on_rect(self, rect):
        self.tracker.add_target(self.imgRGB, rect)

    def OpenCVCode(self, imgRGB, depth32f, frameCount):
        playing = not self.paused and not self.rect_sel.dragging
        if playing:
            self.imgRGB = imgRGB.copy()

        vis = self.imgRGB.copy()
        if playing:
            tracked = self.tracker.track(self.imgRGB)
            for tr in tracked:
                cv.polylines(vis, [np.int32(tr.quad)], True, (255, 255, 255), 2)
                for (x, y) in np.int32(tr.p1):
                    cv.circle(vis, (x, y), 2, (255, 255, 255))
                self.draw_overlay(vis, tr)

        self.rect_sel.draw(vis)
        cv.imshow(titleWindow, vis)
        ch = cv.waitKey(1)
        if ch == ord(' '):
            self.paused = not self.paused
        if ch == ord('c'):
            self.tracker.clear()
        return vis, None

    def draw_overlay(self, vis, tracked):
        x0, y0, x1, y1 = tracked.target.rect
        quad_3d = np.float32([[x0, y0, 0], [x1, y0, 0], [x1, y1, 0], [x0, y1, 0]])
        fx = 0.5 + cv.getTrackbarPos('focal', titleWindow) / 50.0
        h, w = vis.shape[:2]
        K = np.float64([[fx*w, 0, 0.5*(w-1)],
                        [0, fx*w, 0.5*(h-1)],
                        [0.0,0.0,      1.0]])
        dist_coef = np.zeros(4)
        _ret, rvec, tvec = cv.solvePnP(quad_3d, tracked.quad, K, dist_coef)
        verts = ar_verts * [(x1-x0), (y1-y0), -(x1-x0)*0.3] + (x0, y0, 0)
        verts = cv.projectPoints(verts, rvec, tvec, K, dist_coef)[0].reshape(-1, 2)
        for i, j in ar_edges:
            (x0, y0), (x1, y1) = verts[i], verts[j]
            cv.line(vis, (int(x0), int(y0)), (int(x1), int(y1)), (255, 255, 0), 2)


if __name__ == '__main__':
    print(__doc__)
    App().Open()
