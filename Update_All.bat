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

if not exist OrbbecSDK (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/orbbec/OrbbecSDK.git"
) 

if not exist zed-c-api (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/stereolabs/zed-c-api"
) 

if not exist OrbbecSDK_CSharp (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/orbbec/OrbbecSDK_CSharp.git"
) 

if not exist zed-csharp-api (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/stereolabs/zed-csharp-api"
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
	"C:\Program Files\CMake\bin\Cmake.exe" -DBUILD_CSHARP_BINDINGS=ON -DBUILD_SHARED_LIBS=ON -S librealsense -B librealsense/Build
	msbuild.exe librealsense/Build/realsense2.sln /p:Configuration=Debug
	msbuild.exe librealsense/Build/realsense2.sln /p:Configuration=Release
)

if not exist zed-c-api\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S zed-c-api -B zed-c-api/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release
	msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Debug
	msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Release
)

if not exist OrbbecSDK\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S OrbbecSDK -B OrbbecSDK/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release; -DOpenCVDir=opencv/Build -DCMAKE_INSTALL_PREFIX=OrbbecSDK/Build
	msbuild.exe OrbbecSDK/Build/OrbbecSDK.sln /p:Configuration=Debug
	msbuild.exe OrbbecSDK/Build/OrbbecSDK.sln /p:Configuration=Release
)

if not exist OrbbecSDK_CSharp\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S OrbbecSDK_CSharp -B OrbbecSDK_CSharp/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release; -DCMAKE_INSTALL_PREFIX=OrbbecSDK_CSharp/Build
	msbuild.exe OrbbecSDK/Build/OrbbecSDK.sln /p:Configuration=Debug
	msbuild.exe OrbbecSDK/Build/OrbbecSDK.sln /p:Configuration=Release
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

if not exist zed-csharp-api\Stereolabs.zed\Build (
	"C:\Program Files\CMake\bin\Cmake.exe" -S Stereolabs.zed -B Stereolabs.zed\Build  -DCMAKE_CONFIGURATION_TYPES=Debug;Release;
	msbuild.exe zed-csharp-api/Stereolabs.zed/Build/Cam_Oak-D.sln /p:Configuration=Release
	msbuild.exe zed-csharp-api/Stereolabs.zed/Build/Cam_Oak-D.sln /p:Configuration=Debug
)

echo "Goto: https://download.stereolabs.com/zedsdk/4.1/cu121/win and install Stereolabs SDK with CUDA 12"
echo "Goto: https://download.stereolabs.com/zedsdk/4.1/cu121/win and install Stereolabs SDK with CUDA 12"
echo "Goto: https://download.stereolabs.com/zedsdk/4.1/cu121/win and install Stereolabs SDK with CUDA 12"
echo "Goto: https://download.stereolabs.com/zedsdk/4.1/cu121/win and install Stereolabs SDK with CUDA 12"
echo "If you compile OpenCVB and see 'SL/Camera.hpp is missing', it means the stereolabs download is missing..."
echo "If you compile OpenCVB and see 'SL/Camera.hpp is missing', it means the stereolabs download is missing..."
echo "If you compile OpenCVB and see 'SL/Camera.hpp is missing', it means the stereolabs download is missing..."
echo "If you compile OpenCVB and see 'SL/Camera.hpp is missing', it means the stereolabs download is missing..."
echo "To turn off StereoLabs support, edit OpenCVB's 'camera/cameraDefines.hpp' and comment out StereoLabs."
echo "To turn off StereoLabs support, edit OpenCVB's 'camera/cameraDefines.hpp' and comment out StereoLabs."
echo "To turn off StereoLabs support, edit OpenCVB's 'camera/cameraDefines.hpp' and comment out StereoLabs."
echo "To turn off StereoLabs support, edit OpenCVB's 'camera/cameraDefines.hpp' and comment out StereoLabs."
echo "Download from here for StereoLabs SDK 4.1 with CUDA 12: https://download.stereolabs.com/zedsdk/4.1/cu121/win"
echo "Download from here for StereoLabs SDK 4.1 with CUDA 12: https://download.stereolabs.com/zedsdk/4.1/cu121/win"
echo "Download from here for StereoLabs SDK 4.1 with CUDA 12: https://download.stereolabs.com/zedsdk/4.1/cu121/win"
echo "Download from here for StereoLabs SDK 4.1 with CUDA 12: https://download.stereolabs.com/zedsdk/4.1/cu121/win"


echo "StereoLabs SDK install may also download and install CUDA if not already present."
echo "Set CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.3" <<< or whatever version was downloaded.
set /p ok="And hit enter after reading the above messages."

:: Once done, exit the batch file -- skips executing the errorNoPython section
goto:eof


:errorNoPython
echo.
echo.
echo.
echo.
python -c "import sys; print(sys.prefix)"
echo Error^: Python not installed.  Download and install Python and try again.