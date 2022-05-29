#include "pch.h"
#include "logging.h"

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

    DWORD wr;
    WriteFile(GetStdHandle(STD_ERROR_HANDLE), &estr[0], static_cast<DWORD>(estr.size()), &wr, nullptr);

    if (log_file.is_open())
    {
        log_file << estr;
        log_file.flush();
    }
}
