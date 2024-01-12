'''
Coherence-enhancing filtering example
=====================================

inspired by
  Joachim Weickert "Coherence-Enhancing Shock Filters"
  http://www.mia.uni-saarland.de/Publications/weickert-dagm03.pdf
'''
import sys
titleWindow = 'z_Coherence.py'
import numpy as np
import cv2 as cv
def coherence_filter(img, sigma = 11, str_sigma = 11, blend = 0.5, iter_n = 4):
    height, width = img.shape[:2]

    for i in range(iter_n):
        print(i)

        gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
        eigen = cv.cornerEigenValsAndVecs(gray, str_sigma, 3)
        eigen = eigen.reshape(height, width, 3, 2)  # [[e1, e2], v1, v2]
        x, y = eigen[:,:,1,0], eigen[:,:,1,1]

        gxx = cv.Sobel(gray, cv.CV_32F, 2, 0, ksize=sigma)
        gxy = cv.Sobel(gray, cv.CV_32F, 1, 1, ksize=sigma)
        gyy = cv.Sobel(gray, cv.CV_32F, 0, 2, ksize=sigma)
        gvv = x*x*gxx + 2*x*y*gxy + y*y*gyy
        m = gvv < 0

        ero = cv.erode(img, None)
        dil = cv.dilate(img, None)
        img1 = ero
        img1[m] = dil[m]
        img = np.uint8(img*(1.0 - blend) + img1*blend)
    print('done')
    return img


def main():
    import sys
    src = cv.imread('../Data/baboon.jpg')

    def update():
        sigma = cv.getTrackbarPos('sigma', 'control')*2+1
        str_sigma = cv.getTrackbarPos('str_sigma', 'control')*2+1
        blend = cv.getTrackbarPos('blend', 'control') / 10.0
        print('sigma: %d  str_sigma: %d  blend_coef: %f' % (sigma, str_sigma, blend))
        dst2 = coherence_filter(src, sigma=sigma, str_sigma = str_sigma, blend = blend)
        cv.imshow('dst2', dst2)

    def controlUpdate(val): # placeholder for all the trackbars.
        pass

    cv.namedWindow('control', 0)
    cv.createTrackbar('sigma', 'control', 9, 15, controlUpdate)
    cv.createTrackbar('blend', 'control', 7, 10, controlUpdate)
    cv.createTrackbar('str_sigma', 'control', 9, 15, controlUpdate)

    print('Press SPACE to update the image\n')

    cv.imshow('src', src)
    update()
    while True:
        ch = cv.waitKey()
        if ch == ord(' '):
            update()

if __name__ == '__main__':
    print(__doc__)
    main()
    cv.destroyAllWindows()
