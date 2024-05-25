'''
This example illustrates how to use Hough Transform to find lines

Usage:
    houghlines.py [<image_name>]
    image argument defaults to pic1.png
'''
import cv2 as cv
import numpy as np

import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

import sys
import math
titleWindow = 'Hough_example5.py'

def main():
    fn = '../opencv/Samples/Data/pic1.png'
    src = cv.imread(cv.samples.findFile(fn))
    dst2 = cv.Canny(src, 50, 200)
    cdst1 = cv.cvtColor(dst2, cv.COLOR_GRAY2BGR)

    if True: # HoughLinesP
        lines = cv.HoughLinesP(dst2, 1, math.pi/180.0, 40, np.array([]), 50, 10)
        a,b,c = lines.shape
        for i in range(a):
            cv.line(cdst1, (lines[i][0][0], lines[i][0][1]), (lines[i][0][2], lines[i][0][3]), (0, 0, 255), 3, cv.LINE_AA)

    else:    # HoughLines
        lines = cv.HoughLines(dst2, 1, math.pi/180.0, 50, np.array([]), 0, 0)
        if lines is not None:
            a,b,c = lines.shape
            for i in range(a):
                rho = lines[i][0][0]
                theta = lines[i][0][1]
                a = math.cos(theta)
                b = math.sin(theta)
                x0, y0 = a*rho, b*rho
                pt1 = ( int(x0+1000*(-b)), int(y0+1000*(a)) )
                pt2 = ( int(x0-1000*(-b)), int(y0-1000*(a)) )
                cv.line(cdst1, pt1, pt2, (0, 0, 255), 3, cv.LINE_AA)

    cv.imshow("detected lines", cdst1)

    cv.imshow("source", src)
    cv.waitKey(0)
    print('Done')


if __name__ == '__main__':
    main()
