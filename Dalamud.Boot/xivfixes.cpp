#include "pch.h"

#include "xivfixes.h"

#include "DalamudStartInfo.h"
#include "hooks.h"
#include "logging.h"
#include "utils.h"

template<typename T>
static std::span<T> assume_nonempty_span(std::span<T> t, const char* descr) {
    if (t.empty())
        throw std::runtime_error(std::format("Unexpected empty span found: {}", descr));
    return t;
}
void xivfixes::unhook_dll(bool bApply) {
    static const auto LogTag = "[xivfixes:unhook_dll]";
    static const auto LogTagW = L"[xivfixes:unhook_dll]";

    if (!bApply)
        return;

    const auto mods = utils::loaded_module::all_modules();

    const auto test_module = [&](size_t i, const utils::loaded_module & mod) {
        std::filesystem::path path;
        try {
            path = mod.path();
            logging::I("{} [{}/{}] Module 0x{:X} ~ 0x{:X} (0x{:X}): \"{}\"", LogTagW, i + 1, mods.size(), mod.address_int(), mod.address_int() + mod.image_size(), mod.image_size(), path.wstring());
        } catch (const std::exception& e) {
            logging::W("{} [{}/{}] Module 0x{:X}: Failed to resolve path: {}", LogTag, i + 1, mods.size(), mod.address_int(), e.what());
            return;
        }

        const auto moduleName = unicode::convert<std::string>(path.filename().wstring());

        std::vector<char> buf;
        std::string formatBuf;
        try {
            const auto& sectionHeader = mod.section_header(".text");
            const auto section = assume_nonempty_span(mod.span_as<char>(sectionHeader.VirtualAddress, sectionHeader.Misc.VirtualSize), ".text[VA:VA+VS]");
            auto hFsDllRaw = CreateFileW(path.c_str(), GENERIC_READ, FILE_SHARE_READ, nullptr, OPEN_EXISTING, 0, nullptr);
            if (hFsDllRaw == INVALID_HANDLE_VALUE) {
                logging::W("{} Module loaded in current process but could not open file: Win32 error {}", LogTag, GetLastError());
                return;
            }
            auto hFsDll = std::unique_ptr<void, decltype(CloseHandle)*>(hFsDllRaw, &CloseHandle);

            buf.resize(section.size());
            SetFilePointer(hFsDll.get(), sectionHeader.PointerToRawData, nullptr, FILE_CURRENT);
            if (DWORD read{}; ReadFile(hFsDll.get(), &buf[0], static_cast<DWORD>(buf.size()), &read, nullptr)) {
                if (read < section.size_bytes()) {
                    logging::W("{} ReadFile: read {} bytes < requested {} bytes", LogTagW, read, section.size_bytes());
                    return;
                }
            } else {
                logging::I("{} ReadFile: Win32 error {}", LogTagW, GetLastError());
                return;
            }

            const auto doRestore = g_startInfo.BootUnhookDlls.contains(unicode::convert<std::string>(path.filename().u8string()));

            std::optional<utils::memory_tenderizer> tenderizer;
            for (size_t i = 0, instructionLength = 1, printed = 0; i < buf.size(); i += instructionLength) {
                if (section[i] == buf[i]) {
                    instructionLength = 1;
                    continue;
                }

                const auto rva = sectionHeader.VirtualAddress + i;
                nmd_x86_instruction instruction{};
                if (!nmd_x86_decode(&section[i], section.size() - i, &instruction, NMD_X86_MODE_64, NMD_X86_DECODER_FLAGS_ALL)) {
                    instructionLength = 1;
                    if (printed < 64) {
                        logging::W("{} {}+0x{:0X}: dd {:02X}", LogTag, moduleName, rva, static_cast<uint8_t>(section[i]));
                        printed++;
                    }
                } else {
                    instructionLength = instruction.length;
                    if (printed < 64) {
                        formatBuf.resize(128);
                        nmd_x86_format(&instruction, &formatBuf[0], reinterpret_cast<size_t>(&section[i]), NMD_X86_FORMAT_FLAGS_DEFAULT | NMD_X86_FORMAT_FLAGS_BYTES);
                        formatBuf.resize(strnlen(&formatBuf[0], formatBuf.size()));

                        const auto& directory = mod.data_directory(IMAGE_DIRECTORY_ENTRY_EXPORT);
                        const auto& exportDirectory = mod.ref_as<IMAGE_EXPORT_DIRECTORY>(directory.VirtualAddress);
                        const auto names = mod.span_as<DWORD>(exportDirectory.AddressOfNames, exportDirectory.NumberOfNames);
                        const auto ordinals = mod.span_as<WORD>(exportDirectory.AddressOfNameOrdinals, exportDirectory.NumberOfNames);
                        const auto functions = mod.span_as<DWORD>(exportDirectory.AddressOfFunctions, exportDirectory.NumberOfFunctions);

                        std::string resolvedExportName;
                        for (size_t j = 0; j < names.size(); ++j) {
                            std::string_view name;
                            if (const char* pcszName = mod.address_as<char>(names[j]); pcszName < mod.address() || pcszName >= mod.address() + mod.image_size()) {
                                if (IsBadReadPtr(pcszName, 256)) {
                                    logging::W("{} Name #{} points to an invalid address outside the executable. Skipping.", LogTag, j);
                                    continue;
                                }

                                name = std::string_view(pcszName, strnlen(pcszName, 256));
                                logging::W("{} Name #{} points to a seemingly valid address outside the executable: {}", LogTag, j, name);
                            }

                            if (ordinals[j] >= functions.size()) {
                                logging::W("{} Ordinal #{} points to function index #{} >= #{}. Skipping.", LogTag, j, ordinals[j], functions.size());
                                continue;
                            }

                            const auto rva = functions[ordinals[j]];
                            if (rva == &section[i] - mod.address()) {
                                resolvedExportName = std::format("[export:{}]", name);
                                break;
                            }
                        }

                        logging::W("{} {}+0x{:0X}{}: {}", LogTag, moduleName, rva, resolvedExportName, formatBuf);
                        printed++;
                    }
                }

                if (doRestore) {
                    if (!tenderizer)
                        tenderizer.emplace(section, PAGE_EXECUTE_READWRITE);
                    memcpy(&section[i], &buf[i], instructionLength);
                }
            }

            if (tenderizer)
                logging::I("{} Verification and overwriting complete.", LogTag);
            else if (doRestore)
                logging::I("{} Verification complete. Overwriting was not required.", LogTag);

        } catch (const std::exception& e) {
            logging::W("{} Error: {}", LogTag, e.what());
        }
    };

    // This is needed since try and __try cannot be used in the same function. Lambdas circumvent the limitation.
    const auto windows_exception_handler = [&]() {
        for (size_t i = 0; i < mods.size(); i++) {
            const auto& mod = mods[i];
            __try {
                test_module(i, mod);
            } __except (EXCEPTION_EXECUTE_HANDLER) {
                logging::W("{} Error: Access Violation", LogTag);
            }
        }
    };

    windows_exception_handler();
}

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
        .look_in(utils::loaded_module(g_hGameInstance), ".text")
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
    static const char* LogTag = "[xivfixes:prevent_devicechange_crashes]";

    // We hook RegisterClassExA, since if the game has already launched (inject mode), the very crash we're trying to fix cannot happen at that point.
    static std::optional<hooks::import_hook<decltype(RegisterClassExA)>> s_hookRegisterClassExA;
    static WNDPROC s_pfnGameWndProc = nullptr;

    // We're intentionally leaking memory for this one.
    static const auto s_pfnBinder = static_cast<WNDPROC>(VirtualAlloc(nullptr, 64, MEM_RESERVE | MEM_COMMIT, PAGE_EXECUTE_READWRITE));
    static const auto s_pfnAlternativeWndProc = static_cast<WNDPROC>([](HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam) -> LRESULT {
        if (uMsg == WM_DEVICECHANGE && wParam == DBT_DEVNODES_CHANGED) {
            if (!GetGetInputDeviceManager(hWnd)()) {
                logging::I("{} WndProc(0x{:X}, WM_DEVICECHANGE, DBT_DEVNODES_CHANGED, {}) called but the game does not have InputDeviceManager initialized; doing nothing.", LogTag, reinterpret_cast<size_t>(hWnd), lParam);
                return 0;
            }
        }

        return s_pfnGameWndProc(hWnd, uMsg, wParam, lParam);
    });

    if (bApply) {
        if (!g_startInfo.BootEnabledGameFixes.contains("prevent_devicechange_crashes")) {
            logging::I("{} Turned off via environment variable.", LogTag);
            return;
        }

        s_hookRegisterClassExA.emplace("user32.dll!RegisterClassExA (prevent_devicechange_crashes)", "user32.dll", "RegisterClassExA", 0);
        s_hookRegisterClassExA->set_detour([](const WNDCLASSEXA* pWndClassExA)->ATOM {
            // If this RegisterClassExA isn't initiated by the game executable, we do not handle it.
            if (pWndClassExA->hInstance != GetModuleHandleW(nullptr))
                return s_hookRegisterClassExA->call_original(pWndClassExA);

            // If this RegisterClassExA isn't about FFXIVGAME, the game's main window, we do not handle it.
            if (strncmp(pWndClassExA->lpszClassName, "FFXIVGAME", 10) != 0)
                return s_hookRegisterClassExA->call_original(pWndClassExA);

            // push qword ptr [rip+1]
            // ret
            // <pointer to new wndproc>
            memcpy(s_pfnBinder, "\xFF\x35\x01\x00\x00\x00\xC3", 7);
            *reinterpret_cast<void**>(reinterpret_cast<char*>(s_pfnBinder) + 7) = s_pfnAlternativeWndProc;
            
            s_pfnGameWndProc = pWndClassExA->lpfnWndProc;

            WNDCLASSEXA wndClassExA = *pWndClassExA;
            wndClassExA.lpfnWndProc = s_pfnBinder;
            return s_hookRegisterClassExA->call_original(&wndClassExA);
        });

        logging::I("{} Enable", LogTag);

    } else {
        if (s_hookRegisterClassExA) {
            logging::I("{} Disable RegisterClassExA", LogTag);
            s_hookRegisterClassExA.reset();
        }

        *reinterpret_cast<void**>(reinterpret_cast<char*>(s_pfnBinder) + 7) = s_pfnGameWndProc;
    }
}

