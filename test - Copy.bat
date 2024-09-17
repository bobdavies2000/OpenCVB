
@echo off
:: Check for Python Installation
python --version 2>NUL
if errorlevel 1 goto errorNoPython

:: Reaching here means Python is installed.
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64

<<<<<<< HEAD
if not exist zed-c-api\Build (
	if exist "c:\Program Files\NVIDIA GPU Computing Toolkit\CUDA" (
		"C:\Program Files\CMake\bin\Cmake.exe" -S zed-c-api -B zed-c-api/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release
		msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Debug
		msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Release
	)
=======
if not exist zed-c-api\Build and exist "c:\Program Files\NVIDIA GPU Computing Toolkit\CUDA" (
	"C:\Program Files\CMake\bin\Cmake.exe" -S zed-c-api -B zed-c-api/Build -DCMAKE_CONFIGURATION_TYPES=Debug;Release
	msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Debug
	msbuild.exe zed-c-api/Build/C.sln /p:Configuration=Release
>>>>>>> 24afe0302d940764db9942e3a5f979f2c7ff3a13
)