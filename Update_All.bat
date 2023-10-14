@echo off
:: Check for Python Installation
python --version 2>NUL
if errorlevel 1 goto errorNoPython

:: Reaching here means Python is installed.
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64

if not exist librealsense (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"
)

if not exist OpenCV (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv"
	cd OpenCV
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib"	
	cd ..\
) 

if not exist Azure-Kinect-Sensor-SDK (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/microsoft/Azure-Kinect-Sensor-SDK"
)

if not exist OakD\depthai-core (
	cd OakD
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/luxonis/depthai-core.git"
	cd depthai-core
	"c:\Program Files\Git\bin\git.exe" submodule update --init --recursive
	cd ..\..\
)

if not exist opencv\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -DBUILD_PERF_TESTS=NO -DBUILD_TESTS=NO -DBUILD_opencv_python_tests=NO -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules -S OpenCV -B OpenCV/Build
	:: cannot cmake Kinect or depthai until OpenCV is built.
	msbuild.exe OpenCV/Build/OpenCV.sln /p:Configuration=Debug
	msbuild.exe OpenCV/Build/OpenCV.sln /p:Configuration=Release
)

:: update the pragma comments for the OpenCV libraries to point to the latest version of OpenCV
msbuild.exe VersionUpdates/VersionUpdates.sln /p:Configuration=Debug
bin\Debug\VersionUpdates.exe

if not exist librealsense\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S librealsense -B librealsense/Build
	msbuild.exe librealsense/Build/realsense2.sln /p:Configuration=Debug
	msbuild.exe librealsense/Build/realsense2.sln /p:Configuration=Release
)

if not exist Azure-Kinect-Sensor-SDK\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -DOpenCV_DIR=OpenCV/Build -DCMAKE_BUILD_TYPE=Debug -S Azure-Kinect-Sensor-SDK -B Azure-Kinect-Sensor-SDK/Build
	msbuild.exe Azure-Kinect-Sensor-SDK/Build/k4a.sln /p:Configuration=Debug
	msbuild.exe Azure-Kinect-Sensor-SDK/Build/k4a.sln /p:Configuration=Release
)

if not exist OakD\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S OakD -B OakD\Build -DOpenCV_DIR='%CD%/opencv/Build/'
	msbuild.exe OakD/Build/Cam_Oak-D.sln /p:Configuration=Release
)

if not exist zed-sdk (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/stereolabs/zed-sdk.git"
	echo "Goto: https://www.stereolabs.com/developers/release/3.8/ and download the .exe"
	echo "Install the StereoLabs SDK - it may also download and install CUDA if not already present."
	echo "Set CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.7"
	set /p ok="And hit enter when install is complete:"
)

:: Once done, exit the batch file -- skips executing the errorNoPython section
goto:eof


:errorNoPython
echo.
echo.
echo.
echo.
python -c "import sys; print(sys.prefix)"
echo Error^: Python not installed.  Download and install Python and try again.