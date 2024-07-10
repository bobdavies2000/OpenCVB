import pyrealsense2 as rs
import numpy as np
import cv2 as cv

p = rs.pipeline()
conf = rs.config()
conf.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
conf.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)
conf.enable_stream(rs.stream.infrared, 1, 640, 480, rs.format.y8, 30)
conf.enable_stream(rs.stream.infrared, 2, 640, 480, rs.format.y8, 30)
conf.enable_stream(rs.stream.accel)
conf.enable_stream(rs.stream.gyro)
prof = p.start(conf)

try:
    while True:
        f = p.wait_for_frames()
        color_frame = f.get_color_frame()
        imgRGB = np.asanyarray(color_frame.get_data())
        cv.imshow("imgRGB", imgRGB)
        cv.waitKey(0)
        #motion_data = f.as_motion_frame().get_motion_data()
        #print(motion_data) 

except Exception as exception:
    print(str(exception))