if exist OpenCV (rmdir OpenCV /s)
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv"
cd OpenCV
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib"

cd ..\		
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DWITH_OPENGL=ON -DBUILD_EXAMPLES=OFF -DCPU_DISPATCH=SSE4_1;SSE4_2;AVX;FP16 -DBUILD_PERF_TESTS=OFF -DBUILD_TESTS=OFF -DBUILD_opencv_python_tests=OFF -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules -S OpenCV -B OpenCV/Build
start OpenCV/Build/OpenCV.sln

if exist librealsense (rmdir librealsense /s)
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DBUILD_CSHARP_BINDINGS=1 -DBUILD_CV_EXAMPLES=0 -S librealsense -B librealsense/Build
start librealsense/Build/librealsense2.sln

if exist Azure-Kinect-Sensor-SDK (rmdir Azure-Kinect-Sensor-SDK /s)
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/microsoft/Azure-Kinect-Sensor-SDK"
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DOpenCV_DIR=OpenCV/Build -DCMAKE_BUILD_TYPE=Debug -S Azure-Kinect-Sensor-SDK -B Azure-Kinect-Sensor-SDK/Build
start Azure-Kinect-Sensor-SDK/Build/k4a.sln

