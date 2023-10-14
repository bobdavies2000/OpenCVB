import sys
import cv2 as cv
import numpy as np
# https://opencv-python-tutroals.readthedocs.io/en/latest/py_tutorials/py_imgproc/py_template_matching/py_template_matching.html
use_mask = False
img = None
templ = None
mask = None
result_window = 'Source Image (left) and intermediate data (right)'
match_method = 4
max_Trackbar = 5

def main(argv):
    global img
    global templ
    img = cv.imread("../Data/Messi5.jpg", cv.IMREAD_COLOR)
    templ = cv.imread("../Data/Messi1.jpg", cv.IMREAD_COLOR)

    if (len(sys.argv) > 3):
        global use_mask
        use_mask = True
        global mask
        mask = cv.imread( sys.argv[3], cv.IMREAD_COLOR )

    if ((img is None) or (templ is None) or (use_mask and (mask is None))):
        print('Can\'t read one of the images')
        return -1

    cv.namedWindow( result_window, cv.WINDOW_AUTOSIZE )

    trackbar_label = 'Method: \n 0: SQDIFF \n 1: SQDIFF NORMED \n 2: TM CCORR \n 3: TM CCORR NORMED \n 4: TM COEFF \n 5: TM COEFF NORMED'
    cv.createTrackbar( trackbar_label, result_window, match_method, max_Trackbar, MatchingMethod )

    MatchingMethod(match_method)

    cv.waitKey(0)
    return 0

def MatchingMethod(param):

    global match_method
    match_method = param

    img_display = img.copy()
    method_accepts_mask = (cv.TM_SQDIFF == match_method or match_method == cv.TM_CCORR_NORMED)
    if (use_mask and method_accepts_mask):
        result = cv.matchTemplate(img, templ, match_method, None, mask)
    else:
        result = cv.matchTemplate(img, templ, match_method)

    cv.normalize( result, result, 0, 1, cv.NORM_MINMAX, -1 )
    _minVal, _maxVal, minLoc, maxLoc = cv.minMaxLoc(result, None)

    if (match_method == cv.TM_SQDIFF or match_method == cv.TM_SQDIFF_NORMED):
        matchLoc = minLoc
    else:
        matchLoc = maxLoc

    cv.rectangle(img_display, matchLoc, (matchLoc[0] + templ.shape[0], matchLoc[1] + templ.shape[1]), (0,0,0), 2, 8, 0 )
    cv.rectangle(result, matchLoc, (matchLoc[0] + templ.shape[0], matchLoc[1] + templ.shape[1]), (0,0,0), 2, 8, 0 )
    result = np.uint8(result * 255)
    result = cv.resize(result, (img_display.shape[1], img_display.shape[0]))
    result = cv.cvtColor(result, cv.COLOR_GRAY2BGR)
    both = np.empty((img.shape[0], img.shape[1]*2, 3), np.uint8)
    both = cv.hconcat([img_display, result])
    cv.imshow(result_window, both)
    pass

if __name__ == "__main__":
    main(sys.argv[1:])
