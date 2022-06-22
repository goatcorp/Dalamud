#include "unicode.h"

size_t unicode::decode(EncodingTag<char8_t>, char32_t& out, const char8_t* in, size_t nRemainingBytes, bool strict) {
    if (nRemainingBytes == 0) {
        out = 0;
        return 0;
    }

    if (0 == (*in & 0x80)) {
        out = *in;
        return 1;
    }

    if (0xC0 == (*in & 0xE0)) {
        if (nRemainingBytes < 2) goto invalid;
        if (0x80 != (in[1] & 0xC0)) goto invalid;
        out = (
            ((static_cast<char32_t>(in[0]) & 0x1F) << 6) |
            ((static_cast<char32_t>(in[1]) & 0x3F) << 0));
        return 2;
    }

    if (0xE0 == (*in & 0xF0)) {
        if (nRemainingBytes < 3) goto invalid;
        if (0x80 != (in[1] & 0xC0)) goto invalid;
        if (0x80 != (in[2] & 0xC0)) goto invalid;
        out = static_cast<char32_t>(
            ((static_cast<char32_t>(in[0]) & 0x0F) << 12) |
            ((static_cast<char32_t>(in[1]) & 0x3F) << 6) |
            ((static_cast<char32_t>(in[2]) & 0x3F) << 0));
        return 3;
    }

    if (0xF0 == (*in & 0xF8)) {
        if (nRemainingBytes < 4) goto invalid;
        if (0x80 != (in[1] & 0xC0)) goto invalid;
        if (0x80 != (in[2] & 0xC0)) goto invalid;
        if (0x80 != (in[3] & 0xC0)) goto invalid;
        out = (
            ((static_cast<char32_t>(in[0]) & 0x07) << 18) |
            ((static_cast<char32_t>(in[1]) & 0x3F) << 12) |
            ((static_cast<char32_t>(in[2]) & 0x3F) << 6) |
            ((static_cast<char32_t>(in[3]) & 0x3F) << 0));
        return 4;
    }

    if (!strict) {
        if (0xF8 == (*in & 0xFC)) {
            if (nRemainingBytes < 5) goto invalid;
            if (0x80 != (in[1] & 0xC0)) goto invalid;
            if (0x80 != (in[2] & 0xC0)) goto invalid;
            if (0x80 != (in[3] & 0xC0)) goto invalid;
            if (0x80 != (in[4] & 0xC0)) goto invalid;
            out = (
                ((static_cast<char32_t>(in[0]) & 0x07) << 24) |
                ((static_cast<char32_t>(in[1]) & 0x3F) << 18) |
                ((static_cast<char32_t>(in[2]) & 0x3F) << 12) |
                ((static_cast<char32_t>(in[3]) & 0x3F) << 6) |
                ((static_cast<char32_t>(in[4]) & 0x3F) << 0));
            return 4;
        }

        if (0xFC == (*in & 0xFE)) {
            if (nRemainingBytes < 6) goto invalid;
            if (0x80 != (in[1] & 0xC0)) goto invalid;
            if (0x80 != (in[2] & 0xC0)) goto invalid;
            if (0x80 != (in[3] & 0xC0)) goto invalid;
            if (0x80 != (in[4] & 0xC0)) goto invalid;
            if (0x80 != (in[5] & 0xC0)) goto invalid;
            out = (
                ((static_cast<char32_t>(in[0]) & 0x07) << 30) |
                ((static_cast<char32_t>(in[1]) & 0x3F) << 24) |
                ((static_cast<char32_t>(in[2]) & 0x3F) << 18) |
                ((static_cast<char32_t>(in[3]) & 0x3F) << 12) |
                ((static_cast<char32_t>(in[4]) & 0x3F) << 6) |
                ((static_cast<char32_t>(in[5]) & 0x3F) << 0));
            return 5;
        }
    }

invalid:
    out = UReplacement;
    return 1;
}

size_t unicode::decode(EncodingTag<char16_t>, char32_t& out, const char16_t* in, size_t nRemainingBytes, bool strict) {
    if (nRemainingBytes == 0) {
        out = 0;
        return 0;
    }

    if ((*in & 0xFC00) == 0xD800) {
        if (nRemainingBytes < 2 || (in[1] & 0xFC00) != 0xDC00)
            goto invalid;
        out = 0x10000 + (
            ((static_cast<char32_t>(in[0]) & 0x03FF) << 10) |
            ((static_cast<char32_t>(in[1]) & 0x03FF) << 0)
            );
        return 2;
    }

    if (0xD800 <= *in && *in <= 0xDFFF && strict)
        out = UReplacement;
    else
        out = *in;
    return 1;

invalid:
    out = UReplacement;
    return 1;
}

