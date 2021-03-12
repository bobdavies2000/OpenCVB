#https://matplotlib.org/2.0.2/examples/animation/histogram.html
import numpy as np
from PyStream import PyStreamRun
import cv2 as cv
import io

import matplotlib.pyplot as plt
import matplotlib.patches as patches
import matplotlib.path as path
titleWindow = 'MatPlot_Histogram_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    data = depth32f.flatten('C')
    n, bins = np.histogram(data, 20)
    fig, ax = plt.subplots()
    # get the corners of the rectangles for the histogram
    left = np.array(bins[:-1])
    right = np.array(bins[1:])
    bottom = np.zeros(len(left))
    top = bottom + n
    nrects = len(left)

    # for each rect: 1 for the MOVETO, 3 for the LINETO, 1 for the
    # CLOSEPOLY; the vert for the closepoly is ignored but we still need
    # it to keep the codes aligned with the vertices
    nverts = nrects*(1 + 3 + 1)
    verts = np.zeros((nverts, 2))
    codes = np.ones(nverts, int) * path.Path.LINETO
    codes[0::5] = path.Path.MOVETO
    codes[4::5] = path.Path.CLOSEPOLY
    verts[0::5, 0] = left
    verts[0::5, 1] = bottom
    verts[1::5, 0] = left
    verts[1::5, 1] = top
    verts[2::5, 0] = right
    verts[2::5, 1] = top
    verts[3::5, 0] = right
    verts[3::5, 1] = bottom

    barpath = path.Path(verts, codes)
    patch = patches.PathPatch(barpath, facecolor='green', edgecolor='yellow', alpha=0.5)
    ax.add_patch(patch)

    ax.set_xlim(left[0], right[-1])
    ax.set_ylim(bottom.min(), top.max())

    buf = io.BytesIO()
    plt.savefig(buf, format='rgba', dpi=100)

    img_byte_arr = buf.getvalue()
    rgbaSize = 480, 640, 4 
    tmp = np.array(np.frombuffer(img_byte_arr, np.uint8).reshape(rgbaSize)) 
    tmp = cv.cvtColor(tmp, cv.COLOR_RGBA2BGR)
    tmp = cv.resize(tmp, (imgRGB.shape[0], imgRGB.shape[1]))
    buf.close()
    plt.close()
    return tmp, None

PyStreamRun(OpenCVCode, titleWindow)
