import cv2 as cv
import numpy as np
import argparse
titleWindow = 'Morphology_2.py'

morph_size = 0
max_operator = 4
max_elem = 2
max_kernel_size = 21
title_trackbar_operator_type = 'Operator:\n 0: Opening - 1: Closing  \n 2: Gradient - 3: Top Hat \n 4: Black Hat'
title_trackbar_element_type = 'Element:\n 0: Rect - 1: Cross - 2: Ellipse'
title_trackbar_kernel_size = 'Kernel size:\n 2n + 1'
morph_op_dic = {0: cv.MORPH_OPEN, 1: cv.MORPH_CLOSE, 2: cv.MORPH_GRADIENT, 3: cv.MORPH_TOPHAT, 4: cv.MORPH_BLACKHAT}

def morphology_operations(val):
    morph_operator = cv.getTrackbarPos(title_trackbar_operator_type, titleWindow)
    morph_size = cv.getTrackbarPos(title_trackbar_kernel_size, titleWindow)
    morph_elem = 0
    val_type = cv.getTrackbarPos(title_trackbar_element_type, titleWindow)
    if val_type == 0:
        morph_elem = cv.MORPH_RECT
    elif val_type == 1:
        morph_elem = cv.MORPH_CROSS
    elif val_type == 2:
        morph_elem = cv.MORPH_ELLIPSE

    element = cv.getStructuringElement(morph_elem, (2*morph_size + 1, 2*morph_size+1), (morph_size, morph_size))
    operation = morph_op_dic[morph_operator]
    dst2 = cv.morphologyEx(src, operation, element)
    cv.imshow(titleWindow, dst2)

parser = argparse.ArgumentParser(description='Code for More Morphology Transformations tutorial.')
parser.add_argument('--input', help='Path to input image.', default='../opencv/Samples/Data/LinuxLogo.jpg')
args = parser.parse_args()

src = cv.imread(cv.samples.findFile(args.input))
if src is None:
    print('Could not open or find the image: ', args.input)
    exit(0)

cv.namedWindow(titleWindow)
cv.createTrackbar(title_trackbar_operator_type, titleWindow , 0, max_operator, morphology_operations)
cv.createTrackbar(title_trackbar_element_type, titleWindow , 0, max_elem, morphology_operations)
cv.createTrackbar(title_trackbar_kernel_size, titleWindow , 0, max_kernel_size, morphology_operations)

morphology_operations(0)
cv.waitKey()