size_t unicode::decode(EncodingTag<char32_t>, char32_t& out, const char32_t* in, size_t nRemainingBytes, bool strict) {
    if (nRemainingBytes == 0) {
        out = 0;
        return 0;
    }

    out = *in;
    return 1;
}

size_t unicode::decode(EncodingTag<char>, char32_t& out, const char* in, size_t nRemainingBytes, bool strict) {
    return decode(EncodingTag<char8_t>(), out, reinterpret_cast<const char8_t*>(in), nRemainingBytes, strict);
}

size_t unicode::decode(EncodingTag<wchar_t>, char32_t& out, const wchar_t* in, size_t nRemainingBytes, bool strict) {
    return decode(EncodingTag<char16_t>(), out, reinterpret_cast<const char16_t*>(in), nRemainingBytes, strict);
}

size_t unicode::encode(EncodingTag<char8_t>, char8_t* ptr, char32_t c, bool strict) {
    if (c < (1 << 7)) {
        if (ptr)
            *(ptr++) = static_cast<char8_t>(c);
        return 1;
    }

    if (c < (1 << (5 + 6))) {
        if (ptr) {
            *(ptr++) = 0xC0 | static_cast<char8_t>(c >> 6);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 0) & 0x3F);
        }
        return 2;
    }
    if (c < (1 << (4 + 6 + 6))) {
        if (ptr) {
            *(ptr++) = 0xE0 | static_cast<char8_t>(c >> 12);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 6) & 0x3F);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 0) & 0x3F);
        }
        return 3;
    }

    if (c < (1 << (3 + 6 + 6 + 6))) {
        if (ptr) {
            *(ptr++) = 0xF0 | static_cast<char8_t>(c >> 18);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 12) & 0x3F);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 6) & 0x3F);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 0) & 0x3F);
        }
        return 4;
    }

    if (strict) {
        if (ptr) { // Replacement character U+FFFD
            *(ptr++) = 0xEF;
            *(ptr++) = 0xBF;
            *(ptr++) = 0xBD;
        }
        return 3;
    }

    if (c < (1 << (3 + 6 + 6 + 6 + 6))) {
        if (ptr) {
            *(ptr++) = 0xF8 | static_cast<char8_t>(c >> 24);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 18) & 0x3F);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 12) & 0x3F);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 6) & 0x3F);
            *(ptr++) = 0x80 | static_cast<char8_t>((c >> 0) & 0x3F);
        }
        return 5;
    }

    if (ptr) {
        *(ptr++) = 0xFC | static_cast<char8_t>(c >> 30);
        *(ptr++) = 0x80 | static_cast<char8_t>((c >> 24) & 0x3F);
        *(ptr++) = 0x80 | static_cast<char8_t>((c >> 18) & 0x3F);
        *(ptr++) = 0x80 | static_cast<char8_t>((c >> 12) & 0x3F);
        *(ptr++) = 0x80 | static_cast<char8_t>((c >> 6) & 0x3F);
        *(ptr++) = 0x80 | static_cast<char8_t>((c >> 0) & 0x3F);
    }
    return 6;
}

size_t unicode::encode(EncodingTag<char16_t>, char16_t* ptr, char32_t c, bool strict) {
    if (c < 0x10000) {
        if (ptr) {
            if (0xD800 <= c && c <= 0xDFFF && strict)
                *(ptr++) = 0xFFFD;
            else
                *(ptr++) = static_cast<char16_t>(c);
        }
        return 1;
    }

    c -= 0x10000;

    if (c < (1 << 20)) {
        if (ptr) {
            *(ptr++) = 0xD800 | static_cast<char16_t>((c >> 10) & 0x3FF);
            *(ptr++) = 0xDC00 | static_cast<char16_t>((c >> 0) & 0x3FF);
        }
        return 2;
    }

    if (ptr)
        *(ptr++) = 0xFFFD;
    return 1;
}

size_t unicode::encode(EncodingTag<char32_t>, char32_t* ptr, char32_t c, bool strict) {
    if (ptr)
        *ptr = c;
    return 1;
}

size_t unicode::encode(EncodingTag<char>, char* ptr, char32_t c, bool strict) {
    return encode(EncodingTag<char8_t>(), reinterpret_cast<char8_t*>(ptr), c, strict);
}

size_t unicode::encode(EncodingTag<wchar_t>, wchar_t* ptr, char32_t c, bool strict) {
    return encode(EncodingTag<char16_t>(), reinterpret_cast<char16_t*>(ptr), c, strict);
}

char32_t unicode::lower(char32_t in) {
    if ('A' <= in && in <= 'Z')
        return in - 'A' + 'a';
    return in;
}

char32_t unicode::upper(char32_t in) {
    if ('a' <= in && in <= 'z')
        return in - 'a' + 'A';
    return in;
}
