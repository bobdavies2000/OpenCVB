import cv2
import depthai as dai
import numpy as np
from OakD_Stream import OakDStreamRun

titleWindow = 'OakD_Interface.py'
fps=30

pipeline = dai.Pipeline()
monoResolution = dai.MonoCameraProperties.SensorResolution.THE_400_P

left = pipeline.createMonoCamera()
left.setResolution(dai.MonoCameraProperties.SensorResolution.THE_480_P)
left.setBoardSocket(dai.CameraBoardSocket.LEFT)

right = pipeline.createMonoCamera()
right.setResolution(dai.MonoCameraProperties.SensorResolution.THE_480_P)
right.setBoardSocket(dai.CameraBoardSocket.RIGHT)

depth = pipeline.createStereoDepth()
depth.initialConfig.setConfidenceThreshold(245)
stereo = pipeline.create(dai.node.StereoDepth)

# Note: the rectified streams are horizontally mirrored by default
# depth.setOutputRectified(True)
depth.setRectifyEdgeFillColor(0) # Black, to better see the cutout
left.out.link(depth.left)
right.out.link(depth.right)

# Define a source - color camera
cam_rgb = pipeline.createColorCamera()
cam_rgb.setPreviewSize(1280, 720)
cam_rgb.setBoardSocket(dai.CameraBoardSocket.RGB)
cam_rgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
cam_rgb.setInterleaved(False)
cam_rgb.setIspScale(2, 3)
cam_rgb.setFps(fps)
# For now, RGB needs fixed focus to properly align with depth.
# This value was used during calibration
cam_rgb.initialControl.setManualFocus(130)

depthOut = pipeline.create(dai.node.XLinkOut)
depthOut.setStreamName("depth")
depth.disparity.link(depthOut.input)

#xout_depth = pipeline.createXLinkOut()
#xout_depth.setStreamName("depth")
#depth.disparity.link(xout_depth.input)

xout_left = pipeline.createXLinkOut()
xout_left.setStreamName("rect_left")
depth.rectifiedLeft.link(xout_left.input)

xout_right = pipeline.createXLinkOut()
xout_right.setStreamName('rect_right')
depth.rectifiedRight.link(xout_right.input)

# Create output
xout_rgb = pipeline.createXLinkOut()
xout_rgb.setStreamName("rgb")
cam_rgb.preview.link(xout_rgb.input)

device = dai.Device()
device.startPipeline(pipeline)

left.setResolution(monoResolution)
left.setBoardSocket(dai.CameraBoardSocket.LEFT)
left.setFps(fps)
right.setResolution(monoResolution)
right.setBoardSocket(dai.CameraBoardSocket.RIGHT)
right.setFps(fps)

stereo.initialConfig.setConfidenceThreshold(245)
# LR-check is required for depth alignment
stereo.setLeftRightCheck(True)
stereo.setDepthAlign(dai.CameraBoardSocket.RGB)

q_left = device.getOutputQueue(name="rect_left", maxSize=4, blocking=False)
q_right = device.getOutputQueue(name="rect_right", maxSize=4, blocking=False)
q_depth = device.getOutputQueue(name="depth", maxSize=4, blocking=False)
q_rgb = device.getOutputQueue(name="rgb", maxSize=4, blocking=True)

left.out.link(stereo.left)
right.out.link(stereo.right)
stereo.disparity.link(depthOut.input)

frame_left = None
frame_right = None
frame_depth8U = None

class App(object):
    def Open(self):
        print("Open OakD_Stream Interface for Oak-D camera")
        OakDStreamRun(self.OpenCVCode, titleWindow)

    def OpenCVCode(self, frameCount):
        latestPacket = {}
        latestPacket["rgb"] = None
        latestPacket["depth"] = None
        latestPacket["rect_left"] = None
        latestPacket["rect_right"] = None

        queueEvents = device.getQueueEvents(("rgb", "depth", "rect_left", "rect_right"))
        for queueName in queueEvents:
            packets = device.getOutputQueue(queueName).tryGetAll()
            if len(packets) > 0:
                latestPacket[queueName] = packets[-1]

        if latestPacket["rgb"] is not None:
            frameRgb = latestPacket["rgb"].getCvFrame()
            #cv2.imshow("rgb", frameRgb)

        if latestPacket["depth"] is not None:
            frameDepth = latestPacket["depth"].getFrame()
            maxDisp = depth.initialConfig.getMaxDisparity()
            frameDepth = (frameDepth * 255. / maxDisp).astype(np.uint8)

        if latestPacket["rect_left"] is not None:
            frame_left = latestPacket["rect_left"].getCvFrame()

        if latestPacket["rect_right"] is not None:
            frame_right = latestPacket["rect_right"].getCvFrame()

        return frameRgb, frameDepth, frame_left, frame_right

    def OpenCVCodeOld(self, frameCount):
        in_rgb = q_rgb.get()  # blocking call, will wait until a new data has arrived
        in_left = q_left.tryGet()
        in_right = q_right.tryGet()
        in_depth = q_depth.tryGet()

        if in_left is not None:
            shape = (in_left.getHeight(), in_left.getWidth())
            frame_left = in_left.getData().reshape(shape).astype(np.uint8)
            frame_left = np.ascontiguousarray(frame_left)

        if in_right is not None:
            shape = (in_right.getHeight(), in_right.getWidth())
            frame_right = in_right.getData().reshape(shape).astype(np.uint8)
            frame_right = np.ascontiguousarray(frame_right)

        if in_depth is not None:
            frame_depth8U = in_depth.getData().reshape(in_depth.getHeight(), in_depth.getWidth())
            #frame_depth8u = frame_depth8u * (255 / depth.initialConfig.getMaxDisparity())

        #if frame_left is not None:
        #    cv2.imshow("rectif_left", frame_left)

        #if frame_right is not None:
        #    cv2.imshow("rectif_right", frame_right)

        #if frame_depth8U is not None:
        #    cv2.imshow("depth8U", frame_depth8U)

        shape = (3, in_rgb.getHeight(), in_rgb.getWidth())
        frame_rgb = in_rgb.getData().reshape(shape).transpose(1, 2, 0).astype(np.uint8)
        frame_rgb = np.ascontiguousarray(frame_rgb)
        #cv2.imshow("rgb", frame_rgb)

        cv2.waitKey(1) # remove once the imshows are removed.
        return frame_rgb, frame_depth8U, frame_left, frame_right



App().Open()