import cv2 as cv
import numpy as np
import argparse
titleWindow = 'z_Remap_Demo.py'

def update_map(ind, map_x, map_y):
    if ind == 0:
        for i in range(map_x.shape[0]):
            for j in range(map_x.shape[1]):
                if j > map_x.shape[1]*0.25 and j < map_x.shape[1]*0.75 and i > map_x.shape[0]*0.25 and i < map_x.shape[0]*0.75:
                    map_x[i,j] = 2 * (j-map_x.shape[1]*0.25) + 0.5
                    map_y[i,j] = 2 * (i-map_y.shape[0]*0.25) + 0.5
                else:
                    map_x[i,j] = 0
                    map_y[i,j] = 0
    elif ind == 1:
        for i in range(map_x.shape[0]):
            map_x[i,:] = [x for x in range(map_x.shape[1])]
        for j in range(map_y.shape[1]):
            map_y[:,j] = [map_y.shape[0]-y for y in range(map_y.shape[0])]
    elif ind == 2:
        for i in range(map_x.shape[0]):
            map_x[i,:] = [map_x.shape[1]-x for x in range(map_x.shape[1])]
        for j in range(map_y.shape[1]):
            map_y[:,j] = [y for y in range(map_y.shape[0])]
    elif ind == 3:
        for i in range(map_x.shape[0]):
            map_x[i,:] = [map_x.shape[1]-x for x in range(map_x.shape[1])]
        for j in range(map_y.shape[1]):
            map_y[:,j] = [map_y.shape[0]-y for y in range(map_y.shape[0])]

parser = argparse.ArgumentParser(description='Code for Remapping tutorial.')
parser.add_argument('--input', help='Path to input image.', default='../Data/chicky_512.png')
args = parser.parse_args()

src = cv.imread("../Data/starry_night.jpg")

map_x = np.zeros((src.shape[0], src.shape[1]), dtype=np.float32)
map_y = np.zeros((src.shape[0], src.shape[1]), dtype=np.float32)

cv.namedWindow(titleWindow)

ind = 0
while True:
    update_map(ind, map_x, map_y)
    ind = (ind + 1) % 4
    dst2 = cv.remap(src, map_x, map_y, cv.INTER_LINEAR)
    cv.imshow(titleWindow, dst2)
    c = cv.waitKey(1000)
    if c == 27:
        break
