#https://matplotlib.org/2.0.2/examples/animation/animate_decay.html
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.animation as animation
from PyStream import PyStreamRun
import cv2 as cv
import io
titleWindow = 'MPL_PlotFunction_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global xdata, ydata, ax, line
    fig, ax = plt.subplots()
    if frameCount == 0: 
        xdata, ydata = [], []
        line, = ax.plot([], [], lw=2)    # update the data
        ax.set_ylim(-1.1, 1.1)
        ax.set_xlim(0, 10)

    ax.grid()
    t = frameCount / 10

    xdata.append(t)
    ydata.append(np.sin(2*np.pi*t) * np.exp(-t/10.))
    line.set_data(xdata, ydata)
    xmin, xmax = ax.get_xlim()

    if t >= xmax:
        ax.set_xlim(0, 2*t)
        ax.figure.canvas.draw()

    plt.plot(xdata, ydata)
    buf = io.BytesIO()
    plt.savefig(buf, format='rgba', dpi=100)

    img_byte_arr = buf.getvalue()
    rgbaSize = 480, 640, 4 
    tmp = np.array(np.frombuffer(img_byte_arr, np.uint8).reshape(rgbaSize)) 
    buf.close()
    plt.close()
    return imgRGB, tmp

PyStreamRun(OpenCVCode, titleWindow)