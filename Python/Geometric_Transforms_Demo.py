import cv2 as cv
import numpy as np
import argparse
titleWindow = 'Geometric_Transforms_Demo.py'

## [Load the image]
parser = argparse.ArgumentParser(description='Code for Affine Transformations tutorial.')
parser.add_argument('--input', help='Path to input image.', default='../opencv/Samples/Data/lena.jpg')
args = parser.parse_args()

src = cv.imread(cv.samples.findFile(args.input))
if src is None:
    print('Could not open or find the image:', args.input)
    exit(0)
## [Load the image]

## [Set your 3 points to calculate the  Affine Transform]
srcTri = np.array( [[0, 0], [src.shape[1] - 1, 0], [0, src.shape[0] - 1]] ).astype(np.float32)
dstTri = np.array( [[0, src.shape[1]*0.33], [src.shape[1]*0.85, src.shape[0]*0.25], [src.shape[1]*0.15, src.shape[0]*0.7]] ).astype(np.float32)
## [Set your 3 points to calculate the  Affine Transform]

## [Get the Affine Transform]
warp_mat = cv.getAffineTransform(srcTri, dstTri)
## [Get the Affine Transform]

## [Apply the Affine Transform just found to the src image]
warp_dst1 = cv.warpAffine(src, warp_mat, (src.shape[1], src.shape[0]))
## [Apply the Affine Transform just found to the src image]

# Rotating the image after Warp

## [Compute a rotation matrix with respect to the center of the image]
center = (warp_dst1.shape[1]//2, warp_dst1.shape[0]//2)
angle = -50
scale = 0.6
## [Compute a rotation matrix with respect to the center of the image]

## [Get the rotation matrix with the specifications above]
rot_mat = cv.getRotationMatrix2D( center, angle, scale )
## [Get the rotation matrix with the specifications above]

## [Rotate the warped image]
warp_rotate_dst1 = cv.warpAffine(warp_dst1, rot_mat, (warp_dst1.shape[1], warp_dst1.shape[0]))
## [Rotate the warped image]

## [Show what you got]
cv.imshow('Source image', src)
cv.imshow('Warp', warp_dst1)
cv.imshow('Warp + Rotate', warp_rotate_dst1)
## [Show what you got]

## [Wait until user exits the program]
cv.waitKey()
## [Wait until user exits the program]
