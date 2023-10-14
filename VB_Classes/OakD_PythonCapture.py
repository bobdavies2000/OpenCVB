import cv2
import numpy as np
import depthai as dai
import argparse
import ctypes

import time
import math
from datetime import timedelta

from OakD_Stream import OakDStreamRun
titleWindow = "CameraOakDPython.py"

parser = argparse.ArgumentParser()
parser.add_argument("-md", "--mesh_dir", type=str, default=None, help="Output directory for mesh files. If not specified mesh files won't be saved",)
parser.add_argument("-lm", "--load_mesh", default=False, action="store_true",help="Read camera intrinsics, generate mesh files and load them into the stereo node.",)
parser.add_argument("-e", "--extended", default=False, action="store_true", help="Closer-in minimum depth, disparity range is doubled",)
parser.add_argument("-s", "--subpixel", default=True, action="store_true", help="Better accuracy for longer distance, fractional disparity 32-levels",)
parser.add_argument("-m", "--median", type=str, default="7x7", help="Choose the size of median filtering. Options: OFF | 3x3 | 5x5 | 7x7 (default)",)
parser.add_argument('-MemMapLength', "--MemMapLength", type=int, default=400, help='The number of bytes are in the memory mapped file.')
parser.add_argument('-pipeName', "--pipeName", type=str, default='', help='The name of the input pipe for image data.')
parser.add_argument('-fps', "--fps", type=int, default=30, help='frames per second for the input images.')
args = parser.parse_args()

resolution = (1280, 720)
meshDirectory = args.mesh_dir  # Output dir for mesh files
generateMesh = args.load_mesh  # Load mesh files

extended = args.extended  # Closer-in minimum depth, disparity range is doubled
subpixel = args.subpixel  # Better accuracy for longer distance, fractional disparity 32-levels

medianMap = {
    "OFF": dai.StereoDepthProperties.MedianFilter.MEDIAN_OFF,
    "3x3": dai.StereoDepthProperties.MedianFilter.KERNEL_3x3,
    "5x5": dai.StereoDepthProperties.MedianFilter.KERNEL_5x5,
    "7x7": dai.StereoDepthProperties.MedianFilter.KERNEL_7x7,
}
if args.median not in medianMap:
    exit("Unsupported median size!")

median = medianMap[args.median]

print("StereoDepth config options:")
print("    Resolution:  ", resolution)
print("    Left-Right check:  ", True)
print("    Extended disparity:", extended)
print("    Subpixel:          ", subpixel)
print("    Median filtering:  ", median)
print("    Generating mesh files:  ", generateMesh)
print("    Outputting mesh files to:  ", meshDirectory)
print("    MemMapLength: ", str(args.MemMapLength))
print("    pipename: ", args.pipeName)

def getMesh(calibData):
    M1 = np.array(calibData.getCameraIntrinsics(dai.CameraBoardSocket.LEFT, resolution[0], resolution[1]))
    d1 = np.array(calibData.getDistortionCoefficients(dai.CameraBoardSocket.LEFT))
    R1 = np.array(calibData.getStereoLeftRectificationRotation())
    M2 = np.array(calibData.getCameraIntrinsics(dai.CameraBoardSocket.RIGHT, resolution[0], resolution[1]))
    d2 = np.array(calibData.getDistortionCoefficients(dai.CameraBoardSocket.RIGHT))
    R2 = np.array(calibData.getStereoRightRectificationRotation())
    mapXL, mapYL = cv2.initUndistortRectifyMap(M1, d1, R1, M2, resolution, cv2.CV_32FC1)
    mapXR, mapYR = cv2.initUndistortRectifyMap(M2, d2, R2, M2, resolution, cv2.CV_32FC1)

    meshCellSize = 16
    meshLeft = []
    meshRight = []

    for y in range(mapXL.shape[0] + 1):
        if y % meshCellSize == 0:
            rowLeft = []
            rowRight = []
            for x in range(mapXL.shape[1] + 1):
                if x % meshCellSize == 0:
                    if y == mapXL.shape[0] and x == mapXL.shape[1]:
                        rowLeft.append(mapYL[y - 1, x - 1])
                        rowLeft.append(mapXL[y - 1, x - 1])
                        rowRight.append(mapYR[y - 1, x - 1])
                        rowRight.append(mapXR[y - 1, x - 1])
                    elif y == mapXL.shape[0]:
                        rowLeft.append(mapYL[y - 1, x])
                        rowLeft.append(mapXL[y - 1, x])
                        rowRight.append(mapYR[y - 1, x])
                        rowRight.append(mapXR[y - 1, x])
                    elif x == mapXL.shape[1]:
                        rowLeft.append(mapYL[y, x - 1])
                        rowLeft.append(mapXL[y, x - 1])
                        rowRight.append(mapYR[y, x - 1])
                        rowRight.append(mapXR[y, x - 1])
                    else:
                        rowLeft.append(mapYL[y, x])
                        rowLeft.append(mapXL[y, x])
                        rowRight.append(mapYR[y, x])
                        rowRight.append(mapXR[y, x])
            if (mapXL.shape[1] % meshCellSize) % 2 != 0:
                rowLeft.append(0)
                rowLeft.append(0)
                rowRight.append(0)
                rowRight.append(0)

            meshLeft.append(rowLeft)
            meshRight.append(rowRight)

    meshLeft = np.array(meshLeft)
    meshRight = np.array(meshRight)

    return meshLeft, meshRight


