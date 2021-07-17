if exist eigen (rmdir eigen /s)
"c:\Program Files\Git\bin\git.exe" clone "https://gitlab.com/libeigen/eigen.git"
"C:\Program Files\CMake\bin\Cmake.exe" -DCMAKE_CONFIGURATION_TYPES=Debug;Release -S eigen -B eigen/Build