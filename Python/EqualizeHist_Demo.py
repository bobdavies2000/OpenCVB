import cv2 as cv
import argparse
import numpy as np
titleWindow = 'EqualizedHist_Demo.py'

parser = argparse.ArgumentParser(description='Code for Histogram Equalization tutorial.')
parser.add_argument('--input', help='Path to input image.', default='../opencv/Samples/Data/lena.jpg')
args = parser.parse_args()

src = cv.imread(cv.samples.findFile(args.input))
if src is None:
    print('Could not open or find the image:', args.input)
    exit(0)

src = cv.cvtColor(src, cv.COLOR_BGR2GRAY)

dst2 = cv.equalizeHist(src)

both = np.empty((src.shape[0], src.shape[1]*2, 1), src.dtype)
both = cv.hconcat([src, dst2])
cv.imshow("Original (left) and Equalized Image (right)", both)

cv.waitKey()
