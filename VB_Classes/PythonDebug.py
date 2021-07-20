import pyrealsense2 as rs
import numpy as np
import cv2
import tensorflow as tf
titleWindow = 'TensorFlow_Example1_PS.py'
from PyStream import PyStreamRun
# https://github.com/IntelRealSense/librealsense/blob/master/wrappers/tensorflow/example1%20-%20object%20detection.py
def OpenCVCode(color_image, depth32f, frameCount):
    global image_tensor, detection_boxes, detection_scores, detection_classes, num_detections, colors_hash
    width = color_image.shape[1]
    height = color_image.shape[0]
    # expand image dimensions to have shape: [1, None, None, 3]
    # i.e. a single-column array, where each item in the column has the pixel RGB value
    image_expanded = np.expand_dims(color_image, axis=0)
    # Perform the actual detection by running the model with the image as input
    (boxes, scores, classes, num) = sess.Run([detection_boxes, detection_scores, detection_classes, num_detections],
                                                feed_dict={image_tensor: image_expanded})

    boxes = np.squeeze(boxes)
    classes = np.squeeze(classes).astype(np.int32)
    scores = np.squeeze(scores)

    for idx in range(int(num)):
        class_ = classes[idx]
        score = scores[idx]
        box = boxes[idx]
        
        if class_ not in colors_hash:
            colors_hash[class_] = tuple(np.random.choice(range(256), size=3))
        
        if score > 0.6:
            left = int(box[1] * width)
            top = int(box[0] * height)
            right = int(box[3] * width)
            bottom = int(box[2] * height)
            
            p1 = (left, top)
            p2 = (right, bottom)
            # draw box
            r, g, b = colors_hash[class_]
            cv2.rectangle(color_image, p1, p2, (int(r), int(g), int(b)), 2, 1)

    return color_image, None


# download_Databases automatically downloads the database used here.
# It is available from https://github.com/opencv/opencv/wiki/TensorFlow-Object-Detection-API#run-network-in-opencv
# Load the Tensorflow model into memory.
detection_graph = tf.Graph()
with detection_graph.as_default():
    od_graph_def = tf.compat.v1.GraphDef()
    with tf.compat.v1.gfile.GFile("../Data/faster_rcnn_inception_v2_coco_2018_01_28/frozen_inference_graph.pb" , 'rb') as fid:
        serialized_graph = fid.read()
        od_graph_def.ParseFromString(serialized_graph)
        tf.compat.v1.import_graph_def(od_graph_def, name='')
    sess = tf.compat.v1.Session(graph=detection_graph)

# Input tensor is the image
image_tensor = detection_graph.get_tensor_by_name('image_tensor:0')
# Output tensors are the detection boxes, scores, and classes
# Each box represents a part of the image where a particular object was detected
detection_boxes = detection_graph.get_tensor_by_name('detection_boxes:0')
# Each score represents level of confidence for each of the objects.
# The score is shown on the result image, together with the class label.
detection_scores = detection_graph.get_tensor_by_name('detection_scores:0')
detection_classes = detection_graph.get_tensor_by_name('detection_classes:0')
# Number of objects detected
num_detections = detection_graph.get_tensor_by_name('num_detections:0')
# code source of tensorflow model loading: https://www.geeksforgeeks.org/ml-training-image-classifier-using-tensorflow-object-detection-api/
colors_hash = {}
PyStreamRun(OpenCVCode, titleWindow)
