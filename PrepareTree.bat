if exist Azure-Kinect-Sensor-SDK (rmdir Azure-Kinect-Sensor-SDK /s)
if exist opencv (rmdir Azure-Kinect-Sensor-SDK /s)
if exist librealsense (rmdir Azure-Kinect-Sensor-SDK /s)

cd support

prepareOpenCV.bat
PrepareLibrealsense.bat
PrepareAzure4Kinect.bat