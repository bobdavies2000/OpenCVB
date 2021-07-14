#!/usr/bin/env python3

import cv2
import depthai as dai
import numpy as np

# Start defining a pipeline
pipeline = dai.Pipeline()

# Define a source - two mono (grayscale) cameras
cam_left = pipeline.createMonoCamera()
cam_left.setBoardSocket(dai.CameraBoardSocket.LEFT)
cam_left.setResolution(dai.MonoCameraProperties.SensorResolution.THE_720_P)

cam_right = pipeline.createMonoCamera()
cam_right.setBoardSocket(dai.CameraBoardSocket.RIGHT)
cam_right.setResolution(dai.MonoCameraProperties.SensorResolution.THE_720_P)

# Create outputs
xout_left = pipeline.createXLinkOut()
xout_left.setStreamName('left')
cam_left.out.link(xout_left.input)

xout_right = pipeline.createXLinkOut()
xout_right.setStreamName('right')
cam_right.out.link(xout_right.input)

# Define a source - color camera
cam_rgb = pipeline.createColorCamera()
cam_rgb.setPreviewSize(1280, 720)
cam_rgb.setBoardSocket(dai.CameraBoardSocket.RGB)
cam_rgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
cam_rgb.setInterleaved(False)

# Create output
xout_rgb = pipeline.createXLinkOut()
xout_rgb.setStreamName("rgb")
cam_rgb.preview.link(xout_rgb.input)

# Create a node that will produce the depth map
depth = pipeline.createStereoDepth()
depth.setConfidenceThreshold(200)
cam_left.out.link(depth.left)
cam_right.out.link(depth.right)

# Pipeline defined, now the device is assigned and pipeline is started
device = dai.Device(pipeline)
device.startPipeline()

# Output queues will be used to get the grayscale frames from the outputs defined above
q_left = device.getOutputQueue(name="left", maxSize=4, blocking=False)
q_right = device.getOutputQueue(name="right", maxSize=4, blocking=False)
q_rgb = device.getOutputQueue(name="rgb", maxSize=4, blocking=True)
q = device.getOutputQueue(name="disparity", maxSize=4, blocking=False)

frame_left = None
frame_right = None
frame = None

while True:
    in_rgb = q_rgb.get()  # blocking call, will wait until a new data has arrived
    #in_depth = q_rgb.get()
    ## data is originally represented as a flat 1D array, it needs to be converted into HxW form
    #frame = in_depth.getData().reshape((in_depth.getHeight(), in_depth.getWidth())).astype(np.uint8)
    #frame = np.ascontiguousarray(frame)

    # instead of get (blocking) used tryGet (nonblocking) which will return the available data or None otherwise
    in_left = q_left.tryGet()
    in_right = q_right.tryGet()

    if in_left is not None:
        # if the data from the left camera is available, transform the 1D data into a frame
        frame_left = in_left.getData().reshape((in_left.getHeight(), in_left.getWidth())).astype(np.uint8)
        frame_left = np.ascontiguousarray(frame_left)

    if in_right is not None:
        # if the data from the right camera is available, transform the 1D data into a frame
        frame_right = in_right.getData().reshape((in_right.getHeight(), in_right.getWidth())).astype(np.uint8)
        frame_right = np.ascontiguousarray(frame_right)

    # data is originally represented as a flat 1D array, it needs to be converted into HxWxC form
    shape = (3, in_rgb.getHeight(), in_rgb.getWidth())
    frame_rgb = in_rgb.getData().reshape(shape).transpose(1, 2, 0).astype(np.uint8)
    frame_rgb = np.ascontiguousarray(frame_rgb)
    # frame is transformed and ready to be shown
    cv2.imshow("rgb", frame_rgb)
    
    if frame is not None:
        cv2.imshow("disparity", frame)

    # show the frames if available
    if frame_left is not None:
        cv2.imshow("left", frame_left)
    if frame_right is not None:
        cv2.imshow("right", frame_right)


    if cv2.waitKey(1) == ord('q'):
        break
