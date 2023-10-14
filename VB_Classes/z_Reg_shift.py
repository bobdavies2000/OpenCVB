import cv2 as cv
import numpy as np
import sys
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

img1 = cv.imread("../Data/lena.jpg")
cv.imshow("img1", img1)
img1 = img1.astype(np.float32)
shift = np.array([5., 5.])
mapTest = cv.reg_MapShift(shift)

img2 = mapTest.warp(img1)

mapper = cv.reg_MapperGradShift()
mappPyr = cv.reg_MapperPyramid(mapper)

resMap = mappPyr.calculate(img1, img2)
mapShift = cv.reg.MapTypeCaster_toShift(resMap)

test = mapShift.getShift()

str = "The original image has been shifted 5 to the right and 5 down. \n"
str += "Then image registration was used to compute the shift. \n"
str += "\n\nResults:\t%.6f to the right and \t%.6f down.\n" % (test[0], test[1])
Mbox('reg_shift', str, 1)
