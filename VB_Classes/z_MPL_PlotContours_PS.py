"""
=========================
Frontpage contour example
=========================
https://matplotlib.org/2.0.2/examples/frontpage/plot_contour.html
This example reproduces the frontpage contour example.
"""

import matplotlib.pyplot as plt
import numpy as np
from matplotlib import mlab, cm
import io
from PIL import Image
from z_PyStream import PyStreamRun
import cv2 as cv
titleWindow = 'z_MPL_PlotContour_PS.py'

def bivariate_normal(X, Y, sigmax=1.0, sigmay=1.0,
                     mux=0.0, muy=0.0, sigmaxy=0.0):
    """
    Bivariate Gaussian distribution for equal shape *X*, *Y*.
    See `bivariate normal
    <http://mathworld.wolfram.com/BivariateNormalDistribution.html>`_
    at mathworld.
    """
    Xmu = X-mux
    Ymu = Y-muy

    rho = sigmaxy/(sigmax*sigmay)
    z = Xmu**2/sigmax**2 + Ymu**2/sigmay**2 - 2*rho*Xmu*Ymu/(sigmax*sigmay)
    denom = 2*np.pi*sigmax*sigmay*np.sqrt(1-rho**2)
    return np.exp(-z/(2*(1-rho**2))) / denom

def OpenCVCode(imgRGB, depth32f, frameCount):
    global tmp
    if frameCount == 0:  # this example is unchanging and shows how little the pipeline costs.
        buf = io.BytesIO()
        fig, ax = plt.subplots()
        extent = (-3, 3, -3, 3)
        delta = 0.5
        x = np.arange(-3.0, 4.001, delta)
        y = np.arange(-4.0, 3.001, delta)
        X, Y = np.meshgrid(x, y)
        Z1 = bivariate_normal(X, Y, 1.0, 1.0, 0.0, -0.5)
        Z2 = bivariate_normal(X, Y, 1.5, 0.5, 1, 1)
        Z = (Z1 - Z2) * 10

        levels = np.linspace(-2.0, 1.601, 40)
        norm = cm.colors.Normalize(vmax=abs(Z).max(), vmin=-abs(Z).max())

        cset1 = ax.contourf(X, Y, Z, levels,norm=norm)
        ax.set_xlim(-3, 3)
        ax.set_ylim(-3, 3)
        ax.set_xticks([])
        ax.set_yticks([])
        plt.title("Bivariate Normal Distribution example")

        buf.seek(0) 
        plt.savefig(buf, format='rgba', dpi=100)
        img_byte_arr = buf.getvalue()
        rgbaSize = 480, 640, 4 
        tmp = np.array(np.frombuffer(img_byte_arr, np.uint8).reshape(rgbaSize)) 
        buf.close()
        plt.close()
    return tmp, None
tmp = np.empty((480, 640, 3), dtype=np.uint8)
try:
    PyStreamRun(OpenCVCode, titleWindow)
finally:
    print("done")