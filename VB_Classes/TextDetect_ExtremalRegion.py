#!/usr/bin/python
# https://docs.opencv.org/3.4/da/d56/group__text__detect.html
import sys
import os

import cv2 as cv
import numpy as np
titleWindow = 'TextDetect_ExtremalRegion.py'

print(os.path.basename(__file__))
print('       A simple demo script using the Extremal Region Filter algorithm described in:')
print('       Neumann L., Matas J.: Real-Time Scene Text Localization and Recognition, CVPR 2012\n')


#if (len(sys.argv) < 2):
#  print(' (ERROR) You must call this script with an argument (path_to_image_to_be_processed)\n')
#  quit()

img  = cv.imread(str('../Data/Opencv-logo.png'))
gray = cv.imread(str('../Data/Opencv-logo.png'),0)

erc1 = cv.text.loadClassifierNM1('../Data/trained_classifierNM1.xml')
er1 = cv.text.createERFilterNM1(erc1)

erc2 = cv.text.loadClassifierNM2('../Data/trained_classifierNM2.xml')
er2 = cv.text.createERFilterNM2(erc2)

regions = cv.text.detectRegions(gray,er1,er2)

#Visualization
rects = [cv.boundingRect(p.reshape(-1, 1, 2)) for p in regions]
for rect in rects:
  cv.rectangle(img, rect[0:2], (rect[0]+rect[2],rect[1]+rect[3]), (0, 0, 0), 2)
for rect in rects:
  cv.rectangle(img, rect[0:2], (rect[0]+rect[2],rect[1]+rect[3]), (255, 255, 255), 1)
cv.imshow("Text detection result", img)
cv.waitKey(0)
