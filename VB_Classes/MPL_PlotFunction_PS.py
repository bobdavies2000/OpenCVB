#https://matplotlib.org/2.0.2/examples/animation/animate_decay.html
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation
from PyStream import PyStreamRun
import cv2 as cv
import io
titleWindow = 'MPL_PlotFunction_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global xdata, ydata, line, maxX
    fig, ax = plt.subplots()
    if frameCount == 0: 
        line, = ax.plot([], [], lw=2)    # update the data
        ax.set_ylim(-1.1, 1.1)
        maxX = 10
    
    for i in range(0, 9):
        t = frameCount + i / 10
        xdata.append(t)
        ydata.append(np.sin(2*np.pi*t) * np.exp(-t/10.))
    line.set_data(xdata, ydata)

    if frameCount >= maxX:
        maxX *= 2
        ax.set_xlim(0, maxX)
        ax.figure.canvas.draw()

    ax.set_xlim(0, maxX)
    ax.grid()
    plt.plot(xdata, ydata)

    buf = io.BytesIO()
    plt.savefig(buf, format='rgba', dpi=100)

    img_byte_arr = buf.getvalue()
    rgbaSize = 480, 640, 4 
    tmp = np.array(np.frombuffer(img_byte_arr, np.uint8).reshape(rgbaSize)) 
    buf.close()
    plt.close()
    return tmp, None

xdata, ydata = [], []
PyStreamRun(OpenCVCode, titleWindow)