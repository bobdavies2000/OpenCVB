import argparse
import mmap
import array
import cv2
import numpy as np
import os, time, sys
import depthai as dai
from time import sleep
import ctypes

def OakDStreamRun(OpenCVCode, MemMapLength, pipeName, titleWindow, rgbCalib):
    try:
        pipeName = '\\\\.\\pipe\\' + pipeName
        print(pipeName)
        while True:
            try:
                pipeOut = open(pipeName, 'wb')
                pipeIn = open(pipeName + 'Results', 'rb')
                break
            except Exception as exception:
                time.sleep(0.1) # sleep for a bit to wait for OpenCVB to start...
        mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
        frameCount = -1
        try:
            while True:
                mm.seek(0)
                mmArray = array.array('d', mm.read(MemMapLength))
                rows = int(mmArray[3])
                cols = int(mmArray[4])
                if rows > 0:
                    if mmArray[0] == frameCount:
                        sleep(0.001) # there is no new data...
                    else:
                        frameCount = mmArray[0] 
                        signalBuffer = pipeIn.read(int(4)) # read the signal buffer 
                        try:
                            rgb, depth16U, leftRect, rightRect, disparity, xyzData = OpenCVCode(frameCount)
                        except:
                            print("OakD_Stream.py failure when running OpenCVCode")

                        if rgb is not None: 
                            if xyzData is not None: 
                                rgb = cv2.resize(rgb, (cols, rows), interpolation = cv2.INTER_NEAREST)
                                leftRect = cv2.resize(leftRect, (cols, rows), interpolation = cv2.INTER_NEAREST)
                                rightRect = cv2.resize(rightRect, (cols, rows), interpolation = cv2.INTER_NEAREST)
                                disparity = cv2.resize(rightRect, (cols, rows), interpolation = cv2.INTER_NEAREST)

                                pipeOut.write(np.asarray(xyzData.tobytes()))
                                pipeOut.write(np.asarray(rgbCalib.tobytes()))
                            
                                pipeOut.write(np.asarray(rgb)) 
                                pipeOut.write(np.asarray(depth16U)) # note that depth16u is not resized.  Pointcloud computation needs full resolution.
                                pipeOut.write(np.asarray(leftRect)) 
                                pipeOut.write(np.asarray(rightRect))
                                pipeOut.write(np.asarray(disparity))
        except:
            print("Exception in OakStreamRun while loop ")
            sys.exit(0)

    except:
        print("Exception in OakStreamRun ")
        sys.exit(0)