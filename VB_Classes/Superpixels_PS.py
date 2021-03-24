import numpy as np
import cv2 as cv
import common
import sys
from PyStream import PyStreamRun
titleWindow = "SuperPixels_PS.py"

def OpenCVCode(imgRGB, depth32f, frameCount):
    global seeds, display_mode, num_superpixels, prior, num_levels, num_histogram_bins, scalarRed
    converted_img = cv.cvtColor(imgRGB, cv.COLOR_BGR2HSV)
    height,width,channels = converted_img.shape
    num_SuperPixel_new = cv.getTrackbarPos('Number of Superpixels', titleWindow)
    num_iterations = cv.getTrackbarPos('Iterations', titleWindow)

    if frameCount == 0:
        scalarRed = np.zeros((height,width,3), np.uint8)
        scalarRed[:] = (0, 0, 255)

    if not seeds or num_SuperPixel_new != num_superpixels:
        num_superpixels = num_SuperPixel_new
        seeds = cv.ximgproc.createSuperpixelSEEDS(width, height, channels,
                num_superpixels, num_levels, prior, num_histogram_bins)

    seeds.iterate(converted_img, num_iterations)

    # retrieve the segmentation result
    labels = seeds.getLabels()

    # labels output: use the last x bits to determine the color
    num_label_bits = 2
    labels &= (1<<num_label_bits)-1
    labels *= 1<<(16-num_label_bits)

    mask = seeds.getLabelContourMask(False)

    # stitch foreground & background together
    mask_inv = cv.bitwise_not(mask) 
    result_bg = cv.bitwise_and(imgRGB, imgRGB, mask=mask_inv)
    result_fg = cv.bitwise_and(scalarRed, scalarRed, mask=mask)
    result = cv.add(result_bg, result_fg)

    if display_mode == 0:
        cv.imshow(titleWindow, result)
    elif display_mode == 1:
        cv.imshow(titleWindow, mask)
    else:
        cv.imshow(titleWindow, labels)

    ch = cv.waitKey(1)
    if ch & 0xff == ord(' '):
        display_mode = (display_mode + 1) % 2 # set this to 3 to get the labels working but it won't display...
    return result, None

if __name__ == '__main__':
    cv.namedWindow(titleWindow)
    cv.createTrackbar('Number of Superpixels', titleWindow, 400, 1000, common.nothing)
    cv.createTrackbar('Iterations', titleWindow, 4, 12, common.nothing)

    seeds = None
    display_mode = 0
    num_superpixels = 400
    prior = 2
    num_levels = 4
    num_histogram_bins = 5
    PyStreamRun(OpenCVCode, titleWindow)