static bool is_xivalex(const std::filesystem::path& dllPath) {
    DWORD verHandle = 0;
    std::vector<uint8_t> block;
    block.resize(GetFileVersionInfoSizeW(dllPath.c_str(), &verHandle));
    if (block.empty())
        return false;
    if (!GetFileVersionInfoW(dllPath.c_str(), 0, static_cast<DWORD>(block.size()), &block[0]))
        return false;
    struct LANGANDCODEPAGE {
        WORD wLanguage;
        WORD wCodePage;
    } * lpTranslate;
    UINT cbTranslate;
    if (!VerQueryValueW(&block[0],
        TEXT("\\VarFileInfo\\Translation"),
        reinterpret_cast<LPVOID*>(&lpTranslate),
        &cbTranslate)) {
        return false;
    }

    for (size_t i = 0; i < (cbTranslate / sizeof(struct LANGANDCODEPAGE)); i++) {
        wchar_t* buf = nullptr;
        UINT size = 0;
        if (!VerQueryValueW(&block[0],
            std::format(L"\\StringFileInfo\\{:04x}{:04x}\\FileDescription",
                lpTranslate[i].wLanguage,
                lpTranslate[i].wCodePage).c_str(),
            reinterpret_cast<LPVOID*>(&buf),
            &size)) {
            continue;
        }
        auto currName = std::wstring_view(buf, size);
        while (!currName.empty() && currName.back() == L'\0')
            currName = currName.substr(0, currName.size() - 1);
        if (currName.empty())
            continue;
        if (currName == L"XivAlexander Main DLL")
            return true;
    }
    return false;
}