def saveMeshFiles(meshLeft, meshRight, outputPath):
    print("Saving mesh to:", outputPath)
    meshLeft.tofile(outputPath + "/left_mesh.calib")
    meshRight.tofile(outputPath + "/right_mesh.calib")


def getDisparityFrame(frame):
    maxDisp = stereo.initialConfig.getMaxDisparity()
    disp = (frame * (255.0 / maxDisp)).astype(np.uint8)
    disp = cv2.applyColorMap(disp, cv2.COLORMAP_JET)

    return disp

print("Creating Stereo Depth pipeline")
devices = dai.Device.getAllAvailableDevices()

pipeline = dai.Pipeline()

imuPresent = False
calibData = dai.Device().readCalibration()
boardName = calibData.getEepromData().boardName
if "LITE" not in boardName:
    imuPresent = True
print(boardName)

camLeft = pipeline.create(dai.node.MonoCamera)
camRight = pipeline.create(dai.node.MonoCamera)
stereo = pipeline.create(dai.node.StereoDepth)

imu = None
xlinkIMU = None
imuQueue = None
imuData = None

if imuPresent:
    imu = pipeline.create(dai.node.IMU)
    xlinkIMU = pipeline.create(dai.node.XLinkOut)
    xlinkIMU.setStreamName("imu")

    # enable ACCELEROMETER_RAW and GYROSCOPE_RAW at 500 hz rate
    imu.enableIMUSensor([dai.IMUSensor.ACCELEROMETER_RAW, dai.IMUSensor.GYROSCOPE_RAW], 200)
    # above this threshold packets will be sent in batch of X, if the host is not blocked and USB bandwidth is available
    imu.setBatchReportThreshold(1)
    # maximum number of IMU packets in a batch, if it's reached device will block sending until host can receive it
    # if lower or equal to batchReportThreshold then the sending is always blocking on device
    # useful to reduce device's CPU load  and number of lost packets, if CPU load is high on device side due to multiple nodes
    imu.setMaxBatchReports(28)

    # Link plugins IMU -> XLINK
    imu.out.link(xlinkIMU.input)

xoutLeft = pipeline.create(dai.node.XLinkOut)
xoutRight = pipeline.create(dai.node.XLinkOut)
xoutDisparity = pipeline.create(dai.node.XLinkOut)
xoutDepth = pipeline.create(dai.node.XLinkOut)
xoutRectifLeft = pipeline.create(dai.node.XLinkOut)
xoutRectifRight = pipeline.create(dai.node.XLinkOut)
camRgb = pipeline.create(dai.node.ColorCamera)
rgbOut = pipeline.create(dai.node.XLinkOut)

camRgb.setBoardSocket(dai.CameraBoardSocket.RGB)
camRgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
camRgb.setFps(args.fps)
camRgb.setIspScale(2, 3)
# For now, RGB needs fixed focus to properly align with depth.
# This value was used during calibration
camRgb.initialControl.setManualFocus(130)

camLeft.setBoardSocket(dai.CameraBoardSocket.LEFT)
camRight.setBoardSocket(dai.CameraBoardSocket.RIGHT)

for monoCam in (camLeft, camRight):  # Common config
    monoCam.setResolution( dai.MonoCameraProperties.SensorResolution.THE_400_P)
    monoCam.setFps(args.fps)

