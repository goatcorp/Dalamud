#pragma once

#include <filesystem>
#include <format>
#include <string>

#include "unicode.h"

namespace logging {
    enum class Level : int {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5,
    };

    enum FastFailErrorCode : int {
        Unspecified = 12345,
        MinHookUnload,
    };

    /**
     * @brief Starts writing log to specified file.
     */
    void start_file_logging(const std::filesystem::path& logPath, bool redirect_stderrout = false);

    /**
     * @brief Marks this DLL either as loaded or unloaded, top prevent accessing handles when the DLL is not loaded.
     */
    void update_dll_load_status(bool loaded);

    /**
     * @brief Prints log, unformatted.
     * @param level Log level.
     * @param s Log to print, as a C-string.
     */
    template<typename TElem>
    void print(Level level, const TElem* s) { print(level, unicode::convert<std::string>(s).c_str()); }

    /**
     * @brief Prints log, unformatted.
     * @param level Log level.
     * @param s Log to print, as a basic_string.
     */
    template<typename TElem, typename TTraits = std::char_traits<TElem>, typename TAlloc = std::allocator<TElem>>
    void print(Level level, const std::basic_string<TElem, TTraits, TAlloc>& s) { print(level, s.c_str()); }

    /**
     * @brief Prints log, unformatted.
     * @param level Log level.
     * @param s Log to print, as a basic_string_view.
     */
    template<typename TElem, typename TTraits = std::char_traits<TElem>>
    void print(Level level, const std::basic_string_view<TElem, TTraits>& s) { print(level, unicode::convert<std::string>(s).c_str()); }

    template<>
    void print<char>(Level level, const char* s);

    template<typename>
    struct is_basic_string : std::false_type {};

    template<typename TElem, typename TTraits, typename TAlloc>
    struct is_basic_string<std::basic_string<TElem, TTraits, TAlloc>> : std::true_type {};

    template<typename T>
    inline constexpr auto is_basic_string_v = is_basic_string<T>::value;

    template<typename>
    struct is_basic_string_view : std::false_type {};

    template<typename TElem, typename TTraits, typename TAlloc>
    struct is_basic_string_view<std::basic_string<TElem, TTraits, TAlloc>> : std::true_type {};

    template<typename T>
    inline constexpr auto is_basic_string_view_v = is_basic_string_view<T>::value;

    template<typename T>
    auto to_format_arg(T&& x) {
        using Td = std::remove_cvref_t<T>;
        if constexpr (std::is_pointer_v<Td>) {
            using Tdd = std::remove_cvref_t<std::remove_pointer_t<Td>>;
            if constexpr (std::is_same_v<Tdd, wchar_t> || std::is_same_v<Tdd, char8_t> || std::is_same_v<Tdd, char16_t> || std::is_same_v<Tdd, char32_t>)
                return unicode::convert<std::string>(x);
            else
                return std::forward<T>(x);

        } else {
            if constexpr (is_basic_string_v<Td> || is_basic_string_view_v<Td>) {
                using Tdd = Td::value_type;
                if constexpr (std::is_same_v<Tdd, wchar_t> || std::is_same_v<Tdd, char8_t> || std::is_same_v<Tdd, char16_t> || std::is_same_v<Tdd, char32_t>)
                    return unicode::convert<std::string>(x);
                else
                    return std::forward<T>(x);

            } else if constexpr (std::is_same_v<Td, std::filesystem::path>) {
                auto u8s = x.u8string();
                return std::move(*reinterpret_cast<std::string*>(&u8s));

            } else {
                return std::forward<T>(x);
            }
        }
    }

    /**
     * @brief Prints log, formatted.
     * @param level Log level.
     * @param fmt C-string.
     * @param arg1 First format parameter.
     * @param args Second and further format parameters, if any.
     */
    template<typename Arg, typename...Args>
    void print(Level level, const char* fmt, Arg&& arg1, Args&&...args) {
        print(level, std::vformat(fmt, std::make_format_args(to_format_arg(std::forward<Arg>(arg1)), to_format_arg(std::forward<Args>(args))...)));
    }

    template<typename...Args> void V(Args&&...args) { print(Level::Verbose, std::forward<Args>(args)...); }
    template<typename...Args> void D(Args&&...args) { print(Level::Debug, std::forward<Args>(args)...); }
    template<typename...Args> void I(Args&&...args) { print(Level::Info, std::forward<Args>(args)...); }
    template<typename...Args> void W(Args&&...args) { print(Level::Warning, std::forward<Args>(args)...); }
    template<typename...Args> void E(Args&&...args) { print(Level::Error, std::forward<Args>(args)...); }
    template<typename...Args> void F(Args&&...args) { print(Level::Fatal, std::forward<Args>(args)...); }
}
