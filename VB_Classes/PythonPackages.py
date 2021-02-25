import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

titleWindow = 'PythonPackages.py'

print("Checking the packages used by the OpenCVB Python scripts.")
warningMsg = False
try:
    import numpy
except ImportError as err:
    print('You need to install numpy', err)
    warningMsg = True

try:
    import cv2
except ImportError as err:
    print('You need to install opencv-python and opencv-contrib-python', err)
    warningMsg = True

try:
    import sklearn
except ImportError as err:
    print('You need to install scikit-learn', err)
    warningMsg = True

try:
    import matplotlib.pyplot as plt
except ImportError as err:
    print('You need to install matplotlib', err)
    warningMsg = True

try:
    import OpenGL
except ImportError as err:
    print('You need to install PyOpenGL', err)
    warningMsg = True
    
try:
    import pygame
except ImportError as err:
    print('You need to install Pygame', err)
    warningMsg = True
	        
try:
    import vcam
except ImportError as err:
    print('You need to install vcam', err)
    warningMsg = True
	    
try:
    import imutils
except ImportError as err:
    print('You need to install imutils', err)
    warningMsg = True

try:
    from cv2_rolling_ball import subtract_background_rolling_ball
except ImportError as err:
    print('You need to install opencv-rolling-ball')
    warningMsg = True
    
try:
    import tensorflow as tf
except ImportError as err:
    print('You need to install tensorflow')
    warningMsg = True
    
try:
    import pyglet
except ImportError as err:
    print('You need to install pyglet')
    warningMsg = True
    
try:
    import pyrealsense2
except ImportError as err:
    print('You need to install pyrealsense2')
    warningMsg = True

if warningMsg:
    Mbox('PythonPackages', 'Needed packages are not present.  Review console log.', 1)
else:
    Mbox('PythonPackages', 'Python is present and all the necessary packages appear to be installed.', 1)
cv2.waitKey(3000)