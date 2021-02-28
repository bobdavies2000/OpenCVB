import numpy as np
import cv2 as cv
import os.path
from os import path
import sys
from PyStream import PyStreamRun
titleWindow = 'DNN_Inception_PS.py'
import ctypes
databaseMissing = False

# https://github.com/IntelRealSense/librealsense/blob/master/wrappers/tensorflow/example3%20-%20opencv%20deploy.py

def Mbox(titleWindow, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, titleWindow, style)

if path.exists("../Data/faster_rcnn_inception_v2_coco_2018_01_28/frozen_inference_graph.pb") == False:
    Mbox('DNN_Inception_PS.py', "Use the 'Download_Databases' algorithm to get this database: \n\n Select: 'TensorFlow Faster-RCNN Inception v2'", 1)
    databaseMissing = True
else:
    # download model from: https://github.com/opencv/opencv/wiki/TensorFlow-Object-Detection-API#run-network-in-opencv
    net = cv.dnn.readNetFromTensorflow("../Data/faster_rcnn_inception_v2_coco_2018_01_28/frozen_inference_graph.pb", 
                                   "../Data/faster_rcnn_inception_v2_coco_2018_01_28.pbtxt")
    
def OpenCVCode(imgRGB, depth32f, frameCount):
    global databaseMissing
    H, W, depth = imgRGB.shape
    
    if databaseMissing == False:
        scaled_size = (int(W), int(H))
        net.setInput(cv.dnn.blobFromImage(imgRGB, size=scaled_size, swapRB=True, crop=False))
        detections = net.forward()

        for detection in detections[0,0]:
            score = float(detection[2])
            idx = int(detection[1])

            if score > 0.8 and idx == 0:
                left = detection[3] * W
                top = detection[4] * H
                right = detection[5] * W
                bottom = detection[6] * H
                width = right - left
                height = bottom - top

                bbox = (int(left), int(top), int(width), int(height))

                p1 = (int(bbox[0]), int(bbox[1]))
                p2 = (int(bbox[0] + bbox[2]), int(bbox[1] + bbox[3]))
                cv.rectangle(imgRGB, p1, p2, (255, 0, 0), 2, 1)

                font = cv.FONT_HERSHEY_SIMPLEX
                bottomLeftCornerOfText = (p1[0], p1[1]+20)
                fontScale = 1
                fontColor = (255, 255, 255)
                lineType = 2
                cv.putText(imgRGB, "Person", bottomLeftCornerOfText, font, fontScale, fontColor, lineType)
                break
    return imgRGB, None

PyStreamRun(OpenCVCode, titleWindow)