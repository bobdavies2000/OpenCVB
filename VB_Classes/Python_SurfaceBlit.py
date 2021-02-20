import pygame
from pygame.locals import *

from OpenGL.GL import *
from OpenGL.GLU import *

import mmap
import array
import argparse
import cv2 as cv
import numpy as np
titleWindow = 'Python_SurfaceBlit.py'
import ctypes
import os, time, sys
from time import sleep
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

parser = argparse.ArgumentParser(description='Pass in length of MemMap region.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('--MemMapLength', type=int, default=0, help='The number of bytes are in the memory mapped file.')
parser.add_argument('--pipeName', default='OpenCVBImages0', help='The name of the pipe to connect for image data.')
args = parser.parse_args()

pid = 0 # pid of any spawned task
MemMapLength = args.MemMapLength
if MemMapLength == 0:
    MemMapLength = 400 # these values have been generously padded (on both sides) but if they grow...
    args.pipeName = 'OpenCVBImages0' # we always start with 0 and since it is only invoked once, 0 is all it will ever be.
    ocvb = os.getcwd() + '\\..\\..\\bin\Debug\OpenCVB.exe'
    if os.path.exists():
        pid = os.spawnv(os.P_NOWAIT, 'Python_SurfaceBlit')

pipeName = '\\\\.\\pipe\\' + args.pipeName
while True:
    try:
        pipeIn = open(pipeName, 'rb')
        break
    except Exception as exception:
        time.sleep(0.1) # sleep for a bit to wait for OpenCVB to start...

try:
    pygame.init()
except Exception as exception:
    print(exception)
    Mbox('Python_SurfaceBlit 1', 'Failure 1 - see print output', 1)    

pygame.display.set_caption("OpenCVB - Python_SurfaceBlit.py")
initialized = False

try:
    mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
    frameCount = -1

    while True:
        for event in pygame.event.get():
            if event.type == pygame.QUIT:
                pygame.quit()
                quit()

        mm.seek(0)
        arrayDoubles = array.array('d', mm.read(MemMapLength))
        rgbBufferSize = int(arrayDoubles[1])
        pointCloudSize = int(arrayDoubles[2])
        rows = int(arrayDoubles[3])
        cols = int(arrayDoubles[4])

        if rows > 0:
            display = (cols, rows)

            if initialized == False:
                initialized = True
                screen = pygame.display.set_mode(display)
                surface = pygame.Surface(display)
                surface.fill((255, 0, 0))
                screen.blit(surface, (0, 0))
            if arrayDoubles[0] == frameCount:
                sleep(0.001)
            else:
                frameCount = arrayDoubles[0] 
                rgb = pipeIn.read(int(rgbBufferSize))

                surface = pygame.image.frombuffer(rgb, (cols, rows), "RGB")
                screen.blit(surface, (0, 0))
                size = rows, cols, 3
                img = np.frombuffer(rgb, np.uint8).reshape(size)

                pygame.display.flip()
                pygame.time.wait(1)

except Exception as exception:
    print(exception)
    Mbox('Python_SurfaceBlit 2', 'Failure 2 - see print output', 1)    

