@echo off
setlocal
cd /d "%~dp0..\.."

echo === Open3D alpha-shape C++ example (local OpenCVB\Open3D C++ library) ===
if exist "bin\AlphaShapeExample.exe" (
    bin\AlphaShapeExample.exe %*
) else (
    echo Build Open3D_AlphaShapeExample ^(Release^|x64^) in OpenCVB.sln first.
)

echo.
echo === Open3D alpha-shape Python example (pip open3d for visualization) ===
python "%~dp0triangle_mesh_from_point_cloud_alpha_shapes.py"
endlocal
