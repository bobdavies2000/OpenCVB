# https://github.com/WillBrennan/BlurDetection2/tree/master
import numpy
import cv2
titleWindow = 'z_Blur_Detection.py'

def fix_image_size(image: numpy.array, expected_pixels: float = 2E6):
    ratio = numpy.sqrt(expected_pixels / (image.shape[0] * image.shape[1]))
    return cv2.resize(image, (0, 0), fx=ratio, fy=ratio)


def estimate_blur(image: numpy.array, threshold: int = 100):
    if image.ndim == 3:
        image = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    blur_map = cv2.Laplacian(image, cv2.CV_64F)
    score = numpy.var(blur_map)
    return blur_map, score, bool(score < threshold)


def pretty_blur_map(blur_map: numpy.array, sigma: int = 5, min_abs: float = 0.5):
    abs_image = numpy.abs(blur_map).astype(numpy.float32)
    abs_image[abs_image < min_abs] = min_abs

    abs_image = numpy.log(abs_image)
    cv2.blur(abs_image, (sigma, sigma))
    return cv2.medianBlur(abs_image, sigma)

def main():
    image = cv2.imread(str("../Data/blurDetection.png"))
    cv2.imshow('input', image)

    image = fix_image_size(image)

    blur_map, score, blurry = estimate_blur(image, 100)

    cv2.imshow('result - lighter is more in focus', pretty_blur_map(blur_map))

    if cv2.waitKey(100000) == ord('q'):
        exit()

if __name__ == '__main__':
    main()
    cv2.destroyAllWindows()