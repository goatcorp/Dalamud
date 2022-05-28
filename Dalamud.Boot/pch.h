// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H

// Exclude rarely-used stuff from Windows headers
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX

// Windows Header Files
#include <windows.h>
#include <DbgHelp.h>
#include <PathCch.h>
#include <Psapi.h>
#include <Shlobj.h>
#include <TlHelp32.h>
#include <Dbt.h>

// MSVC Compiler Intrinsic
#include <intrin.h>

// C++ Standard Libraries
#include <cassert>
#include <cstdio>
#include <filesystem>
#include <format>
#include <fstream>
#include <functional>
#include <ranges>
#include <span>
#include <mutex>
#include <type_traits>

// https://www.akenotsuki.com/misc/srell/en/
#include "../lib/srell3_009/single-header/srell.hpp"

// https://github.com/Nomade040/nmd
#include "../lib/Nomade040-nmd/nmd_assembly.h"

// https://github.com/dotnet/coreclr
#include "../lib/CoreCLR/CoreCLR.h"
#include "../lib/CoreCLR/boot.h"

// Commonly used macros
#define DllExport extern "C" __declspec(dllexport)

// Global variables
extern HMODULE g_hModule;
extern HINSTANCE g_hGameInstance;
extern std::optional<CoreCLR> g_clr;

#endif //PCH_H