static bool is_openprocess_already_dealt_with() {
    static const auto s_value = [] {
        for (const auto& mod : utils::loaded_module::all_modules()) {
            try {
                if (is_xivalex(mod.path()))
                    return true;
                
            } catch (...) {
                // pass
            }
        }
        return false;
    }();
    return s_value;
}

void xivfixes::disable_game_openprocess_access_check(bool bApply) {
    static const char* LogTag = "[xivfixes:disable_game_openprocess_access_check]";
    static std::optional<hooks::import_hook<decltype(OpenProcess)>> s_hook;

    if (bApply) {
        if (!g_startInfo.BootEnabledGameFixes.contains("disable_game_openprocess_access_check")) {
            logging::I("{} Turned off via environment variable.", LogTag);
            return;
        }
        if (is_openprocess_already_dealt_with()) {
            logging::I("{} Someone else already did it.", LogTag);
            return;
        }

        s_hook.emplace("kernel32.dll!OpenProcess (import, disable_game_openprocess_access_check)", "kernel32.dll", "OpenProcess", 0);
        s_hook->set_detour([](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
            logging::I("{} OpenProcess(0x{:08X}, {}, {}) was invoked by thread {}.", LogTag, dwDesiredAccess, bInheritHandle, dwProcessId, GetCurrentThreadId());

            if (dwProcessId == GetCurrentProcessId()) {
                // Prevent game from feeling unsafe that it restarts
                if (dwDesiredAccess & PROCESS_VM_WRITE) {
                    logging::I("{} Returning failure with last error code set to ERROR_ACCESS_DENIED(5).", LogTag);
                    SetLastError(ERROR_ACCESS_DENIED);
                    return {};
                }
            }

            return s_hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
        });

        logging::I("{} Enable", LogTag);
    } else {
        if (s_hook) {
            logging::I("{} Disable OpenProcess", LogTag);
            s_hook.reset();
        }
    }
}

