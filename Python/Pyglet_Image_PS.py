import pyglet
import threading
from PyStream import PyStreamRun
import cv2 as cv
import io
import numpy as np
from PIL import Image as im
window = pyglet.window.Window()
titleWindow = 'Pyglet_Image_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global image, imageReady
    img = cv.cvtColor(imgRGB, cv.COLOR_BGR2RGB)
    image = pyglet.image.ImageData(imgRGB.shape[1], imgRGB.shape[0], 'RGB', img.tobytes(), pitch = -imgRGB.shape[1]*3)
    imageReady = True
    return imgRGB, None

class PyStreamThread(object):
    def __init__(self, interval=1):
        thread = threading.Thread(target=self.run, args=())
        thread.daemon = True                            
        thread.start()                                  

    def run(self):
        PyStreamRun(OpenCVCode, titleWindow)        

@window.event
def on_draw():
    global image, imageReady
    window.clear()
    if imageReady: 
        image.blit(0, 0)

def update(dt):
    pass

image = None
imageReady = False
pyThread = PyStreamThread()
pyglet.clock.schedule_interval(update, 1/30) # schedule 30 times per second
pyglet.app.Run()