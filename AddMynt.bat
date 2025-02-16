@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat" x86_amd64

if not exist Mynt (
	echo "Goto: https://github.com/slightech/MYNT-EYE-S-SDK.git and download the .exe"
	echo "Install the Mynt SDK in %cd%/Mynt"
	set /p ok="And hit enter when install is complete:"
)

cd MYNT-EYE-S-SDK/samples
"C:\Program Files\CMake\bin\Cmake.exe" -DOpenCV_Dir=../3rdparty/OpenCV/build/x64/vc15/lib -B Build
msbuild.exe Build/mynteye_samples.sln /p:Configuration=Debug
msbuild.exe Build/mynteye_samples.sln /p:Configuration=Release
