#include "pch.h"
#include "logging.h"

static bool s_bLoaded = false;

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
    }
}

void logging::update_dll_load_status(bool loaded) {
    s_bLoaded = loaded;
}
