import mmap
import struct
import array
import argparse
import ctypes
import os, time, sys
import cv2 as cv
from time import sleep
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

parser = argparse.ArgumentParser(description='Pass in length of MemMap region.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
parser.add_argument('--MemMapLength', type=int, default=0, help='The number of bytes are in the memory mapped region.')
args = parser.parse_args()

MemMapLength = args.MemMapLength
if MemMapLength == 0:
    MemMapLength = 400 # these values have been generously padded (on both sides) but if they grow...
    args.pipeName = 'OpenCVBImages0' # we always start with 0 and since it is only invoked once, 0 is all it will ever be.
    ocvb = os.getcwd() + '\\..\\..\\bin\Debug\OpenCVB.exe' # 
    if os.path.exists():
        pid = os.spawnv(os.P_NOWAIT, 'Python_MemMap')
try:
    mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
    frameCount = -1
    while 1:
        mm.seek(0)
        arrayDoubles = array.array('d', mm.read(MemMapLength))
        if arrayDoubles[0] == frameCount:
            sleep(0.001)
        else:
            print("frame number (from MemMap region) = ", int(frameCount))
            frameCount = arrayDoubles[0]
 
except Exception as exception:
    print(exception)
    Mbox('Python_MemMap', 'Failure - see print output', 1)    
