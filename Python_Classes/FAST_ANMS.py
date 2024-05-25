#!/usr/bin/env python

import argparse

import cv2  # pylint: disable=import-error
# https://github.com/BAILOOL/ANMS-Codes/blob/master/Python/demo.py
# https://github.com/BAILOOL/ANMS-Codes/blob/master/Python/ssc.py

import math


def ssc(keypoints, num_ret_points, tolerance, cols, rows):
    exp1 = rows + cols + 2 * num_ret_points
    exp2 = (
        4 * cols
        + 4 * num_ret_points
        + 4 * rows * num_ret_points
        + rows * rows
        + cols * cols
        - 2 * rows * cols
        + 4 * rows * cols * num_ret_points
    )
    exp3 = math.sqrt(exp2)
    exp4 = num_ret_points - 1

    sol1 = -round(float(exp1 + exp3) / exp4)  # first solution
    sol2 = -round(float(exp1 - exp3) / exp4)  # second solution

    high = (
        sol1 if (sol1 > sol2) else sol2
    )  # binary search range initialization with positive solution
    low = math.floor(math.sqrt(len(keypoints) / num_ret_points))

    prev_width = -1
    selected_keypoints = []
    result_list = []
    result = []
    complete = False
    k = num_ret_points
    k_min = round(k - (k * tolerance))
    k_max = round(k + (k * tolerance))

    while not complete:
        width = low + (high - low) / 2
        if (
            width == prev_width or low > high
        ):  # needed to reassure the same radius is not repeated again
            result_list = result  # return the keypoints from the previous iteration
            break

        c = width / 2  # initializing Grid
        num_cell_cols = int(math.floor(cols / c))
        num_cell_rows = int(math.floor(rows / c))
        covered_vec = [
            [False for _ in range(num_cell_cols + 1)] for _ in range(num_cell_rows + 1)
        ]
        result = []

        for i in range(len(keypoints)):
            row = int(
                math.floor(keypoints[i].pt[1] / c)
            )  # get position of the cell current point is located at
            col = int(math.floor(keypoints[i].pt[0] / c))
            if not covered_vec[row][col]:  # if the cell is not covered
                result.append(i)
                # get range which current radius is covering
                row_min = int(
                    (row - math.floor(width / c))
                    if ((row - math.floor(width / c)) >= 0)
                    else 0
                )
                row_max = int(
                    (row + math.floor(width / c))
                    if ((row + math.floor(width / c)) <= num_cell_rows)
                    else num_cell_rows
                )
                col_min = int(
                    (col - math.floor(width / c))
                    if ((col - math.floor(width / c)) >= 0)
                    else 0
                )
                col_max = int(
                    (col + math.floor(width / c))
                    if ((col + math.floor(width / c)) <= num_cell_cols)
                    else num_cell_cols
                )
                for row_to_cover in range(row_min, row_max + 1):
                    for col_to_cover in range(col_min, col_max + 1):
                        if not covered_vec[row_to_cover][col_to_cover]:
                            # cover cells within the square bounding box with width w
                            covered_vec[row_to_cover][col_to_cover] = True

        if k_min <= len(result) <= k_max:  # solution found
            result_list = result
            complete = True
        elif len(result) < k_min:
            high = width - 1  # update binary search range
        else:
            low = width + 1
        prev_width = width

    for i in range(len(result_list)):
        selected_keypoints.append(keypoints[result_list[i]])

    return selected_keypoints


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--image_path", type=str, default="../Data/TopM.png")
    parser.add_argument("--num_ret_points", type=int, default=10)
    parser.add_argument("--tolerance", type=float, default=0.1)
    args = parser.parse_args()

    img = cv2.imread(args.image_path)
    cv2.imshow("Input Image", img)
    cv2.waitKey(0)

    fast = cv2.FastFeatureDetector_create()
    keypoints = fast.detect(img, None)
    img2 = cv2.drawKeypoints(img, keypoints, outImage=None, color=(255, 0, 0))
    cv2.imshow("Detected FAST keypoints", img2)
    cv2.waitKey(0)

    # keypoints should be sorted by strength in descending order
    # before feeding to SSC to work correctly
    keypoints = sorted(keypoints, key=lambda x: x.response, reverse=True)

    selected_keypoints = ssc(
        keypoints, args.num_ret_points, args.tolerance, img.shape[1], img.shape[0]
    )

    img3 = cv2.drawKeypoints(img, selected_keypoints, outImage=None, color=(255, 0, 0))
    cv2.imshow("Selected keypoints", img3)
    cv2.waitKey(0)


if __name__ == "__main__":
    main()