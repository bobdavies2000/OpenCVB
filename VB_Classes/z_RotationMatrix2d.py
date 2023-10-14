# https://www.programcreek.com/python/example/89459/cv2.getRotationMatrix2D
import sys
titleWindow = 'z_GetRotationMatrix2D.py'
import numpy as np
import cv2 as cv
import random 

class App(object):
    def crop_minAreaRect(self, img, rect):

        # rotate img
        angle = 25
        rows, cols = img.shape[0], img.shape[1]
        M = cv.getRotationMatrix2D((cols / 2, rows / 2), angle, 1)
        img_rot = cv.warpAffine(img, M, (cols, rows))

        # rotate bounding box
        rect0 = (rect[0], rect[1], 0.0)
        box = cv.boxPoints(rect0)
        pts = np.int0(cv.transform(np.array([box]), M))[0]
        pts[pts < 0] = 0

        # crop
        img_crop = img_rot[pts[1][1]:pts[0][1], pts[2][1]:pts[3][1]]
        return img_crop 

    def test1(self, src):
        #rect = cv.selectROI(src, False)
        rect = ((100, 100), (200, 200))
        result = self.crop_minAreaRect(src, rect)
        cv.imshow("src(result)", result)
        cv.imshow("src", src)

    def Open(self):
        src = cv.imread('../Data/baboon.jpg')
        self.test1(src)
        cv.waitKey()

App().Open()