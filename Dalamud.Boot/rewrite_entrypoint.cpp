#include "pch.h"

DllExport DWORD WINAPI Initialize(LPVOID lpParam);

struct RewrittenEntryPointParameters {
    LPVOID pAllocation;
    LPVOID pEntrypoint;
    char* pEntrypointBytes;
    size_t entrypointLength;
    LPSTR pLoadInfo;
    HANDLE hMainThread;
    HANDLE hMainThreadContinue;
};

void write_process_memory_or_throw(HANDLE hProcess, void* pAddress, const void* data, size_t len) {
    SIZE_T written = 0;
    if (!WriteProcessMemory(hProcess, pAddress, data, len, &written))
        throw std::runtime_error("WriteProcessMemory failure");
    if (written != len)
        throw std::runtime_error("WriteProcessMemory written size does not match requested size");
}

/// @brief Find the game main window.
/// @return Handle to the game main window, or nullptr if it doesn't exist (yet).
HWND try_find_game_window() {
    HWND hwnd = nullptr;
    while ((hwnd = FindWindowExW(nullptr, hwnd, L"FFXIVGAME", nullptr))) {
        DWORD pid;
        GetWindowThreadProcessId(hwnd, &pid);

        if (pid == GetCurrentProcessId() && IsWindowVisible(hwnd))
            break;
    }
    return hwnd;
}

void wait_for_game_window() {
    HWND game_window;
    while (!(game_window = try_find_game_window())) {
        WaitForInputIdle(GetCurrentProcess(), INFINITE);
        Sleep(100);
    };
    SendMessageW(game_window, WM_NULL, 0, 0);
}

/// @brief Entry point function "called" instead of game's original main entry point.
/// @param params Parameters set up from RewriteRemoteEntryPoint.
DllExport void WINAPI RewrittenEntryPoint(RewrittenEntryPointParameters& params) {
    params.hMainThreadContinue = CreateEventW(nullptr, true, false, nullptr);
    if (!params.hMainThreadContinue)
        ExitProcess(-1);

    // Do whatever the work in a separate thread to minimize the stack usage at this context,
    // as this function really should have been a naked procedure but __declspec(naked) isn't supported in x64 version of msvc.
    params.hMainThread = CreateThread(nullptr, 0, [](void* p) -> DWORD {
        try {
            std::string loadInfo;
            {
                auto& params = *reinterpret_cast<RewrittenEntryPointParameters*>(p);

                // Restore original entry point.
                // Use WriteProcessMemory instead of memcpy to avoid having to fiddle with VirtualProtect.
                write_process_memory_or_throw(GetCurrentProcess(), params.pEntrypoint, params.pEntrypointBytes, params.entrypointLength);

                // Make a copy of load info, as the whole params will be freed after this code block.
                loadInfo = params.pLoadInfo;

                // Let the game initialize.
                SetEvent(params.hMainThreadContinue);
            }

            wait_for_game_window();

            Initialize(&loadInfo[0]);
            return 0;
        }
        catch (const std::exception& e) {
            MessageBoxA(nullptr, std::format("Failed to load Dalamud.\n\nError: {}", e.what()).c_str(), "Dalamud.Boot", MB_OK | MB_ICONERROR);
            ExitProcess(-1);
        }
        }, &params, 0, nullptr);
    if (!params.hMainThread)
        ExitProcess(-1);

    CloseHandle(params.hMainThread);
    WaitForSingleObject(params.hMainThreadContinue, INFINITE);
    CloseHandle(params.hMainThreadContinue);
    VirtualFree(params.pAllocation, 0, MEM_RELEASE);
}
