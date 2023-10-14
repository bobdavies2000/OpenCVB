from builtins import input
import cv2 as cv
import numpy as np
import argparse

parser = argparse.ArgumentParser(description='Code for Changing the contrast and brightness of an image! tutorial.')
parser.add_argument('--input', help='Path to input image.', default='../Data/lena.jpg')
args = parser.parse_args()

image = cv.imread(cv.samples.findFile(args.input))
if image is None:
    print('Could not open or find the image: ', args.input)
    exit(0)

new_image = np.zeros(image.shape, image.dtype)

alpha = 1.0 # Simple contrast control
beta = 0    # Simple brightness control

print(' Basic Linear Transforms ')
print('-------------------------')
alpha = 3.0 # float(input('* Enter the alpha value [1.0-3.0]: '))
beta = 50 # int(input('* Enter the beta value [0-100]: '))

new_image = cv.convertScaleAbs(image, new_image, alpha, beta)

both = np.empty((image.shape[0], image.shape[1]*2, image.shape[2]), image.dtype)
both = cv.hconcat([image, new_image])
cv.imshow('PhotoShop_Contrast_Example1.py: Alpha 3.0 and beta = 50', both)

cv.waitKey()
 
