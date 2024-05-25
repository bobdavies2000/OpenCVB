import cv2 as cv
import numpy as np
# https://docs.opencv.org/master/de/dbc/tutorial_py_fourier_transform.html
from matplotlib import pyplot as plt
from z_PyStream import PyStreamRun
import cv2 as cv
import io
titleWindow = 'z_MPL_FourierTransforms_PS.py'

# simple averaging filter without scaling parameter
mean_filter = np.ones((3,3))

# creating a gaussian filter
x = cv.getGaussianKernel(5,10)
gaussian = x*x.T
# different edge detecting filters
# scharr in x-direction
scharr = np.array([[-3, 0, 3],
                   [-10,0,10],
                   [-3, 0, 3]])
# sobel in x direction
sobel_x= np.array([[-1, 0, 1],
                   [-2, 0, 2],
                   [-1, 0, 1]])
# sobel in y direction
sobel_y= np.array([[-1,-2,-1],
                   [0, 0, 0],
                   [1, 2, 1]])
# Laplacian
Laplacian=np.array([[0, 1, 0],
                    [1,-4, 1],
                    [0, 1, 0]])
filters = [mean_filter, gaussian, Laplacian, sobel_x, sobel_y, scharr]
filter_name = ['mean_filter', 'gaussian','Laplacian', 'sobel_x', \
                'sobel_y', 'scharr_x']
fft_filters = [np.fft.fft2(x) for x in filters]
fft_shift = [np.fft.fftshift(y) for y in fft_filters]
mag_spectrum = [np.log(np.abs(z)+1) for z in fft_shift]
for i in range(6):
    plt.subplot(2,3,i+1),plt.imshow(mag_spectrum[i],cmap = 'gray')
    plt.title(filter_name[i]), plt.xticks([]), plt.yticks([])

buf = io.BytesIO()
plt.savefig(buf, format='rgba', dpi=100)

img_byte_arr = buf.getvalue()
rgbaSize = 480, 640, 4 
plotImage = np.array(np.frombuffer(img_byte_arr, np.uint8).reshape(rgbaSize)) 
buf.close()
plt.close()

def OpenCVCode(imgRGB, depth32f, frameCount):
    return plotImage, None

PyStreamRun(OpenCVCode, titleWindow)
