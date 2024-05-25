import cv2 as cv
import numpy as np
import math
import sys
from PyStream import PyStreamRun
# https://github.com/spmallick/learnopencv/blob/master/FunnyMirrors/FunnyMirrorsVideo.py
# https://www.learnopencv.com/funny-mirrors-using-opencv/?ck_subscriber_id=785741175

from vcam import vcam,meshGen
saveMode=0

titleWindow = 'FunnyMirror_PS.py'
def on_trackbar(val):
	global saveMode 
	saveMode = val 

def OpenCVCode(imgRGB, depth32f, frameCount):
	H,W = imgRGB.shape[:2]
	# Creating the virtual camera object
	c1 = vcam(H=H,W=W)

	# Creating the surface object
	plane = meshGen(H,W)

	mode = saveMode

	# We generate a mirror where for each 3D point, its Z coordinate is defined as Z = F(X,Y)
	if mode == 0:
		plane.Z += 20*np.exp(-0.5*((plane.X*1.0/plane.W)/0.1)**2)/(0.1*np.sqrt(2*np.pi))
	elif mode == 1:
		plane.Z += 20*np.exp(-0.5*((plane.Y*1.0/plane.H)/0.1)**2)/(0.1*np.sqrt(2*np.pi))
	elif mode == 2:
		plane.Z -= 10*np.exp(-0.5*((plane.X*1.0/plane.W)/0.1)**2)/(0.1*np.sqrt(2*np.pi))
	elif mode == 3:
		plane.Z -= 10*np.exp(-0.5*((plane.Y*1.0/plane.W)/0.1)**2)/(0.1*np.sqrt(2*np.pi))
	elif mode == 4:
		plane.Z += 20*np.sin(2*np.pi*((plane.X-plane.W/4.0)/plane.W)) + 20*np.sin(2*np.pi*((plane.Y-plane.H/4.0)/plane.H))
	elif mode == 5:
		plane.Z -= 20*np.sin(2*np.pi*((plane.X-plane.W/4.0)/plane.W)) - 20*np.sin(2*np.pi*((plane.Y-plane.H/4.0)/plane.H))
	elif mode == 6:
		plane.Z += 100*np.sqrt((plane.X*1.0/plane.W)**2+(plane.Y*1.0/plane.H)**2)
	elif mode == 7:
		plane.Z -= 100*np.sqrt((plane.X*1.0/plane.W)**2+(plane.Y*1.0/plane.H)**2)
	else:
		print("Wrong mode selected")
		exit(-1)

	# Extracting the generated 3D plane
	pts3d = plane.getPlane()

	# Projecting (Capturing) the plane in the virtual camera
	pts2d = c1.project(pts3d)

	# Deriving mapping functions for mesh based warping.
	map_x,map_y = c1.getMaps(pts2d)

	output = cv.remap(imgRGB,map_x,map_y,interpolation=cv.INTER_LINEAR,borderMode=4)
	output = cv.flip(output,1)
	out1 = np.hstack((imgRGB,output))
	out1 = cv.resize(out1,(700,350))
	cv.imshow(titleWindow,out1)
	return output, None

cv.namedWindow(titleWindow)
trackbar_name = 'Distort'
cv.createTrackbar(trackbar_name, titleWindow, saveMode, 7, on_trackbar)
on_trackbar(saveMode)

PyStreamRun(OpenCVCode, titleWindow)