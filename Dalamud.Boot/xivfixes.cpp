#include "pch.h"

#include "xivfixes.h"

#include "hooks.h"
#include "utils.h"

using TFnGetInputDeviceManager = void* ();
static TFnGetInputDeviceManager* GetGetInputDeviceManager(HWND hwnd) {
    static TFnGetInputDeviceManager* pCached = nullptr;
    if (pCached)
        return pCached;

    char szClassName[256];
    GetClassNameA(hwnd, szClassName, static_cast<int>(sizeof szClassName));

    WNDCLASSEXA wcx{};
    GetClassInfoExA(g_hGameInstance, szClassName, &wcx);
    const auto match = utils::signature_finder()
        .look_in(g_hGameInstance, ".text")
        .look_for_hex("41 81 fe 19 02 00 00 0f 87 ?? ?? 00 00 0f 84 ?? ?? 00 00")
        .find_one();

    auto ptr = match.data() + match.size() + *reinterpret_cast<const int*>(match.data() + match.size() - 4);
    ptr += 4;  // CMP RBX, 0x7
    ptr += 2;  // JNZ <giveup>
    ptr += 7;  // MOV RCX, <Framework::Instance>
    ptr += 3;  // TEST RCX, RCX
    ptr += 2;  // JZ <giveup>
    ptr += 5;  // CALL <GetInputDeviceManagerInstance()>
    ptr += *reinterpret_cast<const int*>(ptr - 4);

    return pCached = reinterpret_cast<TFnGetInputDeviceManager*>(ptr);
}

void xivfixes::prevent_devicechange_crashes(bool bApply) {
    static std::optional<hooks::import_hook<decltype(CreateWindowExA)>> s_hookCreateWindowExA;
    static std::optional<hooks::wndproc_hook> s_hookWndProc;

    if (bApply) {
        s_hookCreateWindowExA.emplace("user32.dll", "CreateWindowExA", 0);
        s_hookCreateWindowExA->set_detour([](DWORD dwExStyle, LPCSTR lpClassName, LPCSTR lpWindowName, DWORD dwStyle, int X, int Y, int nWidth, int nHeight, HWND hWndParent, HMENU hMenu, HINSTANCE hInstance, LPVOID lpParam)->HWND {
            const auto hWnd = s_hookCreateWindowExA->call_original(dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu, hInstance, lpParam);

            if (!hWnd
                || hInstance != g_hGameInstance
                || 0 != strcmp(lpClassName, "FFXIVGAME"))
                return hWnd;

            std::cerr << std::format("[prevent_devicechange_crashes] CreateWindow(0x{:08X}, \"{}\", \"{}\", 0x{:08X}, {}, {}, {}, {}, 0x{:X}, 0x{:X}, 0x{:X}, 0x{:X}) called; unhooking CreateWindowExA and hooking WndProc.\n",
                dwExStyle, lpClassName, lpWindowName, dwStyle, X, Y, nWidth, nHeight, reinterpret_cast<size_t>(hWndParent), reinterpret_cast<size_t>(hMenu), reinterpret_cast<size_t>(hInstance), reinterpret_cast<size_t>(lpParam));

            s_hookCreateWindowExA.reset();

            s_hookWndProc.emplace(hWnd);
            s_hookWndProc->set_detour([](HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam) -> LRESULT {
                if (uMsg == WM_DEVICECHANGE && wParam == DBT_DEVNODES_CHANGED) {
                    if (!GetGetInputDeviceManager(hWnd)()) {
                        std::cerr << std::format("[prevent_devicechange_crashes] WndProc(0x{:X}, WM_DEVICECHANGE, DBT_DEVNODES_CHANGED, {}) called but the game does not have InputDeviceManager initialized; doing nothing.\n", lParam);
                        return 0;
                    }
                }

                return s_hookWndProc->call_original(hWnd, uMsg, wParam, lParam);
            });

            return hWnd;
        });

    } else {
        s_hookCreateWindowExA.reset();

        // This will effectively revert any other WndProc alterations, including Dalamud.
        s_hookWndProc.reset();
    }
}

void xivfixes::disable_game_openprocess_access_check(bool bApply) {
    static std::optional<hooks::import_hook<decltype(OpenProcess)>> hook;

    if (bApply) {
        hook.emplace("kernel32.dll", "OpenProcess", 0);
        hook->set_detour([](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
            if (dwProcessId == GetCurrentProcessId()) {
                std::cerr << std::format("[xivfixes:disable_game_openprocess_access_check] OpenProcess(0{:08X}, {}, {}) was invoked by thread {}.\n", dwDesiredAccess, bInheritHandle, dwProcessId, GetCurrentThreadId());

                // Prevent game from feeling unsafe that it restarts
                if (dwDesiredAccess & PROCESS_VM_WRITE) {
                    std::cerr << "Returning failure with last error code set to ERROR_ACCESS_DENIED(5).\n";
                    SetLastError(ERROR_ACCESS_DENIED);
                    return {};
                }
            }

            return hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
        });

    } else
        hook.reset();
}

void xivfixes::redirect_openprocess(bool bApply) {
    static std::optional<hooks::export_hook<decltype(OpenProcess)>> hook;

    if (bApply) {
        hook.emplace(::OpenProcess);
        hook->set_detour([](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
            if (dwProcessId == GetCurrentProcessId()) {
                std::cerr << std::format("[xivfixes:redirect_openprocess] OpenProcess(0{:08X}, {}, {}) was invoked by thread {}. Redirecting to DuplicateHandle.\n", dwDesiredAccess, bInheritHandle, dwProcessId, GetCurrentThreadId());

                if (HANDLE res; DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &res, dwDesiredAccess, bInheritHandle, 0))
                    return res;

                return {};
            }
            return hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
        });

    } else
        hook.reset();
}
