'''
Multitarget planar tracking
==================

Example of using features2d framework for interactive video homography matching.
ORB features and FLANN matcher are used. This sample provides PlaneTracker class
and an example of its usage.

video: http://www.youtube.com/watch?v=pzVbhxx6aog

Usage
-----
PlaneTracker_PS.py 

Keys:
   SPACE  -  pause video
   c      -  clear targets

Select a textured planar object to track by drawing a box with a mouse.
'''

#import sys
#import numpy as np
#import cv2 as cv
#from PyStream import PyStreamRun
#titleWindow = "TrackerPlane_PS.py"

## built-in modules
#from collections import namedtuple

## local modules
#import common

#FLANN_INDEX_KDTREE = 1
#FLANN_INDEX_LSH    = 6
#flann_params= dict(algorithm = FLANN_INDEX_LSH,
#                   table_number = 6, # 12
#                   key_size = 12,     # 20
#                   multi_probe_level = 1) #2

#MIN_MATCH_COUNT = 10

#'''
#  image     - image to track
#  rect      - tracked rectangle (x1, y1, x2, y2)
#  keypoints - keypoints detected inside rect
#  descrs    - their descriptors
#  data      - some user-provided data
#'''
#PlanarTarget = namedtuple('PlaneTarget', 'image, rect, keypoints, descrs, data')

#'''
#  target - reference to PlanarTarget
#  p0     - matched points coords in target image
#  p1     - matched points coords in input frame
#  H      - homography matrix from p0 to p1
#  quad   - target boundary quad in input frame
#'''
#TrackedTarget = namedtuple('TrackedTarget', 'target, p0, p1, H, quad')

#class PlaneTracker:
#    def __init__(self):
#        self.detector = cv.ORB_create( nfeatures = 100 )
#        self.matcher = cv.FlannBasedMatcher(flann_params, {})  # bug : need to pass empty dict (#1329)
#        self.targets = []
#        self.frame_points = []

#    def add_target(self, image, rect, data=None):
#        print("Flann has been failing here recently.  Not sure why.  Needs work here - look for add_target function.")
#        '''Add a new tracking target.'''
#        #x0, y0, x1, y1 = rect
#        #raw_points, raw_descrs = self.detect_features(image)
#        #points, descs = [], []
#        #for kp, desc in zip(raw_points, raw_descrs):
#        #    x, y = kp.pt
#        #    if x0 <= x <= x1 and y0 <= y <= y1:
#        #        points.append(kp)
#        #        descs.append(desc)
#        #descs = np.uint8(descs)
#        #self.matcher.add([descs])
#        #target = PlanarTarget(image = image, rect=rect, keypoints = points, descrs=descs, data=data)
#        #self.targets.append(target)

#    def clear(self):
#        '''Remove all targets'''
#        self.targets = []
#        self.matcher.clear()

#    def track(self, frame):
#        '''Returns a list of detected TrackedTarget objects'''
#        self.frame_points, frame_descrs = self.detect_features(frame)
#        if len(self.frame_points) < MIN_MATCH_COUNT:
#            return []
#        matches = self.matcher.knnMatch(frame_descrs, k = 2)
#        matches = [m[0] for m in matches if len(m) == 2 and m[0].distance < m[1].distance * 0.75]
#        if len(matches) < MIN_MATCH_COUNT:
#            return []
#        matches_by_id = [[] for _ in range(len(self.targets))]
#        for m in matches:
#            matches_by_id[m.imgIdx].append(m)
#        tracked = []
#        for imgIdx, matches in enumerate(matches_by_id):
#            if len(matches) < MIN_MATCH_COUNT:
#                continue
#            target = self.targets[imgIdx]
#            p0 = [target.keypoints[m.trainIdx].pt for m in matches]
#            p1 = [self.frame_points[m.queryIdx].pt for m in matches]
#            p0, p1 = np.float32((p0, p1))
#            H, status = cv.findHomography(p0, p1, cv.RANSAC, 3.0)
#            status = status.ravel() != 0
#            if status.sum() < MIN_MATCH_COUNT:
#                continue
#            p0, p1 = p0[status], p1[status]

#            x0, y0, x1, y1 = target.rect
#            quad = np.float32([[x0, y0], [x1, y0], [x1, y1], [x0, y1]])
#            quad = cv.perspectiveTransform(quad.reshape(1, -1, 2), H).reshape(-1, 2)

#            track = TrackedTarget(target=target, p0=p0, p1=p1, H=H, quad=quad)
#            tracked.append(track)
#        tracked.sort(key = lambda t: len(t.p0), reverse=True)
#        return tracked

#    def detect_features(self, frame):
#        '''detect_features(self, frame) -> keypoints, descrs'''
#        keypoints, descrs = self.detector.detectAndCompute(frame, None)
#        if descrs is None:  # detectAndCompute returns descs=None if not keypoints found
#            descrs = []
#        return keypoints, descrs


#class App:
#    def Open(self):
#        self.frame = None
#        self.paused = False
#        self.tracker = PlaneTracker()

#        cv.namedWindow(titleWindow)
#        self.rect_sel = common.RectSelector(titleWindow, self.on_rect)
#        PyStreamRun(self.OpenCVCode, titleWindow)

#    def on_rect(self, rect):
#        self.tracker.add_target(self.frame, rect)

#    def OpenCVCode(self, imgRGB, depth32f, frameCount):
#        playing = not self.paused and not self.rect_sel.dragging
#        self.frame = imgRGB.copy()

#        vis = self.frame.copy()
#        if playing:
#            tracked = self.tracker.track(self.frame)
#            for tr in tracked:
#                cv.polylines(vis, [np.int32(tr.quad)], True, (255, 255, 255), 2)
#                for (x, y) in np.int32(tr.p1):
#                    cv.circle(vis, (x, y), 2, (255, 255, 255))

#        self.rect_sel.draw(vis)
#        cv.imshow(titleWindow, vis)
#        ch = cv.waitKey(1)
#        if ch == ord(' '):
#            self.paused = not self.paused
#        if ch == ord('c'):
#            self.tracker.clear()
#        return vis, None

#if __name__ == '__main__':
#    print(__doc__)
#    App().Open()
