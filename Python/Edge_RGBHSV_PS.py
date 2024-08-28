from skimage.color.adapt_rgb import adapt_rgb, each_channel, hsv_value
from skimage import filters
from skimage.exposure import rescale_intensity
import cv2 as cv
from PyStream import PyStreamRun
import numpy as np
titleWindow = 'Edge_RGBHSV_PS.py'

@adapt_rgb(each_channel)
def sobel_each(image):
    return filters.sobel(image)

@adapt_rgb(hsv_value)
def sobel_hsv(image):
    return filters.sobel(image)

# https://scikit-image.org/docs/dev/auto_examples/color_exposure/plot_adapt_rgb.html#sphx-glr-auto-examples-color-exposure-plot-adapt-rgb-py
def OpenCVCode(image, depth32f, frameCount):
    rgb64f = rescale_intensity(1 - sobel_each(image))
    hsv64f = rescale_intensity(1 - sobel_hsv(image))
    return np.asarray(255 * rgb64f, dtype=np.uint8),  np.asarray(255 * hsv64f, dtype=np.uint8) 
PyStreamRun(OpenCVCode, titleWindow)