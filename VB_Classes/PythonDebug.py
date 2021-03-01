import pyrealsense2 as rs
import argparse
import mmap
import array
import cv2 as cv
import numpy as np
import os, time, sys
from time import sleep
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

parser = argparse.ArgumentParser(description='Pass in width and height of buffers.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('--Width', type=int, default=1280, help='Image width expected by OpenCVB')
parser.add_argument('--Height', type=int, default=720, help='Image height expected by OpenCVB')
parser.add_argument('--pipeName', default='', help='The name of the input pipe for image data.')
args = parser.parse_args()

pipeName = '//./pipe/' + args.pipeName
pipeOut = open(pipeName, 'wb')
pipeIn = open(pipeName + 'in', 'rb')

rsPipeline = rs.pipeline()
rsConfig = rs.config()

rsPipeline_wrapper = rs.pipeline_wrapper(rsPipeline)
rsPipeline_profile = rsConfig.resolve(rsPipeline_wrapper)
device = rsPipeline_profile.get_device()
device_product_line = str(device.get_info(rs.camera_info.product_line))

rsConfig.enable_stream(rs.stream.depth, args.Width, args.Height, rs.format.z16, 30)
rsConfig.enable_stream(rs.stream.color, args.Width, args.Height, rs.format.bgr8, 30)
rsConfig.enable_stream(rs.stream.infrared, 1, args.Width, args.Height, rs.format.y8, 30)
rsConfig.enable_stream(rs.stream.infrared, 2, args.Width, args.Height, rs.format.y8, 30)
rsConfig.enable_stream(rs.stream.gyro)
rsConfig.enable_stream(rs.stream.accel)

# Start streaming
profile = rsPipeline.start(rsConfig)

depth_sensor = profile.get_device().first_depth_sensor()
depth_scale = depth_sensor.get_depth_scale()
point_cloud = rs.pointcloud()

align_to = rs.stream.color
align = rs.align(align_to)

try:
    while True:
        # Get frameset of color and depth
        frames = rsPipeline.wait_for_frames()
		#gyro = frames.first_or_default(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F)
		#accel = frames.first_or_default(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F)

        # Align the depth frame to color frame
        aligned_frames = align.process(frames)

        # Get aligned frames
        aligned_depth_frame = aligned_frames.get_depth_frame() # aligned_depth_frame is a 640x480 depth image
        color_frame = aligned_frames.get_color_frame()
        leftImage = aligned_frames.get_infrared_frame(1).get_data()
        rightImage = aligned_frames.get_infrared_frame(2).get_data()

        # Validate that both frames are valid
        if not aligned_depth_frame or not color_frame:
            continue

        shape = (args.Height, args.Width)
        depth_image = np.asanyarray(aligned_depth_frame.get_data())
        imgRGB = np.asanyarray(color_frame.get_data())

        points = point_cloud.calculate(aligned_depth_frame)
        verts = np.asanyarray(points.get_vertices()).view(np.float32).reshape(-1, args.Width , 3) 

        pipeOut.write(np.asarray(imgRGB))
        pipeOut.write(np.asarray(leftImage))
        pipeOut.write(np.asarray(rightImage))
        pipeOut.write(np.asarray(depth_image))
        pipeOut.write(np.asarray(verts))

        frameIndex = pipeIn.read(1)

except Exception as exception:
    rsPipeline.stop()   
    print(str(exception))
    sys.exit(0)

finally:
    rsPipeline.stop()
    print("PythonRS2 complete")
    sys.exit(0)