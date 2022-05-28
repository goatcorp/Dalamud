#pragma once

#include <array>
#include <cstdint>
#include <string>
#include <type_traits>

namespace unicode {
    constexpr char32_t UReplacement = U'\uFFFD';
    constexpr char32_t UInvalid = U'\uFFFF';

    template<typename T> struct EncodingTag {};

    size_t decode(EncodingTag<char8_t>, char32_t& out, const char8_t* in, size_t nRemainingBytes, bool strict);

    size_t decode(EncodingTag<char16_t>, char32_t& out, const char16_t* in, size_t nRemainingBytes, bool strict);

    size_t decode(EncodingTag<char32_t>, char32_t& out, const char32_t* in, size_t nRemainingBytes, bool strict);

    size_t decode(EncodingTag<char>, char32_t& out, const char* in, size_t nRemainingBytes, bool strict);

    size_t decode(EncodingTag<wchar_t>, char32_t& out, const wchar_t* in, size_t nRemainingBytes, bool strict);

    template<typename T>
    inline size_t decode(char32_t& out, const T* in, size_t nRemainingBytes, bool strict = true) {
        return decode(EncodingTag<T>(), out, in, nRemainingBytes, strict);
    }

    size_t encode(EncodingTag<char8_t>, char8_t* ptr, char32_t c, bool strict);

    size_t encode(EncodingTag<char16_t>, char16_t* ptr, char32_t c, bool strict);

    size_t encode(EncodingTag<char32_t>, char32_t* ptr, char32_t c, bool strict);

    size_t encode(EncodingTag<char>, char* ptr, char32_t c, bool strict);

    size_t encode(EncodingTag<wchar_t>, wchar_t* ptr, char32_t c, bool strict);

    template<typename T>
    inline size_t encode(T* ptr, char32_t c, bool strict = true) {
        return encode(EncodingTag<T>(), ptr, c, strict);
    }

    template<class TTo, class TFromElem, class TFromTraits = std::char_traits<TFromElem>>
    inline TTo& convert(TTo& out, const std::basic_string_view<TFromElem, TFromTraits>& in, bool strict = true) {
        out.reserve(out.size() + in.size() * 4 / sizeof(in[0]) / sizeof(out[0]));

        char32_t c{};
        for (size_t decLen = 0, decIdx = 0; decIdx < in.size() && (decLen = unicode::decode(c, &in[decIdx], in.size() - decIdx, strict)); decIdx += decLen) {
            const auto encIdx = out.size();
            const auto encLen = unicode::encode<TTo::value_type>(nullptr, c, strict);
            out.resize(encIdx + encLen);
            unicode::encode(&out[encIdx], c, strict);
        }

        return out;
    }

    template<class TTo, class TFromElem, class TFromTraits = std::char_traits<TFromElem>, class TFromAlloc = std::allocator<TFromElem>>
    inline TTo& convert(TTo& out, const std::basic_string<TFromElem, TFromTraits, TFromAlloc>& in, bool strict = true) {
        return convert(out, std::basic_string_view<TFromElem, TFromTraits>(in), strict);
    }

    template<class TTo, class TFromElem, typename = std::enable_if_t<std::is_integral_v<TFromElem>>>
    inline TTo& convert(TTo& out, const TFromElem* in, size_t length = (std::numeric_limits<size_t>::max)(), bool strict = true) {
        if (length == (std::numeric_limits<size_t>::max)())
            length = std::char_traits<TFromElem>::length(in);

        return convert(out, std::basic_string_view<TFromElem>(in, length), strict);
    }

    template<class TTo, class TFromElem, class TFromTraits = std::char_traits<TFromElem>>
    inline TTo convert(const std::basic_string_view<TFromElem, TFromTraits>& in, bool strict = true) {
        TTo out{};
        return convert(out, in, strict);
    }

    template<class TTo, class TFromElem, class TFromTraits = std::char_traits<TFromElem>, class TFromAlloc = std::allocator<TFromElem>>
    inline TTo convert(const std::basic_string<TFromElem, TFromTraits, TFromAlloc>& in, bool strict = true) {
        TTo out{};
        return convert(out, std::basic_string_view<TFromElem, TFromTraits>(in), strict);
    }

    template<class TTo, class TFromElem, typename = std::enable_if_t<std::is_integral_v<TFromElem>>>
    inline TTo convert(const TFromElem* in, size_t length = (std::numeric_limits<size_t>::max)(), bool strict = true) {
        if (length == (std::numeric_limits<size_t>::max)())
            length = std::char_traits<TFromElem>::length(in);

        TTo out{};
        return convert(out, std::basic_string_view<TFromElem>(in, length), strict);
    }
}
