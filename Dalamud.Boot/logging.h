#pragma once

#include <format>
#include <numeric>
#include <string>

#include "unicode.h"

namespace logging {
    enum Level : int {
        Verbose = 0,
        V = 0,
        Debug = 1,
        D = 1,
        Info = 2,
        I = 2,
        Warning = 3,
        W = 3,
        Error = 4,
        E = 4,
        Fatal = 5,
        F = 5,
    };

    enum FastFailErrorCode : int {
        Unspecified = 12345,
        MinHookUnload,
    };

    void print(Level level, const char* s);

    inline void print(Level level, const wchar_t* s) {
        const auto cs = unicode::convert<std::string>(s);
        print(level, cs.c_str());
    }

    inline void print(Level level, const std::string& s) {
        print(level, s.c_str());
    }

    inline void print(Level level, const std::wstring& s) {
        print(level, s.c_str());
    }

    template<Level level, typename T>
    inline void print(const T* s) {
        print(level, s);
    }

    template<typename Arg, typename...Args>
    inline void print(Level level, const char* pcszFormat, Arg arg1, Args...args) {
        print(level, std::format(pcszFormat, std::forward<Arg>(arg1), std::forward<Args>(args)...));
    }

    template<typename Arg, typename...Args>
    inline void print(Level level, const wchar_t* pcszFormat, Arg arg1, Args...args) {
        print(level, std::format(pcszFormat, std::forward<Arg>(arg1), std::forward<Args>(args)...));
    }

    template<Level level, typename T, typename Arg, typename...Args, typename = std::enable_if_t<std::is_integral_v<T>>>
    inline void print(const T* pcszFormat, Arg arg1, Args...args) {
        print(level, std::format(pcszFormat, std::forward<Arg>(arg1), std::forward<Args>(args)...));
    }

    void update_dll_load_status(bool loaded);
};
