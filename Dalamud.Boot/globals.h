#pragma once

#include <optional>

#include <Windows.h>

#include <CoreCLR.h>

extern HMODULE g_hModule;
extern HINSTANCE g_hGameInstance;
extern std::optional<CoreCLR> g_clr;
