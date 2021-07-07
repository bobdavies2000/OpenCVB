"c:\Program Files\Git\bin\git.exe" clone "https://github.com/microsoft/Azure-Kinect-Sensor-SDK"
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DOpenCV_DIR=OpenCV/Build -DCMAKE_BUILD_TYPE=Debug -S Azure-Kinect-Sensor-SDK -B Azure-Kinect-Sensor-SDK/Build

msbuild.exe Azure-Kinect-Sensor-SDK/Build/k4a.sln /p:Configuration=Debug
msbuild.exe Azure-Kinect-Sensor-SDK/Build/k4a.sln /p:Configuration=Release





"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DBUILD_CSHARP_BINDINGS=1 -DBUILD_CV_EXAMPLES=0 -S librealsense -B librealsense/Build
msbuild.exe librealsense/Build/librealsense2.sln /p:Configuration=Debug
msbuild.exe librealsense/Build/librealsense2.sln /p:Configuration=Release



"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv"
cd OpenCV
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib"	

cd ..\		
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DBUILD_opencv_viz=OFF -DWITH_OPENGL=OFF -DBUILD_EXAMPLES=OFF -DCPU_DISPATCH=SSE4_1;SSE4_2;AVX;FP16 -DBUILD_PERF_TESTS=OFF -DBUILD_TESTS=OFF -DBUILD_opencv_python_tests=OFF -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules -S OpenCV -B OpenCV/Build

msbuild.exe OpenCV/Build/OpenCV.sln /p:Configuration=Debug
msbuild.exe OpenCV/Build/OpenCV.sln /p:Configuration=Release
msbuild.exe VersionOpenCV/VersionOpenCV.sln /p:Configuration=Debug
cd bin/Debug
VersionOpenCV.exe
