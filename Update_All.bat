@echo off
call "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64

@echo off

if not exist librealsense (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/IntelRealSense/librealsense"
)


if not exist zed-sdk (
	"c:\Program Files\Git\bin\git.exe" clone "https://github.com/stereolabs/zed-sdk.git"
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

if not exist Open3D (
	"c:\Program Files\Git\bin\git.exe" clone --recursive "https://github.com/isl-org/Open3D.git"
) else (
	cd Open3D
	"c:\Program Files\Git\bin\git.exe" submodule update --init --recursive
	cd ..\
)

if not exist Open3D\Install (
	echo Building Open3D ^(C++ library, may take a long time^)...
	cmake -S Open3D -B Open3D/Build -G "Visual Studio 18 2026" -A x64 ^
		-DCMAKE_INSTALL_PREFIX="%CD%\Open3D\Install" ^
		-DBUILD_PYTHON_MODULE=OFF ^
		-DBUILD_EXAMPLES=OFF ^
		-DBUILD_UNIT_TESTS=OFF ^
		-DBUILD_GUI=OFF ^
		-DBUILD_WEBRTC=OFF
	cmake --build Open3D/Build --config Release --target INSTALL
	cmake --build Open3D/Build --config Debug --target INSTALL
	echo Open3D C++ installed to %CD%\Open3D\Install
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

if not exist zed-sdk\Build (
	cmake -S OrbbecSDK_CSharp -B OrbbecSDK_CSharp/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release; -DCMAKE_INSTALL_PREFIX=OrbbecSDK/Build
	msbuild.exe OrbbecSDK_CSharp/Build/ob_csharp.slnx /p:Configuration=Debug
	msbuild.exe OrbbecSDK_CSharp/Build/ob_csharp.slnx /p:Configuration=Release
)

if not exist OakD\depthai-core\Build (
	echo Building Oak-D camera support...
	mkdir OakD\depthai-core\Build
	cd OakD\depthai-core\Build
	cmake .. -DCMAKE_BUILD_TYPE=Release -DDEPTHAI_BUILD_EXAMPLES=OFF -DBUILD_SHARED_LIBS=ON -DDEPTHAI_SANITIZE=OFF -DOpenCV_DIR=..\..\..\OpenCV\Build
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
echo   - should not require any additional setup
echo.
echo StereoLabs ZED Camera:
echo   - Run: powershell -ExecutionPolicy Bypass -File ZED_SDK\Install-ZedSdk.ps1
echo   - Or: https://download.stereolabs.com/zedsdk/5.3/cu12/win
echo   - OpenCVB uses ZED SDK 5.3 + Stereolabs.zed NuGet 5.3 (CUDA 12 build matches CUDA 12.x)
echo.
echo Intel RealSense Camera:
echo   - librealsense is built automatically with C# bindings
echo.
echo Orbbec Gemini Camera:
echo   - OrbbecSDK and OrbbecSDK_CSharp are built automatically
echo.
echo Open3D:
echo   - C++: built to Open3D\Install ^(use -DCMAKE_PREFIX_PATH=...\Open3D\Install in CMake^)
echo   - Python: pip install open3d ^(if python is on PATH^)
echo   - Docs: https://www.open3d.org/docs/latest/compilation.html
echo.
SET /P ok="Press Enter to continue after reviewing the log."