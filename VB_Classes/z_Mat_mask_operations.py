import sys
import time

import numpy as np
import cv2 as cv
titleWindow = 'z_Mat_mask_operations.py'
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

## [basic_method]
def is_grayscale(my_image):
    return len(my_image.shape) < 3


def saturated(sum_value):
    if sum_value > 255:
        sum_value = 255
    if sum_value < 0:
        sum_value = 0

    return sum_value


def sharpen(my_image):
    if is_grayscale(my_image):
        height, width = my_image.shape
    else:
        my_image = cv.cvtColor(my_image, cv.CV_8U)
        height, width, n_channels = my_image.shape

    result = np.zeros(my_image.shape, my_image.dtype)
    ## [basic_method_loop]
    for j in range(1, height - 1):
        for i in range(1, width - 1):
            if is_grayscale(my_image):
                sum_value = 5 * my_image[j, i] - my_image[j + 1, i] - my_image[j - 1, i] \
                            - my_image[j, i + 1] - my_image[j, i - 1]
                result[j, i] = saturated(sum_value)
            else:
                for k in range(0, n_channels):
                    sum_value = 5 * my_image[j, i, k] - my_image[j + 1, i, k]  \
                                - my_image[j - 1, i, k] - my_image[j, i + 1, k]\
                                - my_image[j, i - 1, k]
                    result[j, i, k] = saturated(sum_value)
    ## [basic_method_loop]
    return result
## [basic_method]

def main(argv):
    filename = '../Data/lena.jpg'

    img_codec = cv.IMREAD_COLOR
    if argv:
        filename = sys.argv[1]
        if len(argv) >= 2 and sys.argv[2] == "G":
            img_codec = cv.IMREAD_GRAYSCALE

    src = cv.imread(cv.samples.findFile(filename), img_codec)

    if src is None:
        print("Can't open image [" + filename + "]")
        print("Usage:")
        print("mat_mask_operations.py [image_path -- default lena.jpg] [G -- grayscale]")
        return -1

    t = time.time()
    ## [kern]
    kernel = np.array([[0, -1, 0],
                       [-1, 5, -1],
                       [0, -1, 0]], np.float32)  # kernel should be floating point type
    
    dstx = cv.filter2D(src, -1, kernel)
    t = (time.time() - t) 
  
    both = np.empty((src.shape[0], src.shape[1]*2, src.shape[2]), src.dtype)
    both = cv.hconcat([src, dstx])
    cv.imshow("Original and sharpened version", both)

    str = "Filter2D function time in seconds: {0} \r(Now hit enter to see how slow manual method is) ".format(t) 
    Mbox('Mat_mask_operations.py', str, 1)

    t = round(time.time())
    dst0 = sharpen(src)
    t = (time.time() - t)
    
    dst2 = np.empty((src.shape[0], src.shape[1], src.shape[2]), src.dtype)
    cv.cvtColor(src, cv.COLOR_BGRA2BGR, dst2)
    both = np.empty((src.shape[0], src.shape[1]*2, src.shape[2]), src.dtype)
    both = cv.hconcat([src, dst])
    cv.imshow("Original and sharpened version", both)

    str = "Hand written function time in seconds: %s" % t
    Mbox('Mat_mask_operations.py', str, 1)

    cv.waitKey(0)
    cv.destroyAllWindows()
    return 0

if __name__ == "__main__":
    main(sys.argv[1:])
