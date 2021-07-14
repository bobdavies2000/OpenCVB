if exist depthai-core (rmdir depthai-core /s)
"c:\Program Files\Git\bin\git.exe" clone "https://github.com/luxonis/depthai-core"
cd depthai-core
git submodule update --init --recursive 
cd ..\
set OpenCV_DIR=opencv\Build
"C:\Program Files\CMake\bin\Cmake.exe" -DOpenCV_DIR=opencv\Build\ -DCMAKE_CONFIGURATION_TYPES=Debug;Release -DDEPTHAI_BUILD_EXAMPLES=ON -S depthai-core -B depthai-core/Build
msbuild.exe depthai-core/Build/depthai.sln /p:Configuration=Debug
msbuild.exe depthai-core/Build/depthai.sln /p:Configuration=Release