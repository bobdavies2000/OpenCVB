import cv2 as cv
import numpy as np
import argparse
import os
titleWindow = 'HDR_imaging.py'

def loadExposureSeq(path):
    images = []
    times = []
    with open(os.path.join(path, 'list.txt')) as f:
        content = f.readlines()
    for line in content:
        tokens = line.split()
        images.append(cv.imread(os.path.join(path, tokens[0])))
        times.append(1 / float(tokens[1]))

    return images, np.asarray(times, dtype=np.float32)

print("Working on the HDR image.  (takes a few seconds.)")
## [Load images and exposure times]
images, times = loadExposureSeq('../Data')
## [Load images and exposure times]

## [Estimate camera response]
calibrate = cv.createCalibrateDebevec()
response = calibrate.process(images, times)
## [Estimate camera response]

## [Make HDR image]
merge_debevec = cv.createMergeDebevec()
hdr = merge_debevec.process(images, times, response)
## [Make HDR image]

## [Tonemap HDR image]
tonemap = cv.createTonemap(2.2)
ldr = tonemap.process(hdr)
## [Tonemap HDR image]

## [Perform exposure fusion]
merge_mertens = cv.createMergeMertens()
fusion = merge_mertens.process(images)
## [Perform exposure fusion]

CombinedImages = cv.hconcat([fusion, hdr])
CombinedImagesAll = cv.hconcat([CombinedImages, ldr])
cv.imshow("Fusion hdr ldr", CombinedImagesAll)
cv.waitKey()

