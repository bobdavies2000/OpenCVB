#include <Windows.h>
#include <objbase.h>
#include <cstring>
#include <string>
#include "Open3DPragmaLibs.h"
#include <open3d/Open3DConfig.h>

extern "C" __declspec(dllexport)
char* Open3D_PrintVersion()
{
    const std::string version = std::string("Open3D ") + OPEN3D_VERSION;
    const size_t byteCount = version.size() + 1;
    char* buffer = static_cast<char*>(CoTaskMemAlloc(byteCount));
    if (buffer != nullptr) {
        std::memcpy(buffer, version.c_str(), byteCount);
    }
    return buffer;
}
