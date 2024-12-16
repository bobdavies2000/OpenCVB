@echo off
:: Check for Python Installation
python --version 2>NUL
if errorlevel 1 goto errorNoPython

:: Reaching here means Python is installed.
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64

echo "OpenCVB requires that .Net Framework 3.5 be installed."
echo "You need to check before installing OpenCVB."
echo "When you close the optionalfeatures window, OpenCVB will install."
start /wait optionalfeatures.exe
@echo off

if not exist librealsense (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"
)

if not exist OpenCV (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv.git"
	cd OpenCV
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib.git"	
	cd ..\
) 

if not exist OrbbecSDK (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/orbbec/OrbbecSDK.git"
) 

if not exist OrbbecSDK_CSharp (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/orbbec/OrbbecSDK_CSharp.git"
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

if not exist librealsense\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -DBUILD_CSHARP_BINDINGS=ON -S librealsense -B librealsense/Build
	msbuild.exe librealsense/Build/realsense2.sln /p:Configuration=Debug
	msbuild.exe librealsense/Build/realsense2.sln /p:Configuration=Release
	msbuild.exe librealsense/Build/wrappers/RealsenseWrappers.sln /p:Configuration=Release
	msbuild.exe librealsense/Build/wrappers/RealsenseWrappers.sln /p:Configuration=Debug
)

if not exist OrbbecSDK_CSharp\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S OrbbecSDK_CSharp -B OrbbecSDK_CSharp/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release; -DCMAKE_INSTALL_PREFIX=OrbbecSDK/Build
	msbuild.exe OrbbecSDK_CSharp/Build/ob_csharp.sln /p:Configuration=Debug
	msbuild.exe OrbbecSDK_CSharp/Build/ob_csharp.sln /p:Configuration=Release
)

if not exist Azure-Kinect-Sensor-SDK\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -DOpenCV_DIR=OpenCV/Build -DCMAKE_BUILD_TYPE=Debug -S Azure-Kinect-Sensor-SDK -B Azure-Kinect-Sensor-SDK/Build
	msbuild.exe Azure-Kinect-Sensor-SDK/Build/k4a.sln /p:Configuration=Debug
	msbuild.exe Azure-Kinect-Sensor-SDK/Build/k4a.sln /p:Configuration=Release
)

if not exist OakD\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S OakD -B OakD\Build -DOpenCV_DIR='%CD%/opencv/Build/'
	msbuild.exe OakD/Build/Cam_Oak-D.sln /p:Configuration=Release
	msbuild.exe OakD/Build/Cam_Oak-D.sln /p:Configuration=Debug
)

if not exist zed-c-api\Build (
	if exist "c:\Program Files\NVIDIA GPU Computing Toolkit\CUDA" (
		"C:\Program Files\CMake\bin\Cmake.exe" -S zed-c-api -B zed-c-api/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release
		msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Debug
		msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Release
	)
)

if not exist zed-csharp-api\StereoLabs.zed\Build (
	if exist "c:\Program Files\NVIDIA GPU Computing Toolkit\CUDA" (
		"C:\Program Files\CMake\bin\Cmake.exe" -S zed-csharp-api/StereoLabs.zed/ -B zed-csharp-api/StereoLabs.zed/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release
		msbuild.exe zed-csharp-api/StereoLabs.zed/Build/Stereolabs.zed.sln /p:Configuration=Debug
		msbuild.exe zed-csharp-api/StereoLabs.zed/Build/Stereolabs.zed.sln /p:Configuration=Release

		rem "This is really just a note to myself about the zed-csharp-api project:"
		rem "For zed-csharp-api, you need to change the 'resolution' variable to 'resolutionStruct'"
		rem "Accessing zed-csharp-api from VB.Net won't work because it is not case sensitive."
		rem "C# access will work because it is case sensitive."
		rem "There are 3 variables that map to 'Resolution' and the zed_camera.vb interface fails to compile."
		rem "There are also 2 missing commas - errors will show up.  How did they miss that?"
		rem "Also change the resolution variable to CURResolution."
		rem "And you can remove the ZERO_CHECK reference to remove the warning."
	)
)

echo "Goto: https://download.stereolabs.com/zedsdk/4.1/cu121/win and install Stereolabs SDK with CUDA 12"
echo "To turn off StereoLabs support, edit OpenCVB's 'camera/cameraDefines.hpp' and comment out StereoLabs."
echo "Download from here for StereoLabs SDK 4.1 with CUDA 12: https://download.stereolabs.com/zedsdk/4.1/cu121/win"
echo "StereoLabs SDK install may also download and install CUDA if not already present."
echo "Set CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.3"
SET /P ok="And hit enter after reading the above messages."

:: Once done, exit the batch file -- skips executing the errorNoPython section
goto:eof


:errorNoPython
echo.
echo.
echo.
echo.
python -c "import sys; print(sys.prefix)"
echo Error^: Python not installed.  Download and install Python and try again.