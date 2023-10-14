#!/usr/bin/env python

# https://github.com/opencv/opencv/blob/master/samples/python/digits_video.py

import numpy as np
import cv2 as cv
from z_PyStream import PyStreamRun

# built-in modules
import os
import sys
titleWindow = 'z_SVM_Digits_PS.py'

# local modules
from common import mosaic
from digits import *

def OpenCVCode(imgRGB, depth32f, frameCount):
    gray = cv.cvtColor(imgRGB, cv.COLOR_BGR2GRAY)

    bin = cv.adaptiveThreshold(gray, 255, cv.ADAPTIVE_THRESH_MEAN_C, cv.THRESH_BINARY_INV, 31, 10)
    bin = cv.medianBlur(bin, 3)
    contours, heirs = cv.findContours( bin.copy(), cv.RETR_CCOMP, cv.CHAIN_APPROX_SIMPLE)
    try:
        heirs = heirs[0]
    except:
        heirs = []

    for cnt, heir in zip(contours, heirs):
        _, _, _, outer_i = heir
        if outer_i >= 0:
            continue
        x, y, w, h = cv.boundingRect(cnt)
        if not (16 <= h <= 64  and w <= 1.2*h):
            continue
        pad = max(h-w, 0)
        x, w = x - (pad // 2), w + pad
        cv.rectangle(imgRGB, (x, y), (x+w, y+h), (0, 255, 0))

        bin_roi = bin[y:,x:][:h,:w]

        m = bin_roi != 0
        if not 0.1 < m.mean() < 0.4:
            continue
        '''
        gray_roi = gray[y:,x:][:h,:w]
        v_in, v_out = gray_roi[m], gray_roi[~m]
        if v_out.std() > 10.0:
            continue
        s = "%f, %f" % (abs(v_in.mean() - v_out.mean()), v_out.std())
        cv.putText(imgRGB, s, (x, y), cv.FONT_HERSHEY_PLAIN, 1.0, (200, 0, 0), thickness = 1)
        '''

        s = 1.5*float(h)/SZ
        m = cv.moments(bin_roi)
        c1 = np.float32([m['m10'], m['m01']]) / m['m00']
        c0 = np.float32([SZ/2, SZ/2])
        t = c1 - s*c0
        A = np.zeros((2, 3), np.float32)
        A[:,:2] = np.eye(2)*s
        A[:,2] = t
        bin_norm = cv.warpAffine(bin_roi, A, (SZ, SZ), flags=cv.WARP_INVERSE_MAP | cv.INTER_LINEAR)
        bin_norm = deskew(bin_norm)
        if x+w+SZ < imgRGB.shape[1] and y+SZ < imgRGB.shape[0]:
            imgRGB[y:,x+w:][:SZ, :SZ] = bin_norm[...,np.newaxis]

        sample = preprocess_hog([bin_norm])
        digit = model.predict(sample)[1].ravel()
        cv.putText(imgRGB, '%d'%digit, (x, y), cv.FONT_HERSHEY_PLAIN, 1.0, (200, 0, 0), thickness = 1)

    h, w = imgRGB.shape[:2]
    rgb = cv.resize(imgRGB, (int(w / 2), int(h / 2)))
    bin = cv.resize(bin, (int(w / 2), int(h / 2)))
    binRGB = np.empty(rgb.shape, rgb.dtype)
    cv.cvtColor(bin, cv.COLOR_GRAY2BGR, binRGB)
    return rgb, binRGB


if __name__ == '__main__':
    classifier_fn = '../Data/digits_svm.dat'
    if not os.path.exists(classifier_fn):
        print('"%s" not found, run digits.py first' % classifier_fn)
        exit

    if True:
        model = cv.ml.SVM_load(classifier_fn)
    else:
        model = cv.ml.SVM_create()
        model.load_(classifier_fn) #Known bug: https://github.com/opencv/opencv/issues/4969

PyStreamRun(OpenCVCode, titleWindow)
