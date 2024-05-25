''' An example of Laplacian Pyramid construction and merging.
References:
  http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.54.299
Alexander Mordvintsev 6/10/12
'''
import sys
import numpy as np
import cv2 as cv
from z_PyStream import PyStreamRun
titleWindow = 'z_Laplacian_PyramidFilter_PS.py'

from common import nothing, getsize

def build_lappyr(img, leveln=6, dtype=np.int16):
    img = dtype(img)
    levels = []
    for _i in range(leveln-1):
        next_img = cv.pyrDown(img)
        img1 = cv.pyrUp(next_img, dstsize=getsize(img))
        levels.append(img-img1)
        img = next_img
    levels.append(img)
    return levels

def merge_lappyr(levels):
    img = levels[-1]
    for lev_img in levels[-2::-1]:
        img = cv.pyrUp(img, dstsize=getsize(lev_img))
        img += lev_img
    return np.uint8(np.clip(img, 0, 255))


def OpenCVCode(imgRGB, depth32f, frameCount):
    global leveln
    pyr = build_lappyr(imgRGB, leveln)
    for i in range(leveln):
        switcher = { 0:"sharpest", 1:"blurryMin", 2:"blurryMed1", 3:"blurryMed2", 4:"blurryMax", 5:"Saturate"}
        v = int(cv.getTrackbarPos(switcher.get(i, "invalid"), 'level control') / 5)
        pyr[i] *= v
    res = merge_lappyr(pyr)

    cv.imshow('Laplacian pyramid filter', res)
    return res, None

if __name__ == '__main__':
    print(__doc__)
    leveln = 6
    cv.namedWindow('level control')
    for i in range(leveln):
        switcher = { 0:"sharpest", 1:"blurryMin", 2:"blurryMed1", 3:"blurryMed2", 4:"blurryMax", 5:"Saturate"}
        cv.createTrackbar(switcher.get(i, "invalid"), 'level control', 5, 50, nothing)


PyStreamRun(OpenCVCode, titleWindow)
