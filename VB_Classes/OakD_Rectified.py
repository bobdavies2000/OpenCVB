import cv2
import depthai as dai
import numpy as np

pipeline = dai.Pipeline()

left = pipeline.createMonoCamera()
left.setResolution(dai.MonoCameraProperties.SensorResolution.THE_480_P)
left.setBoardSocket(dai.CameraBoardSocket.LEFT)

right = pipeline.createMonoCamera()
right.setResolution(dai.MonoCameraProperties.SensorResolution.THE_480_P)
right.setBoardSocket(dai.CameraBoardSocket.RIGHT)

depth = pipeline.createStereoDepth()
depth.initialConfig.setConfidenceThreshold(200)
# Note: the rectified streams are horizontally mirrored by default
#depth.setOutputRectified(True)
depth.setRectifyEdgeFillColor(0) # Black, to better see the cutout
left.out.link(depth.left)
right.out.link(depth.right)

# Define a source - color camera
cam_rgb = pipeline.createColorCamera()
cam_rgb.setPreviewSize(1280, 720)
cam_rgb.setBoardSocket(dai.CameraBoardSocket.RGB)
cam_rgb.setResolution(dai.ColorCameraProperties.SensorResolution.THE_1080_P)
cam_rgb.setInterleaved(False)

xout_depth = pipeline.createXLinkOut()
xout_depth.setStreamName("depth")
depth.disparity.link(xout_depth.input)

xout_left = pipeline.createXLinkOut()
xout_left.setStreamName("rect_left")
depth.rectifiedLeft.link(xout_left.input)

xout_right = pipeline.createXLinkOut()
xout_right.setStreamName('rect_right')
depth.rectifiedRight.link(xout_right.input)

# Create output
xout_rgb = pipeline.createXLinkOut()
xout_rgb.setStreamName("rgb")
cam_rgb.preview.link(xout_rgb.input)

xout_disparity = pipeline.createXLinkOut()
xout_disparity.setStreamName("disparity")

device = dai.Device()
device.startPipeline(pipeline)

q_left = device.getOutputQueue(name="rect_left", maxSize=8, blocking=False)
q_right = device.getOutputQueue(name="rect_right", maxSize=8, blocking=False)
q_depth = device.getOutputQueue(name="depth", maxSize=8, blocking=False)
q_disparity = device.getOutputQueue(name="disparity", maxSize=8, blocking=False)
q_rgb = device.getOutputQueue(name="rgb", maxSize=4, blocking=True)

frame_left = None
frame_right = None
frame_depth = None
frame_depth16 = None
frame_disparity = None

def frame_norm(frame, bbox):
    return (np.clip(np.array(bbox), 0, 1) * np.array([*frame.shape[:2], *frame.shape[:2]])[::-1]).astype(int)

while True:
    in_rgb = q_rgb.get()  # blocking call, will wait until a new data has arrived
    in_left = q_left.tryGet()
    in_right = q_right.tryGet()
    in_depth = q_depth.tryGet()
    in_disp = q_disparity.tryGet()

    if in_left is not None:
        shape = (in_left.getHeight(), in_left.getWidth())
        frame_left = in_left.getData().reshape(shape).astype(np.uint8)
        frame_left = np.ascontiguousarray(frame_left)

    if in_right is not None:
        shape = (in_right.getHeight(), in_right.getWidth())
        frame_right = in_right.getData().reshape(shape).astype(np.uint8)
        frame_right = np.ascontiguousarray(frame_right)

    if in_disp is not None:
        frame_disparity = in_disp.getData().reshape((in_disp.getHeight(), in_disp.getWidth())).astype(np.uint8)
        frame_disparity = np.ascontiguousarray(frame_disparity)
        frame_disparity = cv2.applyColorMap(frame_disparity, cv2.COLORMAP_HSV)

    if in_depth is not None:
        frame_depth16 = in_depth.getData().reshape(in_depth.getHeight(), in_depth.getWidth())
        frame_depth = in_depth.getData().reshape((in_depth.getHeight(), in_depth.getWidth())).astype(np.uint8)
        frame_depth = np.ascontiguousarray(frame_depth)
        frame_depth = cv2.applyColorMap(frame_depth, cv2.COLORMAP_HSV)

    if frame_left is not None:
        cv2.imshow("rectif_left", frame_left)

    if frame_right is not None:
        cv2.imshow("rectif_right", frame_right)

    if frame_depth is not None:
        cv2.imshow("depth", frame_depth)

    if frame_depth16 is not None:
        cv2.imshow("depth16", frame_depth16)

    if frame_disparity is not None:
        print("disparity frame present")
        cv2.imshow("disparity", frame_disparity)

    shape = (3, in_rgb.getHeight(), in_rgb.getWidth())
    frame_rgb = in_rgb.getData().reshape(shape).transpose(1, 2, 0).astype(np.uint8)
    frame_rgb = np.ascontiguousarray(frame_rgb)
    cv2.imshow("rgb", frame_rgb)

    if cv2.waitKey(1) == ord('q'):
        break