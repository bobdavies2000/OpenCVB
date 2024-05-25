import argparse
import mmap
import array
import cv2 as cv
import numpy as np
import os, time, sys
from time import sleep
import os
import ctypes
def Mbox(title, text, style):
    return ctypes.windll.user32.MessageBoxW(0, text, title, style)

def getDrawRect():
    global drawRect
    return drawRect

def PyStreamRun(OpenCVCode, titleWindow):
    try:
        global drawRect 
        drawRect = (0,0,0,0)
        if titleWindow.lower().endswith("_ps.py"):
            parser = argparse.ArgumentParser(description='Pass in length of MemMap region.', formatter_class=argparse.ArgumentDefaultsHelpFormatter)
            parser.add_argument('--MemMapLength', type=int, default=0, help='The number of bytes are in the memory mapped file.')
            parser.add_argument('--pipeName', default='', help='The name of the input pipe for image data.')
            args = parser.parse_args()
            
            # When the PythonDebug project runs a Python script, this code will start OpenCVB.exe and invoke the script.
            MemMapLength = args.MemMapLength
            if MemMapLength == 0:
                MemMapLength = 400 # these values have been generously padded (on both sides) but if they grow...
                args.pipeName = 'PyStream2Way0' 
                ocvb = os.getcwd() + '/../bin/Debug/OpenCVB.exe'
                if os.path.exists(ocvb):
                    tupleArg = (' ', titleWindow)
                    pid = os.spawnv(os.P_NOWAIT, ocvb, tupleArg) # OpenCVB.exe will be run with this .py script

            pipeName = '\\\\.\\pipe\\' + args.pipeName
            while True:
                try:
                    pipeIn = open(pipeName, 'rb')
                    pipeOut = open(pipeName + 'Results', 'wb')
                    break
                except Exception as exception:
                    time.sleep(0.1) # sleep for a bit to wait for OpenCVB to start...
            mm = mmap.mmap(0, MemMapLength, tagname='Python_MemMap')
            frameCount = -1
            try:
                while True:
                    mm.seek(0)
                    arrayDoubles = array.array('d', mm.read(MemMapLength))
                    rgbBufferSize = int(arrayDoubles[1])
                    depthBufferSize = int(arrayDoubles[2])
                    if rgbBufferSize == 0: continue
                    if depthBufferSize == 0: continue
                    rows = int(arrayDoubles[3])
                    cols = int(arrayDoubles[4])
                    # this is the task.drawRect in OpenCVB
                    drawRect = (int(arrayDoubles[5]),int(arrayDoubles[6]),int(arrayDoubles[7]),int(arrayDoubles[8]))

                    if rows > 0: 
                        if arrayDoubles[0] == frameCount:
                            sleep(0.001)
                        else:
                            frameCount = arrayDoubles[0] 
                            rgb = pipeIn.read(int(rgbBufferSize))
                            depthData = pipeIn.read(int(depthBufferSize))
                            depthSize = rows, cols, 1
                            try:
                                depth32f = np.array(np.frombuffer(depthData, np.float32).reshape(depthSize))
                            except:
                                print("unable to reshape the depth data")
                                sys.exit(0)
                            rgbSize = rows, cols, 3
                            try:
                                imgRGB = np.array(np.frombuffer(rgb, np.uint8).reshape(rgbSize))
                            except:
                                print("Unable to reshape the RGB data")
                                sys.exit(0)
                            try:
                                dst2, dst3 = OpenCVCode(imgRGB, depth32f, frameCount)
                            except Exception as e:
                                print(e)
                                print("PyStreamRun Failure: running the OpenCVCode")
                            if len(dst2.shape) == 2:
                                dst2 = cv.cvtColor(dst2, cv.COLOR_GRAY2BGR)
                            if dst2.shape[2]==4:
                                dst2 = cv.cvtColor(dst2, cv.COLOR_RGBA2BGR)
                            dst2 = cv.resize(dst2, (cols, rows))
                            if np.any(dst3 != None):
                                if len(dst3.shape) == 2:
                                    dst3 = cv.cvtColor(dst3, cv.COLOR_GRAY2BGR)
                                if dst3.shape[2] == 4:
                                    dst3 = cv.cvtColor(dst3, cv.COLOR_RGBA2BGR)
                                dst3 = cv.resize(dst3, (cols, rows))
                            else:
                                dst3 = np.zeros(dst2.shape, np.uint8)
                            pipeOut.write(np.asarray(dst2)) # Assumption here is that we are always returning 8uC3.  Needs more work to generalize...
                            pipeOut.write(np.asarray(dst3))

                            cv.waitKey(1) # this is only needed if the OpenCVCode function is calling imshow
            except Exception as inst:
                print("Exception in " + titleWindow + " is " + type(inst))
                sys.exit(0)
        else:
            msg = "PyStream scripts need to end with '_PS.py' to be recognized in OpenCVB. And be sure to rebuild all after renaming the Python script so it appears in the OpenCVB interface."
            print(msg) 
            Mbox("PyStream", msg, 1)

    except Exception as inst:
        print("Exception in " + titleWindow + " is " + inst)
        sys.exit(0)