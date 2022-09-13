#define WIN32_LEAN_AND_MEAN
#include <Windows.h>

#include <fstream>
#include <memory>

#include "logging.h"

static bool s_bLoaded = false;
static bool s_bSkipLogFileWrite = false;
static std::shared_ptr<void> s_hLogFile;

void logging::start_file_logging(const std::filesystem::path& logPath, bool redirect_stderrout) {
    constexpr auto MaxLogFileSize = 1 * 1024 * 1024;
    constexpr auto MaxOldFileSize = 10 * 1024 * 1024;
    char buf[4096];

    if (s_hLogFile)
        return;

    const auto oldPath = std::filesystem::path(logPath).replace_extension(".old.log");
    
    try {
        const auto oldPathOld = std::filesystem::path(logPath).replace_extension(".log.old");
        if (exists(oldPathOld)) {
            if (exists(oldPath))
                remove(oldPathOld);
            else
                rename(oldPathOld, oldPath);
        }
    } catch (...) {
        // whatever
    }

    const auto h = CreateFileW(logPath.wstring().c_str(),
                               GENERIC_READ | GENERIC_WRITE,
                               FILE_SHARE_READ | FILE_SHARE_WRITE,
                               nullptr, OPEN_ALWAYS, 0, nullptr);
    if (h == INVALID_HANDLE_VALUE)
        throw std::runtime_error(std::format("Win32 error {}(0x{:x})", GetLastError(), GetLastError()));

    s_hLogFile = {h, &CloseHandle};

    // 1. Move excess data from logPath to oldPath
    if (LARGE_INTEGER fsize; SetFilePointerEx(h, {}, &fsize, FILE_END) && fsize.QuadPart > MaxLogFileSize) {
        const auto amountToMove = (std::min<int64_t>)(fsize.QuadPart - MaxLogFileSize, MaxOldFileSize);
        SetFilePointerEx(h, LARGE_INTEGER{.QuadPart = -(MaxLogFileSize + amountToMove)}, nullptr, FILE_END);

        const auto hOld = CreateFileW(oldPath.c_str(),
                                      GENERIC_READ | GENERIC_WRITE,
                                      FILE_SHARE_READ | FILE_SHARE_WRITE,
                                      nullptr, OPEN_ALWAYS, 0, nullptr);
        if (hOld != INVALID_HANDLE_VALUE) {
            const auto hOldCloser = std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>(hOld, &CloseHandle);
            SetFilePointerEx(hOld, {}, nullptr, FILE_END);

            DWORD read = 0, written = 0;
            for (int64_t i = 0; i < amountToMove; i += sizeof buf) {
                const auto chunkSize = static_cast<DWORD>((std::min<int64_t>)(sizeof buf, amountToMove - i));
                if (!ReadFile(h, buf, chunkSize, &read, nullptr) || read != chunkSize)
                    break;
                if (!WriteFile(hOld, buf, read, &written, nullptr) || read != written)
                    break;
            }
        }
    }

    // 2. Cull each of .log and .old files
    for (const auto& [path, maxSize] : std::initializer_list<std::pair<std::filesystem::path, int64_t>>{
        {oldPath, MaxOldFileSize},
        {logPath, MaxLogFileSize},
    }) {
        try {
            const auto hFileRead = CreateFileW(path.c_str(),
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                nullptr, OPEN_EXISTING, 0, nullptr);
            if (hFileRead == INVALID_HANDLE_VALUE)
                continue;
            const auto closeRead = std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>(hFileRead, &CloseHandle);

            if (LARGE_INTEGER ptr; !SetFilePointerEx(hFileRead, { .QuadPart = -maxSize }, &ptr, FILE_END) || ptr.QuadPart <= 0)
                continue;

            const auto hFileWrite = CreateFileW(path.c_str(),
                GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                nullptr, OPEN_EXISTING, 0, nullptr);
            if (hFileWrite == INVALID_HANDLE_VALUE)
                continue;
            const auto closeWrite = std::unique_ptr<std::remove_pointer_t<HANDLE>, decltype(&CloseHandle)>(hFileWrite, &CloseHandle);

            DWORD read = 0, written = 0;
            for (int64_t i = 0; i < maxSize; i += sizeof buf) {
                const auto chunkSize = static_cast<DWORD>((std::min<int64_t>)(sizeof buf, maxSize - i));
                if (!ReadFile(hFileRead, buf, chunkSize, &read, nullptr) || read != chunkSize)
                    break;
                if (!WriteFile(hFileWrite, buf, read, &written, nullptr) || read != written)
                    break;
            }

            SetEndOfFile(hFileWrite);
        } catch (...) {
            // ignore
        }
    }

    SetFilePointerEx(h, {}, nullptr, FILE_END);
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
        if (s_hLogFile)
            SetFilePointerEx(s_hLogFile.get(), {}, nullptr, FILE_END);

        DWORD wr{};
        WriteFile(GetStdHandle(STD_ERROR_HANDLE), &estr[0], static_cast<DWORD>(estr.size()), &wr, nullptr);

        if (s_hLogFile && !s_bSkipLogFileWrite)
            WriteFile(s_hLogFile.get(), &estr[0], static_cast<DWORD>(estr.size()), &wr, nullptr);
    }
}
