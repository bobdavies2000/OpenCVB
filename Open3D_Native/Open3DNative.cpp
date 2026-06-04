#include <Windows.h>
#include "Open3DPragmaLibs.h"
#include <open3d/Open3DConfig.h>

extern "C" __declspec(dllexport)
void Open3D_PrintVersion()
{
    open3d::PrintOpen3DVersion();
}
