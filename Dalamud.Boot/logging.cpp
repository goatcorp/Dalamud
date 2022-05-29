#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

#include <fstream>
#include <memory>

#include "logging.h"

static bool s_bLoaded = false;
static std::shared_ptr<void> s_hLogFile;

void logging::print(Level level, const char* s) {
    SYSTEMTIME st;
    GetLocalTime(&st);

    std::string estr;
    switch (level) {
        case Verbose:
            estr = std::format("[{:02}:{:02}:{:02} CPP/VRB] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Debug:
            estr = std::format("[{:02}:{:02}:{:02} CPP/DBG] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Info:
            estr = std::format("[{:02}:{:02}:{:02} CPP/INF] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Warning:
            estr = std::format("[{:02}:{:02}:{:02} CPP/WRN] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Error:
            estr = std::format("[{:02}:{:02}:{:02} CPP/ERR] {}\n", st.wHour, st.wMinute, st.wSecond, s);
            break;
        case Fatal:
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
      
        if (s_hLogFile) {
            WriteFile(s_hLogFile.get(), &estr[0], static_cast<DWORD>(estr.size()), &wr, nullptr);
        }
    }
}

void logging::start_file_logging(const std::filesystem::path& path) {
    if (s_hLogFile)
        return;

    const auto h = CreateFile(path.wstring().c_str(),
        GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        nullptr, OPEN_ALWAYS, 0, nullptr);
    if (h == INVALID_HANDLE_VALUE)
        throw std::runtime_error(std::format("Win32 error {}(0x{:x})", GetLastError(), GetLastError()));
    
    SetFilePointer(h, 0, 0, FILE_END);
    s_hLogFile = { h, &CloseHandle };
}

void logging::update_dll_load_status(bool loaded) {
    s_bLoaded = loaded;
}