stereo.initialConfig.setConfidenceThreshold(245)
stereo.initialConfig.setMedianFilter(median)  # KERNEL_7x7 default
stereo.setRectifyEdgeFillColor(0)  # Black, to better see the cutout
stereo.setLeftRightCheck(True)
stereo.setDepthAlign(dai.CameraBoardSocket.RGB)
stereo.setExtendedDisparity(extended)
stereo.setSubpixel(subpixel)

xoutLeft.setStreamName("left")
xoutRight.setStreamName("right")
xoutDisparity.setStreamName("disparity")
xoutDepth.setStreamName("depth")
xoutRectifLeft.setStreamName("rectifiedLeft")
xoutRectifRight.setStreamName("rectifiedRight")
rgbOut.setStreamName("rgb")

camLeft.out.link(stereo.left)
camRight.out.link(stereo.right)
stereo.syncedLeft.link(xoutLeft.input)
stereo.syncedRight.link(xoutRight.input)
stereo.disparity.link(xoutDisparity.input)
stereo.depth.link(xoutDepth.input)
stereo.rectifiedLeft.link(xoutRectifLeft.input)
stereo.rectifiedRight.link(xoutRectifRight.input)
camRgb.isp.link(rgbOut.input)

streams = ["left", "right", "rectifiedLeft", "rectifiedRight", "disparity", "depth", "rgb"]

leftMesh, rightMesh = getMesh(calibData)
if generateMesh:
    meshLeft = list(leftMesh.tobytes())
    meshRight = list(rightMesh.tobytes())
    stereo.loadMeshData(meshLeft, meshRight)

if meshDirectory is not None:
    saveMeshFiles(leftMesh, rightMesh, meshDirectory)

device = dai.Device(pipeline)
# Create a receive queue for each stream
qList = [device.getOutputQueue(stream, 8, blocking=False) for stream in streams]
frameDepth = None
frameRectLeft = None
frameRectRight = None
frameDisparity = None

dev_info = device.getDeviceInfo()
device_info = device.getDeviceInfo()
print(device_info.desc.platform.name)
mx_serial_id = dev_info.getMxId()

if imuPresent:
    try:
        imuQueue = device.getOutputQueue(name="imu", maxSize=50, blocking=False)
    except:
        imuPresent = False
if imuPresent: 
    print("IMU is present") 
else: 
    print("IMU is not present")

rgbCalib = np.array(calibData.getCameraIntrinsics(dai.CameraBoardSocket.RGB, resolution[0], resolution[1]))
rgbCoeffs = np.array(calibData.getDistortionCoefficients(dai.CameraBoardSocket.RGB))

class App(object):
    def Open(self):
        print("Open OakD_Stream Interface for Oak-D camera")
        OakDStreamRun(self.OpenCVCode, args.MemMapLength, args.pipeName, titleWindow, rgbCalib)

    def OpenCVCode(self, frameCount):
        xyzData = np.float32([0, 0, 0, 0, 0, 0, 0, 0]) # with no imu, just send zeros.
        if imuPresent: 
            imuData = imuQueue.get()  
            imuPackets = imuData.packets
            for imuPacket in imuPackets:
                acceleroValues = imuPacket.acceleroMeter
                gyroValues = imuPacket.gyroscope

                accTs = acceleroValues.timestamp.get() 
                gyroTs = gyroValues.timestamp.get()
                xyzData = np.float32([acceleroValues.x, acceleroValues.y, -acceleroValues.z, gyroValues.x, gyroValues.y, gyroValues.z, 
                                      accTs.seconds + accTs.microseconds / 1000000, gyroTs.seconds + gyroTs.microseconds / 1000000])

        for q in qList:
            name = q.getName()
            if name == "depth":
                frame = q.get().getCvFrame()
                frameDepth = frame.astype(np.uint16)
            elif name == "disparity":
                frame = q.get().getCvFrame()
                frameDisparity = getDisparityFrame(frame)
            elif name == "rectifiedLeft":
                frame = q.get().getCvFrame()
                frameRectLeft = frame
            elif name == "rectifiedRight":
                frame = q.get().getCvFrame()
                frameRectRight = frame
            elif name == "rgb":
                frameRGB = q.get().getCvFrame()

        return frameRGB, frameDepth, frameRectLeft, frameRectRight, frameDisparity, xyzData

App().Open()