# https://towardsdatascience.com/animations-with-matplotlib-d96375c5442c
from mpl_toolkits.mplot3d import Axes3D
import matplotlib.pyplot as plt
import pandas as pd

import numpy as np
from PyStream import PyStreamRun
import cv2 as cv
import io
titleWindow = 'MPL_Graph3D_PS.py'

def OpenCVCode(imgRGB, depth32f, frameCount):
    global df, angle

    angle += 10
    if angle > 210: angle = 70

    fig = plt.figure()
    ax = fig.gca(projection='3d')
    ax.plot_trisurf(df['Y'], df['X'], df['Z'], cmap=plt.cm.viridis, linewidth=0.2)

    ax.view_init(30,angle)

    buf = io.BytesIO()
    plt.savefig(buf, format='rgba', dpi=100)
    img_byte_arr = buf.getvalue()
    rgbaSize = 480, 640, 4 
    tmp = np.array(np.frombuffer(img_byte_arr, np.uint8).reshape(rgbaSize)) 
    buf.close()
    plt.close()
    return tmp, None

# Get the data (csv file is hosted on the web)
url = 'https://raw.githubusercontent.com/holtzy/The-Python-Graph-Gallery/master/static/data/volcano.csv'
data = pd.read_csv(url)

# Transform it to a long format
df=data.unstack().reset_index()
df.columns=["X","Y","Z"]

# And transform the old column name in something numeric
df['X']=pd.Categorical(df['X'])
df['X']=df['X'].cat.codes
angle = 70

PyStreamRun(OpenCVCode, titleWindow)