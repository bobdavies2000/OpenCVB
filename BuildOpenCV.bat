 @echo off
:: Check for Python Installation
python --version 2>NUL
if errorlevel 1 goto errorNoPython

:: Reaching here means Python is installed.
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64


rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv.git"
rem cd OpenCV
rem "c:\Program Files\Git\bin\git.exe" clone "https://github.com/opencv/opencv_contrib.git"	
rem cd ..\

"C:\Program Files\CMake\bin\Cmake.exe" "C:\Program Files\CMake\bin\Cmake.exe" -D BUILD_SHARED_LIBS=ON -D CMAKE_BUILD_TYPE=Release -D WITH_FFMPEG=ON -DBUILD_PERF_TESTS=OFF -DBUILD_TESTS=OFF -S OpenCV -B OpenCV/Build

msbuild.exe OpenCV/Build/OpenCV.sln /p:Configuration=Release

cd OpenCV/Build
"C:\Program Files\CMake\bin\Cmake.exe" --install .