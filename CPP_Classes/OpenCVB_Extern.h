#pragma once

#ifdef STATIC_EXTERNS
#define extern "C" __declspec(dllexport) static
#else
#define extern "C" __declspec(dllexport) extern "C" __declspec(dllexport)
#endif