void xivfixes::redirect_openprocess(bool bApply) {
    static const char* LogTag = "[xivfixes:redirect_openprocess]";
    static std::shared_ptr<hooks::base_untyped_hook> s_hook;
    static std::mutex s_silenceSetMtx;
    static std::set<DWORD> s_silenceSet;

    if (bApply) {
        if (!g_startInfo.BootEnabledGameFixes.contains("redirect_openprocess")) {
            logging::I("{} Turned off via environment variable.", LogTag);
            return;
        }
        if (is_openprocess_already_dealt_with()) {
            logging::I("{} Someone else already did it.", LogTag);
            return;
        }

        if (g_startInfo.BootDotnetOpenProcessHookMode == DalamudStartInfo::DotNetOpenProcessHookMode::ImportHooks) {
            auto hook = std::make_shared<hooks::global_import_hook<decltype(OpenProcess)>>("kernel32.dll!OpenProcess (global import, redirect_openprocess)", L"kernel32.dll", "OpenProcess");
            hook->set_detour([hook = hook.get()](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
                if (dwProcessId == GetCurrentProcessId()) {
                    if (s_silenceSet.emplace(GetCurrentThreadId()).second)
                        logging::I("{} OpenProcess(0x{:08X}, {}, {}) was invoked by thread {}. Redirecting to DuplicateHandle.", LogTag, dwDesiredAccess, bInheritHandle, dwProcessId, GetCurrentThreadId());

                    if (HANDLE res; DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &res, dwDesiredAccess, bInheritHandle, 0))
                        return res;

                    return {};
                }
                return hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
            });
            s_hook = std::dynamic_pointer_cast<hooks::base_untyped_hook>(std::move(hook));

            logging::I("{} Enable via import_hook", LogTag);

        } else {
            auto hook = std::make_shared<hooks::direct_hook<decltype(OpenProcess)>>("kernel32.dll!OpenProcess (direct, redirect_openprocess)", OpenProcess);
            hook->set_detour([hook = hook.get()](DWORD dwDesiredAccess, BOOL bInheritHandle, DWORD dwProcessId)->HANDLE {
                if (dwProcessId == GetCurrentProcessId()) {
                    if (s_silenceSet.emplace(GetCurrentThreadId()).second)
                        logging::I("{} OpenProcess(0x{:08X}, {}, {}) was invoked by thread {}. Redirecting to DuplicateHandle.", LogTag, dwDesiredAccess, bInheritHandle, dwProcessId, GetCurrentThreadId());

                    if (HANDLE res; DuplicateHandle(GetCurrentProcess(), GetCurrentProcess(), GetCurrentProcess(), &res, dwDesiredAccess, bInheritHandle, 0))
                        return res;

                    return {};
                }
                return hook->call_original(dwDesiredAccess, bInheritHandle, dwProcessId);
            });
            s_hook = std::dynamic_pointer_cast<hooks::base_untyped_hook>(std::move(hook));

            logging::I("{} Enable via direct_hook", LogTag);
        }

        //std::thread([]() {
        //    SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_IDLE);
        //    for (const auto to = GetTickCount64() + 3000; GetTickCount64() < to;)
        //        s_hook->assert_dominance();
        //}).detach();

    } else {
        if (s_hook) {
            logging::I("{} Disable OpenProcess", LogTag);
            s_hook.reset();
        }
    }
}

void xivfixes::apply_all(bool bApply) {
    for (const auto& [taskName, taskFunction] : std::initializer_list<std::pair<const char*, void(*)(bool)>>
        {
            { "unhook_dll", &unhook_dll },
            { "prevent_devicechange_crashes", &prevent_devicechange_crashes },
            { "disable_game_openprocess_access_check", &disable_game_openprocess_access_check },
            { "redirect_openprocess", &redirect_openprocess },
        }
        ) {
        try {
            taskFunction(bApply);

        } catch (const std::exception& e) {
            if (bApply)
                logging::W("Error trying to activate fixup [{}]: {}", taskName, e.what());
            else
                logging::W("Error trying to deactivate fixup [{}]: {}", taskName, e.what());

            continue;
        }

        if (bApply)
            logging::I("Fixup [{}] activated.", taskName);
        else
            logging::I("Fixup [{}] deactivated.", taskName);
    }
}
