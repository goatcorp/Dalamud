#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

#include <fstream>
#include <memory>

#include "logging.h"

static bool s_bLoaded = false;
static bool s_bSkipLogFileWrite = false;
static std::shared_ptr<void> s_hLogFile;

void logging::start_file_logging(const std::filesystem::path& path, bool redirect_stderrout) {
    if (s_hLogFile)
        return;

    try {
        if (exists(path) && file_size(path) > 1048576) {
            auto oldPath = std::filesystem::path(path);
            oldPath.replace_extension(".log.old");
            if (exists(oldPath))
                remove(oldPath);
            rename(path, oldPath);
        }
    } catch (...) {
        // whatever
    }

    const auto h = CreateFile(path.wstring().c_str(),
                              GENERIC_WRITE,
                              FILE_SHARE_READ | FILE_SHARE_WRITE,
                              nullptr, OPEN_ALWAYS, 0, nullptr);
    if (h == INVALID_HANDLE_VALUE)
        throw std::runtime_error(std::format("Win32 error {}(0x{:x})", GetLastError(), GetLastError()));

    SetFilePointer(h, 0, 0, FILE_END);
    s_hLogFile = {h, &CloseHandle};

    if (redirect_stderrout) {
        SetStdHandle(STD_ERROR_HANDLE, h);
        SetStdHandle(STD_OUTPUT_HANDLE, h);
        s_bSkipLogFileWrite = true;
    }
}

void logging::update_dll_load_status(bool loaded) {
    s_bLoaded = loaded;
}

template<>
void logging::print<char>(Level level, const char* s) {
    SYSTEMTIME st;
    GetLocalTime(&st);

    std::string estr;
    switch (level) {
        case Level::Verbose:
            estr = std::format("[{:02}:{:02}:{:02} CPP/VRB] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Level::Debug:
            estr = std::format("[{:02}:{:02}:{:02} CPP/DBG] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Level::Info:
            estr = std::format("[{:02}:{:02}:{:02} CPP/INF] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Level::Warning:
            estr = std::format("[{:02}:{:02}:{:02} CPP/WRN] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Level::Error:
            estr = std::format("[{:02}:{:02}:{:02} CPP/ERR] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Level::Fatal:
            estr = std::format("[{:02}:{:02}:{:02} CPP/FTL] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        default:
            estr = std::format("[{:02}:{:02}:{:02} CPP/???] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
    }

    OutputDebugStringW(unicode::convert<std::wstring>(estr).c_str());

    // Handle accesses should not be done during DllMain process attach/detach calls
    if (s_bLoaded) {
        DWORD wr{};
        WriteFile(GetStdHandle(STD_ERROR_HANDLE), &estr[0], static_cast<DWORD>(estr.size()), &wr, nullptr);

        if (s_hLogFile && !s_bSkipLogFileWrite) {
            WriteFile(s_hLogFile.get(), &estr[0], static_cast<DWORD>(estr.size()), &wr, nullptr);
        }
    }
}
