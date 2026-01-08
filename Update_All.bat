@echo off
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64

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

if not exist OakD\depthai-core (
	cd OakD
	git.exe clone "https://github.com/luxonis/depthai-core.git"
	cd depthai-core
	git.exe submodule update --init --recursive
	cd ..\..\
)

if not exist opencv\Build (
	cmake -S OpenCV -B OpenCV/Build -G "Visual Studio 18 2026" -A x64 -DBUILD_PERF_TESTS=NO -DBUILD_TESTS=NO -DBUILD_opencv_python_tests=NO -DOPENCV_EXTRA_MODULES_PATH=OpenCV/OpenCV_Contrib/Modules
	msbuild.exe OpenCV/Build/OpenCV.slnx /p:Configuration=Debug
	msbuild.exe OpenCV/Build/OpenCV.slnx /p:Configuration=Release
	cd OpenCV/Build
	cmake --install .
	cd ../../
)

if not exist librealsense\Build (
	cmake -DBUILD_CSHARP_BINDINGS=ON -S librealsense -B librealsense/Build
	msbuild.exe librealsense/Build/realsense2.slnx /p:Configuration=Debug
	msbuild.exe librealsense/Build/realsense2.slnx /p:Configuration=Release
	msbuild.exe librealsense/Build/wrappers/RealsenseWrappers.slnx /p:Configuration=Release
	msbuild.exe librealsense/Build/wrappers/RealsenseWrappers.slnx /p:Configuration=Debug
)

if not exist OrbbecSDK_CSharp\Build (
	cmake -S OrbbecSDK_CSharp -B OrbbecSDK_CSharp/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release; -DCMAKE_INSTALL_PREFIX=OrbbecSDK/Build
	msbuild.exe OrbbecSDK_CSharp/Build/ob_csharp.slnx /p:Configuration=Debug
	msbuild.exe OrbbecSDK_CSharp/Build/ob_csharp.slnx /p:Configuration=Release
)

if not exist OakD\depthai-core\Build (
	echo Building Oak-D camera support...
	mkdir OakD\depthai-core\Build
	cd OakD\depthai-core\Build
	cmake .. -DCMAKE_BUILD_TYPE=Debug;Release -DDEPTHAI_BUILD_EXAMPLES=ON  -DDEPTHAI_SANITIZE=OFF -DOpenCV_DIR=..\..\..\OpenCV\Build
	cmake --build . --config Release
	cmake --build . --config Debug
	echo Oak-D camera support built successfully.
	cd ..\..\..\
)

echo.
echo ========================================
echo Camera Support Installation Summary
echo ========================================
echo.
echo Oak-D Camera (Luxonis):
echo   - depthai-core is cloned to OakD/depthai-core
echo   - Cam_Oak-D.dll is built and copied to the bin folder
echo   - No additional SDK installation required
echo.
echo StereoLabs ZED Camera:
echo   - Goto: https://download.stereolabs.com/zedsdk/4.1/cu121/win
echo   - Install Stereolabs SDK with CUDA 12
echo   - StereoLabs SDK install may also download and install CUDA if not already present.
echo   - Set CUDA_PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.X
echo   - To disable StereoLabs support, edit OpenCVB's 'camera/cameraDefines.hpp'
echo.
echo Intel RealSense Camera:
echo   - librealsense is built automatically with C# bindings
echo.
echo Orbbec Gemini Camera:
echo   - OrbbecSDK and OrbbecSDK_CSharp are built automatically
echo.
SET /P ok="Press Enter to continue after reading the above messages."