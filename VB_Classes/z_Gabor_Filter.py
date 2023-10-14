# https://gist.github.com/kendricktan/93f0da88d0b25087d751ed2244cf770c
import numpy as np
import cv2
titleWindow = 'z_Gabor_Filter.py'

# cv2.getGaborKernel(ksize, sigma, theta, lambda, gamma, psi, ktype)
# ksize - size of gabor filter (n, n)
# sigma - standard deviation of the gaussian function
# theta - orientation of the normal to the parallel stripes
# lambda - wavelength of the sunusoidal factor
# gamma - spatial aspect ratio
# psi - phase offset
# ktype - type and range of values that each pixel in the gabor kernel can hold

g_kernel = cv2.getGaborKernel((31, 31), 4.0, np.pi/4, 10.0, 0.5, 0, ktype=cv2.CV_32F)

img = cv2.imread('../Data/baboon.jpg')
img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
filtered_img = cv2.filter2D(img, cv2.CV_8UC3, g_kernel)

both = np.empty((img.shape[0], img.shape[1]*2, 3), img.dtype)
both = cv2.hconcat([img, filtered_img])
cv2.imshow("Original and filtered image", both)

h, w = g_kernel.shape[:2]
g_kernel = cv2.resize(g_kernel, (3*w, 3*h), interpolation=cv2.INTER_CUBIC)
cv2.imshow('gabor kernel (resized)', g_kernel)
cv2.waitKey(0)
cv2.destroyAllWindows()