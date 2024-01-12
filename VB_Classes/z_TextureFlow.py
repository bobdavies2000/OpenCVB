'''
Texture flow direction estimation.

Sample shows how cv.cornerEigenValsAndVecs function can be used
to estimate image texture flow direction.

Usage:
    texture_flow.py [<image>]
'''
import numpy as np
import cv2 as cv
titleWindow = 'z_TextureFlow.py'

def main():
    import sys
    try:
        fn = sys.argv[1]
    except:
        fn = '../Data/starry_night.jpg'

    img = cv.imread(cv.samples.findFile(fn))
    if img is None:
        print('Failed to load image file:', fn)
        sys.exit(1)

    gray = cv.cvtColor(img, cv.COLOR_BGR2GRAY)
    height, width = img.shape[:2]

    eigen = cv.cornerEigenValsAndVecs(gray, 15, 3)
    eigen = eigen.reshape(height, width, 3, 2)  # [[e1, e2], v1, v2]
    flow = eigen[:,:,2]

    vis = img.copy()
    vis[:] = (192 + np.uint32(vis)) / 2
    d = 12
    points =  np.dstack( np.mgrid[d/2:width:d, d/2:height:d] ).reshape(-1, 2)
    for x, y in np.int32(points):
        vx, vy = np.int32(flow[y, x]*d)
        cv.line(vis, (x-vx, y-vy), (x+vx, y+vy), (0, 0, 0), 1, cv.LINE_AA)

    size = img.shape[0], img.shape[1], 3
    both = np.empty((img.shape[0], img.shape[1]*2, img.shape[2]), img.dtype)
    both = cv.hconcat([img, vis])
    cv.imshow("Original and texture flow results", both)
    cv.waitKey()

    print('Done')


if __name__ == '__main__':
    print(__doc__)
    main()
    cv.destroyAllWindows()
