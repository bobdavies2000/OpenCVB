#pragma once

#ifdef STATIC_EXTERNS
#define VB_EXTERN static
#else
#define VB_EXTERN extern "C" __declspec(dllexport)
#endif