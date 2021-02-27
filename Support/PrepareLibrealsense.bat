cd ..\
if exist librealsense (rmdir librealsense /s)
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"

"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DBUILD_CSHARP_BINDINGS=1 -DBUILD_CV_EXAMPLES=0 -S librealsense -B librealsense/Build
start librealsense/Build/librealsense2.sln
