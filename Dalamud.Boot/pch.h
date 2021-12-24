// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// Exclude rarely-used stuff from Windows headers
#define WIN32_LEAN_AND_MEAN 

// Windows Header Files
#include <windows.h>
#include <DbgHelp.h>
#include <PathCch.h>
#include <Psapi.h>
#include <Shlobj.h>
#include <TlHelp32.h>

// C++ Standard Libraries
#include <cassert>
#include <cstdio>
#include <filesystem>
#include <format>
#include <fstream>
#include <span>
#include <mutex>

// https://github.com/dotnet/coreclr
#include "..\lib\CoreCLR\CoreCLR.h"
#include "..\lib\CoreCLR\boot.h"

// Commonly used macros
#define DllExport extern "C" __declspec(dllexport)

// Global variables
extern HMODULE g_hModule;
extern std::optional<CoreCLR> g_clr;

#endif //PCH_H
