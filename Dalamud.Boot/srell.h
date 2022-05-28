/*****************************************************************************
**
**  SRELL (std::regex-like library) version 3.009
**
**  Copyright (c) 2012-2022, Nozomu Katoo. All rights reserved.
**
**  Redistribution and use in source and binary forms, with or without
**  modification, are permitted provided that the following conditions are
**  met:
**
**  1. Redistributions of source code must retain the above copyright notice,
**     this list of conditions and the following disclaimer.
**
**  2. Redistributions in binary form must reproduce the above copyright
**     notice, this list of conditions and the following disclaimer in the
**     documentation and/or other materials provided with the distribution.
**
**  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS ``AS
**  IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
**  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
**  PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
**  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
**  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
**  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
**  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
**  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
**  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
**  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**
******************************************************************************
**/

#ifndef SRELL_REGEX_TEMPLATE_LIBRARY
#define SRELL_REGEX_TEMPLATE_LIBRARY

#include <stdexcept>
#include <climits>
#include <cwchar>
#include <string>
#include <locale>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <cstddef>
#include <utility>
#include <vector>
#include <iterator>
#include <memory>
#include <algorithm>

#ifdef __cpp_unicode_characters
#ifndef SRELL_CPP11_CHAR1632_ENABLED
#define SRELL_CPP11_CHAR1632_ENABLED
#endif
#endif
#ifdef __cpp_initializer_lists
#include <initializer_list>
#ifndef SRELL_CPP11_INITIALIZER_LIST_ENABLED
#define SRELL_CPP11_INITIALIZER_LIST_ENABLED
#endif
#endif
#ifdef __cpp_rvalue_references
#ifndef SRELL_CPP11_MOVE_ENABLED
#define SRELL_CPP11_MOVE_ENABLED
#endif
#endif
#ifdef SRELL_CPP11_MOVE_ENABLED
#if defined(_MSC_VER) && _MSC_VER < 1900
#define SRELL_NOEXCEPT
#else
#define SRELL_NOEXCEPT noexcept
#endif
#endif
#ifdef __cpp_char8_t
#ifndef SRELL_CPP20_CHAR8_ENABLED
#ifdef __cpp_lib_char8_t
#define SRELL_CPP20_CHAR8_ENABLED 2
#else
#define SRELL_CPP20_CHAR8_ENABLED 1
#endif
#endif
#endif

//  The following SRELL_NO_* macros would be useful when wanting to
//  reduce the size of a binary by turning off some feature(s).

#ifdef SRELL_NO_UNICODE_DATA

//  Prevents Unicode data used for icase (case-insensitive) matching
//  from being output into a resulting binary. In this case only the
//  ASCII characters are case-folded when icase matching is performed
//  (i.e., [A-Z] -> [a-z] only).
#define SRELL_NO_UNICODE_ICASE

//  Disables the Unicode property (\p{...} and \P{...}) and prevents
//  Unicode property data from being output into a resulting binary.
#define SRELL_NO_UNICODE_PROPERTY
#endif

//  Prevents icase matching specific functions into a resulting binary.
//  In this case the icase flag is ignored and icase matching becomes
//  unavailable.
#ifdef SRELL_NO_ICASE
#ifndef SRELL_NO_UNICODE_ICASE
#define SRELL_NO_UNICODE_ICASE
#endif
#endif

//  This macro might be removed in the future.
#ifdef SRELL_V1_COMPATIBLE
#ifndef SRELL_NO_UNICODE_PROPERTY
#define SRELL_NO_UNICODE_PROPERTY
#endif
#define SRELL_NO_NAMEDCAPTURE
#define SRELL_NO_SINGLELINE
#define SRELL_FIXEDWIDTHLOOKBEHIND
#endif

namespace srell {
    //  ["regex_constants.h" ...

    namespace regex_constants {
        enum syntax_option_type {
            icase = 1 << 0,
            nosubs = 1 << 1,
            optimize = 1 << 2,
            collate = 1 << 3,
            ECMAScript = 1 << 4,
            basic = 1 << 5,
            extended = 1 << 6,
            awk = 1 << 7,
            grep = 1 << 8,
            egrep = 1 << 9,
            multiline = 1 << 10,

            //  SRELL's extension.
            dotall = 1 << 11	//  singleline.
        };

        inline syntax_option_type operator&(const syntax_option_type left, const syntax_option_type right) {
            return static_cast<syntax_option_type>(static_cast<int>(left) & static_cast<int>(right));
        }
        inline syntax_option_type operator|(const syntax_option_type left, const syntax_option_type right) {
            return static_cast<syntax_option_type>(static_cast<int>(left) | static_cast<int>(right));
        }
        inline syntax_option_type operator^(const syntax_option_type left, const syntax_option_type right) {
            return static_cast<syntax_option_type>(static_cast<int>(left) ^ static_cast<int>(right));
        }
        inline syntax_option_type operator~(const syntax_option_type b) {
            return static_cast<syntax_option_type>(~static_cast<int>(b));
        }
        inline syntax_option_type& operator&=(syntax_option_type& left, const syntax_option_type right) {
            left = left & right;
            return left;
        }
        inline syntax_option_type& operator|=(syntax_option_type& left, const syntax_option_type right) {
            left = left | right;
            return left;
        }
        inline syntax_option_type& operator^=(syntax_option_type& left, const syntax_option_type right) {
            left = left ^ right;
            return left;
        }
    }
    //  namespace regex_constants

    namespace regex_constants {
        enum match_flag_type {
            match_default = 0,
            match_not_bol = 1 << 0,
            match_not_eol = 1 << 1,
            match_not_bow = 1 << 2,
            match_not_eow = 1 << 3,
            match_any = 1 << 4,
            match_not_null = 1 << 5,
            match_continuous = 1 << 6,
            match_prev_avail = 1 << 7,

            format_default = 0,
            format_sed = 1 << 8,
            format_no_copy = 1 << 9,
            format_first_only = 1 << 10,

            //  For internal use.
            match_match_ = 1 << 11
        };

        inline match_flag_type operator&(const match_flag_type left, const match_flag_type right) {
            return static_cast<match_flag_type>(static_cast<int>(left) & static_cast<int>(right));
        }
        inline match_flag_type operator|(const match_flag_type left, const match_flag_type right) {
            return static_cast<match_flag_type>(static_cast<int>(left) | static_cast<int>(right));
        }
        inline match_flag_type operator^(const match_flag_type left, const match_flag_type right) {
            return static_cast<match_flag_type>(static_cast<int>(left) ^ static_cast<int>(right));
        }
        inline match_flag_type operator~(const match_flag_type b) {
            return static_cast<match_flag_type>(~static_cast<int>(b));
        }
        inline match_flag_type& operator&=(match_flag_type& left, const match_flag_type right) {
            left = left & right;
            return left;
        }
        inline match_flag_type& operator|=(match_flag_type& left, const match_flag_type right) {
            left = left | right;
            return left;
        }
        inline match_flag_type& operator^=(match_flag_type& left, const match_flag_type right) {
            left = left ^ right;
            return left;
        }
    }
    //  namespace regex_constants

    //  28.5, regex constants:
    namespace regex_constants {
        typedef int error_type;

        static const error_type error_collate = 100;
        static const error_type error_ctype = 101;
        static const error_type error_escape = 102;
        static const error_type error_backref = 103;
        static const error_type error_brack = 104;
        static const error_type error_paren = 105;
        static const error_type error_brace = 106;
        static const error_type error_badbrace = 107;
        static const error_type error_range = 108;
        static const error_type error_space = 109;
        static const error_type error_badrepeat = 110;
        static const error_type error_complexity = 111;
        static const error_type error_stack = 112;

        //  SRELL's extension.
        static const error_type error_utf8 = 113;

#if defined(SRELL_FIXEDWIDTHLOOKBEHIND)
        static const error_type error_lookbehind = 200;
#endif
        static const error_type error_internal = 999;
    }
    //  namespace regex_constants

//  ... "regex_constants.h"]
//  ["regex_error.hpp" ...

//  28.6, class regex_error:
    class regex_error : public std::runtime_error {
    public:

        explicit regex_error(const regex_constants::error_type ecode)
            : std::runtime_error("regex_error")	//  added for error C2512.
            , ecode_(ecode) {
        }

        regex_constants::error_type code() const {
            return ecode_;
        }

    private:

        regex_constants::error_type ecode_;
    };

    //  ... "regex_error.hpp"]
    //  ["rei_type.h" ...

    namespace regex_internal {

#if defined(SRELL_CPP11_CHAR1632_ENABLED)

        typedef char32_t uchar32;

#elif defined(UINT_MAX) && UINT_MAX >= 0xFFFFFFFF

        typedef unsigned int uchar32;

#elif defined(ULONG_MAX) && ULONG_MAX >= 0xFFFFFFFF

        typedef unsigned long uchar32;

#else
#error could not find a suitable type for 32-bit Unicode integer values.
#endif	//  defined(SRELL_CPP11_CHAR1632_ENABLED)

        typedef uchar32 uint_l32;	//  uint_least32.

    }	//  regex_internal

//  ... "rei_type.h"]
//  ["rei_constants.h" ...

    namespace regex_internal {
        enum re_state_type {
            st_character,               //  0x00
            st_character_class,         //  0x01

            st_epsilon,                 //  0x02

            st_check_counter,           //  0x03
//			st_increment_counter,       //  0x04
st_decrement_counter,       //  0x04
st_save_and_reset_counter,  //  0x05
st_restore_counter,         //  0x06

st_roundbracket_open,       //  0x07
st_roundbracket_pop,        //  0x08
st_roundbracket_close,      //  0x09

st_repeat_in_push,          //  0x0a
st_repeat_in_pop,           //  0x0b
st_check_0_width_repeat,    //  0x0c

st_backreference,           //  0x0d

st_lookaround_open,         //  0x0e

//			st_lookaround_pop,          //  0x10

st_bol,                     //  0x0f
st_eol,                     //  0x10
st_boundary,                //  0x11

st_success,                 //  0x12

#if !defined(SRELLDBG_NO_NEXTPOS_OPT)
st_move_nextpos,            //  0x13
#endif

st_lookaround_close = st_success,
st_zero_width_boundary = st_lookaround_open,
        };
        //  re_state_type

        namespace constants {
            static const uchar32 unicode_max_codepoint = 0x10ffff;
            static const uchar32 invalid_u32value = static_cast<uchar32>(-1);
            static const uchar32 max_u32value = static_cast<uchar32>(-2);
            static const uchar32 asc_icase = 0x20;
            static const uchar32 ccstr_empty = static_cast<uchar32>(-3);
        }
        //  constants

        namespace meta_char {
            static const uchar32 mc_exclam = 0x21;	//  '!'
            static const uchar32 mc_dollar = 0x24;	//  '$'
            static const uchar32 mc_rbraop = 0x28;	//  '('
            static const uchar32 mc_rbracl = 0x29;	//  ')'
            static const uchar32 mc_astrsk = 0x2a;	//  '*'
            static const uchar32 mc_plus = 0x2b;	//  '+'
            static const uchar32 mc_comma = 0x2c;	//  ','
            static const uchar32 mc_minus = 0x2d;	//  '-'
            static const uchar32 mc_period = 0x2e;	//  '.'
            static const uchar32 mc_colon = 0x3a;	//  ':'
            static const uchar32 mc_lt = 0x3c;		//  '<'
            static const uchar32 mc_eq = 0x3d;		//  '='
            static const uchar32 mc_gt = 0x3e;		//  '>'
            static const uchar32 mc_query = 0x3f;	//  '?'
            static const uchar32 mc_sbraop = 0x5b;	//  '['
            static const uchar32 mc_escape = 0x5c;	//  '\\'
            static const uchar32 mc_sbracl = 0x5d;	//  ']'
            static const uchar32 mc_caret = 0x5e;	//  '^'
            static const uchar32 mc_cbraop = 0x7b;	//  '{'
            static const uchar32 mc_bar = 0x7c;	//  '|'
            static const uchar32 mc_cbracl = 0x7d;	//  '}'
        }
        //  meta_char

        namespace char_ctrl {
            static const uchar32 cc_nul = 0x00;	//  '\0'	//0x00:NUL
            static const uchar32 cc_bs = 0x08;	//  '\b'	//0x08:BS
            static const uchar32 cc_htab = 0x09;	//  '\t'	//0x09:HT
            static const uchar32 cc_nl = 0x0a;	//  '\n'	//0x0a:LF
            static const uchar32 cc_vtab = 0x0b;	//  '\v'	//0x0b:VT
            static const uchar32 cc_ff = 0x0c;	//  '\f'	//0x0c:FF
            static const uchar32 cc_cr = 0x0d;	//  '\r'	//0x0d:CR
        }
        //  char_ctrl

        namespace char_alnum {
            static const uchar32 ch_0 = 0x30;	//  '0'
            static const uchar32 ch_1 = 0x31;	//  '1'
            static const uchar32 ch_7 = 0x37;	//  '7'
            static const uchar32 ch_8 = 0x38;	//  '8'
            static const uchar32 ch_9 = 0x39;	//  '9'
            static const uchar32 ch_A = 0x41;	//  'A'
            static const uchar32 ch_B = 0x42;	//  'B'
            static const uchar32 ch_D = 0x44;	//  'D'
            static const uchar32 ch_F = 0x46;	//  'F'
            static const uchar32 ch_P = 0x50;	//  'P'
            static const uchar32 ch_S = 0x53;	//  'S'
            static const uchar32 ch_W = 0x57;	//  'W'
            static const uchar32 ch_Z = 0x5a;	//  'Z'
            static const uchar32 ch_a = 0x61;	//  'a'
            static const uchar32 ch_b = 0x62;	//  'b'
            static const uchar32 ch_c = 0x63;	//  'c'
            static const uchar32 ch_d = 0x64;	//  'd'
            static const uchar32 ch_f = 0x66;	//  'f'
            static const uchar32 ch_k = 0x6b;	//  'k'
            static const uchar32 ch_n = 0x6e;	//  'n'
            static const uchar32 ch_p = 0x70;	//  'p'
            static const uchar32 ch_r = 0x72;	//  'r'
            static const uchar32 ch_s = 0x73;	//  's'
            static const uchar32 ch_t = 0x74;	//  't'
            static const uchar32 ch_u = 0x75;	//  'u'
            static const uchar32 ch_v = 0x76;	//  'v'
            static const uchar32 ch_w = 0x77;	//  'w'
            static const uchar32 ch_x = 0x78;	//  'x'
            static const uchar32 ch_z = 0x7a;	//  'z'
        }
        //  char_alnum

        namespace char_other {
            static const uchar32 co_sp = 0x20;	//  ' '
            static const uchar32 co_amp = 0x26;	//  '&'
            static const uchar32 co_apos = 0x27;	//  '\''
            static const uchar32 co_slash = 0x2f;	//  '/'
            static const uchar32 co_ll = 0x5f;	//  '_'
            static const uchar32 co_grav = 0x60;	//  '`'
        }
        //  char_other
    }
    //  namespace regex_internal

//  ... "rei_constants.h"]
//  ["rei_utf_traits.hpp" ...

    namespace regex_internal {

        template <typename charT>
        struct utf_traits_core {
        public:

            static const std::size_t maxseqlen = 1;
            static const int utftype = 0;

            static const std::size_t bitsetsize = 0x100;
            static const uchar32 bitsetmask = 0xff;
            static const uchar32 cumask = 0xff;

            //  *iter
            template <typename ForwardIterator>
            static uchar32 codepoint(ForwardIterator begin, const ForwardIterator /* end */) {
                return static_cast<uchar32>(*begin);
                //  Caller is responsible for begin != end.
            }

            //  *iter++
            template <typename ForwardIterator>
            static uchar32 codepoint_inc(ForwardIterator& begin, const ForwardIterator /* end */) {
                return static_cast<uchar32>(*begin++);
                //  Caller is responsible for begin != end.
            }

            //  iter2 = iter; return *--iter2;
            template <typename BidirectionalIterator>
            static uchar32 prevcodepoint(BidirectionalIterator cur, const BidirectionalIterator /* begin */) {
                return static_cast<uchar32>(*--cur);
            }

            //  *--iter
            template <typename BidirectionalIterator>
            static uchar32 dec_codepoint(BidirectionalIterator& cur, const BidirectionalIterator /* begin */) {
                return static_cast<uchar32>(*--cur);
                //  Caller is responsible for cur != begin.
            }

#if !defined(SRELLDBG_NO_BMH)

            template <typename charT2>
            static bool is_trailing(const charT2 /* cu */) {
                return false;
            }

#endif	//  !defined(SRELLDBG_NO_BMH)

            static uchar32 to_codeunits(charT out[maxseqlen], uchar32 cp) {
                out[0] = static_cast<charT>(cp);
                return 1;
            }

            static uchar32 firstcodeunit(const uchar32 cp) {
                return cp;
            }

            template <typename ForwardIterator>
            static bool seek_charboundary(ForwardIterator& begin, const ForwardIterator end) {
                return begin != end;
            }
        };	//  utf_traits_core

        //  common and utf-32.
        template <typename charT>
        struct utf_traits : public utf_traits_core<charT> {
            static const int utftype = 32;

            static const std::size_t bitsetsize = 0x10000;
            static const uchar32 bitsetmask = 0xffff;
            static const uchar32 cumask = 0x1fffff;
        };	//  utf_traits

        //  utf-8 specific.
        template <typename charT>
        struct utf8_traits : public utf_traits_core<charT> {
        public:

            //  utf-8 specific.
            static const std::size_t maxseqlen = 4;
            static const int utftype = 8;

            template <typename ForwardIterator>
            static uchar32 codepoint(ForwardIterator begin, const ForwardIterator end) {
                //		return codepoint_inc(begin, end);

                uchar32 codepoint = static_cast<uchar32>(*begin & 0xff);

                if ((codepoint & 0x80) == 0)	//  1 octet.
                    return codepoint;

                if (++begin != end && (codepoint >= 0xc0 && codepoint <= 0xf7) && (*begin & 0xc0) == 0x80) {
                    codepoint = static_cast<uchar32>((codepoint << 6) | (*begin & 0x3f));

                    if ((codepoint & 0x800) == 0)	//  2 octets.
                        return static_cast<uchar32>(codepoint & 0x7ff);

                    if (++begin != end && (*begin & 0xc0) == 0x80) {
                        codepoint = static_cast<uchar32>((codepoint << 6) | (*begin & 0x3f));

                        if ((codepoint & 0x10000) == 0)	//  3 octets.
                            return static_cast<uchar32>(codepoint & 0xffff);

                        if (++begin != end && (*begin & 0xc0) == 0x80)	//  4 octets.
                        {
                            codepoint = static_cast<uchar32>((codepoint << 6) | (*begin & 0x3f));

                            return static_cast<uchar32>(codepoint & 0x1fffff);
                        }
                    }
                }
                //		else	//  80-bf, f8-ff: invalid.

                return regex_internal::constants::invalid_u32value;
            }

            template <typename ForwardIterator>
            static uchar32 codepoint_inc(ForwardIterator& begin, const ForwardIterator end) {
                uchar32 codepoint = static_cast<uchar32>(*begin++ & 0xff);

                if ((codepoint & 0x80) == 0)	//  1 octet.
                    return codepoint;

                //  Expects transformation to (codepoint - 0xc0) <= 0x37 by optimisation.
                //  0xF7 instead of 0xF4 is for consistency with reverse iterators.
                if (begin != end && (codepoint >= 0xc0 && codepoint <= 0xf7) && (*begin & 0xc0) == 0x80)
                    //		if (begin != end && (0x7f00 & (1 << ((codepoint >> 3) & 0xf))) && (*begin & 0xc0) == 0x80)	//  c0, c8, d0, d8, e0, e8, f0.
                {
                    codepoint = static_cast<uchar32>((codepoint << 6) | (*begin++ & 0x3f));

                    //  11 ?aaa aabb bbbb
                    if ((codepoint & 0x800) == 0)	//  2 octets.
                        return static_cast<uchar32>(codepoint & 0x7ff);
                    //  c080-c1bf: invalid. 00-7F.
                    //  c280-dfbf: valid. 080-7FF.

                //  11 1aaa aabb bbbb
                    if (begin != end && (*begin & 0xc0) == 0x80) {
                        codepoint = static_cast<uchar32>((codepoint << 6) | (*begin++ & 0x3f));

                        //  111? aaaa bbbb bbcc cccc
                        if ((codepoint & 0x10000) == 0)	//  3 octets.
                            return static_cast<uchar32>(codepoint & 0xffff);
                        //  e08080-e09fbf: invalid. 000-7FF.
                        //  e0a080-efbfbf: valid. 0800-FFFF.

                    //  1111 0aaa bbbb bbcc cccc
                        if (begin != end && (*begin & 0xc0) == 0x80)	//  4 octets.
                        {
                            codepoint = static_cast<uchar32>((codepoint << 6) | (*begin++ & 0x3f));
                            //  f0808080-f08fbfbf: invalid. 0000-FFFF.
                            //  f0908080-f3bfbfbf: valid. 10000-FFFFF.
                            //  f4808080-f48fbfbf: valid. 100000-10FFFF.
                            //  f4908080-f4bfbfbf: invalid. 110000-13FFFF.
                            //  f5808080-f7bfbfbf: invalid. 140000-1FFFFF.

                            //  11 110a aabb bbbb cccc ccdd dddd
                            return static_cast<uchar32>(codepoint & 0x1fffff);
                        }
                    }
                }
                //		else	//  80-bf, f8-ff: invalid.

                return regex_internal::constants::invalid_u32value;
            }

            template <typename BidirectionalIterator>
            static uchar32 prevcodepoint(BidirectionalIterator cur, const BidirectionalIterator begin) {
                uchar32 codepoint = static_cast<uchar32>(*--cur);

                if ((codepoint & 0x80) == 0)
                    return static_cast<uchar32>(codepoint & 0xff);

                if ((codepoint & 0x40) == 0 && cur != begin) {
                    codepoint = static_cast<uchar32>((codepoint & 0x3f) | (*--cur << 6));

                    if ((codepoint & 0x3800) == 0x3000)	//  2 octets.
                        return static_cast<uchar32>(codepoint & 0x7ff);

                    if ((codepoint & 0x3000) == 0x2000 && cur != begin) {
                        codepoint = static_cast<uchar32>((codepoint & 0xfff) | (*--cur << 12));

                        if ((codepoint & 0xf0000) == 0xe0000)	//  3 octets.
                            return static_cast<uchar32>(codepoint & 0xffff);

                        if ((codepoint & 0xc0000) == 0x80000 && cur != begin) {
                            if ((*--cur & 0xf8) == 0xf0)	//  4 octets.
                                return static_cast<uchar32>((codepoint & 0x3ffff) | ((*cur & 7) << 18));
                        }
                    }
                }
                return regex_internal::constants::invalid_u32value;
            }

            template <typename BidirectionalIterator>
            static uchar32 dec_codepoint(BidirectionalIterator& cur, const BidirectionalIterator begin) {
                uchar32 codepoint = static_cast<uchar32>(*--cur);

                if ((codepoint & 0x80) == 0)
                    return static_cast<uchar32>(codepoint & 0xff);

                if ((codepoint & 0x40) == 0 && cur != begin) {
                    codepoint = static_cast<uchar32>((codepoint & 0x3f) | (*--cur << 6));

                    //  11 0bbb bbaa aaaa?
                    if ((codepoint & 0x3800) == 0x3000)	//  2 octets.
        //			if ((*cur & 0xe0) == 0xc0)
                        return static_cast<uchar32>(codepoint & 0x7ff);

                    //  10 bbbb bbaa aaaa?
                    if ((codepoint & 0x3000) == 0x2000 && cur != begin)	//  [\x80-\xbf]{2}.
        //			if ((*cur & 0xc0) == 0x80 && cur != begin)
                    {
                        codepoint = static_cast<uchar32>((codepoint & 0xfff) | (*--cur << 12));

                        //  1110 cccc bbbb bbaa aaaa?
                        if ((codepoint & 0xf0000) == 0xe0000)	//  3 octets.
        //				if ((*cur & 0xf0) == 0xe0)
                            return static_cast<uchar32>(codepoint & 0xffff);

                        //  10cc cccc bbbb bbaa aaaa?
                        if ((codepoint & 0xc0000) == 0x80000 && cur != begin)	//  [\x80-\xbf]{3}.
        //				if ((*cur & 0xc0) == 0x80 && cur != begin)
                        {
                            if ((*--cur & 0xf8) == 0xf0)	//  4 octets.
                                return static_cast<uchar32>((codepoint & 0x3ffff) | ((*cur & 7) << 18));
                            //  d ddcc cccc bbbb bbaa aaaa
                        //else	//  [\0-\xef\xf8-\xff][\x80-\xbf]{3}.

                        //  Sequences [\xc0-\xdf][\x80-\xbf] and [\xe0-\xef][\x80-\xbf]{2} are valid.
                        //  To give a chance to them, rewinds cur.
                            ++cur;
                        }
                        //else	//  [\0-\x7f\xc0-\xdf\xf0-\xff][\x80-\xbf]{2}.
                        ++cur;	//  Sequence [\xc0-\xdf][\x80-\xbf] is valid. Rewinds to give a chance to it.
                    }
                    //else	//  [\0-\x7f\xe0-\xff][\x80-\xbf].
                    ++cur;	//  Rewinds to give a chance to [\0-\x7f].
                }
                //else	//  [\xc0-\xff].

                return regex_internal::constants::invalid_u32value;
            }

#if !defined(SRELLDBG_NO_BMH)

            template <typename charT2>
            static bool is_trailing(const charT2 cu) {
                return (cu & 0xc0) == 0x80;
            }

#endif	//  !defined(SRELLDBG_NO_BMH)

            static uchar32 to_codeunits(charT out[maxseqlen], uchar32 cp) {
                if (cp < 0x80) {
                    out[0] = static_cast<charT>(cp);
                    return 1;
                } else if (cp < 0x800) {
                    out[0] = static_cast<charT>(((cp >> 6) & 0x1f) | 0xc0);
                    out[1] = static_cast<charT>((cp & 0x3f) | 0x80);
                    return 2;
                } else if (cp < 0x10000) {
                    out[0] = static_cast<charT>(((cp >> 12) & 0x0f) | 0xe0);
                    out[1] = static_cast<charT>(((cp >> 6) & 0x3f) | 0x80);
                    out[2] = static_cast<charT>((cp & 0x3f) | 0x80);
                    return 3;
                }
                //		else	//  if (cp < 0x110000)
                {
                    out[0] = static_cast<charT>(((cp >> 18) & 0x07) | 0xf0);
                    out[1] = static_cast<charT>(((cp >> 12) & 0x3f) | 0x80);
                    out[2] = static_cast<charT>(((cp >> 6) & 0x3f) | 0x80);
                    out[3] = static_cast<charT>((cp & 0x3f) | 0x80);
                    return 4;
                }
            }

            static uchar32 firstcodeunit(const uchar32 cp) {
                if (cp < 0x80)
                    return cp;

                if (cp < 0x800)
                    return static_cast<uchar32>(((cp >> 6) & 0x1f) | 0xc0);

                if (cp < 0x10000)
                    return static_cast<uchar32>(((cp >> 12) & 0x0f) | 0xe0);

                return static_cast<uchar32>(((cp >> 18) & 0x07) | 0xf0);
            }

            template <typename ForwardIterator>
            static bool seek_charboundary(ForwardIterator& begin, const ForwardIterator end) {
                for (; begin != end; ++begin) {
                    //			if ((*begin & 0xc0) != 0x80 && (*begin & 0xf8) != 0xf8)	//  00-7f, c0-f7.
                    if ((*begin & 0xc0) != 0x80)	//  00-7f, c0-ff.
                        return true;
                }
                return false;
            }
        };	//  utf8_traits

        //  utf-16 specific.
        template <typename charT>
        struct utf16_traits : public utf_traits_core<charT> {
        public:

            //  utf-16 specific.
            static const std::size_t maxseqlen = 2;
            static const int utftype = 16;

            static const std::size_t bitsetsize = 0x10000;
            static const uchar32 bitsetmask = 0xffff;
            static const uchar32 cumask = 0xffff;

            template <typename ForwardIterator>
            static uchar32 codepoint(ForwardIterator begin, const ForwardIterator end) {
                const uchar32 codeunit = *begin;

                if ((codeunit & 0xdc00) != 0xd800)
                    return static_cast<uchar32>(codeunit & 0xffff);

                if (++begin != end && (*begin & 0xdc00) == 0xdc00)
                    return static_cast<uchar32>((((codeunit & 0x3ff) << 10) | (*begin & 0x3ff)) + 0x10000);

                return static_cast<uchar32>(codeunit & 0xffff);
            }

            template <typename ForwardIterator>
            static uchar32 codepoint_inc(ForwardIterator& begin, const ForwardIterator end) {
                const uchar32 codeunit = *begin++;

                if ((codeunit & 0xdc00) != 0xd800)
                    return static_cast<uchar32>(codeunit & 0xffff);

                if (begin != end && (*begin & 0xdc00) == 0xdc00)
                    return static_cast<uchar32>((((codeunit & 0x3ff) << 10) | (*begin++ & 0x3ff)) + 0x10000);

                return static_cast<uchar32>(codeunit & 0xffff);
            }

            template <typename BidirectionalIterator>
            static uchar32 prevcodepoint(BidirectionalIterator cur, const BidirectionalIterator begin) {
                const uchar32 codeunit = *--cur;

                if ((codeunit & 0xdc00) != 0xdc00 || cur == begin)
                    return static_cast<uchar32>(codeunit & 0xffff);

                if ((*--cur & 0xdc00) == 0xd800)
                    return static_cast<uchar32>((((*cur & 0x3ff) << 10) | (codeunit & 0x3ff)) + 0x10000);

                return static_cast<uchar32>(codeunit & 0xffff);
            }

            template <typename BidirectionalIterator>
            static uchar32 dec_codepoint(BidirectionalIterator& cur, const BidirectionalIterator begin) {
                const uchar32 codeunit = *--cur;

                if ((codeunit & 0xdc00) != 0xdc00 || cur == begin)
                    return static_cast<uchar32>(codeunit & 0xffff);

                if ((*--cur & 0xdc00) == 0xd800)
                    return static_cast<uchar32>((((*cur & 0x3ff) << 10) | (codeunit & 0x3ff)) + 0x10000);
                //else	//  (codeunit & 0xdc00) == 0xdc00 && (*cur & 0xdc00) != 0xd800

                ++cur;

                return static_cast<uchar32>(codeunit & 0xffff);
            }

#if !defined(SRELLDBG_NO_BMH)

            template <typename charT2>
            static bool is_trailing(const charT2 cu) {
                return (cu & 0xdc00) == 0xdc00;
            }

#endif	//  !defined(SRELLDBG_NO_BMH)

            static uchar32 to_codeunits(charT out[maxseqlen], uchar32 cp) {
                if (cp < 0x10000) {
                    out[0] = static_cast<charT>(cp);
                    return 1;
                }
                //		else	//  if (cp < 0x110000)
                {
                    cp -= 0x10000;
                    out[0] = static_cast<charT>(((cp >> 10) & 0x3ff) | 0xd800);
                    out[1] = static_cast<charT>((cp & 0x3ff) | 0xdc00);
                    return 2;
                }
            }

            static uchar32 firstcodeunit(const uchar32 cp) {
                if (cp < 0x10000)
                    return cp;

                return static_cast<uchar32>((cp >> 10) + 0xd7c0);
                //  aaaaa bbbbcccc ddddeeee -> AA AAbb bbcc/cc dddd eeee where AAAA = aaaaa - 1.
            }

            template <typename ForwardIterator>
            static bool seek_charboundary(ForwardIterator& begin, const ForwardIterator end) {
                for (; begin != end; ++begin) {
                    if ((*begin & 0xdc00) != 0xdc00)
                        return true;
                }
                return false;
            }
        };	//  utf16_traits

        //  specialisation for char.
        template <>
        struct utf_traits<char> : public utf_traits_core<char> {
        public:

            template <typename ForwardIterator>
            static uchar32 codepoint(ForwardIterator begin, const ForwardIterator /* end */) {
                return static_cast<uchar32>(static_cast<unsigned char>(*begin));
            }

            template <typename ForwardIterator>
            static uchar32 codepoint_inc(ForwardIterator& begin, const ForwardIterator /* end */) {
                return static_cast<uchar32>(static_cast<unsigned char>(*begin++));
            }

            template <typename BidirectionalIterator>
            static uchar32 prevcodepoint(BidirectionalIterator cur, const BidirectionalIterator /* begin */) {
                return static_cast<uchar32>(static_cast<unsigned char>(*--cur));
            }

            template <typename BidirectionalIterator>
            static uchar32 dec_codepoint(BidirectionalIterator& cur, const BidirectionalIterator /* begin */) {
                return static_cast<uchar32>(static_cast<unsigned char>(*--cur));
            }

#if !defined(SRELLDBG_NO_BMH)
#endif	//  !defined(SRELLDBG_NO_BMH)
        };	//  utf_traits<char>

        //  specialisation for signed char.
        template <>
        struct utf_traits<signed char> : public utf_traits<char> {
        };

        //  (signed) short, (signed) int, (signed) long, (signed) long long, ...

#if defined(SRELL_CPP11_CHAR1632_ENABLED)
        template <>
        struct utf_traits<char16_t> : public utf16_traits<char16_t> {
        };
#endif

#if defined(SRELL_CPP20_CHAR8_ENABLED)
        template <>
        struct utf_traits<char8_t> : public utf8_traits<char8_t> {
        };
#endif

    }	//  regex_internal

//  ... "rei_utf_traits.hpp"]
//  ["regex_traits.hpp" ...

//  28.7, class template regex_traits:
    template <class charT>
    struct regex_traits {
    public:

        typedef charT char_type;
        typedef std::basic_string<char_type> string_type;
        typedef std::locale locale_type;
        //	typedef bitmask_type char_class_type;
        typedef int char_class_type;

        typedef regex_internal::utf_traits<charT> utf_traits;

    public:

        //	regex_traits();

        static std::size_t length(const char_type* p) {
            return std::char_traits<charT>::length(p);
        }

        charT translate(const charT c) const {
            return c;
        }

        charT translate_nocase(const charT c) const {
            return c;
        }

        template <class ForwardIterator>
        string_type transform(ForwardIterator first, ForwardIterator last) const {
            return string_type(first, last);
        }

        template <class ForwardIterator>
        string_type transform_primary(ForwardIterator first, ForwardIterator last) const {
            return string_type(first, last);
        }

        template <class ForwardIterator>
        string_type lookup_collatename(ForwardIterator first, ForwardIterator last) const {
            return string_type(first, last);
        }

        template <class ForwardIterator>
        char_class_type lookup_classname(ForwardIterator /* first */, ForwardIterator /* last */, bool /* icase */ = false) const {
            return static_cast<char_class_type>(0);
        }

        bool isctype(const charT /* c */, const char_class_type /* f */) const {
            return false;
        }

        int value(const charT /* ch */, const int /* radix */) const {
            return -1;
        }

        locale_type imbue(const locale_type /* l */) {
            return locale_type();
        }

        locale_type getloc() const {
            return locale_type();
        }
    };	//  regex_traits

    template <class charT>
    struct u8regex_traits : public regex_traits<charT> {
        typedef regex_internal::utf8_traits<charT> utf_traits;
    };

    template <class charT>
    struct u16regex_traits : public regex_traits<charT> {
        typedef regex_internal::utf16_traits<charT> utf_traits;
    };

    //  ... "regex_traits.hpp"]
    //  ["rei_memory.hpp" ...

    namespace regex_internal {
        /*
         *  Similar to std::basic_string, except for:
         *    a. only allocates memory, does not initialise it.
         *    b. uses realloc() to avoid moving data as much as possible when
         *       resizing an allocated buffer.
         */
        template <typename ElemT>
        class simple_array {
        public:

            typedef ElemT value_type;
            typedef std::size_t size_type;
            typedef ElemT& reference;
            typedef const ElemT& const_reference;
            typedef ElemT* pointer;
            typedef const ElemT* const_pointer;

            static const size_type npos = static_cast<size_type>(-1);

        public:

            simple_array()
                : buffer_(NULL)
                , size_(0)
                , capacity_(0) {
            }

            simple_array(const size_type initsize)
                : buffer_(NULL)
                , size_(0)
                , capacity_(0) {
                if (initsize) {
                    buffer_ = static_cast<pointer>(std::malloc(initsize * sizeof(ElemT)));

                    if (buffer_ != NULL)
                        size_ = capacity_ = initsize;
                    else
                        throw std::bad_alloc();
                }
            }

            simple_array(const simple_array& right, size_type pos, size_type len = npos)
                : buffer_(NULL)
                , size_(0)
                , capacity_(0) {
                if (pos > right.size_)
                    pos = right.size_;

                {
                    const size_type len2 = right.size_ - pos;
                    if (len > len2)
                        len = len2;
                }

                if (len) {
                    buffer_ = static_cast<pointer>(std::malloc(len * sizeof(ElemT)));

                    if (buffer_ != NULL) {
                        for (capacity_ = len; size_ < capacity_;)
                            buffer_[size_++] = right[pos++];
                    } else {
                        throw std::bad_alloc();
                    }
                }
            }

            simple_array(const simple_array& right)
                : buffer_(NULL)
                , size_(0)
                , capacity_(0) {
                operator=(right);
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            simple_array(simple_array&& right) SRELL_NOEXCEPT
                : buffer_(right.buffer_)
                , size_(right.size_)
                , capacity_(right.capacity_) {
                right.size_ = 0;
                right.capacity_ = 0;
                right.buffer_ = NULL;
            }
#endif

            simple_array& operator=(const simple_array& right) {
                if (this != &right) {
                    resize(right.size_);
                    for (size_type i = 0; i < right.size_; ++i)
                        buffer_[i] = right.buffer_[i];
                }
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            simple_array& operator=(simple_array&& right) SRELL_NOEXCEPT {
                if (this != &right) {
                    if (this->buffer_ != NULL)
                        std::free(this->buffer_);

                    this->size_ = right.size_;
                    this->capacity_ = right.capacity_;
                    this->buffer_ = right.buffer_;

                    right.size_ = 0;
                    right.capacity_ = 0;
                    right.buffer_ = NULL;
                }
                return *this;
            }
#endif

            ~simple_array() {
                if (buffer_ != NULL)
                    std::free(buffer_);
            }

            size_type size() const {
                return size_;
            }

            void clear() {
                size_ = 0;
            }

            void resize(const size_type newsize) {
                if (newsize > capacity_)
                    reserve(newsize);

                size_ = newsize;
            }

            void resize(const size_type newsize, const ElemT& type) {
                size_type oldsize = size_;

                resize(newsize);
                for (; oldsize < size_; ++oldsize)
                    buffer_[oldsize] = type;
            }

            reference operator[](const size_type pos) {
                return buffer_[pos];
            }

            const_reference operator[](const size_type pos) const {
                return buffer_[pos];
            }

            void push_back(const_reference n) {
                const size_type oldsize = size_;

                if (++size_ > capacity_)
                    reserve(size_);

                buffer_[oldsize] = n;
            }

            const_reference back() const {
                return buffer_[size_ - 1];
            }

            reference back() {
                return buffer_[size_ - 1];
            }

            void pop_back() {
                --size_;
            }

            simple_array& operator+=(const simple_array& right) {
                return append(right);
            }

            simple_array& append(const size_type size, const ElemT& type) {
                resize(size_ + size, type);
                return *this;
            }

            simple_array& append(const simple_array& right) {
                size_type oldsize = size_;

                resize(size_ + right.size_);
                for (size_type i = 0; i < right.size_; ++i, ++oldsize)
                    buffer_[oldsize] = right.buffer_[i];

                return *this;
            }

            simple_array& append(const simple_array& right, size_type pos, size_type len /* = npos */) {
                {
                    const size_type len2 = right.size_ - pos;
                    if (len > len2)
                        len = len2;
                }

                size_type oldsize = size_;

                resize(size_ + len);
                len += pos;	//  end.
                for (; pos < len; ++oldsize, ++pos)
                    buffer_[oldsize] = right.buffer_[pos];

                return *this;
            }

            //  For rei_char_class class.
            void erase(const size_type pos) {
                if (pos < size_) {
                    std::memmove(buffer_ + pos, buffer_ + pos + 1, (size_ - pos - 1) * sizeof(ElemT));
                    --size_;
                }
            }

            //  For rei_compiler class.
            void insert(const size_type pos, const ElemT& type) {
                move_forward(pos, 1);
                buffer_[pos] = type;
            }

            void insert(size_type pos, const simple_array& right) {
                move_forward(pos, right.size_);
                for (size_type i = 0; i < right.size_; ++i, ++pos)
                    buffer_[pos] = right.buffer_[i];
            }

            void insert(size_type destpos, const simple_array& right, size_type srcpos, size_type srclen = npos) {
                {
                    const size_type len2 = right.size_ - srcpos;
                    if (srclen > len2)
                        srclen = len2;
                }

                move_forward(destpos, srclen);
                srclen += srcpos;	//  srcend.
                for (; srcpos < srclen; ++destpos, ++srcpos)
                    buffer_[destpos] = right.buffer_[srcpos];
            }

            simple_array& replace(size_type pos, size_type count, const simple_array& right) {
                if (count < right.size_)
                    move_forward(pos + count, right.size_ - count);
                else if (count > right.size_) {
                    const pointer base = buffer_ + pos;

                    std::memmove(base + right.size_, base + count, (size_ - pos - count) * sizeof(ElemT));
                    size_ -= count - right.size_;
                }

                for (size_type i = 0; i < right.size_; ++pos, ++i)
                    buffer_[pos] = right[i];

                return *this;
            }

            size_type find(const value_type c, size_type pos = 0) const {
                for (; pos <= size_; ++pos)
                    if (buffer_[pos] == c)
                        return pos;

                return npos;
            }

            const_pointer data() const {
                return buffer_;
            }

            int compare(size_type pos, const size_type count1, const_pointer p, const size_type count2) const {
                size_type count = count1 <= count2 ? count1 : count2;

                for (; count; ++pos, ++p, --count) {
                    const value_type& v = buffer_[pos];
                    if (v != *p)
                        return v < *p ? -1 : 1;
                }
                return count1 == count2 ? 0 : (count1 < count2 ? -1 : 1);
            }

            size_type max_size() const {
                return maxsize_;
            }

            void swap(simple_array& right) {
                if (this != &right) {
                    const pointer tmpbuffer = this->buffer_;
                    const size_type tmpsize = this->size_;
                    const size_type tmpcapacity = this->capacity_;

                    this->buffer_ = right.buffer_;
                    this->size_ = right.size_;
                    this->capacity_ = right.capacity_;

                    right.buffer_ = tmpbuffer;
                    right.size_ = tmpsize;
                    right.capacity_ = tmpcapacity;
                }
            }

        private:

            void reserve(const size_type newsize) {
                //		if (newsize > capacity_)
                {
                    if (newsize <= maxsize_) {
                        //				capacity_ = newsize + (newsize >> 1);	//  newsize * 1.5.
                        capacity_ = ((newsize >> 8) + 1) << 8;	//  Round up to a multiple of 256.

                        if (capacity_ > maxsize_)
                            capacity_ = maxsize_;

                        const size_type newsize_in_byte = capacity_ * sizeof(ElemT);
                        const pointer oldbuffer = buffer_;

                        buffer_ = static_cast<pointer>(std::realloc(buffer_, newsize_in_byte));
                        if (buffer_ != NULL)
                            return;

                        //  Even if realloc() failed, already-existing buffer remains valid.
                        std::free(oldbuffer);
                        //				buffer_ = NULL;
                        size_ = capacity_ = 0;
                    }
                    throw std::bad_alloc();
                }
            }

            void move_forward(const size_type pos, const size_type count) {
                const size_type oldsize = size_;

                resize(size_ + count);

                if (pos < oldsize) {
                    const pointer base = buffer_ + pos;

                    std::memmove(base + count, base, (oldsize - pos) * sizeof(ElemT));
                }
            }

        private:

            pointer buffer_;
            size_type size_;
            size_type capacity_;

            //	static const size_type maxsize_ = (npos - sizeof (simple_array)) / sizeof (ElemT);
            static const size_type maxsize_ = (npos - sizeof(pointer) - sizeof(size_type) * 2) / sizeof(ElemT) / 2;
        };
        //  simple_array

    }	//  namespace regex_internal

//  ... "rei_memory.hpp"]
//  ["rei_bitset.hpp" ...

    namespace regex_internal {

        //  Always uses a heap instead of the stack.
        template <const std::size_t Bits>
        class bitset {
        private:

            typedef unsigned long array_type;

        public:

            bitset()
                : buffer_(static_cast<array_type*>(std::malloc(size_in_byte_))) {
                if (buffer_ != NULL) {
                    reset();
                    return;
                }
                throw std::bad_alloc();
            }

            bitset(const bitset& right)
                : buffer_(static_cast<array_type*>(std::malloc(size_in_byte_))) {
                if (buffer_ != NULL) {
                    operator=(right);
                    return;
                }
                throw std::bad_alloc();
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            bitset(bitset&& right) SRELL_NOEXCEPT
                : buffer_(right.buffer_) {
                right.buffer_ = NULL;
            }
#endif

            bitset& operator=(const bitset& right) {
                if (this != &right) {
                    //			for (std::size_t i = 0; i < arraylength_; ++i)
                    //				buffer_[i] = right.buffer_[i];
                    std::memcpy(buffer_, right.buffer_, size_in_byte_);
                }
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            bitset& operator=(bitset&& right) SRELL_NOEXCEPT {
                if (this != &right) {
                    if (this->buffer_ != NULL)
                        std::free(this->buffer_);

                    this->buffer_ = right.buffer_;
                    right.buffer_ = NULL;
                }
                return *this;
            }
#endif

            ~bitset() {
                if (buffer_ != NULL)
                    std::free(buffer_);
            }

            bitset& reset() {
                std::memset(buffer_, 0, size_in_byte_);
                return *this;
            }

            bitset& reset(const std::size_t bit) {
                buffer_[bit / bits_per_elem_] &= ~(1 << (bit & bitmask_));
                return *this;
            }

            bitset& set(const std::size_t bit) {
                buffer_[bit / bits_per_elem_] |= (1 << (bit & bitmask_));
                return *this;
            }

#if 0
            void set_range(const std::size_t firstbit, const std::size_t lastbit) {
                const std::size_t lastelemidx = lastbit / bits_per_elem_;
                std::size_t firstelemidx = firstbit / bits_per_elem_;
                const array_type lastelemmask = ~(allbits1_ << ((lastbit & bitmask_) + 1));
                array_type ormask = allbits1_ << (firstbit & bitmask_);

                if (firstelemidx < lastelemidx) {
                    buffer_[firstelemidx] |= ormask;
                    ormask = allbits1_;

                    for (++firstelemidx; firstelemidx < lastelemidx; ++firstelemidx)
                        buffer_[firstelemidx] |= ormask;
                }
                ormask &= lastelemmask;
                buffer_[lastelemidx] |= ormask;

            }
#endif

            bool test(const std::size_t bit) const {
                return (buffer_[bit / bits_per_elem_] & (1 << (bit & bitmask_))) != 0;
            }

            bool operator[](const std::size_t bit) const {
                return (buffer_[bit / bits_per_elem_] & (1 << (bit & bitmask_))) != 0;
            }

            bitset<Bits>& flip() {
                for (std::size_t i = 0; i < arraylength_; ++i)
                    buffer_[i] = ~buffer_[i];
                return *this;
            }

            void swap(bitset& right) {
                if (this != &right) {
                    array_type* const tmpbuffer = this->buffer_;
                    this->buffer_ = right.buffer_;
                    right.buffer_ = tmpbuffer;
                }
            }

        private:

#if defined(__cpp_constexpr)
            static constexpr std::size_t pow2leN(const std::size_t n, const std::size_t p2) {
                return ((p2 << 1) == 0 || (p2 << 1) > n) ? p2 : pow2leN(n, p2 << 1);
            }
            static const std::size_t bits_per_elem_ = pow2leN(CHAR_BIT * sizeof(array_type), 8);
#else
            static const std::size_t bpe_tmp_ = CHAR_BIT * sizeof(array_type);
            static const std::size_t bits_per_elem_ = bpe_tmp_ >= 64 ? 64 : (bpe_tmp_ >= 32 ? 32 : (bpe_tmp_ >= 16 ? 16 : 8));
#endif
            static const std::size_t bitmask_ = bits_per_elem_ - 1;
            static const std::size_t arraylength_ = (Bits + bitmask_) / bits_per_elem_;
            static const std::size_t size_in_byte_ = arraylength_ * sizeof(array_type);
            static const array_type allbits1_ = ~static_cast<array_type>(0);

            array_type* buffer_;
        };

    }	//  namespace regex_internal

//  ... "rei_bitset.hpp"]
//  ["rei_ucf.hpp" ...

    namespace regex_internal {

#if !defined(SRELL_NO_UNICODE_ICASE)
        //  ["srell_ucfdata2.hpp" ...
        //  CaseFolding-14.0.0.txt
        //  Date: 2021-03-08, 19:35:41 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html

        template <typename T2, typename T3>
        struct unicode_casefolding {
            static const T2 ucf_maxcodepoint = 0x1E921;
            static const T3 ucf_deltatablesize = 0x1900;
            static const T2 rev_maxcodepoint = 0x1E943;
            static const T3 rev_indextablesize = 0x1B00;
            static const T3 rev_charsettablesize = 4303;	//  1 + 1424 * 2 + 1454
            static const T3 rev_maxset = 4;
            static const T2 eos = 0;

            static const T2 ucf_deltatable[];
            static const T3 ucf_segmenttable[];
            static const T3 rev_indextable[];
            static const T3 rev_segmenttable[];
            static const T2 rev_charsettable[];

            static const T2* ucf_deltatable_ptr() {
                return ucf_deltatable;
            }
            static const T3* ucf_segmenttable_ptr() {
                return ucf_segmenttable;
            }
            static const T3* rev_indextable_ptr() {
                return rev_indextable;
            }
            static const T3* rev_segmenttable_ptr() {
                return rev_segmenttable;
            }
            static const T2* rev_charsettable_ptr() {
                return rev_charsettable;
            }
        };

        template <typename T2, typename T3>
        const T2 unicode_casefolding<T2, T3>::ucf_deltatable[] =
        {
            //  For common (0)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+00xx (256)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 775, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 32, 32,  32, 32, 32, 0,  32, 32, 32, 32,  32, 32, 32, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+01xx (512)
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            0, 0, 1, 0,  1, 0, 1, 0,  0, 1, 0, 1,  0, 1, 0, 1,
            0, 1, 0, 1,  0, 1, 0, 1,  0, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  static_cast<T2>(-121), 1, 0, 1,  0, 1, 0, static_cast<T2>(-268),
            0, 210, 1, 0,  1, 0, 206, 1,  0, 205, 205, 1,  0, 0, 79, 202,
            203, 1, 0, 205,  207, 0, 211, 209,  1, 0, 0, 0,  211, 213, 0, 214,
            1, 0, 1, 0,  1, 0, 218, 1,  0, 218, 0, 0,  1, 0, 218, 1,
            0, 217, 217, 1,  0, 1, 0, 219,  1, 0, 0, 0,  1, 0, 0, 0,
            0, 0, 0, 0,  2, 1, 0, 2,  1, 0, 2, 1,  0, 1, 0, 1,
            0, 1, 0, 1,  0, 1, 0, 1,  0, 1, 0, 1,  0, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            0, 2, 1, 0,  1, 0, static_cast<T2>(-97), static_cast<T2>(-56),  1, 0, 1, 0,  1, 0, 1, 0,

            //  For u+02xx (768)
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            static_cast<T2>(-130), 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  0, 0, 0, 0,  0, 0, 10795, 1,  0, static_cast<T2>(-163), 10792, 0,
            0, 1, 0, static_cast<T2>(-195),  69, 71, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+03xx (1024)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 116, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1, 0, 1, 0,  0, 0, 1, 0,  0, 0, 0, 0,  0, 0, 0, 116,
            0, 0, 0, 0,  0, 0, 38, 0,  37, 37, 37, 0,  64, 0, 63, 63,
            0, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 0, 32,  32, 32, 32, 32,  32, 32, 32, 32,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 1, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 8,
            static_cast<T2>(-30), static_cast<T2>(-25), 0, 0,  0, static_cast<T2>(-15), static_cast<T2>(-22), 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            static_cast<T2>(-54), static_cast<T2>(-48), 0, 0,  static_cast<T2>(-60), static_cast<T2>(-64), 0, 1,  0, static_cast<T2>(-7), 1, 0,  0, static_cast<T2>(-130), static_cast<T2>(-130), static_cast<T2>(-130),

            //  For u+04xx (1280)
            80, 80, 80, 80,  80, 80, 80, 80,  80, 80, 80, 80,  80, 80, 80, 80,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 0, 0,  0, 0, 0, 0,  0, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            15, 1, 0, 1,  0, 1, 0, 1,  0, 1, 0, 1,  0, 1, 0, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,

            //  For u+05xx (1536)
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            0, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,
            48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,
            48, 48, 48, 48,  48, 48, 48, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+10xx (1792)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            7264, 7264, 7264, 7264,  7264, 7264, 7264, 7264,  7264, 7264, 7264, 7264,  7264, 7264, 7264, 7264,
            7264, 7264, 7264, 7264,  7264, 7264, 7264, 7264,  7264, 7264, 7264, 7264,  7264, 7264, 7264, 7264,
            7264, 7264, 7264, 7264,  7264, 7264, 0, 7264,  0, 0, 0, 0,  0, 7264, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+13xx (2048)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), 0, 0,

            //  For u+1Cxx (2304)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            static_cast<T2>(-6222), static_cast<T2>(-6221), static_cast<T2>(-6212), static_cast<T2>(-6210),  static_cast<T2>(-6210), static_cast<T2>(-6211), static_cast<T2>(-6204), static_cast<T2>(-6180),  35267, 0, 0, 0,  0, 0, 0, 0,
            static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),
            static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),
            static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),  static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008), 0,  0, static_cast<T2>(-3008), static_cast<T2>(-3008), static_cast<T2>(-3008),
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+1Exx (2560)
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 0, 0,  0, 0, 0, static_cast<T2>(-58),  0, 0, static_cast<T2>(-7615), 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,

            //  For u+1Fxx (2816)
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, static_cast<T2>(-8), 0, static_cast<T2>(-8),  0, static_cast<T2>(-8), 0, static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-8),
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-74), static_cast<T2>(-74),  static_cast<T2>(-9), 0, static_cast<T2>(-7173), 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-86), static_cast<T2>(-86), static_cast<T2>(-86), static_cast<T2>(-86),  static_cast<T2>(-9), 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-100), static_cast<T2>(-100),  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-8), static_cast<T2>(-8), static_cast<T2>(-112), static_cast<T2>(-112),  static_cast<T2>(-7), 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  static_cast<T2>(-128), static_cast<T2>(-128), static_cast<T2>(-126), static_cast<T2>(-126),  static_cast<T2>(-9), 0, 0, 0,

            //  For u+21xx (3072)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, static_cast<T2>(-7517), 0,  0, 0, static_cast<T2>(-8383), static_cast<T2>(-8262),  0, 0, 0, 0,
            0, 0, 28, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            16, 16, 16, 16,  16, 16, 16, 16,  16, 16, 16, 16,  16, 16, 16, 16,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 1,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+24xx (3328)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 26, 26,  26, 26, 26, 26,  26, 26, 26, 26,
            26, 26, 26, 26,  26, 26, 26, 26,  26, 26, 26, 26,  26, 26, 26, 26,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+2Cxx (3584)
            48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,
            48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,
            48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,  48, 48, 48, 48,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1, 0, static_cast<T2>(-10743), static_cast<T2>(-3814),  static_cast<T2>(-10727), 0, 0, 1,  0, 1, 0, 1,  0, static_cast<T2>(-10780), static_cast<T2>(-10749), static_cast<T2>(-10783),
            static_cast<T2>(-10782), 0, 1, 0,  0, 1, 0, 0,  0, 0, 0, 0,  0, 0, static_cast<T2>(-10815), static_cast<T2>(-10815),
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  0, 0, 0, 0,  0, 0, 0, 1,  0, 1, 0, 0,
            0, 0, 1, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+A6xx (3840)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+A7xx (4096)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            0, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 1, 0, 1,  0, static_cast<T2>(-35332), 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  0, 0, 0, 1,  0, static_cast<T2>(-42280), 0, 0,
            1, 0, 1, 0,  0, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  1, 0, 1, 0,  1, 0, static_cast<T2>(-42308), static_cast<T2>(-42319),  static_cast<T2>(-42315), static_cast<T2>(-42305), static_cast<T2>(-42308), 0,
            static_cast<T2>(-42258), static_cast<T2>(-42282), static_cast<T2>(-42261), 928,  1, 0, 1, 0,  1, 0, 1, 0,  1, 0, 1, 0,
            1, 0, 1, 0,  static_cast<T2>(-48), static_cast<T2>(-42307), static_cast<T2>(-35384), 1,  0, 1, 0, 0,  0, 0, 0, 0,
            1, 0, 0, 0,  0, 0, 1, 0,  1, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 1, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+ABxx (4352)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),
            static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),
            static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),
            static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),
            static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),  static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864), static_cast<T2>(-38864),
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+FFxx (4608)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+104xx (4864)
            40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,
            40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,
            40, 40, 40, 40,  40, 40, 40, 40,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,
            40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,  40, 40, 40, 40,
            40, 40, 40, 40,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+105xx (5120)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            39, 39, 39, 39,  39, 39, 39, 39,  39, 39, 39, 0,  39, 39, 39, 39,
            39, 39, 39, 39,  39, 39, 39, 39,  39, 39, 39, 0,  39, 39, 39, 39,
            39, 39, 39, 0,  39, 39, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+10Cxx (5376)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            64, 64, 64, 64,  64, 64, 64, 64,  64, 64, 64, 64,  64, 64, 64, 64,
            64, 64, 64, 64,  64, 64, 64, 64,  64, 64, 64, 64,  64, 64, 64, 64,
            64, 64, 64, 64,  64, 64, 64, 64,  64, 64, 64, 64,  64, 64, 64, 64,
            64, 64, 64, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+118xx (5632)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+16Exx (5888)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,  32, 32, 32, 32,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+1E9xx (6144)
            34, 34, 34, 34,  34, 34, 34, 34,  34, 34, 34, 34,  34, 34, 34, 34,
            34, 34, 34, 34,  34, 34, 34, 34,  34, 34, 34, 34,  34, 34, 34, 34,
            34, 34, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0
        };

        template <typename T2, typename T3>
        const T3 unicode_casefolding<T2, T3>::ucf_segmenttable[] =
        {
            256, 512, 768, 1024,  1280, 1536, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1792, 0, 0, 2048,  0, 0, 0, 0,  0, 0, 0, 0,  2304, 0, 2560, 2816,
            0, 3072, 0, 0,  3328, 0, 0, 0,  0, 0, 0, 0,  3584, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 3840, 4096,  0, 0, 0, 4352,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 4608,
            0, 0, 0, 0,  4864, 5120, 0, 0,  0, 0, 0, 0,  5376, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  5632, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 5888, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 6144
        };

        template <typename T2, typename T3>
        const T3 unicode_casefolding<T2, T3>::rev_indextable[] =
        {
            //  For common (0)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+00xx (256)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 1, 4, 7,  10, 13, 16, 19,  22, 25, 28, 31,  35, 38, 41, 44,
            47, 50, 53, 56,  60, 63, 66, 69,  72, 75, 78, 0,  0, 0, 0, 0,
            0, 1, 4, 7,  10, 13, 16, 19,  22, 25, 28, 31,  35, 38, 41, 44,
            47, 50, 53, 56,  60, 63, 66, 69,  72, 75, 78, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 81, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            85, 88, 91, 94,  97, 100, 104, 107,  110, 113, 116, 119,  122, 125, 128, 131,
            134, 137, 140, 143,  146, 149, 152, 0,  155, 158, 161, 164,  167, 170, 173, 1924,
            85, 88, 91, 94,  97, 100, 104, 107,  110, 113, 116, 119,  122, 125, 128, 131,
            134, 137, 140, 143,  146, 149, 152, 0,  155, 158, 161, 164,  167, 170, 173, 350,

            //  For u+21xx (512)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 785, 0,  0, 0, 31, 100,  0, 0, 0, 0,
            0, 0, 2359, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 2359, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            2362, 2365, 2368, 2371,  2374, 2377, 2380, 2383,  2386, 2389, 2392, 2395,  2398, 2401, 2404, 2407,
            2362, 2365, 2368, 2371,  2374, 2377, 2380, 2383,  2386, 2389, 2392, 2395,  2398, 2401, 2404, 2407,
            0, 0, 0, 2410,  2410, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+01xx (768)
            176, 176, 179, 179,  182, 182, 185, 185,  188, 188, 191, 191,  194, 194, 197, 197,
            200, 200, 203, 203,  206, 206, 209, 209,  212, 212, 215, 215,  218, 218, 221, 221,
            224, 224, 227, 227,  230, 230, 233, 233,  236, 236, 239, 239,  242, 242, 245, 245,
            0, 0, 248, 248,  251, 251, 254, 254,  0, 257, 257, 260,  260, 263, 263, 266,
            266, 269, 269, 272,  272, 275, 275, 278,  278, 0, 281, 281,  284, 284, 287, 287,
            290, 290, 293, 293,  296, 296, 299, 299,  302, 302, 305, 305,  308, 308, 311, 311,
            314, 314, 317, 317,  320, 320, 323, 323,  326, 326, 329, 329,  332, 332, 335, 335,
            338, 338, 341, 341,  344, 344, 347, 347,  350, 353, 353, 356,  356, 359, 359, 56,
            651, 362, 365, 365,  368, 368, 371, 374,  374, 377, 380, 383,  383, 0, 386, 389,
            392, 395, 395, 398,  401, 540, 404, 407,  410, 410, 642, 0,  413, 416, 606, 419,
            422, 422, 425, 425,  428, 428, 431, 434,  434, 437, 0, 0,  440, 440, 443, 446,
            446, 449, 452, 455,  455, 458, 458, 461,  464, 464, 0, 0,  467, 467, 0, 543,
            0, 0, 0, 0,  470, 470, 470, 474,  474, 474, 478, 478,  478, 482, 482, 485,
            485, 488, 488, 491,  491, 494, 494, 497,  497, 500, 500, 503,  503, 386, 506, 506,
            509, 509, 512, 512,  515, 515, 518, 518,  521, 521, 524, 524,  527, 527, 530, 530,
            0, 533, 533, 533,  537, 537, 540, 543,  546, 546, 549, 549,  552, 552, 555, 555,

            //  For u+03xx (1024)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 675, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            680, 680, 683, 683,  0, 0, 686, 686,  0, 0, 0, 843,  846, 849, 0, 689,
            0, 0, 0, 0,  0, 0, 692, 0,  695, 698, 701, 0,  704, 0, 707, 710,
            0, 713, 716, 720,  723, 726, 730, 733,  736, 675, 741, 745,  81, 748, 751, 754,
            757, 761, 0, 765,  769, 772, 775, 779,  782, 785, 789, 792,  692, 695, 698, 701,
            0, 713, 716, 720,  723, 726, 730, 733,  736, 675, 741, 745,  81, 748, 751, 754,
            757, 761, 765, 765,  769, 772, 775, 779,  782, 785, 789, 792,  704, 707, 710, 795,
            716, 736, 0, 0,  0, 775, 757, 795,  798, 798, 801, 801,  804, 804, 807, 807,
            810, 810, 813, 813,  816, 816, 819, 819,  822, 822, 825, 825,  828, 828, 831, 831,
            741, 761, 837, 689,  736, 726, 0, 834,  834, 837, 840, 840,  0, 843, 846, 849,

            //  For u+02xx (1280)
            558, 558, 561, 561,  564, 564, 567, 567,  570, 570, 573, 573,  576, 576, 579, 579,
            582, 582, 585, 585,  588, 588, 591, 591,  594, 594, 597, 597,  600, 600, 603, 603,
            606, 0, 609, 609,  612, 612, 615, 615,  618, 618, 621, 621,  624, 624, 627, 627,
            630, 630, 633, 633,  0, 0, 0, 0,  0, 0, 636, 639,  639, 642, 645, 2674,
            2677, 648, 648, 651,  654, 657, 660, 660,  663, 663, 666, 666,  669, 669, 672, 672,
            2662, 2656, 2665, 362,  371, 0, 377, 380,  0, 389, 0, 392,  3130, 0, 0, 0,
            398, 3133, 0, 401,  0, 3088, 3127, 0,  407, 404, 3139, 2638,  3136, 0, 0, 413,
            0, 2659, 416, 0,  0, 419, 0, 0,  0, 0, 0, 0,  0, 2644, 0, 0,
            431, 0, 3181, 437,  0, 0, 0, 3145,  443, 654, 449, 452,  657, 0, 0, 0,
            0, 0, 461, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 3148, 3142, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+2Cxx (1536)
            2491, 2494, 2497, 2500,  2503, 2506, 2509, 2512,  2515, 2518, 2521, 2524,  2527, 2530, 2533, 2536,
            2539, 2542, 2545, 2548,  2551, 2554, 2557, 2560,  2563, 2566, 2569, 2572,  2575, 2578, 2581, 2584,
            2587, 2590, 2593, 2596,  2599, 2602, 2605, 2608,  2611, 2614, 2617, 2620,  2623, 2626, 2629, 2632,
            2491, 2494, 2497, 2500,  2503, 2506, 2509, 2512,  2515, 2518, 2521, 2524,  2527, 2530, 2533, 2536,
            2539, 2542, 2545, 2548,  2551, 2554, 2557, 2560,  2563, 2566, 2569, 2572,  2575, 2578, 2581, 2584,
            2587, 2590, 2593, 2596,  2599, 2602, 2605, 2608,  2611, 2614, 2617, 2620,  2623, 2626, 2629, 2632,
            2635, 2635, 2638, 2641,  2644, 636, 645, 2647,  2647, 2650, 2650, 2653,  2653, 2656, 2659, 2662,
            2665, 0, 2668, 2668,  0, 2671, 2671, 0,  0, 0, 0, 0,  0, 0, 2674, 2677,
            2680, 2680, 2683, 2683,  2686, 2686, 2689, 2689,  2692, 2692, 2695, 2695,  2698, 2698, 2701, 2701,
            2704, 2704, 2707, 2707,  2710, 2710, 2713, 2713,  2716, 2716, 2719, 2719,  2722, 2722, 2725, 2725,
            2728, 2728, 2731, 2731,  2734, 2734, 2737, 2737,  2740, 2740, 2743, 2743,  2746, 2746, 2749, 2749,
            2752, 2752, 2755, 2755,  2758, 2758, 2761, 2761,  2764, 2764, 2767, 2767,  2770, 2770, 2773, 2773,
            2776, 2776, 2779, 2779,  2782, 2782, 2785, 2785,  2788, 2788, 2791, 2791,  2794, 2794, 2797, 2797,
            2800, 2800, 2803, 2803,  2806, 2806, 2809, 2809,  2812, 2812, 2815, 2815,  2818, 2818, 2821, 2821,
            2824, 2824, 2827, 2827,  0, 0, 0, 0,  0, 0, 0, 2830,  2830, 2833, 2833, 0,
            0, 0, 2836, 2836,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+1Fxx (1792)
            2071, 2074, 2077, 2080,  2083, 2086, 2089, 2092,  2071, 2074, 2077, 2080,  2083, 2086, 2089, 2092,
            2095, 2098, 2101, 2104,  2107, 2110, 0, 0,  2095, 2098, 2101, 2104,  2107, 2110, 0, 0,
            2113, 2116, 2119, 2122,  2125, 2128, 2131, 2134,  2113, 2116, 2119, 2122,  2125, 2128, 2131, 2134,
            2137, 2140, 2143, 2146,  2149, 2152, 2155, 2158,  2137, 2140, 2143, 2146,  2149, 2152, 2155, 2158,
            2161, 2164, 2167, 2170,  2173, 2176, 0, 0,  2161, 2164, 2167, 2170,  2173, 2176, 0, 0,
            0, 2179, 0, 2182,  0, 2185, 0, 2188,  0, 2179, 0, 2182,  0, 2185, 0, 2188,
            2191, 2194, 2197, 2200,  2203, 2206, 2209, 2212,  2191, 2194, 2197, 2200,  2203, 2206, 2209, 2212,
            2293, 2296, 2302, 2305,  2308, 2311, 2323, 2326,  2344, 2347, 2335, 2338,  2350, 2353, 0, 0,
            2215, 2218, 2221, 2224,  2227, 2230, 2233, 2236,  2215, 2218, 2221, 2224,  2227, 2230, 2233, 2236,
            2239, 2242, 2245, 2248,  2251, 2254, 2257, 2260,  2239, 2242, 2245, 2248,  2251, 2254, 2257, 2260,
            2263, 2266, 2269, 2272,  2275, 2278, 2281, 2284,  2263, 2266, 2269, 2272,  2275, 2278, 2281, 2284,
            2287, 2290, 0, 2299,  0, 0, 0, 0,  2287, 2290, 2293, 2296,  2299, 0, 675, 0,
            0, 0, 0, 2314,  0, 0, 0, 0,  2302, 2305, 2308, 2311,  2314, 0, 0, 0,
            2317, 2320, 0, 0,  0, 0, 0, 0,  2317, 2320, 2323, 2326,  0, 0, 0, 0,
            2329, 2332, 0, 0,  0, 2341, 0, 0,  2329, 2332, 2335, 2338,  2341, 0, 0, 0,
            0, 0, 0, 2356,  0, 0, 0, 0,  2344, 2347, 2350, 2353,  2356, 0, 0, 0,

            //  For u+04xx (2048)
            852, 855, 858, 861,  864, 867, 870, 873,  876, 879, 882, 885,  888, 891, 894, 897,
            900, 903, 906, 910,  913, 917, 920, 923,  926, 929, 932, 935,  938, 941, 944, 948,
            951, 954, 958, 963,  966, 969, 972, 975,  978, 981, 984, 988,  991, 994, 997, 1000,
            900, 903, 906, 910,  913, 917, 920, 923,  926, 929, 932, 935,  938, 941, 944, 948,
            951, 954, 958, 963,  966, 969, 972, 975,  978, 981, 984, 988,  991, 994, 997, 1000,
            852, 855, 858, 861,  864, 867, 870, 873,  876, 879, 882, 885,  888, 891, 894, 897,
            1003, 1003, 1006, 1006,  1010, 1010, 1013, 1013,  1016, 1016, 1019, 1019,  1022, 1022, 1025, 1025,
            1028, 1028, 1031, 1031,  1034, 1034, 1037, 1037,  1040, 1040, 1043, 1043,  1046, 1046, 1049, 1049,
            1052, 1052, 0, 0,  0, 0, 0, 0,  0, 0, 1055, 1055,  1058, 1058, 1061, 1061,
            1064, 1064, 1067, 1067,  1070, 1070, 1073, 1073,  1076, 1076, 1079, 1079,  1082, 1082, 1085, 1085,
            1088, 1088, 1091, 1091,  1094, 1094, 1097, 1097,  1100, 1100, 1103, 1103,  1106, 1106, 1109, 1109,
            1112, 1112, 1115, 1115,  1118, 1118, 1121, 1121,  1124, 1124, 1127, 1127,  1130, 1130, 1133, 1133,
            1136, 1139, 1139, 1142,  1142, 1145, 1145, 1148,  1148, 1151, 1151, 1154,  1154, 1157, 1157, 1136,
            1160, 1160, 1163, 1163,  1166, 1166, 1169, 1169,  1172, 1172, 1175, 1175,  1178, 1178, 1181, 1181,
            1184, 1184, 1187, 1187,  1190, 1190, 1193, 1193,  1196, 1196, 1199, 1199,  1202, 1202, 1205, 1205,
            1208, 1208, 1211, 1211,  1214, 1214, 1217, 1217,  1220, 1220, 1223, 1223,  1226, 1226, 1229, 1229,

            //  For u+1Cxx (2304)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            906, 913, 944, 954,  958, 958, 984, 1006,  1556, 0, 0, 0,  0, 0, 0, 0,
            1560, 1563, 1566, 1569,  1572, 1575, 1578, 1581,  1584, 1587, 1590, 1593,  1596, 1599, 1602, 1605,
            1608, 1611, 1614, 1617,  1620, 1623, 1626, 1629,  1632, 1635, 1638, 1641,  1644, 1647, 1650, 1653,
            1656, 1659, 1662, 1665,  1668, 1671, 1674, 1677,  1680, 1683, 1686, 0,  0, 1689, 1692, 1695,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+05xx (2560)
            1232, 1232, 1235, 1235,  1238, 1238, 1241, 1241,  1244, 1244, 1247, 1247,  1250, 1250, 1253, 1253,
            1256, 1256, 1259, 1259,  1262, 1262, 1265, 1265,  1268, 1268, 1271, 1271,  1274, 1274, 1277, 1277,
            1280, 1280, 1283, 1283,  1286, 1286, 1289, 1289,  1292, 1292, 1295, 1295,  1298, 1298, 1301, 1301,
            0, 1304, 1307, 1310,  1313, 1316, 1319, 1322,  1325, 1328, 1331, 1334,  1337, 1340, 1343, 1346,
            1349, 1352, 1355, 1358,  1361, 1364, 1367, 1370,  1373, 1376, 1379, 1382,  1385, 1388, 1391, 1394,
            1397, 1400, 1403, 1406,  1409, 1412, 1415, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 1304, 1307, 1310,  1313, 1316, 1319, 1322,  1325, 1328, 1331, 1334,  1337, 1340, 1343, 1346,
            1349, 1352, 1355, 1358,  1361, 1364, 1367, 1370,  1373, 1376, 1379, 1382,  1385, 1388, 1391, 1394,
            1397, 1400, 1403, 1406,  1409, 1412, 1415, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+2Dxx (2816)
            1418, 1421, 1424, 1427,  1430, 1433, 1436, 1439,  1442, 1445, 1448, 1451,  1454, 1457, 1460, 1463,
            1466, 1469, 1472, 1475,  1478, 1481, 1484, 1487,  1490, 1493, 1496, 1499,  1502, 1505, 1508, 1511,
            1514, 1517, 1520, 1523,  1526, 1529, 0, 1532,  0, 0, 0, 0,  0, 1535, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+10xx (3072)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            1418, 1421, 1424, 1427,  1430, 1433, 1436, 1439,  1442, 1445, 1448, 1451,  1454, 1457, 1460, 1463,
            1466, 1469, 1472, 1475,  1478, 1481, 1484, 1487,  1490, 1493, 1496, 1499,  1502, 1505, 1508, 1511,
            1514, 1517, 1520, 1523,  1526, 1529, 0, 1532,  0, 0, 0, 0,  0, 1535, 0, 0,
            1560, 1563, 1566, 1569,  1572, 1575, 1578, 1581,  1584, 1587, 1590, 1593,  1596, 1599, 1602, 1605,
            1608, 1611, 1614, 1617,  1620, 1623, 1626, 1629,  1632, 1635, 1638, 1641,  1644, 1647, 1650, 1653,
            1656, 1659, 1662, 1665,  1668, 1671, 1674, 1677,  1680, 1683, 1686, 0,  0, 1689, 1692, 1695,

            //  For u+13xx (3328)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3205, 3208, 3211, 3214,  3217, 3220, 3223, 3226,  3229, 3232, 3235, 3238,  3241, 3244, 3247, 3250,
            3253, 3256, 3259, 3262,  3265, 3268, 3271, 3274,  3277, 3280, 3283, 3286,  3289, 3292, 3295, 3298,
            3301, 3304, 3307, 3310,  3313, 3316, 3319, 3322,  3325, 3328, 3331, 3334,  3337, 3340, 3343, 3346,
            3349, 3352, 3355, 3358,  3361, 3364, 3367, 3370,  3373, 3376, 3379, 3382,  3385, 3388, 3391, 3394,
            3397, 3400, 3403, 3406,  3409, 3412, 3415, 3418,  3421, 3424, 3427, 3430,  3433, 3436, 3439, 3442,
            1538, 1541, 1544, 1547,  1550, 1553, 0, 0,  1538, 1541, 1544, 1547,  1550, 1553, 0, 0,

            //  For u+A6xx (3584)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            2839, 2839, 2842, 2842,  2845, 2845, 2848, 2848,  2851, 2851, 1556, 1556,  2854, 2854, 2857, 2857,
            2860, 2860, 2863, 2863,  2866, 2866, 2869, 2869,  2872, 2872, 2875, 2875,  2878, 2878, 2881, 2881,
            2884, 2884, 2887, 2887,  2890, 2890, 2893, 2893,  2896, 2896, 2899, 2899,  2902, 2902, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            2905, 2905, 2908, 2908,  2911, 2911, 2914, 2914,  2917, 2917, 2920, 2920,  2923, 2923, 2926, 2926,
            2929, 2929, 2932, 2932,  2935, 2935, 2938, 2938,  2941, 2941, 2944, 2944,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+1Exx (3840)
            1698, 1698, 1701, 1701,  1704, 1704, 1707, 1707,  1710, 1710, 1713, 1713,  1716, 1716, 1719, 1719,
            1722, 1722, 1725, 1725,  1728, 1728, 1731, 1731,  1734, 1734, 1737, 1737,  1740, 1740, 1743, 1743,
            1746, 1746, 1749, 1749,  1752, 1752, 1755, 1755,  1758, 1758, 1761, 1761,  1764, 1764, 1767, 1767,
            1770, 1770, 1773, 1773,  1776, 1776, 1779, 1779,  1782, 1782, 1785, 1785,  1788, 1788, 1791, 1791,
            1794, 1794, 1797, 1797,  1800, 1800, 1803, 1803,  1806, 1806, 1809, 1809,  1812, 1812, 1815, 1815,
            1818, 1818, 1821, 1821,  1824, 1824, 1827, 1827,  1830, 1830, 1833, 1833,  1836, 1836, 1839, 1839,
            1842, 1842, 1846, 1846,  1849, 1849, 1852, 1852,  1855, 1855, 1858, 1858,  1861, 1861, 1864, 1864,
            1867, 1867, 1870, 1870,  1873, 1873, 1876, 1876,  1879, 1879, 1882, 1882,  1885, 1885, 1888, 1888,
            1891, 1891, 1894, 1894,  1897, 1897, 1900, 1900,  1903, 1903, 1906, 1906,  1909, 1909, 1912, 1912,
            1915, 1915, 1918, 1918,  1921, 1921, 0, 0,  0, 0, 0, 1842,  0, 0, 1924, 0,
            1927, 1927, 1930, 1930,  1933, 1933, 1936, 1936,  1939, 1939, 1942, 1942,  1945, 1945, 1948, 1948,
            1951, 1951, 1954, 1954,  1957, 1957, 1960, 1960,  1963, 1963, 1966, 1966,  1969, 1969, 1972, 1972,
            1975, 1975, 1978, 1978,  1981, 1981, 1984, 1984,  1987, 1987, 1990, 1990,  1993, 1993, 1996, 1996,
            1999, 1999, 2002, 2002,  2005, 2005, 2008, 2008,  2011, 2011, 2014, 2014,  2017, 2017, 2020, 2020,
            2023, 2023, 2026, 2026,  2029, 2029, 2032, 2032,  2035, 2035, 2038, 2038,  2041, 2041, 2044, 2044,
            2047, 2047, 2050, 2050,  2053, 2053, 2056, 2056,  2059, 2059, 2062, 2062,  2065, 2065, 2068, 2068,

            //  For u+24xx (4096)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 2413, 2416,  2419, 2422, 2425, 2428,  2431, 2434, 2437, 2440,
            2443, 2446, 2449, 2452,  2455, 2458, 2461, 2464,  2467, 2470, 2473, 2476,  2479, 2482, 2485, 2488,
            2413, 2416, 2419, 2422,  2425, 2428, 2431, 2434,  2437, 2440, 2443, 2446,  2449, 2452, 2455, 2458,
            2461, 2464, 2467, 2470,  2473, 2476, 2479, 2482,  2485, 2488, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+1Dxx (4352)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 3067, 0, 0,  0, 2641, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 3184, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+A7xx (4608)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 2947, 2947,  2950, 2950, 2953, 2953,  2956, 2956, 2959, 2959,  2962, 2962, 2965, 2965,
            0, 0, 2968, 2968,  2971, 2971, 2974, 2974,  2977, 2977, 2980, 2980,  2983, 2983, 2986, 2986,
            2989, 2989, 2992, 2992,  2995, 2995, 2998, 2998,  3001, 3001, 3004, 3004,  3007, 3007, 3010, 3010,
            3013, 3013, 3016, 3016,  3019, 3019, 3022, 3022,  3025, 3025, 3028, 3028,  3031, 3031, 3034, 3034,
            3037, 3037, 3040, 3040,  3043, 3043, 3046, 3046,  3049, 3049, 3052, 3052,  3055, 3055, 3058, 3058,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 3061, 3061, 3064,  3064, 3067, 3070, 3070,
            3073, 3073, 3076, 3076,  3079, 3079, 3082, 3082,  0, 0, 0, 3085,  3085, 3088, 0, 0,
            3091, 3091, 3094, 3094,  3178, 0, 3097, 3097,  3100, 3100, 3103, 3103,  3106, 3106, 3109, 3109,
            3112, 3112, 3115, 3115,  3118, 3118, 3121, 3121,  3124, 3124, 3127, 3130,  3133, 3136, 3139, 0,
            3142, 3145, 3148, 3151,  3154, 3154, 3157, 3157,  3160, 3160, 3163, 3163,  3166, 3166, 3169, 3169,
            3172, 3172, 3175, 3175,  3178, 3181, 3184, 3187,  3187, 3190, 3190, 0,  0, 0, 0, 0,
            3193, 3193, 0, 0,  0, 0, 3196, 3196,  3199, 3199, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 3202, 3202, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+ABxx (4864)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 3151,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3205, 3208, 3211, 3214,  3217, 3220, 3223, 3226,  3229, 3232, 3235, 3238,  3241, 3244, 3247, 3250,
            3253, 3256, 3259, 3262,  3265, 3268, 3271, 3274,  3277, 3280, 3283, 3286,  3289, 3292, 3295, 3298,
            3301, 3304, 3307, 3310,  3313, 3316, 3319, 3322,  3325, 3328, 3331, 3334,  3337, 3340, 3343, 3346,
            3349, 3352, 3355, 3358,  3361, 3364, 3367, 3370,  3373, 3376, 3379, 3382,  3385, 3388, 3391, 3394,
            3397, 3400, 3403, 3406,  3409, 3412, 3415, 3418,  3421, 3424, 3427, 3430,  3433, 3436, 3439, 3442,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+FFxx (5120)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 3445, 3448, 3451,  3454, 3457, 3460, 3463,  3466, 3469, 3472, 3475,  3478, 3481, 3484, 3487,
            3490, 3493, 3496, 3499,  3502, 3505, 3508, 3511,  3514, 3517, 3520, 0,  0, 0, 0, 0,
            0, 3445, 3448, 3451,  3454, 3457, 3460, 3463,  3466, 3469, 3472, 3475,  3478, 3481, 3484, 3487,
            3490, 3493, 3496, 3499,  3502, 3505, 3508, 3511,  3514, 3517, 3520, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+104xx (5376)
            3523, 3526, 3529, 3532,  3535, 3538, 3541, 3544,  3547, 3550, 3553, 3556,  3559, 3562, 3565, 3568,
            3571, 3574, 3577, 3580,  3583, 3586, 3589, 3592,  3595, 3598, 3601, 3604,  3607, 3610, 3613, 3616,
            3619, 3622, 3625, 3628,  3631, 3634, 3637, 3640,  3523, 3526, 3529, 3532,  3535, 3538, 3541, 3544,
            3547, 3550, 3553, 3556,  3559, 3562, 3565, 3568,  3571, 3574, 3577, 3580,  3583, 3586, 3589, 3592,
            3595, 3598, 3601, 3604,  3607, 3610, 3613, 3616,  3619, 3622, 3625, 3628,  3631, 3634, 3637, 3640,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3643, 3646, 3649, 3652,  3655, 3658, 3661, 3664,  3667, 3670, 3673, 3676,  3679, 3682, 3685, 3688,
            3691, 3694, 3697, 3700,  3703, 3706, 3709, 3712,  3715, 3718, 3721, 3724,  3727, 3730, 3733, 3736,
            3739, 3742, 3745, 3748,  0, 0, 0, 0,  3643, 3646, 3649, 3652,  3655, 3658, 3661, 3664,
            3667, 3670, 3673, 3676,  3679, 3682, 3685, 3688,  3691, 3694, 3697, 3700,  3703, 3706, 3709, 3712,
            3715, 3718, 3721, 3724,  3727, 3730, 3733, 3736,  3739, 3742, 3745, 3748,  0, 0, 0, 0,

            //  For u+105xx (5632)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3751, 3754, 3757, 3760,  3763, 3766, 3769, 3772,  3775, 3778, 3781, 0,  3784, 3787, 3790, 3793,
            3796, 3799, 3802, 3805,  3808, 3811, 3814, 3817,  3820, 3823, 3826, 0,  3829, 3832, 3835, 3838,
            3841, 3844, 3847, 0,  3850, 3853, 0, 3751,  3754, 3757, 3760, 3763,  3766, 3769, 3772, 3775,
            3778, 3781, 0, 3784,  3787, 3790, 3793, 3796,  3799, 3802, 3805, 3808,  3811, 3814, 3817, 3820,
            3823, 3826, 0, 3829,  3832, 3835, 3838, 3841,  3844, 3847, 0, 3850,  3853, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+10Cxx (5888)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3856, 3859, 3862, 3865,  3868, 3871, 3874, 3877,  3880, 3883, 3886, 3889,  3892, 3895, 3898, 3901,
            3904, 3907, 3910, 3913,  3916, 3919, 3922, 3925,  3928, 3931, 3934, 3937,  3940, 3943, 3946, 3949,
            3952, 3955, 3958, 3961,  3964, 3967, 3970, 3973,  3976, 3979, 3982, 3985,  3988, 3991, 3994, 3997,
            4000, 4003, 4006, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3856, 3859, 3862, 3865,  3868, 3871, 3874, 3877,  3880, 3883, 3886, 3889,  3892, 3895, 3898, 3901,
            3904, 3907, 3910, 3913,  3916, 3919, 3922, 3925,  3928, 3931, 3934, 3937,  3940, 3943, 3946, 3949,
            3952, 3955, 3958, 3961,  3964, 3967, 3970, 3973,  3976, 3979, 3982, 3985,  3988, 3991, 3994, 3997,
            4000, 4003, 4006, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+118xx (6144)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            4009, 4012, 4015, 4018,  4021, 4024, 4027, 4030,  4033, 4036, 4039, 4042,  4045, 4048, 4051, 4054,
            4057, 4060, 4063, 4066,  4069, 4072, 4075, 4078,  4081, 4084, 4087, 4090,  4093, 4096, 4099, 4102,
            4009, 4012, 4015, 4018,  4021, 4024, 4027, 4030,  4033, 4036, 4039, 4042,  4045, 4048, 4051, 4054,
            4057, 4060, 4063, 4066,  4069, 4072, 4075, 4078,  4081, 4084, 4087, 4090,  4093, 4096, 4099, 4102,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+16Exx (6400)
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            4105, 4108, 4111, 4114,  4117, 4120, 4123, 4126,  4129, 4132, 4135, 4138,  4141, 4144, 4147, 4150,
            4153, 4156, 4159, 4162,  4165, 4168, 4171, 4174,  4177, 4180, 4183, 4186,  4189, 4192, 4195, 4198,
            4105, 4108, 4111, 4114,  4117, 4120, 4123, 4126,  4129, 4132, 4135, 4138,  4141, 4144, 4147, 4150,
            4153, 4156, 4159, 4162,  4165, 4168, 4171, 4174,  4177, 4180, 4183, 4186,  4189, 4192, 4195, 4198,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,

            //  For u+1E9xx (6656)
            4201, 4204, 4207, 4210,  4213, 4216, 4219, 4222,  4225, 4228, 4231, 4234,  4237, 4240, 4243, 4246,
            4249, 4252, 4255, 4258,  4261, 4264, 4267, 4270,  4273, 4276, 4279, 4282,  4285, 4288, 4291, 4294,
            4297, 4300, 4201, 4204,  4207, 4210, 4213, 4216,  4219, 4222, 4225, 4228,  4231, 4234, 4237, 4240,
            4243, 4246, 4249, 4252,  4255, 4258, 4261, 4264,  4267, 4270, 4273, 4276,  4279, 4282, 4285, 4288,
            4291, 4294, 4297, 4300,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0
        };

        template <typename T2, typename T3>
        const T3 unicode_casefolding<T2, T3>::rev_segmenttable[] =
        {
            256, 768, 1280, 1024,  2048, 2560, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            3072, 0, 0, 3328,  0, 0, 0, 0,  0, 0, 0, 0,  2304, 4352, 3840, 1792,
            0, 512, 0, 0,  4096, 0, 0, 0,  0, 0, 0, 0,  1536, 2816, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 3584, 4608,  0, 0, 0, 4864,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 5120,
            0, 0, 0, 0,  5376, 5632, 0, 0,  0, 0, 0, 0,  5888, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  6144, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 6400, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,  0, 0, 0, 0,
            0, 0, 0, 0,  0, 0, 0, 0,  0, 6656
        };

        template <typename T2, typename T3>
        const T2 unicode_casefolding<T2, T3>::rev_charsettable[] =
        {
            eos,	//  0
            0x0061, 0x0041, eos,
            0x0062, 0x0042, eos,
            0x0063, 0x0043, eos,
            0x0064, 0x0044, eos,	//  10
            0x0065, 0x0045, eos,
            0x0066, 0x0046, eos,
            0x0067, 0x0047, eos,
            0x0068, 0x0048, eos,	//  22
            0x0069, 0x0049, eos,
            0x006A, 0x004A, eos,
            0x006B, 0x004B, 0x212A, eos,	//  31
            0x006C, 0x004C, eos,
            0x006D, 0x004D, eos,
            0x006E, 0x004E, eos,	//  41
            0x006F, 0x004F, eos,
            0x0070, 0x0050, eos,
            0x0071, 0x0051, eos,	//  50
            0x0072, 0x0052, eos,
            0x0073, 0x0053, 0x017F, eos,
            0x0074, 0x0054, eos,	//  60
            0x0075, 0x0055, eos,
            0x0076, 0x0056, eos,
            0x0077, 0x0057, eos,
            0x0078, 0x0058, eos,	//  72
            0x0079, 0x0059, eos,
            0x007A, 0x005A, eos,
            0x03BC, 0x00B5, 0x039C, eos,	//  81
            0x00E0, 0x00C0, eos,
            0x00E1, 0x00C1, eos,
            0x00E2, 0x00C2, eos,	//  91
            0x00E3, 0x00C3, eos,
            0x00E4, 0x00C4, eos,
            0x00E5, 0x00C5, 0x212B, eos,	//  100
            0x00E6, 0x00C6, eos,
            0x00E7, 0x00C7, eos,
            0x00E8, 0x00C8, eos,	//  110
            0x00E9, 0x00C9, eos,
            0x00EA, 0x00CA, eos,
            0x00EB, 0x00CB, eos,
            0x00EC, 0x00CC, eos,	//  122
            0x00ED, 0x00CD, eos,
            0x00EE, 0x00CE, eos,
            0x00EF, 0x00CF, eos,	//  131
            0x00F0, 0x00D0, eos,
            0x00F1, 0x00D1, eos,
            0x00F2, 0x00D2, eos,	//  140
            0x00F3, 0x00D3, eos,
            0x00F4, 0x00D4, eos,
            0x00F5, 0x00D5, eos,
            0x00F6, 0x00D6, eos,	//  152
            0x00F8, 0x00D8, eos,
            0x00F9, 0x00D9, eos,
            0x00FA, 0x00DA, eos,	//  161
            0x00FB, 0x00DB, eos,
            0x00FC, 0x00DC, eos,
            0x00FD, 0x00DD, eos,	//  170
            0x00FE, 0x00DE, eos,
            0x0101, 0x0100, eos,
            0x0103, 0x0102, eos,
            0x0105, 0x0104, eos,	//  182
            0x0107, 0x0106, eos,
            0x0109, 0x0108, eos,
            0x010B, 0x010A, eos,	//  191
            0x010D, 0x010C, eos,
            0x010F, 0x010E, eos,
            0x0111, 0x0110, eos,	//  200
            0x0113, 0x0112, eos,
            0x0115, 0x0114, eos,
            0x0117, 0x0116, eos,
            0x0119, 0x0118, eos,	//  212
            0x011B, 0x011A, eos,
            0x011D, 0x011C, eos,
            0x011F, 0x011E, eos,	//  221
            0x0121, 0x0120, eos,
            0x0123, 0x0122, eos,
            0x0125, 0x0124, eos,	//  230
            0x0127, 0x0126, eos,
            0x0129, 0x0128, eos,
            0x012B, 0x012A, eos,
            0x012D, 0x012C, eos,	//  242
            0x012F, 0x012E, eos,
            0x0133, 0x0132, eos,
            0x0135, 0x0134, eos,	//  251
            0x0137, 0x0136, eos,
            0x013A, 0x0139, eos,
            0x013C, 0x013B, eos,	//  260
            0x013E, 0x013D, eos,
            0x0140, 0x013F, eos,
            0x0142, 0x0141, eos,
            0x0144, 0x0143, eos,	//  272
            0x0146, 0x0145, eos,
            0x0148, 0x0147, eos,
            0x014B, 0x014A, eos,	//  281
            0x014D, 0x014C, eos,
            0x014F, 0x014E, eos,
            0x0151, 0x0150, eos,	//  290
            0x0153, 0x0152, eos,
            0x0155, 0x0154, eos,
            0x0157, 0x0156, eos,
            0x0159, 0x0158, eos,	//  302
            0x015B, 0x015A, eos,
            0x015D, 0x015C, eos,
            0x015F, 0x015E, eos,	//  311
            0x0161, 0x0160, eos,
            0x0163, 0x0162, eos,
            0x0165, 0x0164, eos,	//  320
            0x0167, 0x0166, eos,
            0x0169, 0x0168, eos,
            0x016B, 0x016A, eos,
            0x016D, 0x016C, eos,	//  332
            0x016F, 0x016E, eos,
            0x0171, 0x0170, eos,
            0x0173, 0x0172, eos,	//  341
            0x0175, 0x0174, eos,
            0x0177, 0x0176, eos,
            0x00FF, 0x0178, eos,	//  350
            0x017A, 0x0179, eos,
            0x017C, 0x017B, eos,
            0x017E, 0x017D, eos,
            0x0253, 0x0181, eos,	//  362
            0x0183, 0x0182, eos,
            0x0185, 0x0184, eos,
            0x0254, 0x0186, eos,	//  371
            0x0188, 0x0187, eos,
            0x0256, 0x0189, eos,
            0x0257, 0x018A, eos,	//  380
            0x018C, 0x018B, eos,
            0x01DD, 0x018E, eos,
            0x0259, 0x018F, eos,
            0x025B, 0x0190, eos,	//  392
            0x0192, 0x0191, eos,
            0x0260, 0x0193, eos,
            0x0263, 0x0194, eos,	//  401
            0x0269, 0x0196, eos,
            0x0268, 0x0197, eos,
            0x0199, 0x0198, eos,	//  410
            0x026F, 0x019C, eos,
            0x0272, 0x019D, eos,
            0x0275, 0x019F, eos,
            0x01A1, 0x01A0, eos,	//  422
            0x01A3, 0x01A2, eos,
            0x01A5, 0x01A4, eos,
            0x0280, 0x01A6, eos,	//  431
            0x01A8, 0x01A7, eos,
            0x0283, 0x01A9, eos,
            0x01AD, 0x01AC, eos,	//  440
            0x0288, 0x01AE, eos,
            0x01B0, 0x01AF, eos,
            0x028A, 0x01B1, eos,
            0x028B, 0x01B2, eos,	//  452
            0x01B4, 0x01B3, eos,
            0x01B6, 0x01B5, eos,
            0x0292, 0x01B7, eos,	//  461
            0x01B9, 0x01B8, eos,
            0x01BD, 0x01BC, eos,
            0x01C6, 0x01C4, 0x01C5, eos,	//  470
            0x01C9, 0x01C7, 0x01C8, eos,
            0x01CC, 0x01CA, 0x01CB, eos,
            0x01CE, 0x01CD, eos,	//  482
            0x01D0, 0x01CF, eos,
            0x01D2, 0x01D1, eos,
            0x01D4, 0x01D3, eos,	//  491
            0x01D6, 0x01D5, eos,
            0x01D8, 0x01D7, eos,
            0x01DA, 0x01D9, eos,	//  500
            0x01DC, 0x01DB, eos,
            0x01DF, 0x01DE, eos,
            0x01E1, 0x01E0, eos,
            0x01E3, 0x01E2, eos,	//  512
            0x01E5, 0x01E4, eos,
            0x01E7, 0x01E6, eos,
            0x01E9, 0x01E8, eos,	//  521
            0x01EB, 0x01EA, eos,
            0x01ED, 0x01EC, eos,
            0x01EF, 0x01EE, eos,	//  530
            0x01F3, 0x01F1, 0x01F2, eos,
            0x01F5, 0x01F4, eos,
            0x0195, 0x01F6, eos,	//  540
            0x01BF, 0x01F7, eos,
            0x01F9, 0x01F8, eos,
            0x01FB, 0x01FA, eos,
            0x01FD, 0x01FC, eos,	//  552
            0x01FF, 0x01FE, eos,
            0x0201, 0x0200, eos,
            0x0203, 0x0202, eos,	//  561
            0x0205, 0x0204, eos,
            0x0207, 0x0206, eos,
            0x0209, 0x0208, eos,	//  570
            0x020B, 0x020A, eos,
            0x020D, 0x020C, eos,
            0x020F, 0x020E, eos,
            0x0211, 0x0210, eos,	//  582
            0x0213, 0x0212, eos,
            0x0215, 0x0214, eos,
            0x0217, 0x0216, eos,	//  591
            0x0219, 0x0218, eos,
            0x021B, 0x021A, eos,
            0x021D, 0x021C, eos,	//  600
            0x021F, 0x021E, eos,
            0x019E, 0x0220, eos,
            0x0223, 0x0222, eos,
            0x0225, 0x0224, eos,	//  612
            0x0227, 0x0226, eos,
            0x0229, 0x0228, eos,
            0x022B, 0x022A, eos,	//  621
            0x022D, 0x022C, eos,
            0x022F, 0x022E, eos,
            0x0231, 0x0230, eos,	//  630
            0x0233, 0x0232, eos,
            0x2C65, 0x023A, eos,
            0x023C, 0x023B, eos,
            0x019A, 0x023D, eos,	//  642
            0x2C66, 0x023E, eos,
            0x0242, 0x0241, eos,
            0x0180, 0x0243, eos,	//  651
            0x0289, 0x0244, eos,
            0x028C, 0x0245, eos,
            0x0247, 0x0246, eos,	//  660
            0x0249, 0x0248, eos,
            0x024B, 0x024A, eos,
            0x024D, 0x024C, eos,
            0x024F, 0x024E, eos,	//  672
            0x03B9, 0x0345, 0x0399, 0x1FBE, eos,
            0x0371, 0x0370, eos,	//  680
            0x0373, 0x0372, eos,
            0x0377, 0x0376, eos,
            0x03F3, 0x037F, eos,
            0x03AC, 0x0386, eos,	//  692
            0x03AD, 0x0388, eos,
            0x03AE, 0x0389, eos,
            0x03AF, 0x038A, eos,	//  701
            0x03CC, 0x038C, eos,
            0x03CD, 0x038E, eos,
            0x03CE, 0x038F, eos,	//  710
            0x03B1, 0x0391, eos,
            0x03B2, 0x0392, 0x03D0, eos,
            0x03B3, 0x0393, eos,	//  720
            0x03B4, 0x0394, eos,
            0x03B5, 0x0395, 0x03F5, eos,
            0x03B6, 0x0396, eos,	//  730
            0x03B7, 0x0397, eos,
            0x03B8, 0x0398, 0x03D1, 0x03F4, eos,
            0x03BA, 0x039A, 0x03F0, eos,	//  741
            0x03BB, 0x039B, eos,
            0x03BD, 0x039D, eos,
            0x03BE, 0x039E, eos,	//  751
            0x03BF, 0x039F, eos,
            0x03C0, 0x03A0, 0x03D6, eos,
            0x03C1, 0x03A1, 0x03F1, eos,	//  761
            0x03C3, 0x03A3, 0x03C2, eos,
            0x03C4, 0x03A4, eos,
            0x03C5, 0x03A5, eos,	//  772
            0x03C6, 0x03A6, 0x03D5, eos,
            0x03C7, 0x03A7, eos,
            0x03C8, 0x03A8, eos,	//  782
            0x03C9, 0x03A9, 0x2126, eos,
            0x03CA, 0x03AA, eos,
            0x03CB, 0x03AB, eos,	//  792
            0x03D7, 0x03CF, eos,
            0x03D9, 0x03D8, eos,
            0x03DB, 0x03DA, eos,	//  801
            0x03DD, 0x03DC, eos,
            0x03DF, 0x03DE, eos,
            0x03E1, 0x03E0, eos,	//  810
            0x03E3, 0x03E2, eos,
            0x03E5, 0x03E4, eos,
            0x03E7, 0x03E6, eos,
            0x03E9, 0x03E8, eos,	//  822
            0x03EB, 0x03EA, eos,
            0x03ED, 0x03EC, eos,
            0x03EF, 0x03EE, eos,	//  831
            0x03F8, 0x03F7, eos,
            0x03F2, 0x03F9, eos,
            0x03FB, 0x03FA, eos,	//  840
            0x037B, 0x03FD, eos,
            0x037C, 0x03FE, eos,
            0x037D, 0x03FF, eos,
            0x0450, 0x0400, eos,	//  852
            0x0451, 0x0401, eos,
            0x0452, 0x0402, eos,
            0x0453, 0x0403, eos,	//  861
            0x0454, 0x0404, eos,
            0x0455, 0x0405, eos,
            0x0456, 0x0406, eos,	//  870
            0x0457, 0x0407, eos,
            0x0458, 0x0408, eos,
            0x0459, 0x0409, eos,
            0x045A, 0x040A, eos,	//  882
            0x045B, 0x040B, eos,
            0x045C, 0x040C, eos,
            0x045D, 0x040D, eos,	//  891
            0x045E, 0x040E, eos,
            0x045F, 0x040F, eos,
            0x0430, 0x0410, eos,	//  900
            0x0431, 0x0411, eos,
            0x0432, 0x0412, 0x1C80, eos,
            0x0433, 0x0413, eos,	//  910
            0x0434, 0x0414, 0x1C81, eos,
            0x0435, 0x0415, eos,
            0x0436, 0x0416, eos,	//  920
            0x0437, 0x0417, eos,
            0x0438, 0x0418, eos,
            0x0439, 0x0419, eos,
            0x043A, 0x041A, eos,	//  932
            0x043B, 0x041B, eos,
            0x043C, 0x041C, eos,
            0x043D, 0x041D, eos,	//  941
            0x043E, 0x041E, 0x1C82, eos,
            0x043F, 0x041F, eos,
            0x0440, 0x0420, eos,	//  951
            0x0441, 0x0421, 0x1C83, eos,
            0x0442, 0x0422, 0x1C84, 0x1C85, eos,
            0x0443, 0x0423, eos,	//  963
            0x0444, 0x0424, eos,
            0x0445, 0x0425, eos,
            0x0446, 0x0426, eos,	//  972
            0x0447, 0x0427, eos,
            0x0448, 0x0428, eos,
            0x0449, 0x0429, eos,	//  981
            0x044A, 0x042A, 0x1C86, eos,
            0x044B, 0x042B, eos,
            0x044C, 0x042C, eos,	//  991
            0x044D, 0x042D, eos,
            0x044E, 0x042E, eos,
            0x044F, 0x042F, eos,	//  1000
            0x0461, 0x0460, eos,
            0x0463, 0x0462, 0x1C87, eos,
            0x0465, 0x0464, eos,	//  1010
            0x0467, 0x0466, eos,
            0x0469, 0x0468, eos,
            0x046B, 0x046A, eos,
            0x046D, 0x046C, eos,	//  1022
            0x046F, 0x046E, eos,
            0x0471, 0x0470, eos,
            0x0473, 0x0472, eos,	//  1031
            0x0475, 0x0474, eos,
            0x0477, 0x0476, eos,
            0x0479, 0x0478, eos,	//  1040
            0x047B, 0x047A, eos,
            0x047D, 0x047C, eos,
            0x047F, 0x047E, eos,
            0x0481, 0x0480, eos,	//  1052
            0x048B, 0x048A, eos,
            0x048D, 0x048C, eos,
            0x048F, 0x048E, eos,	//  1061
            0x0491, 0x0490, eos,
            0x0493, 0x0492, eos,
            0x0495, 0x0494, eos,	//  1070
            0x0497, 0x0496, eos,
            0x0499, 0x0498, eos,
            0x049B, 0x049A, eos,
            0x049D, 0x049C, eos,	//  1082
            0x049F, 0x049E, eos,
            0x04A1, 0x04A0, eos,
            0x04A3, 0x04A2, eos,	//  1091
            0x04A5, 0x04A4, eos,
            0x04A7, 0x04A6, eos,
            0x04A9, 0x04A8, eos,	//  1100
            0x04AB, 0x04AA, eos,
            0x04AD, 0x04AC, eos,
            0x04AF, 0x04AE, eos,
            0x04B1, 0x04B0, eos,	//  1112
            0x04B3, 0x04B2, eos,
            0x04B5, 0x04B4, eos,
            0x04B7, 0x04B6, eos,	//  1121
            0x04B9, 0x04B8, eos,
            0x04BB, 0x04BA, eos,
            0x04BD, 0x04BC, eos,	//  1130
            0x04BF, 0x04BE, eos,
            0x04CF, 0x04C0, eos,
            0x04C2, 0x04C1, eos,
            0x04C4, 0x04C3, eos,	//  1142
            0x04C6, 0x04C5, eos,
            0x04C8, 0x04C7, eos,
            0x04CA, 0x04C9, eos,	//  1151
            0x04CC, 0x04CB, eos,
            0x04CE, 0x04CD, eos,
            0x04D1, 0x04D0, eos,	//  1160
            0x04D3, 0x04D2, eos,
            0x04D5, 0x04D4, eos,
            0x04D7, 0x04D6, eos,
            0x04D9, 0x04D8, eos,	//  1172
            0x04DB, 0x04DA, eos,
            0x04DD, 0x04DC, eos,
            0x04DF, 0x04DE, eos,	//  1181
            0x04E1, 0x04E0, eos,
            0x04E3, 0x04E2, eos,
            0x04E5, 0x04E4, eos,	//  1190
            0x04E7, 0x04E6, eos,
            0x04E9, 0x04E8, eos,
            0x04EB, 0x04EA, eos,
            0x04ED, 0x04EC, eos,	//  1202
            0x04EF, 0x04EE, eos,
            0x04F1, 0x04F0, eos,
            0x04F3, 0x04F2, eos,	//  1211
            0x04F5, 0x04F4, eos,
            0x04F7, 0x04F6, eos,
            0x04F9, 0x04F8, eos,	//  1220
            0x04FB, 0x04FA, eos,
            0x04FD, 0x04FC, eos,
            0x04FF, 0x04FE, eos,
            0x0501, 0x0500, eos,	//  1232
            0x0503, 0x0502, eos,
            0x0505, 0x0504, eos,
            0x0507, 0x0506, eos,	//  1241
            0x0509, 0x0508, eos,
            0x050B, 0x050A, eos,
            0x050D, 0x050C, eos,	//  1250
            0x050F, 0x050E, eos,
            0x0511, 0x0510, eos,
            0x0513, 0x0512, eos,
            0x0515, 0x0514, eos,	//  1262
            0x0517, 0x0516, eos,
            0x0519, 0x0518, eos,
            0x051B, 0x051A, eos,	//  1271
            0x051D, 0x051C, eos,
            0x051F, 0x051E, eos,
            0x0521, 0x0520, eos,	//  1280
            0x0523, 0x0522, eos,
            0x0525, 0x0524, eos,
            0x0527, 0x0526, eos,
            0x0529, 0x0528, eos,	//  1292
            0x052B, 0x052A, eos,
            0x052D, 0x052C, eos,
            0x052F, 0x052E, eos,	//  1301
            0x0561, 0x0531, eos,
            0x0562, 0x0532, eos,
            0x0563, 0x0533, eos,	//  1310
            0x0564, 0x0534, eos,
            0x0565, 0x0535, eos,
            0x0566, 0x0536, eos,
            0x0567, 0x0537, eos,	//  1322
            0x0568, 0x0538, eos,
            0x0569, 0x0539, eos,
            0x056A, 0x053A, eos,	//  1331
            0x056B, 0x053B, eos,
            0x056C, 0x053C, eos,
            0x056D, 0x053D, eos,	//  1340
            0x056E, 0x053E, eos,
            0x056F, 0x053F, eos,
            0x0570, 0x0540, eos,
            0x0571, 0x0541, eos,	//  1352
            0x0572, 0x0542, eos,
            0x0573, 0x0543, eos,
            0x0574, 0x0544, eos,	//  1361
            0x0575, 0x0545, eos,
            0x0576, 0x0546, eos,
            0x0577, 0x0547, eos,	//  1370
            0x0578, 0x0548, eos,
            0x0579, 0x0549, eos,
            0x057A, 0x054A, eos,
            0x057B, 0x054B, eos,	//  1382
            0x057C, 0x054C, eos,
            0x057D, 0x054D, eos,
            0x057E, 0x054E, eos,	//  1391
            0x057F, 0x054F, eos,
            0x0580, 0x0550, eos,
            0x0581, 0x0551, eos,	//  1400
            0x0582, 0x0552, eos,
            0x0583, 0x0553, eos,
            0x0584, 0x0554, eos,
            0x0585, 0x0555, eos,	//  1412
            0x0586, 0x0556, eos,
            0x2D00, 0x10A0, eos,
            0x2D01, 0x10A1, eos,	//  1421
            0x2D02, 0x10A2, eos,
            0x2D03, 0x10A3, eos,
            0x2D04, 0x10A4, eos,	//  1430
            0x2D05, 0x10A5, eos,
            0x2D06, 0x10A6, eos,
            0x2D07, 0x10A7, eos,
            0x2D08, 0x10A8, eos,	//  1442
            0x2D09, 0x10A9, eos,
            0x2D0A, 0x10AA, eos,
            0x2D0B, 0x10AB, eos,	//  1451
            0x2D0C, 0x10AC, eos,
            0x2D0D, 0x10AD, eos,
            0x2D0E, 0x10AE, eos,	//  1460
            0x2D0F, 0x10AF, eos,
            0x2D10, 0x10B0, eos,
            0x2D11, 0x10B1, eos,
            0x2D12, 0x10B2, eos,	//  1472
            0x2D13, 0x10B3, eos,
            0x2D14, 0x10B4, eos,
            0x2D15, 0x10B5, eos,	//  1481
            0x2D16, 0x10B6, eos,
            0x2D17, 0x10B7, eos,
            0x2D18, 0x10B8, eos,	//  1490
            0x2D19, 0x10B9, eos,
            0x2D1A, 0x10BA, eos,
            0x2D1B, 0x10BB, eos,
            0x2D1C, 0x10BC, eos,	//  1502
            0x2D1D, 0x10BD, eos,
            0x2D1E, 0x10BE, eos,
            0x2D1F, 0x10BF, eos,	//  1511
            0x2D20, 0x10C0, eos,
            0x2D21, 0x10C1, eos,
            0x2D22, 0x10C2, eos,	//  1520
            0x2D23, 0x10C3, eos,
            0x2D24, 0x10C4, eos,
            0x2D25, 0x10C5, eos,
            0x2D27, 0x10C7, eos,	//  1532
            0x2D2D, 0x10CD, eos,
            0x13F0, 0x13F8, eos,
            0x13F1, 0x13F9, eos,	//  1541
            0x13F2, 0x13FA, eos,
            0x13F3, 0x13FB, eos,
            0x13F4, 0x13FC, eos,	//  1550
            0x13F5, 0x13FD, eos,
            0xA64B, 0x1C88, 0xA64A, eos,
            0x10D0, 0x1C90, eos,	//  1560
            0x10D1, 0x1C91, eos,
            0x10D2, 0x1C92, eos,
            0x10D3, 0x1C93, eos,
            0x10D4, 0x1C94, eos,	//  1572
            0x10D5, 0x1C95, eos,
            0x10D6, 0x1C96, eos,
            0x10D7, 0x1C97, eos,	//  1581
            0x10D8, 0x1C98, eos,
            0x10D9, 0x1C99, eos,
            0x10DA, 0x1C9A, eos,	//  1590
            0x10DB, 0x1C9B, eos,
            0x10DC, 0x1C9C, eos,
            0x10DD, 0x1C9D, eos,
            0x10DE, 0x1C9E, eos,	//  1602
            0x10DF, 0x1C9F, eos,
            0x10E0, 0x1CA0, eos,
            0x10E1, 0x1CA1, eos,	//  1611
            0x10E2, 0x1CA2, eos,
            0x10E3, 0x1CA3, eos,
            0x10E4, 0x1CA4, eos,	//  1620
            0x10E5, 0x1CA5, eos,
            0x10E6, 0x1CA6, eos,
            0x10E7, 0x1CA7, eos,
            0x10E8, 0x1CA8, eos,	//  1632
            0x10E9, 0x1CA9, eos,
            0x10EA, 0x1CAA, eos,
            0x10EB, 0x1CAB, eos,	//  1641
            0x10EC, 0x1CAC, eos,
            0x10ED, 0x1CAD, eos,
            0x10EE, 0x1CAE, eos,	//  1650
            0x10EF, 0x1CAF, eos,
            0x10F0, 0x1CB0, eos,
            0x10F1, 0x1CB1, eos,
            0x10F2, 0x1CB2, eos,	//  1662
            0x10F3, 0x1CB3, eos,
            0x10F4, 0x1CB4, eos,
            0x10F5, 0x1CB5, eos,	//  1671
            0x10F6, 0x1CB6, eos,
            0x10F7, 0x1CB7, eos,
            0x10F8, 0x1CB8, eos,	//  1680
            0x10F9, 0x1CB9, eos,
            0x10FA, 0x1CBA, eos,
            0x10FD, 0x1CBD, eos,
            0x10FE, 0x1CBE, eos,	//  1692
            0x10FF, 0x1CBF, eos,
            0x1E01, 0x1E00, eos,
            0x1E03, 0x1E02, eos,	//  1701
            0x1E05, 0x1E04, eos,
            0x1E07, 0x1E06, eos,
            0x1E09, 0x1E08, eos,	//  1710
            0x1E0B, 0x1E0A, eos,
            0x1E0D, 0x1E0C, eos,
            0x1E0F, 0x1E0E, eos,
            0x1E11, 0x1E10, eos,	//  1722
            0x1E13, 0x1E12, eos,
            0x1E15, 0x1E14, eos,
            0x1E17, 0x1E16, eos,	//  1731
            0x1E19, 0x1E18, eos,
            0x1E1B, 0x1E1A, eos,
            0x1E1D, 0x1E1C, eos,	//  1740
            0x1E1F, 0x1E1E, eos,
            0x1E21, 0x1E20, eos,
            0x1E23, 0x1E22, eos,
            0x1E25, 0x1E24, eos,	//  1752
            0x1E27, 0x1E26, eos,
            0x1E29, 0x1E28, eos,
            0x1E2B, 0x1E2A, eos,	//  1761
            0x1E2D, 0x1E2C, eos,
            0x1E2F, 0x1E2E, eos,
            0x1E31, 0x1E30, eos,	//  1770
            0x1E33, 0x1E32, eos,
            0x1E35, 0x1E34, eos,
            0x1E37, 0x1E36, eos,
            0x1E39, 0x1E38, eos,	//  1782
            0x1E3B, 0x1E3A, eos,
            0x1E3D, 0x1E3C, eos,
            0x1E3F, 0x1E3E, eos,	//  1791
            0x1E41, 0x1E40, eos,
            0x1E43, 0x1E42, eos,
            0x1E45, 0x1E44, eos,	//  1800
            0x1E47, 0x1E46, eos,
            0x1E49, 0x1E48, eos,
            0x1E4B, 0x1E4A, eos,
            0x1E4D, 0x1E4C, eos,	//  1812
            0x1E4F, 0x1E4E, eos,
            0x1E51, 0x1E50, eos,
            0x1E53, 0x1E52, eos,	//  1821
            0x1E55, 0x1E54, eos,
            0x1E57, 0x1E56, eos,
            0x1E59, 0x1E58, eos,	//  1830
            0x1E5B, 0x1E5A, eos,
            0x1E5D, 0x1E5C, eos,
            0x1E5F, 0x1E5E, eos,
            0x1E61, 0x1E60, 0x1E9B, eos,	//  1842
            0x1E63, 0x1E62, eos,
            0x1E65, 0x1E64, eos,
            0x1E67, 0x1E66, eos,	//  1852
            0x1E69, 0x1E68, eos,
            0x1E6B, 0x1E6A, eos,
            0x1E6D, 0x1E6C, eos,	//  1861
            0x1E6F, 0x1E6E, eos,
            0x1E71, 0x1E70, eos,
            0x1E73, 0x1E72, eos,	//  1870
            0x1E75, 0x1E74, eos,
            0x1E77, 0x1E76, eos,
            0x1E79, 0x1E78, eos,
            0x1E7B, 0x1E7A, eos,	//  1882
            0x1E7D, 0x1E7C, eos,
            0x1E7F, 0x1E7E, eos,
            0x1E81, 0x1E80, eos,	//  1891
            0x1E83, 0x1E82, eos,
            0x1E85, 0x1E84, eos,
            0x1E87, 0x1E86, eos,	//  1900
            0x1E89, 0x1E88, eos,
            0x1E8B, 0x1E8A, eos,
            0x1E8D, 0x1E8C, eos,
            0x1E8F, 0x1E8E, eos,	//  1912
            0x1E91, 0x1E90, eos,
            0x1E93, 0x1E92, eos,
            0x1E95, 0x1E94, eos,	//  1921
            0x00DF, 0x1E9E, eos,
            0x1EA1, 0x1EA0, eos,
            0x1EA3, 0x1EA2, eos,	//  1930
            0x1EA5, 0x1EA4, eos,
            0x1EA7, 0x1EA6, eos,
            0x1EA9, 0x1EA8, eos,
            0x1EAB, 0x1EAA, eos,	//  1942
            0x1EAD, 0x1EAC, eos,
            0x1EAF, 0x1EAE, eos,
            0x1EB1, 0x1EB0, eos,	//  1951
            0x1EB3, 0x1EB2, eos,
            0x1EB5, 0x1EB4, eos,
            0x1EB7, 0x1EB6, eos,	//  1960
            0x1EB9, 0x1EB8, eos,
            0x1EBB, 0x1EBA, eos,
            0x1EBD, 0x1EBC, eos,
            0x1EBF, 0x1EBE, eos,	//  1972
            0x1EC1, 0x1EC0, eos,
            0x1EC3, 0x1EC2, eos,
            0x1EC5, 0x1EC4, eos,	//  1981
            0x1EC7, 0x1EC6, eos,
            0x1EC9, 0x1EC8, eos,
            0x1ECB, 0x1ECA, eos,	//  1990
            0x1ECD, 0x1ECC, eos,
            0x1ECF, 0x1ECE, eos,
            0x1ED1, 0x1ED0, eos,
            0x1ED3, 0x1ED2, eos,	//  2002
            0x1ED5, 0x1ED4, eos,
            0x1ED7, 0x1ED6, eos,
            0x1ED9, 0x1ED8, eos,	//  2011
            0x1EDB, 0x1EDA, eos,
            0x1EDD, 0x1EDC, eos,
            0x1EDF, 0x1EDE, eos,	//  2020
            0x1EE1, 0x1EE0, eos,
            0x1EE3, 0x1EE2, eos,
            0x1EE5, 0x1EE4, eos,
            0x1EE7, 0x1EE6, eos,	//  2032
            0x1EE9, 0x1EE8, eos,
            0x1EEB, 0x1EEA, eos,
            0x1EED, 0x1EEC, eos,	//  2041
            0x1EEF, 0x1EEE, eos,
            0x1EF1, 0x1EF0, eos,
            0x1EF3, 0x1EF2, eos,	//  2050
            0x1EF5, 0x1EF4, eos,
            0x1EF7, 0x1EF6, eos,
            0x1EF9, 0x1EF8, eos,
            0x1EFB, 0x1EFA, eos,	//  2062
            0x1EFD, 0x1EFC, eos,
            0x1EFF, 0x1EFE, eos,
            0x1F00, 0x1F08, eos,	//  2071
            0x1F01, 0x1F09, eos,
            0x1F02, 0x1F0A, eos,
            0x1F03, 0x1F0B, eos,	//  2080
            0x1F04, 0x1F0C, eos,
            0x1F05, 0x1F0D, eos,
            0x1F06, 0x1F0E, eos,
            0x1F07, 0x1F0F, eos,	//  2092
            0x1F10, 0x1F18, eos,
            0x1F11, 0x1F19, eos,
            0x1F12, 0x1F1A, eos,	//  2101
            0x1F13, 0x1F1B, eos,
            0x1F14, 0x1F1C, eos,
            0x1F15, 0x1F1D, eos,	//  2110
            0x1F20, 0x1F28, eos,
            0x1F21, 0x1F29, eos,
            0x1F22, 0x1F2A, eos,
            0x1F23, 0x1F2B, eos,	//  2122
            0x1F24, 0x1F2C, eos,
            0x1F25, 0x1F2D, eos,
            0x1F26, 0x1F2E, eos,	//  2131
            0x1F27, 0x1F2F, eos,
            0x1F30, 0x1F38, eos,
            0x1F31, 0x1F39, eos,	//  2140
            0x1F32, 0x1F3A, eos,
            0x1F33, 0x1F3B, eos,
            0x1F34, 0x1F3C, eos,
            0x1F35, 0x1F3D, eos,	//  2152
            0x1F36, 0x1F3E, eos,
            0x1F37, 0x1F3F, eos,
            0x1F40, 0x1F48, eos,	//  2161
            0x1F41, 0x1F49, eos,
            0x1F42, 0x1F4A, eos,
            0x1F43, 0x1F4B, eos,	//  2170
            0x1F44, 0x1F4C, eos,
            0x1F45, 0x1F4D, eos,
            0x1F51, 0x1F59, eos,
            0x1F53, 0x1F5B, eos,	//  2182
            0x1F55, 0x1F5D, eos,
            0x1F57, 0x1F5F, eos,
            0x1F60, 0x1F68, eos,	//  2191
            0x1F61, 0x1F69, eos,
            0x1F62, 0x1F6A, eos,
            0x1F63, 0x1F6B, eos,	//  2200
            0x1F64, 0x1F6C, eos,
            0x1F65, 0x1F6D, eos,
            0x1F66, 0x1F6E, eos,
            0x1F67, 0x1F6F, eos,	//  2212
            0x1F80, 0x1F88, eos,
            0x1F81, 0x1F89, eos,
            0x1F82, 0x1F8A, eos,	//  2221
            0x1F83, 0x1F8B, eos,
            0x1F84, 0x1F8C, eos,
            0x1F85, 0x1F8D, eos,	//  2230
            0x1F86, 0x1F8E, eos,
            0x1F87, 0x1F8F, eos,
            0x1F90, 0x1F98, eos,
            0x1F91, 0x1F99, eos,	//  2242
            0x1F92, 0x1F9A, eos,
            0x1F93, 0x1F9B, eos,
            0x1F94, 0x1F9C, eos,	//  2251
            0x1F95, 0x1F9D, eos,
            0x1F96, 0x1F9E, eos,
            0x1F97, 0x1F9F, eos,	//  2260
            0x1FA0, 0x1FA8, eos,
            0x1FA1, 0x1FA9, eos,
            0x1FA2, 0x1FAA, eos,
            0x1FA3, 0x1FAB, eos,	//  2272
            0x1FA4, 0x1FAC, eos,
            0x1FA5, 0x1FAD, eos,
            0x1FA6, 0x1FAE, eos,	//  2281
            0x1FA7, 0x1FAF, eos,
            0x1FB0, 0x1FB8, eos,
            0x1FB1, 0x1FB9, eos,	//  2290
            0x1F70, 0x1FBA, eos,
            0x1F71, 0x1FBB, eos,
            0x1FB3, 0x1FBC, eos,
            0x1F72, 0x1FC8, eos,	//  2302
            0x1F73, 0x1FC9, eos,
            0x1F74, 0x1FCA, eos,
            0x1F75, 0x1FCB, eos,	//  2311
            0x1FC3, 0x1FCC, eos,
            0x1FD0, 0x1FD8, eos,
            0x1FD1, 0x1FD9, eos,	//  2320
            0x1F76, 0x1FDA, eos,
            0x1F77, 0x1FDB, eos,
            0x1FE0, 0x1FE8, eos,
            0x1FE1, 0x1FE9, eos,	//  2332
            0x1F7A, 0x1FEA, eos,
            0x1F7B, 0x1FEB, eos,
            0x1FE5, 0x1FEC, eos,	//  2341
            0x1F78, 0x1FF8, eos,
            0x1F79, 0x1FF9, eos,
            0x1F7C, 0x1FFA, eos,	//  2350
            0x1F7D, 0x1FFB, eos,
            0x1FF3, 0x1FFC, eos,
            0x214E, 0x2132, eos,
            0x2170, 0x2160, eos,	//  2362
            0x2171, 0x2161, eos,
            0x2172, 0x2162, eos,
            0x2173, 0x2163, eos,	//  2371
            0x2174, 0x2164, eos,
            0x2175, 0x2165, eos,
            0x2176, 0x2166, eos,	//  2380
            0x2177, 0x2167, eos,
            0x2178, 0x2168, eos,
            0x2179, 0x2169, eos,
            0x217A, 0x216A, eos,	//  2392
            0x217B, 0x216B, eos,
            0x217C, 0x216C, eos,
            0x217D, 0x216D, eos,	//  2401
            0x217E, 0x216E, eos,
            0x217F, 0x216F, eos,
            0x2184, 0x2183, eos,	//  2410
            0x24D0, 0x24B6, eos,
            0x24D1, 0x24B7, eos,
            0x24D2, 0x24B8, eos,
            0x24D3, 0x24B9, eos,	//  2422
            0x24D4, 0x24BA, eos,
            0x24D5, 0x24BB, eos,
            0x24D6, 0x24BC, eos,	//  2431
            0x24D7, 0x24BD, eos,
            0x24D8, 0x24BE, eos,
            0x24D9, 0x24BF, eos,	//  2440
            0x24DA, 0x24C0, eos,
            0x24DB, 0x24C1, eos,
            0x24DC, 0x24C2, eos,
            0x24DD, 0x24C3, eos,	//  2452
            0x24DE, 0x24C4, eos,
            0x24DF, 0x24C5, eos,
            0x24E0, 0x24C6, eos,	//  2461
            0x24E1, 0x24C7, eos,
            0x24E2, 0x24C8, eos,
            0x24E3, 0x24C9, eos,	//  2470
            0x24E4, 0x24CA, eos,
            0x24E5, 0x24CB, eos,
            0x24E6, 0x24CC, eos,
            0x24E7, 0x24CD, eos,	//  2482
            0x24E8, 0x24CE, eos,
            0x24E9, 0x24CF, eos,
            0x2C30, 0x2C00, eos,	//  2491
            0x2C31, 0x2C01, eos,
            0x2C32, 0x2C02, eos,
            0x2C33, 0x2C03, eos,	//  2500
            0x2C34, 0x2C04, eos,
            0x2C35, 0x2C05, eos,
            0x2C36, 0x2C06, eos,
            0x2C37, 0x2C07, eos,	//  2512
            0x2C38, 0x2C08, eos,
            0x2C39, 0x2C09, eos,
            0x2C3A, 0x2C0A, eos,	//  2521
            0x2C3B, 0x2C0B, eos,
            0x2C3C, 0x2C0C, eos,
            0x2C3D, 0x2C0D, eos,	//  2530
            0x2C3E, 0x2C0E, eos,
            0x2C3F, 0x2C0F, eos,
            0x2C40, 0x2C10, eos,
            0x2C41, 0x2C11, eos,	//  2542
            0x2C42, 0x2C12, eos,
            0x2C43, 0x2C13, eos,
            0x2C44, 0x2C14, eos,	//  2551
            0x2C45, 0x2C15, eos,
            0x2C46, 0x2C16, eos,
            0x2C47, 0x2C17, eos,	//  2560
            0x2C48, 0x2C18, eos,
            0x2C49, 0x2C19, eos,
            0x2C4A, 0x2C1A, eos,
            0x2C4B, 0x2C1B, eos,	//  2572
            0x2C4C, 0x2C1C, eos,
            0x2C4D, 0x2C1D, eos,
            0x2C4E, 0x2C1E, eos,	//  2581
            0x2C4F, 0x2C1F, eos,
            0x2C50, 0x2C20, eos,
            0x2C51, 0x2C21, eos,	//  2590
            0x2C52, 0x2C22, eos,
            0x2C53, 0x2C23, eos,
            0x2C54, 0x2C24, eos,
            0x2C55, 0x2C25, eos,	//  2602
            0x2C56, 0x2C26, eos,
            0x2C57, 0x2C27, eos,
            0x2C58, 0x2C28, eos,	//  2611
            0x2C59, 0x2C29, eos,
            0x2C5A, 0x2C2A, eos,
            0x2C5B, 0x2C2B, eos,	//  2620
            0x2C5C, 0x2C2C, eos,
            0x2C5D, 0x2C2D, eos,
            0x2C5E, 0x2C2E, eos,
            0x2C5F, 0x2C2F, eos,	//  2632
            0x2C61, 0x2C60, eos,
            0x026B, 0x2C62, eos,
            0x1D7D, 0x2C63, eos,	//  2641
            0x027D, 0x2C64, eos,
            0x2C68, 0x2C67, eos,
            0x2C6A, 0x2C69, eos,	//  2650
            0x2C6C, 0x2C6B, eos,
            0x0251, 0x2C6D, eos,
            0x0271, 0x2C6E, eos,
            0x0250, 0x2C6F, eos,	//  2662
            0x0252, 0x2C70, eos,
            0x2C73, 0x2C72, eos,
            0x2C76, 0x2C75, eos,	//  2671
            0x023F, 0x2C7E, eos,
            0x0240, 0x2C7F, eos,
            0x2C81, 0x2C80, eos,	//  2680
            0x2C83, 0x2C82, eos,
            0x2C85, 0x2C84, eos,
            0x2C87, 0x2C86, eos,
            0x2C89, 0x2C88, eos,	//  2692
            0x2C8B, 0x2C8A, eos,
            0x2C8D, 0x2C8C, eos,
            0x2C8F, 0x2C8E, eos,	//  2701
            0x2C91, 0x2C90, eos,
            0x2C93, 0x2C92, eos,
            0x2C95, 0x2C94, eos,	//  2710
            0x2C97, 0x2C96, eos,
            0x2C99, 0x2C98, eos,
            0x2C9B, 0x2C9A, eos,
            0x2C9D, 0x2C9C, eos,	//  2722
            0x2C9F, 0x2C9E, eos,
            0x2CA1, 0x2CA0, eos,
            0x2CA3, 0x2CA2, eos,	//  2731
            0x2CA5, 0x2CA4, eos,
            0x2CA7, 0x2CA6, eos,
            0x2CA9, 0x2CA8, eos,	//  2740
            0x2CAB, 0x2CAA, eos,
            0x2CAD, 0x2CAC, eos,
            0x2CAF, 0x2CAE, eos,
            0x2CB1, 0x2CB0, eos,	//  2752
            0x2CB3, 0x2CB2, eos,
            0x2CB5, 0x2CB4, eos,
            0x2CB7, 0x2CB6, eos,	//  2761
            0x2CB9, 0x2CB8, eos,
            0x2CBB, 0x2CBA, eos,
            0x2CBD, 0x2CBC, eos,	//  2770
            0x2CBF, 0x2CBE, eos,
            0x2CC1, 0x2CC0, eos,
            0x2CC3, 0x2CC2, eos,
            0x2CC5, 0x2CC4, eos,	//  2782
            0x2CC7, 0x2CC6, eos,
            0x2CC9, 0x2CC8, eos,
            0x2CCB, 0x2CCA, eos,	//  2791
            0x2CCD, 0x2CCC, eos,
            0x2CCF, 0x2CCE, eos,
            0x2CD1, 0x2CD0, eos,	//  2800
            0x2CD3, 0x2CD2, eos,
            0x2CD5, 0x2CD4, eos,
            0x2CD7, 0x2CD6, eos,
            0x2CD9, 0x2CD8, eos,	//  2812
            0x2CDB, 0x2CDA, eos,
            0x2CDD, 0x2CDC, eos,
            0x2CDF, 0x2CDE, eos,	//  2821
            0x2CE1, 0x2CE0, eos,
            0x2CE3, 0x2CE2, eos,
            0x2CEC, 0x2CEB, eos,	//  2830
            0x2CEE, 0x2CED, eos,
            0x2CF3, 0x2CF2, eos,
            0xA641, 0xA640, eos,
            0xA643, 0xA642, eos,	//  2842
            0xA645, 0xA644, eos,
            0xA647, 0xA646, eos,
            0xA649, 0xA648, eos,	//  2851
            0xA64D, 0xA64C, eos,
            0xA64F, 0xA64E, eos,
            0xA651, 0xA650, eos,	//  2860
            0xA653, 0xA652, eos,
            0xA655, 0xA654, eos,
            0xA657, 0xA656, eos,
            0xA659, 0xA658, eos,	//  2872
            0xA65B, 0xA65A, eos,
            0xA65D, 0xA65C, eos,
            0xA65F, 0xA65E, eos,	//  2881
            0xA661, 0xA660, eos,
            0xA663, 0xA662, eos,
            0xA665, 0xA664, eos,	//  2890
            0xA667, 0xA666, eos,
            0xA669, 0xA668, eos,
            0xA66B, 0xA66A, eos,
            0xA66D, 0xA66C, eos,	//  2902
            0xA681, 0xA680, eos,
            0xA683, 0xA682, eos,
            0xA685, 0xA684, eos,	//  2911
            0xA687, 0xA686, eos,
            0xA689, 0xA688, eos,
            0xA68B, 0xA68A, eos,	//  2920
            0xA68D, 0xA68C, eos,
            0xA68F, 0xA68E, eos,
            0xA691, 0xA690, eos,
            0xA693, 0xA692, eos,	//  2932
            0xA695, 0xA694, eos,
            0xA697, 0xA696, eos,
            0xA699, 0xA698, eos,	//  2941
            0xA69B, 0xA69A, eos,
            0xA723, 0xA722, eos,
            0xA725, 0xA724, eos,	//  2950
            0xA727, 0xA726, eos,
            0xA729, 0xA728, eos,
            0xA72B, 0xA72A, eos,
            0xA72D, 0xA72C, eos,	//  2962
            0xA72F, 0xA72E, eos,
            0xA733, 0xA732, eos,
            0xA735, 0xA734, eos,	//  2971
            0xA737, 0xA736, eos,
            0xA739, 0xA738, eos,
            0xA73B, 0xA73A, eos,	//  2980
            0xA73D, 0xA73C, eos,
            0xA73F, 0xA73E, eos,
            0xA741, 0xA740, eos,
            0xA743, 0xA742, eos,	//  2992
            0xA745, 0xA744, eos,
            0xA747, 0xA746, eos,
            0xA749, 0xA748, eos,	//  3001
            0xA74B, 0xA74A, eos,
            0xA74D, 0xA74C, eos,
            0xA74F, 0xA74E, eos,	//  3010
            0xA751, 0xA750, eos,
            0xA753, 0xA752, eos,
            0xA755, 0xA754, eos,
            0xA757, 0xA756, eos,	//  3022
            0xA759, 0xA758, eos,
            0xA75B, 0xA75A, eos,
            0xA75D, 0xA75C, eos,	//  3031
            0xA75F, 0xA75E, eos,
            0xA761, 0xA760, eos,
            0xA763, 0xA762, eos,	//  3040
            0xA765, 0xA764, eos,
            0xA767, 0xA766, eos,
            0xA769, 0xA768, eos,
            0xA76B, 0xA76A, eos,	//  3052
            0xA76D, 0xA76C, eos,
            0xA76F, 0xA76E, eos,
            0xA77A, 0xA779, eos,	//  3061
            0xA77C, 0xA77B, eos,
            0x1D79, 0xA77D, eos,
            0xA77F, 0xA77E, eos,	//  3070
            0xA781, 0xA780, eos,
            0xA783, 0xA782, eos,
            0xA785, 0xA784, eos,
            0xA787, 0xA786, eos,	//  3082
            0xA78C, 0xA78B, eos,
            0x0265, 0xA78D, eos,
            0xA791, 0xA790, eos,	//  3091
            0xA793, 0xA792, eos,
            0xA797, 0xA796, eos,
            0xA799, 0xA798, eos,	//  3100
            0xA79B, 0xA79A, eos,
            0xA79D, 0xA79C, eos,
            0xA79F, 0xA79E, eos,
            0xA7A1, 0xA7A0, eos,	//  3112
            0xA7A3, 0xA7A2, eos,
            0xA7A5, 0xA7A4, eos,
            0xA7A7, 0xA7A6, eos,	//  3121
            0xA7A9, 0xA7A8, eos,
            0x0266, 0xA7AA, eos,
            0x025C, 0xA7AB, eos,	//  3130
            0x0261, 0xA7AC, eos,
            0x026C, 0xA7AD, eos,
            0x026A, 0xA7AE, eos,
            0x029E, 0xA7B0, eos,	//  3142
            0x0287, 0xA7B1, eos,
            0x029D, 0xA7B2, eos,
            0xAB53, 0xA7B3, eos,	//  3151
            0xA7B5, 0xA7B4, eos,
            0xA7B7, 0xA7B6, eos,
            0xA7B9, 0xA7B8, eos,	//  3160
            0xA7BB, 0xA7BA, eos,
            0xA7BD, 0xA7BC, eos,
            0xA7BF, 0xA7BE, eos,
            0xA7C1, 0xA7C0, eos,	//  3172
            0xA7C3, 0xA7C2, eos,
            0xA794, 0xA7C4, eos,
            0x0282, 0xA7C5, eos,	//  3181
            0x1D8E, 0xA7C6, eos,
            0xA7C8, 0xA7C7, eos,
            0xA7CA, 0xA7C9, eos,	//  3190
            0xA7D1, 0xA7D0, eos,
            0xA7D7, 0xA7D6, eos,
            0xA7D9, 0xA7D8, eos,
            0xA7F6, 0xA7F5, eos,	//  3202
            0x13A0, 0xAB70, eos,
            0x13A1, 0xAB71, eos,
            0x13A2, 0xAB72, eos,	//  3211
            0x13A3, 0xAB73, eos,
            0x13A4, 0xAB74, eos,
            0x13A5, 0xAB75, eos,	//  3220
            0x13A6, 0xAB76, eos,
            0x13A7, 0xAB77, eos,
            0x13A8, 0xAB78, eos,
            0x13A9, 0xAB79, eos,	//  3232
            0x13AA, 0xAB7A, eos,
            0x13AB, 0xAB7B, eos,
            0x13AC, 0xAB7C, eos,	//  3241
            0x13AD, 0xAB7D, eos,
            0x13AE, 0xAB7E, eos,
            0x13AF, 0xAB7F, eos,	//  3250
            0x13B0, 0xAB80, eos,
            0x13B1, 0xAB81, eos,
            0x13B2, 0xAB82, eos,
            0x13B3, 0xAB83, eos,	//  3262
            0x13B4, 0xAB84, eos,
            0x13B5, 0xAB85, eos,
            0x13B6, 0xAB86, eos,	//  3271
            0x13B7, 0xAB87, eos,
            0x13B8, 0xAB88, eos,
            0x13B9, 0xAB89, eos,	//  3280
            0x13BA, 0xAB8A, eos,
            0x13BB, 0xAB8B, eos,
            0x13BC, 0xAB8C, eos,
            0x13BD, 0xAB8D, eos,	//  3292
            0x13BE, 0xAB8E, eos,
            0x13BF, 0xAB8F, eos,
            0x13C0, 0xAB90, eos,	//  3301
            0x13C1, 0xAB91, eos,
            0x13C2, 0xAB92, eos,
            0x13C3, 0xAB93, eos,	//  3310
            0x13C4, 0xAB94, eos,
            0x13C5, 0xAB95, eos,
            0x13C6, 0xAB96, eos,
            0x13C7, 0xAB97, eos,	//  3322
            0x13C8, 0xAB98, eos,
            0x13C9, 0xAB99, eos,
            0x13CA, 0xAB9A, eos,	//  3331
            0x13CB, 0xAB9B, eos,
            0x13CC, 0xAB9C, eos,
            0x13CD, 0xAB9D, eos,	//  3340
            0x13CE, 0xAB9E, eos,
            0x13CF, 0xAB9F, eos,
            0x13D0, 0xABA0, eos,
            0x13D1, 0xABA1, eos,	//  3352
            0x13D2, 0xABA2, eos,
            0x13D3, 0xABA3, eos,
            0x13D4, 0xABA4, eos,	//  3361
            0x13D5, 0xABA5, eos,
            0x13D6, 0xABA6, eos,
            0x13D7, 0xABA7, eos,	//  3370
            0x13D8, 0xABA8, eos,
            0x13D9, 0xABA9, eos,
            0x13DA, 0xABAA, eos,
            0x13DB, 0xABAB, eos,	//  3382
            0x13DC, 0xABAC, eos,
            0x13DD, 0xABAD, eos,
            0x13DE, 0xABAE, eos,	//  3391
            0x13DF, 0xABAF, eos,
            0x13E0, 0xABB0, eos,
            0x13E1, 0xABB1, eos,	//  3400
            0x13E2, 0xABB2, eos,
            0x13E3, 0xABB3, eos,
            0x13E4, 0xABB4, eos,
            0x13E5, 0xABB5, eos,	//  3412
            0x13E6, 0xABB6, eos,
            0x13E7, 0xABB7, eos,
            0x13E8, 0xABB8, eos,	//  3421
            0x13E9, 0xABB9, eos,
            0x13EA, 0xABBA, eos,
            0x13EB, 0xABBB, eos,	//  3430
            0x13EC, 0xABBC, eos,
            0x13ED, 0xABBD, eos,
            0x13EE, 0xABBE, eos,
            0x13EF, 0xABBF, eos,	//  3442
            0xFF41, 0xFF21, eos,
            0xFF42, 0xFF22, eos,
            0xFF43, 0xFF23, eos,	//  3451
            0xFF44, 0xFF24, eos,
            0xFF45, 0xFF25, eos,
            0xFF46, 0xFF26, eos,	//  3460
            0xFF47, 0xFF27, eos,
            0xFF48, 0xFF28, eos,
            0xFF49, 0xFF29, eos,
            0xFF4A, 0xFF2A, eos,	//  3472
            0xFF4B, 0xFF2B, eos,
            0xFF4C, 0xFF2C, eos,
            0xFF4D, 0xFF2D, eos,	//  3481
            0xFF4E, 0xFF2E, eos,
            0xFF4F, 0xFF2F, eos,
            0xFF50, 0xFF30, eos,	//  3490
            0xFF51, 0xFF31, eos,
            0xFF52, 0xFF32, eos,
            0xFF53, 0xFF33, eos,
            0xFF54, 0xFF34, eos,	//  3502
            0xFF55, 0xFF35, eos,
            0xFF56, 0xFF36, eos,
            0xFF57, 0xFF37, eos,	//  3511
            0xFF58, 0xFF38, eos,
            0xFF59, 0xFF39, eos,
            0xFF5A, 0xFF3A, eos,	//  3520
            0x10428, 0x10400, eos,
            0x10429, 0x10401, eos,
            0x1042A, 0x10402, eos,
            0x1042B, 0x10403, eos,	//  3532
            0x1042C, 0x10404, eos,
            0x1042D, 0x10405, eos,
            0x1042E, 0x10406, eos,	//  3541
            0x1042F, 0x10407, eos,
            0x10430, 0x10408, eos,
            0x10431, 0x10409, eos,	//  3550
            0x10432, 0x1040A, eos,
            0x10433, 0x1040B, eos,
            0x10434, 0x1040C, eos,
            0x10435, 0x1040D, eos,	//  3562
            0x10436, 0x1040E, eos,
            0x10437, 0x1040F, eos,
            0x10438, 0x10410, eos,	//  3571
            0x10439, 0x10411, eos,
            0x1043A, 0x10412, eos,
            0x1043B, 0x10413, eos,	//  3580
            0x1043C, 0x10414, eos,
            0x1043D, 0x10415, eos,
            0x1043E, 0x10416, eos,
            0x1043F, 0x10417, eos,	//  3592
            0x10440, 0x10418, eos,
            0x10441, 0x10419, eos,
            0x10442, 0x1041A, eos,	//  3601
            0x10443, 0x1041B, eos,
            0x10444, 0x1041C, eos,
            0x10445, 0x1041D, eos,	//  3610
            0x10446, 0x1041E, eos,
            0x10447, 0x1041F, eos,
            0x10448, 0x10420, eos,
            0x10449, 0x10421, eos,	//  3622
            0x1044A, 0x10422, eos,
            0x1044B, 0x10423, eos,
            0x1044C, 0x10424, eos,	//  3631
            0x1044D, 0x10425, eos,
            0x1044E, 0x10426, eos,
            0x1044F, 0x10427, eos,	//  3640
            0x104D8, 0x104B0, eos,
            0x104D9, 0x104B1, eos,
            0x104DA, 0x104B2, eos,
            0x104DB, 0x104B3, eos,	//  3652
            0x104DC, 0x104B4, eos,
            0x104DD, 0x104B5, eos,
            0x104DE, 0x104B6, eos,	//  3661
            0x104DF, 0x104B7, eos,
            0x104E0, 0x104B8, eos,
            0x104E1, 0x104B9, eos,	//  3670
            0x104E2, 0x104BA, eos,
            0x104E3, 0x104BB, eos,
            0x104E4, 0x104BC, eos,
            0x104E5, 0x104BD, eos,	//  3682
            0x104E6, 0x104BE, eos,
            0x104E7, 0x104BF, eos,
            0x104E8, 0x104C0, eos,	//  3691
            0x104E9, 0x104C1, eos,
            0x104EA, 0x104C2, eos,
            0x104EB, 0x104C3, eos,	//  3700
            0x104EC, 0x104C4, eos,
            0x104ED, 0x104C5, eos,
            0x104EE, 0x104C6, eos,
            0x104EF, 0x104C7, eos,	//  3712
            0x104F0, 0x104C8, eos,
            0x104F1, 0x104C9, eos,
            0x104F2, 0x104CA, eos,	//  3721
            0x104F3, 0x104CB, eos,
            0x104F4, 0x104CC, eos,
            0x104F5, 0x104CD, eos,	//  3730
            0x104F6, 0x104CE, eos,
            0x104F7, 0x104CF, eos,
            0x104F8, 0x104D0, eos,
            0x104F9, 0x104D1, eos,	//  3742
            0x104FA, 0x104D2, eos,
            0x104FB, 0x104D3, eos,
            0x10597, 0x10570, eos,	//  3751
            0x10598, 0x10571, eos,
            0x10599, 0x10572, eos,
            0x1059A, 0x10573, eos,	//  3760
            0x1059B, 0x10574, eos,
            0x1059C, 0x10575, eos,
            0x1059D, 0x10576, eos,
            0x1059E, 0x10577, eos,	//  3772
            0x1059F, 0x10578, eos,
            0x105A0, 0x10579, eos,
            0x105A1, 0x1057A, eos,	//  3781
            0x105A3, 0x1057C, eos,
            0x105A4, 0x1057D, eos,
            0x105A5, 0x1057E, eos,	//  3790
            0x105A6, 0x1057F, eos,
            0x105A7, 0x10580, eos,
            0x105A8, 0x10581, eos,
            0x105A9, 0x10582, eos,	//  3802
            0x105AA, 0x10583, eos,
            0x105AB, 0x10584, eos,
            0x105AC, 0x10585, eos,	//  3811
            0x105AD, 0x10586, eos,
            0x105AE, 0x10587, eos,
            0x105AF, 0x10588, eos,	//  3820
            0x105B0, 0x10589, eos,
            0x105B1, 0x1058A, eos,
            0x105B3, 0x1058C, eos,
            0x105B4, 0x1058D, eos,	//  3832
            0x105B5, 0x1058E, eos,
            0x105B6, 0x1058F, eos,
            0x105B7, 0x10590, eos,	//  3841
            0x105B8, 0x10591, eos,
            0x105B9, 0x10592, eos,
            0x105BB, 0x10594, eos,	//  3850
            0x105BC, 0x10595, eos,
            0x10CC0, 0x10C80, eos,
            0x10CC1, 0x10C81, eos,
            0x10CC2, 0x10C82, eos,	//  3862
            0x10CC3, 0x10C83, eos,
            0x10CC4, 0x10C84, eos,
            0x10CC5, 0x10C85, eos,	//  3871
            0x10CC6, 0x10C86, eos,
            0x10CC7, 0x10C87, eos,
            0x10CC8, 0x10C88, eos,	//  3880
            0x10CC9, 0x10C89, eos,
            0x10CCA, 0x10C8A, eos,
            0x10CCB, 0x10C8B, eos,
            0x10CCC, 0x10C8C, eos,	//  3892
            0x10CCD, 0x10C8D, eos,
            0x10CCE, 0x10C8E, eos,
            0x10CCF, 0x10C8F, eos,	//  3901
            0x10CD0, 0x10C90, eos,
            0x10CD1, 0x10C91, eos,
            0x10CD2, 0x10C92, eos,	//  3910
            0x10CD3, 0x10C93, eos,
            0x10CD4, 0x10C94, eos,
            0x10CD5, 0x10C95, eos,
            0x10CD6, 0x10C96, eos,	//  3922
            0x10CD7, 0x10C97, eos,
            0x10CD8, 0x10C98, eos,
            0x10CD9, 0x10C99, eos,	//  3931
            0x10CDA, 0x10C9A, eos,
            0x10CDB, 0x10C9B, eos,
            0x10CDC, 0x10C9C, eos,	//  3940
            0x10CDD, 0x10C9D, eos,
            0x10CDE, 0x10C9E, eos,
            0x10CDF, 0x10C9F, eos,
            0x10CE0, 0x10CA0, eos,	//  3952
            0x10CE1, 0x10CA1, eos,
            0x10CE2, 0x10CA2, eos,
            0x10CE3, 0x10CA3, eos,	//  3961
            0x10CE4, 0x10CA4, eos,
            0x10CE5, 0x10CA5, eos,
            0x10CE6, 0x10CA6, eos,	//  3970
            0x10CE7, 0x10CA7, eos,
            0x10CE8, 0x10CA8, eos,
            0x10CE9, 0x10CA9, eos,
            0x10CEA, 0x10CAA, eos,	//  3982
            0x10CEB, 0x10CAB, eos,
            0x10CEC, 0x10CAC, eos,
            0x10CED, 0x10CAD, eos,	//  3991
            0x10CEE, 0x10CAE, eos,
            0x10CEF, 0x10CAF, eos,
            0x10CF0, 0x10CB0, eos,	//  4000
            0x10CF1, 0x10CB1, eos,
            0x10CF2, 0x10CB2, eos,
            0x118C0, 0x118A0, eos,
            0x118C1, 0x118A1, eos,	//  4012
            0x118C2, 0x118A2, eos,
            0x118C3, 0x118A3, eos,
            0x118C4, 0x118A4, eos,	//  4021
            0x118C5, 0x118A5, eos,
            0x118C6, 0x118A6, eos,
            0x118C7, 0x118A7, eos,	//  4030
            0x118C8, 0x118A8, eos,
            0x118C9, 0x118A9, eos,
            0x118CA, 0x118AA, eos,
            0x118CB, 0x118AB, eos,	//  4042
            0x118CC, 0x118AC, eos,
            0x118CD, 0x118AD, eos,
            0x118CE, 0x118AE, eos,	//  4051
            0x118CF, 0x118AF, eos,
            0x118D0, 0x118B0, eos,
            0x118D1, 0x118B1, eos,	//  4060
            0x118D2, 0x118B2, eos,
            0x118D3, 0x118B3, eos,
            0x118D4, 0x118B4, eos,
            0x118D5, 0x118B5, eos,	//  4072
            0x118D6, 0x118B6, eos,
            0x118D7, 0x118B7, eos,
            0x118D8, 0x118B8, eos,	//  4081
            0x118D9, 0x118B9, eos,
            0x118DA, 0x118BA, eos,
            0x118DB, 0x118BB, eos,	//  4090
            0x118DC, 0x118BC, eos,
            0x118DD, 0x118BD, eos,
            0x118DE, 0x118BE, eos,
            0x118DF, 0x118BF, eos,	//  4102
            0x16E60, 0x16E40, eos,
            0x16E61, 0x16E41, eos,
            0x16E62, 0x16E42, eos,	//  4111
            0x16E63, 0x16E43, eos,
            0x16E64, 0x16E44, eos,
            0x16E65, 0x16E45, eos,	//  4120
            0x16E66, 0x16E46, eos,
            0x16E67, 0x16E47, eos,
            0x16E68, 0x16E48, eos,
            0x16E69, 0x16E49, eos,	//  4132
            0x16E6A, 0x16E4A, eos,
            0x16E6B, 0x16E4B, eos,
            0x16E6C, 0x16E4C, eos,	//  4141
            0x16E6D, 0x16E4D, eos,
            0x16E6E, 0x16E4E, eos,
            0x16E6F, 0x16E4F, eos,	//  4150
            0x16E70, 0x16E50, eos,
            0x16E71, 0x16E51, eos,
            0x16E72, 0x16E52, eos,
            0x16E73, 0x16E53, eos,	//  4162
            0x16E74, 0x16E54, eos,
            0x16E75, 0x16E55, eos,
            0x16E76, 0x16E56, eos,	//  4171
            0x16E77, 0x16E57, eos,
            0x16E78, 0x16E58, eos,
            0x16E79, 0x16E59, eos,	//  4180
            0x16E7A, 0x16E5A, eos,
            0x16E7B, 0x16E5B, eos,
            0x16E7C, 0x16E5C, eos,
            0x16E7D, 0x16E5D, eos,	//  4192
            0x16E7E, 0x16E5E, eos,
            0x16E7F, 0x16E5F, eos,
            0x1E922, 0x1E900, eos,	//  4201
            0x1E923, 0x1E901, eos,
            0x1E924, 0x1E902, eos,
            0x1E925, 0x1E903, eos,	//  4210
            0x1E926, 0x1E904, eos,
            0x1E927, 0x1E905, eos,
            0x1E928, 0x1E906, eos,
            0x1E929, 0x1E907, eos,	//  4222
            0x1E92A, 0x1E908, eos,
            0x1E92B, 0x1E909, eos,
            0x1E92C, 0x1E90A, eos,	//  4231
            0x1E92D, 0x1E90B, eos,
            0x1E92E, 0x1E90C, eos,
            0x1E92F, 0x1E90D, eos,	//  4240
            0x1E930, 0x1E90E, eos,
            0x1E931, 0x1E90F, eos,
            0x1E932, 0x1E910, eos,
            0x1E933, 0x1E911, eos,	//  4252
            0x1E934, 0x1E912, eos,
            0x1E935, 0x1E913, eos,
            0x1E936, 0x1E914, eos,	//  4261
            0x1E937, 0x1E915, eos,
            0x1E938, 0x1E916, eos,
            0x1E939, 0x1E917, eos,	//  4270
            0x1E93A, 0x1E918, eos,
            0x1E93B, 0x1E919, eos,
            0x1E93C, 0x1E91A, eos,
            0x1E93D, 0x1E91B, eos,	//  4282
            0x1E93E, 0x1E91C, eos,
            0x1E93F, 0x1E91D, eos,
            0x1E940, 0x1E91E, eos,	//  4291
            0x1E941, 0x1E91F, eos,
            0x1E942, 0x1E920, eos,
            0x1E943, 0x1E921, eos	//  4300
        };
#define SRELL_UCFDATA_VERSION 200
        //  ... "srell_ucfdata2.hpp"]

        namespace ucf_internal {

            typedef unicode_casefolding<uchar32, uchar32> ucf_data;

        }	//  namespace ucf_internal
#endif	//  !defined(SRELL_NO_UNICODE_ICASE)

        class unicode_case_folding {
        public:

#if !defined(SRELL_NO_UNICODE_ICASE)
            static const uchar32 rev_maxset = ucf_internal::ucf_data::rev_maxset;
#else
            static const uchar32 rev_maxset = 2;
#endif

            static uchar32 do_casefolding(const uchar32 cp) {
#if !defined(SRELL_NO_UNICODE_ICASE)
                if (cp <= ucf_internal::ucf_data::ucf_maxcodepoint)
                    return cp + ucf_internal::ucf_data::ucf_deltatable[ucf_internal::ucf_data::ucf_segmenttable[cp >> 8] + (cp & 0xff)];
#else
                if (cp >= char_alnum::ch_A && cp <= char_alnum::ch_Z)	//  'A' && 'Z'
                    return static_cast<uchar32>(cp - char_alnum::ch_A + char_alnum::ch_a);	//  - 'A' + 'a'
#endif
                return cp;
            }

            static uchar32 casefoldedcharset(uchar32 out[rev_maxset], const uchar32 cp) {
#if !defined(SRELL_NO_UNICODE_ICASE)
                uchar32 count = 0;

                if (cp <= ucf_internal::ucf_data::rev_maxcodepoint) {
                    const uchar32 offset_of_charset = ucf_internal::ucf_data::rev_indextable[ucf_internal::ucf_data::rev_segmenttable[cp >> 8] + (cp & 0xff)];
                    const uchar32* ptr = &ucf_internal::ucf_data::rev_charsettable[offset_of_charset];

                    for (; *ptr != cfcharset_eos_ && count < rev_maxset; ++ptr, ++count)
                        out[count] = *ptr;
                }
                if (count == 0)
                    out[count++] = cp;

                return count;
#else
                //		const uchar32 nocase = static_cast<uchar32>(cp & ~0x20);
                const uchar32 nocase = static_cast<uchar32>(cp | constants::asc_icase);

                out[0] = cp;
                //		if (nocase >= char_alnum::ch_A && nocase <= char_alnum::ch_Z)
                if (nocase >= char_alnum::ch_a && nocase <= char_alnum::ch_z) {
                    out[1] = static_cast<uchar32>(cp ^ constants::asc_icase);
                    return 2;
                }
                return 1;
#endif
            }

            unicode_case_folding& operator=(const unicode_case_folding&) {
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            unicode_case_folding& operator=(unicode_case_folding&&) SRELL_NOEXCEPT {
                return *this;
            }
#endif

            void swap(unicode_case_folding& /* right */) {
            }

        private:

#if !defined(SRELL_NO_UNICODE_ICASE)
            static const uchar32 cfcharset_eos_ = ucf_internal::ucf_data::eos;
#endif

        public:	//  For debug.

            void print_tables() const;
        };
        //  unicode_case_folding

    }	//  namespace regex_internal

//  ... "rei_ucf.hpp"]
//  ["rei_up.hpp" ...

    namespace regex_internal {

#if !defined(SRELL_NO_UNICODE_PROPERTY)
        //  ["srell_updata.hpp" ...
        //  UnicodeData.txt
        //
        //  PropList-14.0.0.txt
        //  Date: 2021-08-12, 23:13:05 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html
        //
        //  DerivedCoreProperties-14.0.0.txt
        //  Date: 2021-08-12, 23:12:53 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html
        //
        //  emoji-data-14.0.0.txt
        //  Date: 2021-08-26, 17:22:22 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html
        //
        //  DerivedNormalizationProps-14.0.0.txt
        //  Date: 2021-06-04, 02:19:20 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html
        //
        //  Scripts-14.0.0.txt
        //  Date: 2021-07-10, 00:35:31 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html
        //
        //  ScriptExtensions-14.0.0.txt
        //  Date: 2021-06-04, 02:19:38 GMT
        //  © 2021 Unicode®, Inc.
        //  Unicode and the Unicode Logo are registered trademarks of Unicode, Inc. in the U.S. and other countries.
        //  For terms of use, see http://www.unicode.org/terms_of_use.html
        //

        template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
        struct unicode_property_data {
            static const T1 unknown = 0;
            static const T1 gc_Other = 1;	//  #1
            static const T1 gc_Control = 2;	//  #2
            static const T1 gc_Format = 3;	//  #3
            static const T1 gc_Unassigned = 4;	//  #4
            static const T1 gc_Private_Use = 5;	//  #5
            static const T1 gc_Surrogate = 6;	//  #6
            static const T1 gc_Letter = 7;	//  #7
            static const T1 gc_Cased_Letter = 8;	//  #8
            static const T1 gc_Lowercase_Letter = 9;	//  #9
            static const T1 gc_Titlecase_Letter = 10;	//  #10
            static const T1 gc_Uppercase_Letter = 11;	//  #11
            static const T1 gc_Modifier_Letter = 12;	//  #12
            static const T1 gc_Other_Letter = 13;	//  #13
            static const T1 gc_Mark = 14;	//  #14
            static const T1 gc_Spacing_Mark = 15;	//  #15
            static const T1 gc_Enclosing_Mark = 16;	//  #16
            static const T1 gc_Nonspacing_Mark = 17;	//  #17
            static const T1 gc_Number = 18;	//  #18
            static const T1 gc_Decimal_Number = 19;	//  #19
            static const T1 gc_Letter_Number = 20;	//  #20
            static const T1 gc_Other_Number = 21;	//  #21
            static const T1 gc_Punctuation = 22;	//  #22
            static const T1 gc_Connector_Punctuation = 23;	//  #23
            static const T1 gc_Dash_Punctuation = 24;	//  #24
            static const T1 gc_Close_Punctuation = 25;	//  #25
            static const T1 gc_Final_Punctuation = 26;	//  #26
            static const T1 gc_Initial_Punctuation = 27;	//  #27
            static const T1 gc_Other_Punctuation = 28;	//  #28
            static const T1 gc_Open_Punctuation = 29;	//  #29
            static const T1 gc_Symbol = 30;	//  #30
            static const T1 gc_Currency_Symbol = 31;	//  #31
            static const T1 gc_Modifier_Symbol = 32;	//  #32
            static const T1 gc_Math_Symbol = 33;	//  #33
            static const T1 gc_Other_Symbol = 34;	//  #34
            static const T1 gc_Separator = 35;	//  #35
            static const T1 gc_Line_Separator = 36;	//  #36
            static const T1 gc_Paragraph_Separator = 37;	//  #37
            static const T1 gc_Space_Separator = 38;	//  #38
            static const T1 bp_ASCII = 39;	//  #39
            static const T1 bp_ASCII_Hex_Digit = 40;	//  #40
            static const T1 bp_Alphabetic = 41;	//  #41
            static const T1 bp_Any = 42;	//  #42
            static const T1 bp_Assigned = 43;	//  #43
            static const T1 bp_Bidi_Control = 44;	//  #44
            static const T1 bp_Bidi_Mirrored = 45;	//  #45
            static const T1 bp_Case_Ignorable = 46;	//  #46
            static const T1 bp_Cased = 47;	//  #47
            static const T1 bp_Changes_When_Casefolded = 48;	//  #48
            static const T1 bp_Changes_When_Casemapped = 49;	//  #49
            static const T1 bp_Changes_When_Lowercased = 50;	//  #50
            static const T1 bp_Changes_When_NFKC_Casefolded = 51;	//  #51
            static const T1 bp_Changes_When_Titlecased = 52;	//  #52
            static const T1 bp_Changes_When_Uppercased = 53;	//  #53
            static const T1 bp_Dash = 54;	//  #54
            static const T1 bp_Default_Ignorable_Code_Point = 55;	//  #55
            static const T1 bp_Deprecated = 56;	//  #56
            static const T1 bp_Diacritic = 57;	//  #57
            static const T1 bp_Emoji = 58;	//  #58
            static const T1 bp_Emoji_Component = 59;	//  #59
            static const T1 bp_Emoji_Modifier = 60;	//  #60
            static const T1 bp_Emoji_Modifier_Base = 61;	//  #61
            static const T1 bp_Emoji_Presentation = 62;	//  #62
            static const T1 bp_Extended_Pictographic = 63;	//  #63
            static const T1 bp_Extender = 64;	//  #64
            static const T1 bp_Grapheme_Base = 65;	//  #65
            static const T1 bp_Grapheme_Extend = 66;	//  #66
            static const T1 bp_Hex_Digit = 67;	//  #67
            static const T1 bp_IDS_Binary_Operator = 68;	//  #68
            static const T1 bp_IDS_Trinary_Operator = 69;	//  #69
            static const T1 bp_ID_Continue = 70;	//  #70
            static const T1 bp_ID_Start = 71;	//  #71
            static const T1 bp_Ideographic = 72;	//  #72
            static const T1 bp_Join_Control = 73;	//  #73
            static const T1 bp_Logical_Order_Exception = 74;	//  #74
            static const T1 bp_Lowercase = 75;	//  #75
            static const T1 bp_Math = 76;	//  #76
            static const T1 bp_Noncharacter_Code_Point = 77;	//  #77
            static const T1 bp_Pattern_Syntax = 78;	//  #78
            static const T1 bp_Pattern_White_Space = 79;	//  #79
            static const T1 bp_Quotation_Mark = 80;	//  #80
            static const T1 bp_Radical = 81;	//  #81
            static const T1 bp_Regional_Indicator = 82;	//  #82
            static const T1 bp_Sentence_Terminal = 83;	//  #83
            static const T1 bp_Soft_Dotted = 84;	//  #84
            static const T1 bp_Terminal_Punctuation = 85;	//  #85
            static const T1 bp_Unified_Ideograph = 86;	//  #86
            static const T1 bp_Uppercase = 87;	//  #87
            static const T1 bp_Variation_Selector = 88;	//  #88
            static const T1 bp_White_Space = 89;	//  #89
            static const T1 bp_XID_Continue = 90;	//  #90
            static const T1 bp_XID_Start = 91;	//  #91
            static const T1 sc_Adlam = 92;	//  #92
            static const T1 sc_Ahom = 93;	//  #93
            static const T1 sc_Anatolian_Hieroglyphs = 94;	//  #94
            static const T1 sc_Arabic = 95;	//  #95
            static const T1 sc_Armenian = 96;	//  #96
            static const T1 sc_Avestan = 97;	//  #97
            static const T1 sc_Balinese = 98;	//  #98
            static const T1 sc_Bamum = 99;	//  #99
            static const T1 sc_Bassa_Vah = 100;	//  #100
            static const T1 sc_Batak = 101;	//  #101
            static const T1 sc_Bengali = 102;	//  #102
            static const T1 sc_Bhaiksuki = 103;	//  #103
            static const T1 sc_Bopomofo = 104;	//  #104
            static const T1 sc_Brahmi = 105;	//  #105
            static const T1 sc_Braille = 106;	//  #106
            static const T1 sc_Buginese = 107;	//  #107
            static const T1 sc_Buhid = 108;	//  #108
            static const T1 sc_Canadian_Aboriginal = 109;	//  #109
            static const T1 sc_Carian = 110;	//  #110
            static const T1 sc_Caucasian_Albanian = 111;	//  #111
            static const T1 sc_Chakma = 112;	//  #112
            static const T1 sc_Cham = 113;	//  #113
            static const T1 sc_Cherokee = 114;	//  #114
            static const T1 sc_Chorasmian = 115;	//  #115
            static const T1 sc_Common = 116;	//  #116
            static const T1 sc_Coptic = 117;	//  #117
            static const T1 sc_Cypro_Minoan = 118;	//  #118
            static const T1 sc_Cuneiform = 119;	//  #119
            static const T1 sc_Cypriot = 120;	//  #120
            static const T1 sc_Cyrillic = 121;	//  #121
            static const T1 sc_Deseret = 122;	//  #122
            static const T1 sc_Devanagari = 123;	//  #123
            static const T1 sc_Dives_Akuru = 124;	//  #124
            static const T1 sc_Dogra = 125;	//  #125
            static const T1 sc_Duployan = 126;	//  #126
            static const T1 sc_Egyptian_Hieroglyphs = 127;	//  #127
            static const T1 sc_Elbasan = 128;	//  #128
            static const T1 sc_Elymaic = 129;	//  #129
            static const T1 sc_Ethiopic = 130;	//  #130
            static const T1 sc_Georgian = 131;	//  #131
            static const T1 sc_Glagolitic = 132;	//  #132
            static const T1 sc_Gothic = 133;	//  #133
            static const T1 sc_Grantha = 134;	//  #134
            static const T1 sc_Greek = 135;	//  #135
            static const T1 sc_Gujarati = 136;	//  #136
            static const T1 sc_Gunjala_Gondi = 137;	//  #137
            static const T1 sc_Gurmukhi = 138;	//  #138
            static const T1 sc_Han = 139;	//  #139
            static const T1 sc_Hangul = 140;	//  #140
            static const T1 sc_Hanifi_Rohingya = 141;	//  #141
            static const T1 sc_Hanunoo = 142;	//  #142
            static const T1 sc_Hatran = 143;	//  #143
            static const T1 sc_Hebrew = 144;	//  #144
            static const T1 sc_Hiragana = 145;	//  #145
            static const T1 sc_Imperial_Aramaic = 146;	//  #146
            static const T1 sc_Inherited = 147;	//  #147
            static const T1 sc_Inscriptional_Pahlavi = 148;	//  #148
            static const T1 sc_Inscriptional_Parthian = 149;	//  #149
            static const T1 sc_Javanese = 150;	//  #150
            static const T1 sc_Kaithi = 151;	//  #151
            static const T1 sc_Kannada = 152;	//  #152
            static const T1 sc_Katakana = 153;	//  #153
            static const T1 sc_Kayah_Li = 154;	//  #154
            static const T1 sc_Kharoshthi = 155;	//  #155
            static const T1 sc_Khitan_Small_Script = 156;	//  #156
            static const T1 sc_Khmer = 157;	//  #157
            static const T1 sc_Khojki = 158;	//  #158
            static const T1 sc_Khudawadi = 159;	//  #159
            static const T1 sc_Lao = 160;	//  #160
            static const T1 sc_Latin = 161;	//  #161
            static const T1 sc_Lepcha = 162;	//  #162
            static const T1 sc_Limbu = 163;	//  #163
            static const T1 sc_Linear_A = 164;	//  #164
            static const T1 sc_Linear_B = 165;	//  #165
            static const T1 sc_Lisu = 166;	//  #166
            static const T1 sc_Lycian = 167;	//  #167
            static const T1 sc_Lydian = 168;	//  #168
            static const T1 sc_Mahajani = 169;	//  #169
            static const T1 sc_Makasar = 170;	//  #170
            static const T1 sc_Malayalam = 171;	//  #171
            static const T1 sc_Mandaic = 172;	//  #172
            static const T1 sc_Manichaean = 173;	//  #173
            static const T1 sc_Marchen = 174;	//  #174
            static const T1 sc_Masaram_Gondi = 175;	//  #175
            static const T1 sc_Medefaidrin = 176;	//  #176
            static const T1 sc_Meetei_Mayek = 177;	//  #177
            static const T1 sc_Mende_Kikakui = 178;	//  #178
            static const T1 sc_Meroitic_Cursive = 179;	//  #179
            static const T1 sc_Meroitic_Hieroglyphs = 180;	//  #180
            static const T1 sc_Miao = 181;	//  #181
            static const T1 sc_Modi = 182;	//  #182
            static const T1 sc_Mongolian = 183;	//  #183
            static const T1 sc_Mro = 184;	//  #184
            static const T1 sc_Multani = 185;	//  #185
            static const T1 sc_Myanmar = 186;	//  #186
            static const T1 sc_Nabataean = 187;	//  #187
            static const T1 sc_Nandinagari = 188;	//  #188
            static const T1 sc_New_Tai_Lue = 189;	//  #189
            static const T1 sc_Newa = 190;	//  #190
            static const T1 sc_Nko = 191;	//  #191
            static const T1 sc_Nushu = 192;	//  #192
            static const T1 sc_Nyiakeng_Puachue_Hmong = 193;	//  #193
            static const T1 sc_Ogham = 194;	//  #194
            static const T1 sc_Ol_Chiki = 195;	//  #195
            static const T1 sc_Old_Hungarian = 196;	//  #196
            static const T1 sc_Old_Italic = 197;	//  #197
            static const T1 sc_Old_North_Arabian = 198;	//  #198
            static const T1 sc_Old_Permic = 199;	//  #199
            static const T1 sc_Old_Persian = 200;	//  #200
            static const T1 sc_Old_Sogdian = 201;	//  #201
            static const T1 sc_Old_South_Arabian = 202;	//  #202
            static const T1 sc_Old_Turkic = 203;	//  #203
            static const T1 sc_Old_Uyghur = 204;	//  #204
            static const T1 sc_Oriya = 205;	//  #205
            static const T1 sc_Osage = 206;	//  #206
            static const T1 sc_Osmanya = 207;	//  #207
            static const T1 sc_Pahawh_Hmong = 208;	//  #208
            static const T1 sc_Palmyrene = 209;	//  #209
            static const T1 sc_Pau_Cin_Hau = 210;	//  #210
            static const T1 sc_Phags_Pa = 211;	//  #211
            static const T1 sc_Phoenician = 212;	//  #212
            static const T1 sc_Psalter_Pahlavi = 213;	//  #213
            static const T1 sc_Rejang = 214;	//  #214
            static const T1 sc_Runic = 215;	//  #215
            static const T1 sc_Samaritan = 216;	//  #216
            static const T1 sc_Saurashtra = 217;	//  #217
            static const T1 sc_Sharada = 218;	//  #218
            static const T1 sc_Shavian = 219;	//  #219
            static const T1 sc_Siddham = 220;	//  #220
            static const T1 sc_SignWriting = 221;	//  #221
            static const T1 sc_Sinhala = 222;	//  #222
            static const T1 sc_Sogdian = 223;	//  #223
            static const T1 sc_Sora_Sompeng = 224;	//  #224
            static const T1 sc_Soyombo = 225;	//  #225
            static const T1 sc_Sundanese = 226;	//  #226
            static const T1 sc_Syloti_Nagri = 227;	//  #227
            static const T1 sc_Syriac = 228;	//  #228
            static const T1 sc_Tagalog = 229;	//  #229
            static const T1 sc_Tagbanwa = 230;	//  #230
            static const T1 sc_Tai_Le = 231;	//  #231
            static const T1 sc_Tai_Tham = 232;	//  #232
            static const T1 sc_Tai_Viet = 233;	//  #233
            static const T1 sc_Takri = 234;	//  #234
            static const T1 sc_Tamil = 235;	//  #235
            static const T1 sc_Tangsa = 236;	//  #236
            static const T1 sc_Tangut = 237;	//  #237
            static const T1 sc_Telugu = 238;	//  #238
            static const T1 sc_Thaana = 239;	//  #239
            static const T1 sc_Thai = 240;	//  #240
            static const T1 sc_Tibetan = 241;	//  #241
            static const T1 sc_Tifinagh = 242;	//  #242
            static const T1 sc_Tirhuta = 243;	//  #243
            static const T1 sc_Toto = 244;	//  #244
            static const T1 sc_Ugaritic = 245;	//  #245
            static const T1 sc_Vai = 246;	//  #246
            static const T1 sc_Vithkuqi = 247;	//  #247
            static const T1 sc_Wancho = 248;	//  #248
            static const T1 sc_Warang_Citi = 249;	//  #249
            static const T1 sc_Yezidi = 250;	//  #250
            static const T1 sc_Yi = 251;	//  #251
            static const T1 sc_Zanabazar_Square = 252;	//  #252
            static const T1 scx_Adlam = 253;	//  #253
            static const T1 scx_Ahom = 254;	//  #93
            static const T1 scx_Anatolian_Hieroglyphs = 255;	//  #94
            static const T1 scx_Arabic = 256;	//  #254
            static const T1 scx_Armenian = 257;	//  #96
            static const T1 scx_Avestan = 258;	//  #97
            static const T1 scx_Balinese = 259;	//  #98
            static const T1 scx_Bamum = 260;	//  #99
            static const T1 scx_Bassa_Vah = 261;	//  #100
            static const T1 scx_Batak = 262;	//  #101
            static const T1 scx_Bengali = 263;	//  #255
            static const T1 scx_Bhaiksuki = 264;	//  #103
            static const T1 scx_Bopomofo = 265;	//  #256
            static const T1 scx_Brahmi = 266;	//  #105
            static const T1 scx_Braille = 267;	//  #106
            static const T1 scx_Buginese = 268;	//  #257
            static const T1 scx_Buhid = 269;	//  #258
            static const T1 scx_Canadian_Aboriginal = 270;	//  #109
            static const T1 scx_Carian = 271;	//  #110
            static const T1 scx_Caucasian_Albanian = 272;	//  #111
            static const T1 scx_Chakma = 273;	//  #259
            static const T1 scx_Cham = 274;	//  #113
            static const T1 scx_Cherokee = 275;	//  #114
            static const T1 scx_Chorasmian = 276;	//  #115
            static const T1 scx_Common = 277;	//  #260
            static const T1 scx_Coptic = 278;	//  #261
            static const T1 scx_Cypro_Minoan = 279;	//  #262
            static const T1 scx_Cuneiform = 280;	//  #119
            static const T1 scx_Cypriot = 281;	//  #263
            static const T1 scx_Cyrillic = 282;	//  #264
            static const T1 scx_Deseret = 283;	//  #122
            static const T1 scx_Devanagari = 284;	//  #265
            static const T1 scx_Dives_Akuru = 285;	//  #124
            static const T1 scx_Dogra = 286;	//  #266
            static const T1 scx_Duployan = 287;	//  #267
            static const T1 scx_Egyptian_Hieroglyphs = 288;	//  #127
            static const T1 scx_Elbasan = 289;	//  #128
            static const T1 scx_Elymaic = 290;	//  #129
            static const T1 scx_Ethiopic = 291;	//  #130
            static const T1 scx_Georgian = 292;	//  #268
            static const T1 scx_Glagolitic = 293;	//  #269
            static const T1 scx_Gothic = 294;	//  #133
            static const T1 scx_Grantha = 295;	//  #270
            static const T1 scx_Greek = 296;	//  #271
            static const T1 scx_Gujarati = 297;	//  #272
            static const T1 scx_Gunjala_Gondi = 298;	//  #273
            static const T1 scx_Gurmukhi = 299;	//  #274
            static const T1 scx_Han = 300;	//  #275
            static const T1 scx_Hangul = 301;	//  #276
            static const T1 scx_Hanifi_Rohingya = 302;	//  #277
            static const T1 scx_Hanunoo = 303;	//  #278
            static const T1 scx_Hatran = 304;	//  #143
            static const T1 scx_Hebrew = 305;	//  #144
            static const T1 scx_Hiragana = 306;	//  #279
            static const T1 scx_Imperial_Aramaic = 307;	//  #146
            static const T1 scx_Inherited = 308;	//  #280
            static const T1 scx_Inscriptional_Pahlavi = 309;	//  #148
            static const T1 scx_Inscriptional_Parthian = 310;	//  #149
            static const T1 scx_Javanese = 311;	//  #281
            static const T1 scx_Kaithi = 312;	//  #282
            static const T1 scx_Kannada = 313;	//  #283
            static const T1 scx_Katakana = 314;	//  #284
            static const T1 scx_Kayah_Li = 315;	//  #285
            static const T1 scx_Kharoshthi = 316;	//  #155
            static const T1 scx_Khitan_Small_Script = 317;	//  #156
            static const T1 scx_Khmer = 318;	//  #157
            static const T1 scx_Khojki = 319;	//  #286
            static const T1 scx_Khudawadi = 320;	//  #287
            static const T1 scx_Lao = 321;	//  #160
            static const T1 scx_Latin = 322;	//  #288
            static const T1 scx_Lepcha = 323;	//  #162
            static const T1 scx_Limbu = 324;	//  #289
            static const T1 scx_Linear_A = 325;	//  #290
            static const T1 scx_Linear_B = 326;	//  #291
            static const T1 scx_Lisu = 327;	//  #166
            static const T1 scx_Lycian = 328;	//  #167
            static const T1 scx_Lydian = 329;	//  #168
            static const T1 scx_Mahajani = 330;	//  #292
            static const T1 scx_Makasar = 331;	//  #170
            static const T1 scx_Malayalam = 332;	//  #293
            static const T1 scx_Mandaic = 333;	//  #294
            static const T1 scx_Manichaean = 334;	//  #295
            static const T1 scx_Marchen = 335;	//  #174
            static const T1 scx_Masaram_Gondi = 336;	//  #296
            static const T1 scx_Medefaidrin = 337;	//  #176
            static const T1 scx_Meetei_Mayek = 338;	//  #177
            static const T1 scx_Mende_Kikakui = 339;	//  #178
            static const T1 scx_Meroitic_Cursive = 340;	//  #179
            static const T1 scx_Meroitic_Hieroglyphs = 341;	//  #180
            static const T1 scx_Miao = 342;	//  #181
            static const T1 scx_Modi = 343;	//  #297
            static const T1 scx_Mongolian = 344;	//  #298
            static const T1 scx_Mro = 345;	//  #184
            static const T1 scx_Multani = 346;	//  #299
            static const T1 scx_Myanmar = 347;	//  #300
            static const T1 scx_Nabataean = 348;	//  #187
            static const T1 scx_Nandinagari = 349;	//  #301
            static const T1 scx_New_Tai_Lue = 350;	//  #189
            static const T1 scx_Newa = 351;	//  #190
            static const T1 scx_Nko = 352;	//  #302
            static const T1 scx_Nushu = 353;	//  #192
            static const T1 scx_Nyiakeng_Puachue_Hmong = 354;	//  #193
            static const T1 scx_Ogham = 355;	//  #194
            static const T1 scx_Ol_Chiki = 356;	//  #195
            static const T1 scx_Old_Hungarian = 357;	//  #196
            static const T1 scx_Old_Italic = 358;	//  #197
            static const T1 scx_Old_North_Arabian = 359;	//  #198
            static const T1 scx_Old_Permic = 360;	//  #303
            static const T1 scx_Old_Persian = 361;	//  #200
            static const T1 scx_Old_Sogdian = 362;	//  #201
            static const T1 scx_Old_South_Arabian = 363;	//  #202
            static const T1 scx_Old_Turkic = 364;	//  #203
            static const T1 scx_Old_Uyghur = 365;	//  #304
            static const T1 scx_Oriya = 366;	//  #305
            static const T1 scx_Osage = 367;	//  #206
            static const T1 scx_Osmanya = 368;	//  #207
            static const T1 scx_Pahawh_Hmong = 369;	//  #208
            static const T1 scx_Palmyrene = 370;	//  #209
            static const T1 scx_Pau_Cin_Hau = 371;	//  #210
            static const T1 scx_Phags_Pa = 372;	//  #306
            static const T1 scx_Phoenician = 373;	//  #212
            static const T1 scx_Psalter_Pahlavi = 374;	//  #307
            static const T1 scx_Rejang = 375;	//  #214
            static const T1 scx_Runic = 376;	//  #215
            static const T1 scx_Samaritan = 377;	//  #216
            static const T1 scx_Saurashtra = 378;	//  #217
            static const T1 scx_Sharada = 379;	//  #308
            static const T1 scx_Shavian = 380;	//  #219
            static const T1 scx_Siddham = 381;	//  #220
            static const T1 scx_SignWriting = 382;	//  #221
            static const T1 scx_Sinhala = 383;	//  #309
            static const T1 scx_Sogdian = 384;	//  #310
            static const T1 scx_Sora_Sompeng = 385;	//  #224
            static const T1 scx_Soyombo = 386;	//  #225
            static const T1 scx_Sundanese = 387;	//  #226
            static const T1 scx_Syloti_Nagri = 388;	//  #311
            static const T1 scx_Syriac = 389;	//  #312
            static const T1 scx_Tagalog = 390;	//  #313
            static const T1 scx_Tagbanwa = 391;	//  #314
            static const T1 scx_Tai_Le = 392;	//  #315
            static const T1 scx_Tai_Tham = 393;	//  #232
            static const T1 scx_Tai_Viet = 394;	//  #233
            static const T1 scx_Takri = 395;	//  #316
            static const T1 scx_Tamil = 396;	//  #317
            static const T1 scx_Tangsa = 397;	//  #236
            static const T1 scx_Tangut = 398;	//  #237
            static const T1 scx_Telugu = 399;	//  #318
            static const T1 scx_Thaana = 400;	//  #319
            static const T1 scx_Thai = 401;	//  #240
            static const T1 scx_Tibetan = 402;	//  #241
            static const T1 scx_Tifinagh = 403;	//  #242
            static const T1 scx_Tirhuta = 404;	//  #320
            static const T1 scx_Toto = 405;	//  #244
            static const T1 scx_Ugaritic = 406;	//  #245
            static const T1 scx_Vai = 407;	//  #246
            static const T1 scx_Vithkuqi = 408;	//  #247
            static const T1 scx_Wancho = 409;	//  #248
            static const T1 scx_Warang_Citi = 410;	//  #249
            static const T1 scx_Yezidi = 411;	//  #321
            static const T1 scx_Yi = 412;	//  #322
            static const T1 scx_Zanabazar_Square = 413;	//  #252
            static const T1 last_property_number = 322;
            struct ptype {
                static const T2 unknown = 0;
                static const T2 binary = 1;
                static const T2 general_category = 2;
                static const T2 script = 3;
                static const T2 script_extensions = 4;
            };
            static const T3 propertynametable[];
            static const T4 rangetable[];
            static const T5 rangenumbertable[];
            static const T6 positiontable[];

            static const T3* propertyname_table() {
                return propertynametable;
            }
            static const T4* ranges() {
                return rangetable;
            }
            static const T5* rangenumber_table() {
                return rangenumbertable;
            }
            static const T6* position_table() {
                return positiontable;
            }
        };

        template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
        const T3 unicode_property_data<T1, T2, T3, T4, T5, T6>::propertynametable[] =
        {
            "*",	//  #0:unknown
            "*",	//  #1:binary
            "General_Category:gc",	//  #2
            "Script:sc",	//  #3
            "Script_Extensions:scx",	//  #4
            ""
        };

        template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
        const T4 unicode_property_data<T1, T2, T3, T4, T5, T6>::rangetable[] =
        {
            //  #1 (0+725): gc=Other:C
            //  Cc:2 + Cf:21 + Cn:698 + Co:3 + Cs:1
            //  #2 (0+2): gc=Control:Cc:cntrl
            0x0000, 0x001F, 0x007F, 0x009F,
            //  #3 (2+21): gc=Format:Cf
            0x00AD, 0x00AD, 0x0600, 0x0605, 0x061C, 0x061C, 0x06DD, 0x06DD,
            0x070F, 0x070F, 0x0890, 0x0891, 0x08E2, 0x08E2, 0x180E, 0x180E,
            0x200B, 0x200F, 0x202A, 0x202E, 0x2060, 0x2064, 0x2066, 0x206F,
            0xFEFF, 0xFEFF, 0xFFF9, 0xFFFB, 0x110BD, 0x110BD, 0x110CD, 0x110CD,
            0x13430, 0x13438, 0x1BCA0, 0x1BCA3, 0x1D173, 0x1D17A, 0xE0001, 0xE0001,
            0xE0020, 0xE007F,
            //  #4 (23+698): gc=Unassigned:Cn
            0x0378, 0x0379, 0x0380, 0x0383, 0x038B, 0x038B, 0x038D, 0x038D,
            0x03A2, 0x03A2, 0x0530, 0x0530, 0x0557, 0x0558, 0x058B, 0x058C,
            0x0590, 0x0590, 0x05C8, 0x05CF, 0x05EB, 0x05EE, 0x05F5, 0x05FF,
            0x070E, 0x070E, 0x074B, 0x074C, 0x07B2, 0x07BF, 0x07FB, 0x07FC,
            0x082E, 0x082F, 0x083F, 0x083F, 0x085C, 0x085D, 0x085F, 0x085F,
            0x086B, 0x086F, 0x088F, 0x088F, 0x0892, 0x0897, 0x0984, 0x0984,
            0x098D, 0x098E, 0x0991, 0x0992, 0x09A9, 0x09A9, 0x09B1, 0x09B1,
            0x09B3, 0x09B5, 0x09BA, 0x09BB, 0x09C5, 0x09C6, 0x09C9, 0x09CA,
            0x09CF, 0x09D6, 0x09D8, 0x09DB, 0x09DE, 0x09DE, 0x09E4, 0x09E5,
            0x09FF, 0x0A00, 0x0A04, 0x0A04, 0x0A0B, 0x0A0E, 0x0A11, 0x0A12,
            0x0A29, 0x0A29, 0x0A31, 0x0A31, 0x0A34, 0x0A34, 0x0A37, 0x0A37,
            0x0A3A, 0x0A3B, 0x0A3D, 0x0A3D, 0x0A43, 0x0A46, 0x0A49, 0x0A4A,
            0x0A4E, 0x0A50, 0x0A52, 0x0A58, 0x0A5D, 0x0A5D, 0x0A5F, 0x0A65,
            0x0A77, 0x0A80, 0x0A84, 0x0A84, 0x0A8E, 0x0A8E, 0x0A92, 0x0A92,
            0x0AA9, 0x0AA9, 0x0AB1, 0x0AB1, 0x0AB4, 0x0AB4, 0x0ABA, 0x0ABB,
            0x0AC6, 0x0AC6, 0x0ACA, 0x0ACA, 0x0ACE, 0x0ACF, 0x0AD1, 0x0ADF,
            0x0AE4, 0x0AE5, 0x0AF2, 0x0AF8, 0x0B00, 0x0B00, 0x0B04, 0x0B04,
            0x0B0D, 0x0B0E, 0x0B11, 0x0B12, 0x0B29, 0x0B29, 0x0B31, 0x0B31,
            0x0B34, 0x0B34, 0x0B3A, 0x0B3B, 0x0B45, 0x0B46, 0x0B49, 0x0B4A,
            0x0B4E, 0x0B54, 0x0B58, 0x0B5B, 0x0B5E, 0x0B5E, 0x0B64, 0x0B65,
            0x0B78, 0x0B81, 0x0B84, 0x0B84, 0x0B8B, 0x0B8D, 0x0B91, 0x0B91,
            0x0B96, 0x0B98, 0x0B9B, 0x0B9B, 0x0B9D, 0x0B9D, 0x0BA0, 0x0BA2,
            0x0BA5, 0x0BA7, 0x0BAB, 0x0BAD, 0x0BBA, 0x0BBD, 0x0BC3, 0x0BC5,
            0x0BC9, 0x0BC9, 0x0BCE, 0x0BCF, 0x0BD1, 0x0BD6, 0x0BD8, 0x0BE5,
            0x0BFB, 0x0BFF, 0x0C0D, 0x0C0D, 0x0C11, 0x0C11, 0x0C29, 0x0C29,
            0x0C3A, 0x0C3B, 0x0C45, 0x0C45, 0x0C49, 0x0C49, 0x0C4E, 0x0C54,
            0x0C57, 0x0C57, 0x0C5B, 0x0C5C, 0x0C5E, 0x0C5F, 0x0C64, 0x0C65,
            0x0C70, 0x0C76, 0x0C8D, 0x0C8D, 0x0C91, 0x0C91, 0x0CA9, 0x0CA9,
            0x0CB4, 0x0CB4, 0x0CBA, 0x0CBB, 0x0CC5, 0x0CC5, 0x0CC9, 0x0CC9,
            0x0CCE, 0x0CD4, 0x0CD7, 0x0CDC, 0x0CDF, 0x0CDF, 0x0CE4, 0x0CE5,
            0x0CF0, 0x0CF0, 0x0CF3, 0x0CFF, 0x0D0D, 0x0D0D, 0x0D11, 0x0D11,
            0x0D45, 0x0D45, 0x0D49, 0x0D49, 0x0D50, 0x0D53, 0x0D64, 0x0D65,
            0x0D80, 0x0D80, 0x0D84, 0x0D84, 0x0D97, 0x0D99, 0x0DB2, 0x0DB2,
            0x0DBC, 0x0DBC, 0x0DBE, 0x0DBF, 0x0DC7, 0x0DC9, 0x0DCB, 0x0DCE,
            0x0DD5, 0x0DD5, 0x0DD7, 0x0DD7, 0x0DE0, 0x0DE5, 0x0DF0, 0x0DF1,
            0x0DF5, 0x0E00, 0x0E3B, 0x0E3E, 0x0E5C, 0x0E80, 0x0E83, 0x0E83,
            0x0E85, 0x0E85, 0x0E8B, 0x0E8B, 0x0EA4, 0x0EA4, 0x0EA6, 0x0EA6,
            0x0EBE, 0x0EBF, 0x0EC5, 0x0EC5, 0x0EC7, 0x0EC7, 0x0ECE, 0x0ECF,
            0x0EDA, 0x0EDB, 0x0EE0, 0x0EFF, 0x0F48, 0x0F48, 0x0F6D, 0x0F70,
            0x0F98, 0x0F98, 0x0FBD, 0x0FBD, 0x0FCD, 0x0FCD, 0x0FDB, 0x0FFF,
            0x10C6, 0x10C6, 0x10C8, 0x10CC, 0x10CE, 0x10CF, 0x1249, 0x1249,
            0x124E, 0x124F, 0x1257, 0x1257, 0x1259, 0x1259, 0x125E, 0x125F,
            0x1289, 0x1289, 0x128E, 0x128F, 0x12B1, 0x12B1, 0x12B6, 0x12B7,
            0x12BF, 0x12BF, 0x12C1, 0x12C1, 0x12C6, 0x12C7, 0x12D7, 0x12D7,
            0x1311, 0x1311, 0x1316, 0x1317, 0x135B, 0x135C, 0x137D, 0x137F,
            0x139A, 0x139F, 0x13F6, 0x13F7, 0x13FE, 0x13FF, 0x169D, 0x169F,
            0x16F9, 0x16FF, 0x1716, 0x171E, 0x1737, 0x173F, 0x1754, 0x175F,
            0x176D, 0x176D, 0x1771, 0x1771, 0x1774, 0x177F, 0x17DE, 0x17DF,
            0x17EA, 0x17EF, 0x17FA, 0x17FF, 0x181A, 0x181F, 0x1879, 0x187F,
            0x18AB, 0x18AF, 0x18F6, 0x18FF, 0x191F, 0x191F, 0x192C, 0x192F,
            0x193C, 0x193F, 0x1941, 0x1943, 0x196E, 0x196F, 0x1975, 0x197F,
            0x19AC, 0x19AF, 0x19CA, 0x19CF, 0x19DB, 0x19DD, 0x1A1C, 0x1A1D,
            0x1A5F, 0x1A5F, 0x1A7D, 0x1A7E, 0x1A8A, 0x1A8F, 0x1A9A, 0x1A9F,
            0x1AAE, 0x1AAF, 0x1ACF, 0x1AFF, 0x1B4D, 0x1B4F, 0x1B7F, 0x1B7F,
            0x1BF4, 0x1BFB, 0x1C38, 0x1C3A, 0x1C4A, 0x1C4C, 0x1C89, 0x1C8F,
            0x1CBB, 0x1CBC, 0x1CC8, 0x1CCF, 0x1CFB, 0x1CFF, 0x1F16, 0x1F17,
            0x1F1E, 0x1F1F, 0x1F46, 0x1F47, 0x1F4E, 0x1F4F, 0x1F58, 0x1F58,
            0x1F5A, 0x1F5A, 0x1F5C, 0x1F5C, 0x1F5E, 0x1F5E, 0x1F7E, 0x1F7F,
            0x1FB5, 0x1FB5, 0x1FC5, 0x1FC5, 0x1FD4, 0x1FD5, 0x1FDC, 0x1FDC,
            0x1FF0, 0x1FF1, 0x1FF5, 0x1FF5, 0x1FFF, 0x1FFF, 0x2065, 0x2065,
            0x2072, 0x2073, 0x208F, 0x208F, 0x209D, 0x209F, 0x20C1, 0x20CF,
            0x20F1, 0x20FF, 0x218C, 0x218F, 0x2427, 0x243F, 0x244B, 0x245F,
            0x2B74, 0x2B75, 0x2B96, 0x2B96, 0x2CF4, 0x2CF8, 0x2D26, 0x2D26,
            0x2D28, 0x2D2C, 0x2D2E, 0x2D2F, 0x2D68, 0x2D6E, 0x2D71, 0x2D7E,
            0x2D97, 0x2D9F, 0x2DA7, 0x2DA7, 0x2DAF, 0x2DAF, 0x2DB7, 0x2DB7,
            0x2DBF, 0x2DBF, 0x2DC7, 0x2DC7, 0x2DCF, 0x2DCF, 0x2DD7, 0x2DD7,
            0x2DDF, 0x2DDF, 0x2E5E, 0x2E7F, 0x2E9A, 0x2E9A, 0x2EF4, 0x2EFF,
            0x2FD6, 0x2FEF, 0x2FFC, 0x2FFF, 0x3040, 0x3040, 0x3097, 0x3098,
            0x3100, 0x3104, 0x3130, 0x3130, 0x318F, 0x318F, 0x31E4, 0x31EF,
            0x321F, 0x321F, 0xA48D, 0xA48F, 0xA4C7, 0xA4CF, 0xA62C, 0xA63F,
            0xA6F8, 0xA6FF, 0xA7CB, 0xA7CF, 0xA7D2, 0xA7D2, 0xA7D4, 0xA7D4,
            0xA7DA, 0xA7F1, 0xA82D, 0xA82F, 0xA83A, 0xA83F, 0xA878, 0xA87F,
            0xA8C6, 0xA8CD, 0xA8DA, 0xA8DF, 0xA954, 0xA95E, 0xA97D, 0xA97F,
            0xA9CE, 0xA9CE, 0xA9DA, 0xA9DD, 0xA9FF, 0xA9FF, 0xAA37, 0xAA3F,
            0xAA4E, 0xAA4F, 0xAA5A, 0xAA5B, 0xAAC3, 0xAADA, 0xAAF7, 0xAB00,
            0xAB07, 0xAB08, 0xAB0F, 0xAB10, 0xAB17, 0xAB1F, 0xAB27, 0xAB27,
            0xAB2F, 0xAB2F, 0xAB6C, 0xAB6F, 0xABEE, 0xABEF, 0xABFA, 0xABFF,
            0xD7A4, 0xD7AF, 0xD7C7, 0xD7CA, 0xD7FC, 0xD7FF, 0xFA6E, 0xFA6F,
            0xFADA, 0xFAFF, 0xFB07, 0xFB12, 0xFB18, 0xFB1C, 0xFB37, 0xFB37,
            0xFB3D, 0xFB3D, 0xFB3F, 0xFB3F, 0xFB42, 0xFB42, 0xFB45, 0xFB45,
            0xFBC3, 0xFBD2, 0xFD90, 0xFD91, 0xFDC8, 0xFDCE, 0xFDD0, 0xFDEF,
            0xFE1A, 0xFE1F, 0xFE53, 0xFE53, 0xFE67, 0xFE67, 0xFE6C, 0xFE6F,
            0xFE75, 0xFE75, 0xFEFD, 0xFEFE, 0xFF00, 0xFF00, 0xFFBF, 0xFFC1,
            0xFFC8, 0xFFC9, 0xFFD0, 0xFFD1, 0xFFD8, 0xFFD9, 0xFFDD, 0xFFDF,
            0xFFE7, 0xFFE7, 0xFFEF, 0xFFF8, 0xFFFE, 0xFFFF, 0x1000C, 0x1000C,
            0x10027, 0x10027, 0x1003B, 0x1003B, 0x1003E, 0x1003E, 0x1004E, 0x1004F,
            0x1005E, 0x1007F, 0x100FB, 0x100FF, 0x10103, 0x10106, 0x10134, 0x10136,
            0x1018F, 0x1018F, 0x1019D, 0x1019F, 0x101A1, 0x101CF, 0x101FE, 0x1027F,
            0x1029D, 0x1029F, 0x102D1, 0x102DF, 0x102FC, 0x102FF, 0x10324, 0x1032C,
            0x1034B, 0x1034F, 0x1037B, 0x1037F, 0x1039E, 0x1039E, 0x103C4, 0x103C7,
            0x103D6, 0x103FF, 0x1049E, 0x1049F, 0x104AA, 0x104AF, 0x104D4, 0x104D7,
            0x104FC, 0x104FF, 0x10528, 0x1052F, 0x10564, 0x1056E, 0x1057B, 0x1057B,
            0x1058B, 0x1058B, 0x10593, 0x10593, 0x10596, 0x10596, 0x105A2, 0x105A2,
            0x105B2, 0x105B2, 0x105BA, 0x105BA, 0x105BD, 0x105FF, 0x10737, 0x1073F,
            0x10756, 0x1075F, 0x10768, 0x1077F, 0x10786, 0x10786, 0x107B1, 0x107B1,
            0x107BB, 0x107FF, 0x10806, 0x10807, 0x10809, 0x10809, 0x10836, 0x10836,
            0x10839, 0x1083B, 0x1083D, 0x1083E, 0x10856, 0x10856, 0x1089F, 0x108A6,
            0x108B0, 0x108DF, 0x108F3, 0x108F3, 0x108F6, 0x108FA, 0x1091C, 0x1091E,
            0x1093A, 0x1093E, 0x10940, 0x1097F, 0x109B8, 0x109BB, 0x109D0, 0x109D1,
            0x10A04, 0x10A04, 0x10A07, 0x10A0B, 0x10A14, 0x10A14, 0x10A18, 0x10A18,
            0x10A36, 0x10A37, 0x10A3B, 0x10A3E, 0x10A49, 0x10A4F, 0x10A59, 0x10A5F,
            0x10AA0, 0x10ABF, 0x10AE7, 0x10AEA, 0x10AF7, 0x10AFF, 0x10B36, 0x10B38,
            0x10B56, 0x10B57, 0x10B73, 0x10B77, 0x10B92, 0x10B98, 0x10B9D, 0x10BA8,
            0x10BB0, 0x10BFF, 0x10C49, 0x10C7F, 0x10CB3, 0x10CBF, 0x10CF3, 0x10CF9,
            0x10D28, 0x10D2F, 0x10D3A, 0x10E5F, 0x10E7F, 0x10E7F, 0x10EAA, 0x10EAA,
            0x10EAE, 0x10EAF, 0x10EB2, 0x10EFF, 0x10F28, 0x10F2F, 0x10F5A, 0x10F6F,
            0x10F8A, 0x10FAF, 0x10FCC, 0x10FDF, 0x10FF7, 0x10FFF, 0x1104E, 0x11051,
            0x11076, 0x1107E, 0x110C3, 0x110CC, 0x110CE, 0x110CF, 0x110E9, 0x110EF,
            0x110FA, 0x110FF, 0x11135, 0x11135, 0x11148, 0x1114F, 0x11177, 0x1117F,
            0x111E0, 0x111E0, 0x111F5, 0x111FF, 0x11212, 0x11212, 0x1123F, 0x1127F,
            0x11287, 0x11287, 0x11289, 0x11289, 0x1128E, 0x1128E, 0x1129E, 0x1129E,
            0x112AA, 0x112AF, 0x112EB, 0x112EF, 0x112FA, 0x112FF, 0x11304, 0x11304,
            0x1130D, 0x1130E, 0x11311, 0x11312, 0x11329, 0x11329, 0x11331, 0x11331,
            0x11334, 0x11334, 0x1133A, 0x1133A, 0x11345, 0x11346, 0x11349, 0x1134A,
            0x1134E, 0x1134F, 0x11351, 0x11356, 0x11358, 0x1135C, 0x11364, 0x11365,
            0x1136D, 0x1136F, 0x11375, 0x113FF, 0x1145C, 0x1145C, 0x11462, 0x1147F,
            0x114C8, 0x114CF, 0x114DA, 0x1157F, 0x115B6, 0x115B7, 0x115DE, 0x115FF,
            0x11645, 0x1164F, 0x1165A, 0x1165F, 0x1166D, 0x1167F, 0x116BA, 0x116BF,
            0x116CA, 0x116FF, 0x1171B, 0x1171C, 0x1172C, 0x1172F, 0x11747, 0x117FF,
            0x1183C, 0x1189F, 0x118F3, 0x118FE, 0x11907, 0x11908, 0x1190A, 0x1190B,
            0x11914, 0x11914, 0x11917, 0x11917, 0x11936, 0x11936, 0x11939, 0x1193A,
            0x11947, 0x1194F, 0x1195A, 0x1199F, 0x119A8, 0x119A9, 0x119D8, 0x119D9,
            0x119E5, 0x119FF, 0x11A48, 0x11A4F, 0x11AA3, 0x11AAF, 0x11AF9, 0x11BFF,
            0x11C09, 0x11C09, 0x11C37, 0x11C37, 0x11C46, 0x11C4F, 0x11C6D, 0x11C6F,
            0x11C90, 0x11C91, 0x11CA8, 0x11CA8, 0x11CB7, 0x11CFF, 0x11D07, 0x11D07,
            0x11D0A, 0x11D0A, 0x11D37, 0x11D39, 0x11D3B, 0x11D3B, 0x11D3E, 0x11D3E,
            0x11D48, 0x11D4F, 0x11D5A, 0x11D5F, 0x11D66, 0x11D66, 0x11D69, 0x11D69,
            0x11D8F, 0x11D8F, 0x11D92, 0x11D92, 0x11D99, 0x11D9F, 0x11DAA, 0x11EDF,
            0x11EF9, 0x11FAF, 0x11FB1, 0x11FBF, 0x11FF2, 0x11FFE, 0x1239A, 0x123FF,
            0x1246F, 0x1246F, 0x12475, 0x1247F, 0x12544, 0x12F8F, 0x12FF3, 0x12FFF,
            0x1342F, 0x1342F, 0x13439, 0x143FF, 0x14647, 0x167FF, 0x16A39, 0x16A3F,
            0x16A5F, 0x16A5F, 0x16A6A, 0x16A6D, 0x16ABF, 0x16ABF, 0x16ACA, 0x16ACF,
            0x16AEE, 0x16AEF, 0x16AF6, 0x16AFF, 0x16B46, 0x16B4F, 0x16B5A, 0x16B5A,
            0x16B62, 0x16B62, 0x16B78, 0x16B7C, 0x16B90, 0x16E3F, 0x16E9B, 0x16EFF,
            0x16F4B, 0x16F4E, 0x16F88, 0x16F8E, 0x16FA0, 0x16FDF, 0x16FE5, 0x16FEF,
            0x16FF2, 0x16FFF, 0x187F8, 0x187FF, 0x18CD6, 0x18CFF, 0x18D09, 0x1AFEF,
            0x1AFF4, 0x1AFF4, 0x1AFFC, 0x1AFFC, 0x1AFFF, 0x1AFFF, 0x1B123, 0x1B14F,
            0x1B153, 0x1B163, 0x1B168, 0x1B16F, 0x1B2FC, 0x1BBFF, 0x1BC6B, 0x1BC6F,
            0x1BC7D, 0x1BC7F, 0x1BC89, 0x1BC8F, 0x1BC9A, 0x1BC9B, 0x1BCA4, 0x1CEFF,
            0x1CF2E, 0x1CF2F, 0x1CF47, 0x1CF4F, 0x1CFC4, 0x1CFFF, 0x1D0F6, 0x1D0FF,
            0x1D127, 0x1D128, 0x1D1EB, 0x1D1FF, 0x1D246, 0x1D2DF, 0x1D2F4, 0x1D2FF,
            0x1D357, 0x1D35F, 0x1D379, 0x1D3FF, 0x1D455, 0x1D455, 0x1D49D, 0x1D49D,
            0x1D4A0, 0x1D4A1, 0x1D4A3, 0x1D4A4, 0x1D4A7, 0x1D4A8, 0x1D4AD, 0x1D4AD,
            0x1D4BA, 0x1D4BA, 0x1D4BC, 0x1D4BC, 0x1D4C4, 0x1D4C4, 0x1D506, 0x1D506,
            0x1D50B, 0x1D50C, 0x1D515, 0x1D515, 0x1D51D, 0x1D51D, 0x1D53A, 0x1D53A,
            0x1D53F, 0x1D53F, 0x1D545, 0x1D545, 0x1D547, 0x1D549, 0x1D551, 0x1D551,
            0x1D6A6, 0x1D6A7, 0x1D7CC, 0x1D7CD, 0x1DA8C, 0x1DA9A, 0x1DAA0, 0x1DAA0,
            0x1DAB0, 0x1DEFF, 0x1DF1F, 0x1DFFF, 0x1E007, 0x1E007, 0x1E019, 0x1E01A,
            0x1E022, 0x1E022, 0x1E025, 0x1E025, 0x1E02B, 0x1E0FF, 0x1E12D, 0x1E12F,
            0x1E13E, 0x1E13F, 0x1E14A, 0x1E14D, 0x1E150, 0x1E28F, 0x1E2AF, 0x1E2BF,
            0x1E2FA, 0x1E2FE, 0x1E300, 0x1E7DF, 0x1E7E7, 0x1E7E7, 0x1E7EC, 0x1E7EC,
            0x1E7EF, 0x1E7EF, 0x1E7FF, 0x1E7FF, 0x1E8C5, 0x1E8C6, 0x1E8D7, 0x1E8FF,
            0x1E94C, 0x1E94F, 0x1E95A, 0x1E95D, 0x1E960, 0x1EC70, 0x1ECB5, 0x1ED00,
            0x1ED3E, 0x1EDFF, 0x1EE04, 0x1EE04, 0x1EE20, 0x1EE20, 0x1EE23, 0x1EE23,
            0x1EE25, 0x1EE26, 0x1EE28, 0x1EE28, 0x1EE33, 0x1EE33, 0x1EE38, 0x1EE38,
            0x1EE3A, 0x1EE3A, 0x1EE3C, 0x1EE41, 0x1EE43, 0x1EE46, 0x1EE48, 0x1EE48,
            0x1EE4A, 0x1EE4A, 0x1EE4C, 0x1EE4C, 0x1EE50, 0x1EE50, 0x1EE53, 0x1EE53,
            0x1EE55, 0x1EE56, 0x1EE58, 0x1EE58, 0x1EE5A, 0x1EE5A, 0x1EE5C, 0x1EE5C,
            0x1EE5E, 0x1EE5E, 0x1EE60, 0x1EE60, 0x1EE63, 0x1EE63, 0x1EE65, 0x1EE66,
            0x1EE6B, 0x1EE6B, 0x1EE73, 0x1EE73, 0x1EE78, 0x1EE78, 0x1EE7D, 0x1EE7D,
            0x1EE7F, 0x1EE7F, 0x1EE8A, 0x1EE8A, 0x1EE9C, 0x1EEA0, 0x1EEA4, 0x1EEA4,
            0x1EEAA, 0x1EEAA, 0x1EEBC, 0x1EEEF, 0x1EEF2, 0x1EFFF, 0x1F02C, 0x1F02F,
            0x1F094, 0x1F09F, 0x1F0AF, 0x1F0B0, 0x1F0C0, 0x1F0C0, 0x1F0D0, 0x1F0D0,
            0x1F0F6, 0x1F0FF, 0x1F1AE, 0x1F1E5, 0x1F203, 0x1F20F, 0x1F23C, 0x1F23F,
            0x1F249, 0x1F24F, 0x1F252, 0x1F25F, 0x1F266, 0x1F2FF, 0x1F6D8, 0x1F6DC,
            0x1F6ED, 0x1F6EF, 0x1F6FD, 0x1F6FF, 0x1F774, 0x1F77F, 0x1F7D9, 0x1F7DF,
            0x1F7EC, 0x1F7EF, 0x1F7F1, 0x1F7FF, 0x1F80C, 0x1F80F, 0x1F848, 0x1F84F,
            0x1F85A, 0x1F85F, 0x1F888, 0x1F88F, 0x1F8AE, 0x1F8AF, 0x1F8B2, 0x1F8FF,
            0x1FA54, 0x1FA5F, 0x1FA6E, 0x1FA6F, 0x1FA75, 0x1FA77, 0x1FA7D, 0x1FA7F,
            0x1FA87, 0x1FA8F, 0x1FAAD, 0x1FAAF, 0x1FABB, 0x1FABF, 0x1FAC6, 0x1FACF,
            0x1FADA, 0x1FADF, 0x1FAE8, 0x1FAEF, 0x1FAF7, 0x1FAFF, 0x1FB93, 0x1FB93,
            0x1FBCB, 0x1FBEF, 0x1FBFA, 0x1FFFF, 0x2A6E0, 0x2A6FF, 0x2B739, 0x2B73F,
            0x2B81E, 0x2B81F, 0x2CEA2, 0x2CEAF, 0x2EBE1, 0x2F7FF, 0x2FA1E, 0x2FFFF,
            0x3134B, 0xE0000, 0xE0002, 0xE001F, 0xE0080, 0xE00FF, 0xE01F0, 0xEFFFF,
            0xFFFFE, 0xFFFFF, 0x10FFFE, 0x10FFFF,
            //  #5 (721+3): gc=Private_Use:Co
            0xE000, 0xF8FF, 0xF0000, 0xFFFFD, 0x100000, 0x10FFFD,
            //  #6 (724+1): gc=Surrogate:Cs
            0xD800, 0xDFFF,
            //  #7 (725+1883): gc=Letter:L
            //  Ll:657 + Lt:10 + Lu:646 + Lm:69 + Lo:501
            //  #8 (725+1313): gc=Cased_Letter:LC
            //  Ll:657 + Lt:10 + Lu:646
            //  #9 (725+657): gc=Lowercase_Letter:Ll
            0x0061, 0x007A, 0x00B5, 0x00B5, 0x00DF, 0x00F6, 0x00F8, 0x00FF,
            0x0101, 0x0101, 0x0103, 0x0103, 0x0105, 0x0105, 0x0107, 0x0107,
            0x0109, 0x0109, 0x010B, 0x010B, 0x010D, 0x010D, 0x010F, 0x010F,
            0x0111, 0x0111, 0x0113, 0x0113, 0x0115, 0x0115, 0x0117, 0x0117,
            0x0119, 0x0119, 0x011B, 0x011B, 0x011D, 0x011D, 0x011F, 0x011F,
            0x0121, 0x0121, 0x0123, 0x0123, 0x0125, 0x0125, 0x0127, 0x0127,
            0x0129, 0x0129, 0x012B, 0x012B, 0x012D, 0x012D, 0x012F, 0x012F,
            0x0131, 0x0131, 0x0133, 0x0133, 0x0135, 0x0135, 0x0137, 0x0138,
            0x013A, 0x013A, 0x013C, 0x013C, 0x013E, 0x013E, 0x0140, 0x0140,
            0x0142, 0x0142, 0x0144, 0x0144, 0x0146, 0x0146, 0x0148, 0x0149,
            0x014B, 0x014B, 0x014D, 0x014D, 0x014F, 0x014F, 0x0151, 0x0151,
            0x0153, 0x0153, 0x0155, 0x0155, 0x0157, 0x0157, 0x0159, 0x0159,
            0x015B, 0x015B, 0x015D, 0x015D, 0x015F, 0x015F, 0x0161, 0x0161,
            0x0163, 0x0163, 0x0165, 0x0165, 0x0167, 0x0167, 0x0169, 0x0169,
            0x016B, 0x016B, 0x016D, 0x016D, 0x016F, 0x016F, 0x0171, 0x0171,
            0x0173, 0x0173, 0x0175, 0x0175, 0x0177, 0x0177, 0x017A, 0x017A,
            0x017C, 0x017C, 0x017E, 0x0180, 0x0183, 0x0183, 0x0185, 0x0185,
            0x0188, 0x0188, 0x018C, 0x018D, 0x0192, 0x0192, 0x0195, 0x0195,
            0x0199, 0x019B, 0x019E, 0x019E, 0x01A1, 0x01A1, 0x01A3, 0x01A3,
            0x01A5, 0x01A5, 0x01A8, 0x01A8, 0x01AA, 0x01AB, 0x01AD, 0x01AD,
            0x01B0, 0x01B0, 0x01B4, 0x01B4, 0x01B6, 0x01B6, 0x01B9, 0x01BA,
            0x01BD, 0x01BF, 0x01C6, 0x01C6, 0x01C9, 0x01C9, 0x01CC, 0x01CC,
            0x01CE, 0x01CE, 0x01D0, 0x01D0, 0x01D2, 0x01D2, 0x01D4, 0x01D4,
            0x01D6, 0x01D6, 0x01D8, 0x01D8, 0x01DA, 0x01DA, 0x01DC, 0x01DD,
            0x01DF, 0x01DF, 0x01E1, 0x01E1, 0x01E3, 0x01E3, 0x01E5, 0x01E5,
            0x01E7, 0x01E7, 0x01E9, 0x01E9, 0x01EB, 0x01EB, 0x01ED, 0x01ED,
            0x01EF, 0x01F0, 0x01F3, 0x01F3, 0x01F5, 0x01F5, 0x01F9, 0x01F9,
            0x01FB, 0x01FB, 0x01FD, 0x01FD, 0x01FF, 0x01FF, 0x0201, 0x0201,
            0x0203, 0x0203, 0x0205, 0x0205, 0x0207, 0x0207, 0x0209, 0x0209,
            0x020B, 0x020B, 0x020D, 0x020D, 0x020F, 0x020F, 0x0211, 0x0211,
            0x0213, 0x0213, 0x0215, 0x0215, 0x0217, 0x0217, 0x0219, 0x0219,
            0x021B, 0x021B, 0x021D, 0x021D, 0x021F, 0x021F, 0x0221, 0x0221,
            0x0223, 0x0223, 0x0225, 0x0225, 0x0227, 0x0227, 0x0229, 0x0229,
            0x022B, 0x022B, 0x022D, 0x022D, 0x022F, 0x022F, 0x0231, 0x0231,
            0x0233, 0x0239, 0x023C, 0x023C, 0x023F, 0x0240, 0x0242, 0x0242,
            0x0247, 0x0247, 0x0249, 0x0249, 0x024B, 0x024B, 0x024D, 0x024D,
            0x024F, 0x0293, 0x0295, 0x02AF, 0x0371, 0x0371, 0x0373, 0x0373,
            0x0377, 0x0377, 0x037B, 0x037D, 0x0390, 0x0390, 0x03AC, 0x03CE,
            0x03D0, 0x03D1, 0x03D5, 0x03D7, 0x03D9, 0x03D9, 0x03DB, 0x03DB,
            0x03DD, 0x03DD, 0x03DF, 0x03DF, 0x03E1, 0x03E1, 0x03E3, 0x03E3,
            0x03E5, 0x03E5, 0x03E7, 0x03E7, 0x03E9, 0x03E9, 0x03EB, 0x03EB,
            0x03ED, 0x03ED, 0x03EF, 0x03F3, 0x03F5, 0x03F5, 0x03F8, 0x03F8,
            0x03FB, 0x03FC, 0x0430, 0x045F, 0x0461, 0x0461, 0x0463, 0x0463,
            0x0465, 0x0465, 0x0467, 0x0467, 0x0469, 0x0469, 0x046B, 0x046B,
            0x046D, 0x046D, 0x046F, 0x046F, 0x0471, 0x0471, 0x0473, 0x0473,
            0x0475, 0x0475, 0x0477, 0x0477, 0x0479, 0x0479, 0x047B, 0x047B,
            0x047D, 0x047D, 0x047F, 0x047F, 0x0481, 0x0481, 0x048B, 0x048B,
            0x048D, 0x048D, 0x048F, 0x048F, 0x0491, 0x0491, 0x0493, 0x0493,
            0x0495, 0x0495, 0x0497, 0x0497, 0x0499, 0x0499, 0x049B, 0x049B,
            0x049D, 0x049D, 0x049F, 0x049F, 0x04A1, 0x04A1, 0x04A3, 0x04A3,
            0x04A5, 0x04A5, 0x04A7, 0x04A7, 0x04A9, 0x04A9, 0x04AB, 0x04AB,
            0x04AD, 0x04AD, 0x04AF, 0x04AF, 0x04B1, 0x04B1, 0x04B3, 0x04B3,
            0x04B5, 0x04B5, 0x04B7, 0x04B7, 0x04B9, 0x04B9, 0x04BB, 0x04BB,
            0x04BD, 0x04BD, 0x04BF, 0x04BF, 0x04C2, 0x04C2, 0x04C4, 0x04C4,
            0x04C6, 0x04C6, 0x04C8, 0x04C8, 0x04CA, 0x04CA, 0x04CC, 0x04CC,
            0x04CE, 0x04CF, 0x04D1, 0x04D1, 0x04D3, 0x04D3, 0x04D5, 0x04D5,
            0x04D7, 0x04D7, 0x04D9, 0x04D9, 0x04DB, 0x04DB, 0x04DD, 0x04DD,
            0x04DF, 0x04DF, 0x04E1, 0x04E1, 0x04E3, 0x04E3, 0x04E5, 0x04E5,
            0x04E7, 0x04E7, 0x04E9, 0x04E9, 0x04EB, 0x04EB, 0x04ED, 0x04ED,
            0x04EF, 0x04EF, 0x04F1, 0x04F1, 0x04F3, 0x04F3, 0x04F5, 0x04F5,
            0x04F7, 0x04F7, 0x04F9, 0x04F9, 0x04FB, 0x04FB, 0x04FD, 0x04FD,
            0x04FF, 0x04FF, 0x0501, 0x0501, 0x0503, 0x0503, 0x0505, 0x0505,
            0x0507, 0x0507, 0x0509, 0x0509, 0x050B, 0x050B, 0x050D, 0x050D,
            0x050F, 0x050F, 0x0511, 0x0511, 0x0513, 0x0513, 0x0515, 0x0515,
            0x0517, 0x0517, 0x0519, 0x0519, 0x051B, 0x051B, 0x051D, 0x051D,
            0x051F, 0x051F, 0x0521, 0x0521, 0x0523, 0x0523, 0x0525, 0x0525,
            0x0527, 0x0527, 0x0529, 0x0529, 0x052B, 0x052B, 0x052D, 0x052D,
            0x052F, 0x052F, 0x0560, 0x0588, 0x10D0, 0x10FA, 0x10FD, 0x10FF,
            0x13F8, 0x13FD, 0x1C80, 0x1C88, 0x1D00, 0x1D2B, 0x1D6B, 0x1D77,
            0x1D79, 0x1D9A, 0x1E01, 0x1E01, 0x1E03, 0x1E03, 0x1E05, 0x1E05,
            0x1E07, 0x1E07, 0x1E09, 0x1E09, 0x1E0B, 0x1E0B, 0x1E0D, 0x1E0D,
            0x1E0F, 0x1E0F, 0x1E11, 0x1E11, 0x1E13, 0x1E13, 0x1E15, 0x1E15,
            0x1E17, 0x1E17, 0x1E19, 0x1E19, 0x1E1B, 0x1E1B, 0x1E1D, 0x1E1D,
            0x1E1F, 0x1E1F, 0x1E21, 0x1E21, 0x1E23, 0x1E23, 0x1E25, 0x1E25,
            0x1E27, 0x1E27, 0x1E29, 0x1E29, 0x1E2B, 0x1E2B, 0x1E2D, 0x1E2D,
            0x1E2F, 0x1E2F, 0x1E31, 0x1E31, 0x1E33, 0x1E33, 0x1E35, 0x1E35,
            0x1E37, 0x1E37, 0x1E39, 0x1E39, 0x1E3B, 0x1E3B, 0x1E3D, 0x1E3D,
            0x1E3F, 0x1E3F, 0x1E41, 0x1E41, 0x1E43, 0x1E43, 0x1E45, 0x1E45,
            0x1E47, 0x1E47, 0x1E49, 0x1E49, 0x1E4B, 0x1E4B, 0x1E4D, 0x1E4D,
            0x1E4F, 0x1E4F, 0x1E51, 0x1E51, 0x1E53, 0x1E53, 0x1E55, 0x1E55,
            0x1E57, 0x1E57, 0x1E59, 0x1E59, 0x1E5B, 0x1E5B, 0x1E5D, 0x1E5D,
            0x1E5F, 0x1E5F, 0x1E61, 0x1E61, 0x1E63, 0x1E63, 0x1E65, 0x1E65,
            0x1E67, 0x1E67, 0x1E69, 0x1E69, 0x1E6B, 0x1E6B, 0x1E6D, 0x1E6D,
            0x1E6F, 0x1E6F, 0x1E71, 0x1E71, 0x1E73, 0x1E73, 0x1E75, 0x1E75,
            0x1E77, 0x1E77, 0x1E79, 0x1E79, 0x1E7B, 0x1E7B, 0x1E7D, 0x1E7D,
            0x1E7F, 0x1E7F, 0x1E81, 0x1E81, 0x1E83, 0x1E83, 0x1E85, 0x1E85,
            0x1E87, 0x1E87, 0x1E89, 0x1E89, 0x1E8B, 0x1E8B, 0x1E8D, 0x1E8D,
            0x1E8F, 0x1E8F, 0x1E91, 0x1E91, 0x1E93, 0x1E93, 0x1E95, 0x1E9D,
            0x1E9F, 0x1E9F, 0x1EA1, 0x1EA1, 0x1EA3, 0x1EA3, 0x1EA5, 0x1EA5,
            0x1EA7, 0x1EA7, 0x1EA9, 0x1EA9, 0x1EAB, 0x1EAB, 0x1EAD, 0x1EAD,
            0x1EAF, 0x1EAF, 0x1EB1, 0x1EB1, 0x1EB3, 0x1EB3, 0x1EB5, 0x1EB5,
            0x1EB7, 0x1EB7, 0x1EB9, 0x1EB9, 0x1EBB, 0x1EBB, 0x1EBD, 0x1EBD,
            0x1EBF, 0x1EBF, 0x1EC1, 0x1EC1, 0x1EC3, 0x1EC3, 0x1EC5, 0x1EC5,
            0x1EC7, 0x1EC7, 0x1EC9, 0x1EC9, 0x1ECB, 0x1ECB, 0x1ECD, 0x1ECD,
            0x1ECF, 0x1ECF, 0x1ED1, 0x1ED1, 0x1ED3, 0x1ED3, 0x1ED5, 0x1ED5,
            0x1ED7, 0x1ED7, 0x1ED9, 0x1ED9, 0x1EDB, 0x1EDB, 0x1EDD, 0x1EDD,
            0x1EDF, 0x1EDF, 0x1EE1, 0x1EE1, 0x1EE3, 0x1EE3, 0x1EE5, 0x1EE5,
            0x1EE7, 0x1EE7, 0x1EE9, 0x1EE9, 0x1EEB, 0x1EEB, 0x1EED, 0x1EED,
            0x1EEF, 0x1EEF, 0x1EF1, 0x1EF1, 0x1EF3, 0x1EF3, 0x1EF5, 0x1EF5,
            0x1EF7, 0x1EF7, 0x1EF9, 0x1EF9, 0x1EFB, 0x1EFB, 0x1EFD, 0x1EFD,
            0x1EFF, 0x1F07, 0x1F10, 0x1F15, 0x1F20, 0x1F27, 0x1F30, 0x1F37,
            0x1F40, 0x1F45, 0x1F50, 0x1F57, 0x1F60, 0x1F67, 0x1F70, 0x1F7D,
            0x1F80, 0x1F87, 0x1F90, 0x1F97, 0x1FA0, 0x1FA7, 0x1FB0, 0x1FB4,
            0x1FB6, 0x1FB7, 0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FC7,
            0x1FD0, 0x1FD3, 0x1FD6, 0x1FD7, 0x1FE0, 0x1FE7, 0x1FF2, 0x1FF4,
            0x1FF6, 0x1FF7, 0x210A, 0x210A, 0x210E, 0x210F, 0x2113, 0x2113,
            0x212F, 0x212F, 0x2134, 0x2134, 0x2139, 0x2139, 0x213C, 0x213D,
            0x2146, 0x2149, 0x214E, 0x214E, 0x2184, 0x2184, 0x2C30, 0x2C5F,
            0x2C61, 0x2C61, 0x2C65, 0x2C66, 0x2C68, 0x2C68, 0x2C6A, 0x2C6A,
            0x2C6C, 0x2C6C, 0x2C71, 0x2C71, 0x2C73, 0x2C74, 0x2C76, 0x2C7B,
            0x2C81, 0x2C81, 0x2C83, 0x2C83, 0x2C85, 0x2C85, 0x2C87, 0x2C87,
            0x2C89, 0x2C89, 0x2C8B, 0x2C8B, 0x2C8D, 0x2C8D, 0x2C8F, 0x2C8F,
            0x2C91, 0x2C91, 0x2C93, 0x2C93, 0x2C95, 0x2C95, 0x2C97, 0x2C97,
            0x2C99, 0x2C99, 0x2C9B, 0x2C9B, 0x2C9D, 0x2C9D, 0x2C9F, 0x2C9F,
            0x2CA1, 0x2CA1, 0x2CA3, 0x2CA3, 0x2CA5, 0x2CA5, 0x2CA7, 0x2CA7,
            0x2CA9, 0x2CA9, 0x2CAB, 0x2CAB, 0x2CAD, 0x2CAD, 0x2CAF, 0x2CAF,
            0x2CB1, 0x2CB1, 0x2CB3, 0x2CB3, 0x2CB5, 0x2CB5, 0x2CB7, 0x2CB7,
            0x2CB9, 0x2CB9, 0x2CBB, 0x2CBB, 0x2CBD, 0x2CBD, 0x2CBF, 0x2CBF,
            0x2CC1, 0x2CC1, 0x2CC3, 0x2CC3, 0x2CC5, 0x2CC5, 0x2CC7, 0x2CC7,
            0x2CC9, 0x2CC9, 0x2CCB, 0x2CCB, 0x2CCD, 0x2CCD, 0x2CCF, 0x2CCF,
            0x2CD1, 0x2CD1, 0x2CD3, 0x2CD3, 0x2CD5, 0x2CD5, 0x2CD7, 0x2CD7,
            0x2CD9, 0x2CD9, 0x2CDB, 0x2CDB, 0x2CDD, 0x2CDD, 0x2CDF, 0x2CDF,
            0x2CE1, 0x2CE1, 0x2CE3, 0x2CE4, 0x2CEC, 0x2CEC, 0x2CEE, 0x2CEE,
            0x2CF3, 0x2CF3, 0x2D00, 0x2D25, 0x2D27, 0x2D27, 0x2D2D, 0x2D2D,
            0xA641, 0xA641, 0xA643, 0xA643, 0xA645, 0xA645, 0xA647, 0xA647,
            0xA649, 0xA649, 0xA64B, 0xA64B, 0xA64D, 0xA64D, 0xA64F, 0xA64F,
            0xA651, 0xA651, 0xA653, 0xA653, 0xA655, 0xA655, 0xA657, 0xA657,
            0xA659, 0xA659, 0xA65B, 0xA65B, 0xA65D, 0xA65D, 0xA65F, 0xA65F,
            0xA661, 0xA661, 0xA663, 0xA663, 0xA665, 0xA665, 0xA667, 0xA667,
            0xA669, 0xA669, 0xA66B, 0xA66B, 0xA66D, 0xA66D, 0xA681, 0xA681,
            0xA683, 0xA683, 0xA685, 0xA685, 0xA687, 0xA687, 0xA689, 0xA689,
            0xA68B, 0xA68B, 0xA68D, 0xA68D, 0xA68F, 0xA68F, 0xA691, 0xA691,
            0xA693, 0xA693, 0xA695, 0xA695, 0xA697, 0xA697, 0xA699, 0xA699,
            0xA69B, 0xA69B, 0xA723, 0xA723, 0xA725, 0xA725, 0xA727, 0xA727,
            0xA729, 0xA729, 0xA72B, 0xA72B, 0xA72D, 0xA72D, 0xA72F, 0xA731,
            0xA733, 0xA733, 0xA735, 0xA735, 0xA737, 0xA737, 0xA739, 0xA739,
            0xA73B, 0xA73B, 0xA73D, 0xA73D, 0xA73F, 0xA73F, 0xA741, 0xA741,
            0xA743, 0xA743, 0xA745, 0xA745, 0xA747, 0xA747, 0xA749, 0xA749,
            0xA74B, 0xA74B, 0xA74D, 0xA74D, 0xA74F, 0xA74F, 0xA751, 0xA751,
            0xA753, 0xA753, 0xA755, 0xA755, 0xA757, 0xA757, 0xA759, 0xA759,
            0xA75B, 0xA75B, 0xA75D, 0xA75D, 0xA75F, 0xA75F, 0xA761, 0xA761,
            0xA763, 0xA763, 0xA765, 0xA765, 0xA767, 0xA767, 0xA769, 0xA769,
            0xA76B, 0xA76B, 0xA76D, 0xA76D, 0xA76F, 0xA76F, 0xA771, 0xA778,
            0xA77A, 0xA77A, 0xA77C, 0xA77C, 0xA77F, 0xA77F, 0xA781, 0xA781,
            0xA783, 0xA783, 0xA785, 0xA785, 0xA787, 0xA787, 0xA78C, 0xA78C,
            0xA78E, 0xA78E, 0xA791, 0xA791, 0xA793, 0xA795, 0xA797, 0xA797,
            0xA799, 0xA799, 0xA79B, 0xA79B, 0xA79D, 0xA79D, 0xA79F, 0xA79F,
            0xA7A1, 0xA7A1, 0xA7A3, 0xA7A3, 0xA7A5, 0xA7A5, 0xA7A7, 0xA7A7,
            0xA7A9, 0xA7A9, 0xA7AF, 0xA7AF, 0xA7B5, 0xA7B5, 0xA7B7, 0xA7B7,
            0xA7B9, 0xA7B9, 0xA7BB, 0xA7BB, 0xA7BD, 0xA7BD, 0xA7BF, 0xA7BF,
            0xA7C1, 0xA7C1, 0xA7C3, 0xA7C3, 0xA7C8, 0xA7C8, 0xA7CA, 0xA7CA,
            0xA7D1, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D5, 0xA7D7, 0xA7D7,
            0xA7D9, 0xA7D9, 0xA7F6, 0xA7F6, 0xA7FA, 0xA7FA, 0xAB30, 0xAB5A,
            0xAB60, 0xAB68, 0xAB70, 0xABBF, 0xFB00, 0xFB06, 0xFB13, 0xFB17,
            0xFF41, 0xFF5A, 0x10428, 0x1044F, 0x104D8, 0x104FB, 0x10597, 0x105A1,
            0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10CC0, 0x10CF2,
            0x118C0, 0x118DF, 0x16E60, 0x16E7F, 0x1D41A, 0x1D433, 0x1D44E, 0x1D454,
            0x1D456, 0x1D467, 0x1D482, 0x1D49B, 0x1D4B6, 0x1D4B9, 0x1D4BB, 0x1D4BB,
            0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D4CF, 0x1D4EA, 0x1D503, 0x1D51E, 0x1D537,
            0x1D552, 0x1D56B, 0x1D586, 0x1D59F, 0x1D5BA, 0x1D5D3, 0x1D5EE, 0x1D607,
            0x1D622, 0x1D63B, 0x1D656, 0x1D66F, 0x1D68A, 0x1D6A5, 0x1D6C2, 0x1D6DA,
            0x1D6DC, 0x1D6E1, 0x1D6FC, 0x1D714, 0x1D716, 0x1D71B, 0x1D736, 0x1D74E,
            0x1D750, 0x1D755, 0x1D770, 0x1D788, 0x1D78A, 0x1D78F, 0x1D7AA, 0x1D7C2,
            0x1D7C4, 0x1D7C9, 0x1D7CB, 0x1D7CB, 0x1DF00, 0x1DF09, 0x1DF0B, 0x1DF1E,
            0x1E922, 0x1E943,
            //  #10 (1382+10): gc=Titlecase_Letter:Lt
            0x01C5, 0x01C5, 0x01C8, 0x01C8, 0x01CB, 0x01CB, 0x01F2, 0x01F2,
            0x1F88, 0x1F8F, 0x1F98, 0x1F9F, 0x1FA8, 0x1FAF, 0x1FBC, 0x1FBC,
            0x1FCC, 0x1FCC, 0x1FFC, 0x1FFC,
            //  #11 (1392+646): gc=Uppercase_Letter:Lu
            0x0041, 0x005A, 0x00C0, 0x00D6, 0x00D8, 0x00DE, 0x0100, 0x0100,
            0x0102, 0x0102, 0x0104, 0x0104, 0x0106, 0x0106, 0x0108, 0x0108,
            0x010A, 0x010A, 0x010C, 0x010C, 0x010E, 0x010E, 0x0110, 0x0110,
            0x0112, 0x0112, 0x0114, 0x0114, 0x0116, 0x0116, 0x0118, 0x0118,
            0x011A, 0x011A, 0x011C, 0x011C, 0x011E, 0x011E, 0x0120, 0x0120,
            0x0122, 0x0122, 0x0124, 0x0124, 0x0126, 0x0126, 0x0128, 0x0128,
            0x012A, 0x012A, 0x012C, 0x012C, 0x012E, 0x012E, 0x0130, 0x0130,
            0x0132, 0x0132, 0x0134, 0x0134, 0x0136, 0x0136, 0x0139, 0x0139,
            0x013B, 0x013B, 0x013D, 0x013D, 0x013F, 0x013F, 0x0141, 0x0141,
            0x0143, 0x0143, 0x0145, 0x0145, 0x0147, 0x0147, 0x014A, 0x014A,
            0x014C, 0x014C, 0x014E, 0x014E, 0x0150, 0x0150, 0x0152, 0x0152,
            0x0154, 0x0154, 0x0156, 0x0156, 0x0158, 0x0158, 0x015A, 0x015A,
            0x015C, 0x015C, 0x015E, 0x015E, 0x0160, 0x0160, 0x0162, 0x0162,
            0x0164, 0x0164, 0x0166, 0x0166, 0x0168, 0x0168, 0x016A, 0x016A,
            0x016C, 0x016C, 0x016E, 0x016E, 0x0170, 0x0170, 0x0172, 0x0172,
            0x0174, 0x0174, 0x0176, 0x0176, 0x0178, 0x0179, 0x017B, 0x017B,
            0x017D, 0x017D, 0x0181, 0x0182, 0x0184, 0x0184, 0x0186, 0x0187,
            0x0189, 0x018B, 0x018E, 0x0191, 0x0193, 0x0194, 0x0196, 0x0198,
            0x019C, 0x019D, 0x019F, 0x01A0, 0x01A2, 0x01A2, 0x01A4, 0x01A4,
            0x01A6, 0x01A7, 0x01A9, 0x01A9, 0x01AC, 0x01AC, 0x01AE, 0x01AF,
            0x01B1, 0x01B3, 0x01B5, 0x01B5, 0x01B7, 0x01B8, 0x01BC, 0x01BC,
            0x01C4, 0x01C4, 0x01C7, 0x01C7, 0x01CA, 0x01CA, 0x01CD, 0x01CD,
            0x01CF, 0x01CF, 0x01D1, 0x01D1, 0x01D3, 0x01D3, 0x01D5, 0x01D5,
            0x01D7, 0x01D7, 0x01D9, 0x01D9, 0x01DB, 0x01DB, 0x01DE, 0x01DE,
            0x01E0, 0x01E0, 0x01E2, 0x01E2, 0x01E4, 0x01E4, 0x01E6, 0x01E6,
            0x01E8, 0x01E8, 0x01EA, 0x01EA, 0x01EC, 0x01EC, 0x01EE, 0x01EE,
            0x01F1, 0x01F1, 0x01F4, 0x01F4, 0x01F6, 0x01F8, 0x01FA, 0x01FA,
            0x01FC, 0x01FC, 0x01FE, 0x01FE, 0x0200, 0x0200, 0x0202, 0x0202,
            0x0204, 0x0204, 0x0206, 0x0206, 0x0208, 0x0208, 0x020A, 0x020A,
            0x020C, 0x020C, 0x020E, 0x020E, 0x0210, 0x0210, 0x0212, 0x0212,
            0x0214, 0x0214, 0x0216, 0x0216, 0x0218, 0x0218, 0x021A, 0x021A,
            0x021C, 0x021C, 0x021E, 0x021E, 0x0220, 0x0220, 0x0222, 0x0222,
            0x0224, 0x0224, 0x0226, 0x0226, 0x0228, 0x0228, 0x022A, 0x022A,
            0x022C, 0x022C, 0x022E, 0x022E, 0x0230, 0x0230, 0x0232, 0x0232,
            0x023A, 0x023B, 0x023D, 0x023E, 0x0241, 0x0241, 0x0243, 0x0246,
            0x0248, 0x0248, 0x024A, 0x024A, 0x024C, 0x024C, 0x024E, 0x024E,
            0x0370, 0x0370, 0x0372, 0x0372, 0x0376, 0x0376, 0x037F, 0x037F,
            0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x038F,
            0x0391, 0x03A1, 0x03A3, 0x03AB, 0x03CF, 0x03CF, 0x03D2, 0x03D4,
            0x03D8, 0x03D8, 0x03DA, 0x03DA, 0x03DC, 0x03DC, 0x03DE, 0x03DE,
            0x03E0, 0x03E0, 0x03E2, 0x03E2, 0x03E4, 0x03E4, 0x03E6, 0x03E6,
            0x03E8, 0x03E8, 0x03EA, 0x03EA, 0x03EC, 0x03EC, 0x03EE, 0x03EE,
            0x03F4, 0x03F4, 0x03F7, 0x03F7, 0x03F9, 0x03FA, 0x03FD, 0x042F,
            0x0460, 0x0460, 0x0462, 0x0462, 0x0464, 0x0464, 0x0466, 0x0466,
            0x0468, 0x0468, 0x046A, 0x046A, 0x046C, 0x046C, 0x046E, 0x046E,
            0x0470, 0x0470, 0x0472, 0x0472, 0x0474, 0x0474, 0x0476, 0x0476,
            0x0478, 0x0478, 0x047A, 0x047A, 0x047C, 0x047C, 0x047E, 0x047E,
            0x0480, 0x0480, 0x048A, 0x048A, 0x048C, 0x048C, 0x048E, 0x048E,
            0x0490, 0x0490, 0x0492, 0x0492, 0x0494, 0x0494, 0x0496, 0x0496,
            0x0498, 0x0498, 0x049A, 0x049A, 0x049C, 0x049C, 0x049E, 0x049E,
            0x04A0, 0x04A0, 0x04A2, 0x04A2, 0x04A4, 0x04A4, 0x04A6, 0x04A6,
            0x04A8, 0x04A8, 0x04AA, 0x04AA, 0x04AC, 0x04AC, 0x04AE, 0x04AE,
            0x04B0, 0x04B0, 0x04B2, 0x04B2, 0x04B4, 0x04B4, 0x04B6, 0x04B6,
            0x04B8, 0x04B8, 0x04BA, 0x04BA, 0x04BC, 0x04BC, 0x04BE, 0x04BE,
            0x04C0, 0x04C1, 0x04C3, 0x04C3, 0x04C5, 0x04C5, 0x04C7, 0x04C7,
            0x04C9, 0x04C9, 0x04CB, 0x04CB, 0x04CD, 0x04CD, 0x04D0, 0x04D0,
            0x04D2, 0x04D2, 0x04D4, 0x04D4, 0x04D6, 0x04D6, 0x04D8, 0x04D8,
            0x04DA, 0x04DA, 0x04DC, 0x04DC, 0x04DE, 0x04DE, 0x04E0, 0x04E0,
            0x04E2, 0x04E2, 0x04E4, 0x04E4, 0x04E6, 0x04E6, 0x04E8, 0x04E8,
            0x04EA, 0x04EA, 0x04EC, 0x04EC, 0x04EE, 0x04EE, 0x04F0, 0x04F0,
            0x04F2, 0x04F2, 0x04F4, 0x04F4, 0x04F6, 0x04F6, 0x04F8, 0x04F8,
            0x04FA, 0x04FA, 0x04FC, 0x04FC, 0x04FE, 0x04FE, 0x0500, 0x0500,
            0x0502, 0x0502, 0x0504, 0x0504, 0x0506, 0x0506, 0x0508, 0x0508,
            0x050A, 0x050A, 0x050C, 0x050C, 0x050E, 0x050E, 0x0510, 0x0510,
            0x0512, 0x0512, 0x0514, 0x0514, 0x0516, 0x0516, 0x0518, 0x0518,
            0x051A, 0x051A, 0x051C, 0x051C, 0x051E, 0x051E, 0x0520, 0x0520,
            0x0522, 0x0522, 0x0524, 0x0524, 0x0526, 0x0526, 0x0528, 0x0528,
            0x052A, 0x052A, 0x052C, 0x052C, 0x052E, 0x052E, 0x0531, 0x0556,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x13A0, 0x13F5,
            0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1E00, 0x1E00, 0x1E02, 0x1E02,
            0x1E04, 0x1E04, 0x1E06, 0x1E06, 0x1E08, 0x1E08, 0x1E0A, 0x1E0A,
            0x1E0C, 0x1E0C, 0x1E0E, 0x1E0E, 0x1E10, 0x1E10, 0x1E12, 0x1E12,
            0x1E14, 0x1E14, 0x1E16, 0x1E16, 0x1E18, 0x1E18, 0x1E1A, 0x1E1A,
            0x1E1C, 0x1E1C, 0x1E1E, 0x1E1E, 0x1E20, 0x1E20, 0x1E22, 0x1E22,
            0x1E24, 0x1E24, 0x1E26, 0x1E26, 0x1E28, 0x1E28, 0x1E2A, 0x1E2A,
            0x1E2C, 0x1E2C, 0x1E2E, 0x1E2E, 0x1E30, 0x1E30, 0x1E32, 0x1E32,
            0x1E34, 0x1E34, 0x1E36, 0x1E36, 0x1E38, 0x1E38, 0x1E3A, 0x1E3A,
            0x1E3C, 0x1E3C, 0x1E3E, 0x1E3E, 0x1E40, 0x1E40, 0x1E42, 0x1E42,
            0x1E44, 0x1E44, 0x1E46, 0x1E46, 0x1E48, 0x1E48, 0x1E4A, 0x1E4A,
            0x1E4C, 0x1E4C, 0x1E4E, 0x1E4E, 0x1E50, 0x1E50, 0x1E52, 0x1E52,
            0x1E54, 0x1E54, 0x1E56, 0x1E56, 0x1E58, 0x1E58, 0x1E5A, 0x1E5A,
            0x1E5C, 0x1E5C, 0x1E5E, 0x1E5E, 0x1E60, 0x1E60, 0x1E62, 0x1E62,
            0x1E64, 0x1E64, 0x1E66, 0x1E66, 0x1E68, 0x1E68, 0x1E6A, 0x1E6A,
            0x1E6C, 0x1E6C, 0x1E6E, 0x1E6E, 0x1E70, 0x1E70, 0x1E72, 0x1E72,
            0x1E74, 0x1E74, 0x1E76, 0x1E76, 0x1E78, 0x1E78, 0x1E7A, 0x1E7A,
            0x1E7C, 0x1E7C, 0x1E7E, 0x1E7E, 0x1E80, 0x1E80, 0x1E82, 0x1E82,
            0x1E84, 0x1E84, 0x1E86, 0x1E86, 0x1E88, 0x1E88, 0x1E8A, 0x1E8A,
            0x1E8C, 0x1E8C, 0x1E8E, 0x1E8E, 0x1E90, 0x1E90, 0x1E92, 0x1E92,
            0x1E94, 0x1E94, 0x1E9E, 0x1E9E, 0x1EA0, 0x1EA0, 0x1EA2, 0x1EA2,
            0x1EA4, 0x1EA4, 0x1EA6, 0x1EA6, 0x1EA8, 0x1EA8, 0x1EAA, 0x1EAA,
            0x1EAC, 0x1EAC, 0x1EAE, 0x1EAE, 0x1EB0, 0x1EB0, 0x1EB2, 0x1EB2,
            0x1EB4, 0x1EB4, 0x1EB6, 0x1EB6, 0x1EB8, 0x1EB8, 0x1EBA, 0x1EBA,
            0x1EBC, 0x1EBC, 0x1EBE, 0x1EBE, 0x1EC0, 0x1EC0, 0x1EC2, 0x1EC2,
            0x1EC4, 0x1EC4, 0x1EC6, 0x1EC6, 0x1EC8, 0x1EC8, 0x1ECA, 0x1ECA,
            0x1ECC, 0x1ECC, 0x1ECE, 0x1ECE, 0x1ED0, 0x1ED0, 0x1ED2, 0x1ED2,
            0x1ED4, 0x1ED4, 0x1ED6, 0x1ED6, 0x1ED8, 0x1ED8, 0x1EDA, 0x1EDA,
            0x1EDC, 0x1EDC, 0x1EDE, 0x1EDE, 0x1EE0, 0x1EE0, 0x1EE2, 0x1EE2,
            0x1EE4, 0x1EE4, 0x1EE6, 0x1EE6, 0x1EE8, 0x1EE8, 0x1EEA, 0x1EEA,
            0x1EEC, 0x1EEC, 0x1EEE, 0x1EEE, 0x1EF0, 0x1EF0, 0x1EF2, 0x1EF2,
            0x1EF4, 0x1EF4, 0x1EF6, 0x1EF6, 0x1EF8, 0x1EF8, 0x1EFA, 0x1EFA,
            0x1EFC, 0x1EFC, 0x1EFE, 0x1EFE, 0x1F08, 0x1F0F, 0x1F18, 0x1F1D,
            0x1F28, 0x1F2F, 0x1F38, 0x1F3F, 0x1F48, 0x1F4D, 0x1F59, 0x1F59,
            0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F5F, 0x1F68, 0x1F6F,
            0x1FB8, 0x1FBB, 0x1FC8, 0x1FCB, 0x1FD8, 0x1FDB, 0x1FE8, 0x1FEC,
            0x1FF8, 0x1FFB, 0x2102, 0x2102, 0x2107, 0x2107, 0x210B, 0x210D,
            0x2110, 0x2112, 0x2115, 0x2115, 0x2119, 0x211D, 0x2124, 0x2124,
            0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x212D, 0x2130, 0x2133,
            0x213E, 0x213F, 0x2145, 0x2145, 0x2183, 0x2183, 0x2C00, 0x2C2F,
            0x2C60, 0x2C60, 0x2C62, 0x2C64, 0x2C67, 0x2C67, 0x2C69, 0x2C69,
            0x2C6B, 0x2C6B, 0x2C6D, 0x2C70, 0x2C72, 0x2C72, 0x2C75, 0x2C75,
            0x2C7E, 0x2C80, 0x2C82, 0x2C82, 0x2C84, 0x2C84, 0x2C86, 0x2C86,
            0x2C88, 0x2C88, 0x2C8A, 0x2C8A, 0x2C8C, 0x2C8C, 0x2C8E, 0x2C8E,
            0x2C90, 0x2C90, 0x2C92, 0x2C92, 0x2C94, 0x2C94, 0x2C96, 0x2C96,
            0x2C98, 0x2C98, 0x2C9A, 0x2C9A, 0x2C9C, 0x2C9C, 0x2C9E, 0x2C9E,
            0x2CA0, 0x2CA0, 0x2CA2, 0x2CA2, 0x2CA4, 0x2CA4, 0x2CA6, 0x2CA6,
            0x2CA8, 0x2CA8, 0x2CAA, 0x2CAA, 0x2CAC, 0x2CAC, 0x2CAE, 0x2CAE,
            0x2CB0, 0x2CB0, 0x2CB2, 0x2CB2, 0x2CB4, 0x2CB4, 0x2CB6, 0x2CB6,
            0x2CB8, 0x2CB8, 0x2CBA, 0x2CBA, 0x2CBC, 0x2CBC, 0x2CBE, 0x2CBE,
            0x2CC0, 0x2CC0, 0x2CC2, 0x2CC2, 0x2CC4, 0x2CC4, 0x2CC6, 0x2CC6,
            0x2CC8, 0x2CC8, 0x2CCA, 0x2CCA, 0x2CCC, 0x2CCC, 0x2CCE, 0x2CCE,
            0x2CD0, 0x2CD0, 0x2CD2, 0x2CD2, 0x2CD4, 0x2CD4, 0x2CD6, 0x2CD6,
            0x2CD8, 0x2CD8, 0x2CDA, 0x2CDA, 0x2CDC, 0x2CDC, 0x2CDE, 0x2CDE,
            0x2CE0, 0x2CE0, 0x2CE2, 0x2CE2, 0x2CEB, 0x2CEB, 0x2CED, 0x2CED,
            0x2CF2, 0x2CF2, 0xA640, 0xA640, 0xA642, 0xA642, 0xA644, 0xA644,
            0xA646, 0xA646, 0xA648, 0xA648, 0xA64A, 0xA64A, 0xA64C, 0xA64C,
            0xA64E, 0xA64E, 0xA650, 0xA650, 0xA652, 0xA652, 0xA654, 0xA654,
            0xA656, 0xA656, 0xA658, 0xA658, 0xA65A, 0xA65A, 0xA65C, 0xA65C,
            0xA65E, 0xA65E, 0xA660, 0xA660, 0xA662, 0xA662, 0xA664, 0xA664,
            0xA666, 0xA666, 0xA668, 0xA668, 0xA66A, 0xA66A, 0xA66C, 0xA66C,
            0xA680, 0xA680, 0xA682, 0xA682, 0xA684, 0xA684, 0xA686, 0xA686,
            0xA688, 0xA688, 0xA68A, 0xA68A, 0xA68C, 0xA68C, 0xA68E, 0xA68E,
            0xA690, 0xA690, 0xA692, 0xA692, 0xA694, 0xA694, 0xA696, 0xA696,
            0xA698, 0xA698, 0xA69A, 0xA69A, 0xA722, 0xA722, 0xA724, 0xA724,
            0xA726, 0xA726, 0xA728, 0xA728, 0xA72A, 0xA72A, 0xA72C, 0xA72C,
            0xA72E, 0xA72E, 0xA732, 0xA732, 0xA734, 0xA734, 0xA736, 0xA736,
            0xA738, 0xA738, 0xA73A, 0xA73A, 0xA73C, 0xA73C, 0xA73E, 0xA73E,
            0xA740, 0xA740, 0xA742, 0xA742, 0xA744, 0xA744, 0xA746, 0xA746,
            0xA748, 0xA748, 0xA74A, 0xA74A, 0xA74C, 0xA74C, 0xA74E, 0xA74E,
            0xA750, 0xA750, 0xA752, 0xA752, 0xA754, 0xA754, 0xA756, 0xA756,
            0xA758, 0xA758, 0xA75A, 0xA75A, 0xA75C, 0xA75C, 0xA75E, 0xA75E,
            0xA760, 0xA760, 0xA762, 0xA762, 0xA764, 0xA764, 0xA766, 0xA766,
            0xA768, 0xA768, 0xA76A, 0xA76A, 0xA76C, 0xA76C, 0xA76E, 0xA76E,
            0xA779, 0xA779, 0xA77B, 0xA77B, 0xA77D, 0xA77E, 0xA780, 0xA780,
            0xA782, 0xA782, 0xA784, 0xA784, 0xA786, 0xA786, 0xA78B, 0xA78B,
            0xA78D, 0xA78D, 0xA790, 0xA790, 0xA792, 0xA792, 0xA796, 0xA796,
            0xA798, 0xA798, 0xA79A, 0xA79A, 0xA79C, 0xA79C, 0xA79E, 0xA79E,
            0xA7A0, 0xA7A0, 0xA7A2, 0xA7A2, 0xA7A4, 0xA7A4, 0xA7A6, 0xA7A6,
            0xA7A8, 0xA7A8, 0xA7AA, 0xA7AE, 0xA7B0, 0xA7B4, 0xA7B6, 0xA7B6,
            0xA7B8, 0xA7B8, 0xA7BA, 0xA7BA, 0xA7BC, 0xA7BC, 0xA7BE, 0xA7BE,
            0xA7C0, 0xA7C0, 0xA7C2, 0xA7C2, 0xA7C4, 0xA7C7, 0xA7C9, 0xA7C9,
            0xA7D0, 0xA7D0, 0xA7D6, 0xA7D6, 0xA7D8, 0xA7D8, 0xA7F5, 0xA7F5,
            0xFF21, 0xFF3A, 0x10400, 0x10427, 0x104B0, 0x104D3, 0x10570, 0x1057A,
            0x1057C, 0x1058A, 0x1058C, 0x10592, 0x10594, 0x10595, 0x10C80, 0x10CB2,
            0x118A0, 0x118BF, 0x16E40, 0x16E5F, 0x1D400, 0x1D419, 0x1D434, 0x1D44D,
            0x1D468, 0x1D481, 0x1D49C, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2,
            0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B5, 0x1D4D0, 0x1D4E9,
            0x1D504, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514, 0x1D516, 0x1D51C,
            0x1D538, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544, 0x1D546, 0x1D546,
            0x1D54A, 0x1D550, 0x1D56C, 0x1D585, 0x1D5A0, 0x1D5B9, 0x1D5D4, 0x1D5ED,
            0x1D608, 0x1D621, 0x1D63C, 0x1D655, 0x1D670, 0x1D689, 0x1D6A8, 0x1D6C0,
            0x1D6E2, 0x1D6FA, 0x1D71C, 0x1D734, 0x1D756, 0x1D76E, 0x1D790, 0x1D7A8,
            0x1D7CA, 0x1D7CA, 0x1E900, 0x1E921,
            //  #12 (2038+69): gc=Modifier_Letter:Lm
            0x02B0, 0x02C1, 0x02C6, 0x02D1, 0x02E0, 0x02E4, 0x02EC, 0x02EC,
            0x02EE, 0x02EE, 0x0374, 0x0374, 0x037A, 0x037A, 0x0559, 0x0559,
            0x0640, 0x0640, 0x06E5, 0x06E6, 0x07F4, 0x07F5, 0x07FA, 0x07FA,
            0x081A, 0x081A, 0x0824, 0x0824, 0x0828, 0x0828, 0x08C9, 0x08C9,
            0x0971, 0x0971, 0x0E46, 0x0E46, 0x0EC6, 0x0EC6, 0x10FC, 0x10FC,
            0x17D7, 0x17D7, 0x1843, 0x1843, 0x1AA7, 0x1AA7, 0x1C78, 0x1C7D,
            0x1D2C, 0x1D6A, 0x1D78, 0x1D78, 0x1D9B, 0x1DBF, 0x2071, 0x2071,
            0x207F, 0x207F, 0x2090, 0x209C, 0x2C7C, 0x2C7D, 0x2D6F, 0x2D6F,
            0x2E2F, 0x2E2F, 0x3005, 0x3005, 0x3031, 0x3035, 0x303B, 0x303B,
            0x309D, 0x309E, 0x30FC, 0x30FE, 0xA015, 0xA015, 0xA4F8, 0xA4FD,
            0xA60C, 0xA60C, 0xA67F, 0xA67F, 0xA69C, 0xA69D, 0xA717, 0xA71F,
            0xA770, 0xA770, 0xA788, 0xA788, 0xA7F2, 0xA7F4, 0xA7F8, 0xA7F9,
            0xA9CF, 0xA9CF, 0xA9E6, 0xA9E6, 0xAA70, 0xAA70, 0xAADD, 0xAADD,
            0xAAF3, 0xAAF4, 0xAB5C, 0xAB5F, 0xAB69, 0xAB69, 0xFF70, 0xFF70,
            0xFF9E, 0xFF9F, 0x10780, 0x10785, 0x10787, 0x107B0, 0x107B2, 0x107BA,
            0x16B40, 0x16B43, 0x16F93, 0x16F9F, 0x16FE0, 0x16FE1, 0x16FE3, 0x16FE3,
            0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE, 0x1E137, 0x1E13D,
            0x1E94B, 0x1E94B,
            //  #13 (2107+501): gc=Other_Letter:Lo
            0x00AA, 0x00AA, 0x00BA, 0x00BA, 0x01BB, 0x01BB, 0x01C0, 0x01C3,
            0x0294, 0x0294, 0x05D0, 0x05EA, 0x05EF, 0x05F2, 0x0620, 0x063F,
            0x0641, 0x064A, 0x066E, 0x066F, 0x0671, 0x06D3, 0x06D5, 0x06D5,
            0x06EE, 0x06EF, 0x06FA, 0x06FC, 0x06FF, 0x06FF, 0x0710, 0x0710,
            0x0712, 0x072F, 0x074D, 0x07A5, 0x07B1, 0x07B1, 0x07CA, 0x07EA,
            0x0800, 0x0815, 0x0840, 0x0858, 0x0860, 0x086A, 0x0870, 0x0887,
            0x0889, 0x088E, 0x08A0, 0x08C8, 0x0904, 0x0939, 0x093D, 0x093D,
            0x0950, 0x0950, 0x0958, 0x0961, 0x0972, 0x0980, 0x0985, 0x098C,
            0x098F, 0x0990, 0x0993, 0x09A8, 0x09AA, 0x09B0, 0x09B2, 0x09B2,
            0x09B6, 0x09B9, 0x09BD, 0x09BD, 0x09CE, 0x09CE, 0x09DC, 0x09DD,
            0x09DF, 0x09E1, 0x09F0, 0x09F1, 0x09FC, 0x09FC, 0x0A05, 0x0A0A,
            0x0A0F, 0x0A10, 0x0A13, 0x0A28, 0x0A2A, 0x0A30, 0x0A32, 0x0A33,
            0x0A35, 0x0A36, 0x0A38, 0x0A39, 0x0A59, 0x0A5C, 0x0A5E, 0x0A5E,
            0x0A72, 0x0A74, 0x0A85, 0x0A8D, 0x0A8F, 0x0A91, 0x0A93, 0x0AA8,
            0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3, 0x0AB5, 0x0AB9, 0x0ABD, 0x0ABD,
            0x0AD0, 0x0AD0, 0x0AE0, 0x0AE1, 0x0AF9, 0x0AF9, 0x0B05, 0x0B0C,
            0x0B0F, 0x0B10, 0x0B13, 0x0B28, 0x0B2A, 0x0B30, 0x0B32, 0x0B33,
            0x0B35, 0x0B39, 0x0B3D, 0x0B3D, 0x0B5C, 0x0B5D, 0x0B5F, 0x0B61,
            0x0B71, 0x0B71, 0x0B83, 0x0B83, 0x0B85, 0x0B8A, 0x0B8E, 0x0B90,
            0x0B92, 0x0B95, 0x0B99, 0x0B9A, 0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F,
            0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA, 0x0BAE, 0x0BB9, 0x0BD0, 0x0BD0,
            0x0C05, 0x0C0C, 0x0C0E, 0x0C10, 0x0C12, 0x0C28, 0x0C2A, 0x0C39,
            0x0C3D, 0x0C3D, 0x0C58, 0x0C5A, 0x0C5D, 0x0C5D, 0x0C60, 0x0C61,
            0x0C80, 0x0C80, 0x0C85, 0x0C8C, 0x0C8E, 0x0C90, 0x0C92, 0x0CA8,
            0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9, 0x0CBD, 0x0CBD, 0x0CDD, 0x0CDE,
            0x0CE0, 0x0CE1, 0x0CF1, 0x0CF2, 0x0D04, 0x0D0C, 0x0D0E, 0x0D10,
            0x0D12, 0x0D3A, 0x0D3D, 0x0D3D, 0x0D4E, 0x0D4E, 0x0D54, 0x0D56,
            0x0D5F, 0x0D61, 0x0D7A, 0x0D7F, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1,
            0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0E01, 0x0E30,
            0x0E32, 0x0E33, 0x0E40, 0x0E45, 0x0E81, 0x0E82, 0x0E84, 0x0E84,
            0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5, 0x0EA7, 0x0EB0,
            0x0EB2, 0x0EB3, 0x0EBD, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EDC, 0x0EDF,
            0x0F00, 0x0F00, 0x0F40, 0x0F47, 0x0F49, 0x0F6C, 0x0F88, 0x0F8C,
            0x1000, 0x102A, 0x103F, 0x103F, 0x1050, 0x1055, 0x105A, 0x105D,
            0x1061, 0x1061, 0x1065, 0x1066, 0x106E, 0x1070, 0x1075, 0x1081,
            0x108E, 0x108E, 0x1100, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256,
            0x1258, 0x1258, 0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D,
            0x1290, 0x12B0, 0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0,
            0x12C2, 0x12C5, 0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315,
            0x1318, 0x135A, 0x1380, 0x138F, 0x1401, 0x166C, 0x166F, 0x167F,
            0x1681, 0x169A, 0x16A0, 0x16EA, 0x16F1, 0x16F8, 0x1700, 0x1711,
            0x171F, 0x1731, 0x1740, 0x1751, 0x1760, 0x176C, 0x176E, 0x1770,
            0x1780, 0x17B3, 0x17DC, 0x17DC, 0x1820, 0x1842, 0x1844, 0x1878,
            0x1880, 0x1884, 0x1887, 0x18A8, 0x18AA, 0x18AA, 0x18B0, 0x18F5,
            0x1900, 0x191E, 0x1950, 0x196D, 0x1970, 0x1974, 0x1980, 0x19AB,
            0x19B0, 0x19C9, 0x1A00, 0x1A16, 0x1A20, 0x1A54, 0x1B05, 0x1B33,
            0x1B45, 0x1B4C, 0x1B83, 0x1BA0, 0x1BAE, 0x1BAF, 0x1BBA, 0x1BE5,
            0x1C00, 0x1C23, 0x1C4D, 0x1C4F, 0x1C5A, 0x1C77, 0x1CE9, 0x1CEC,
            0x1CEE, 0x1CF3, 0x1CF5, 0x1CF6, 0x1CFA, 0x1CFA, 0x2135, 0x2138,
            0x2D30, 0x2D67, 0x2D80, 0x2D96, 0x2DA0, 0x2DA6, 0x2DA8, 0x2DAE,
            0x2DB0, 0x2DB6, 0x2DB8, 0x2DBE, 0x2DC0, 0x2DC6, 0x2DC8, 0x2DCE,
            0x2DD0, 0x2DD6, 0x2DD8, 0x2DDE, 0x3006, 0x3006, 0x303C, 0x303C,
            0x3041, 0x3096, 0x309F, 0x309F, 0x30A1, 0x30FA, 0x30FF, 0x30FF,
            0x3105, 0x312F, 0x3131, 0x318E, 0x31A0, 0x31BF, 0x31F0, 0x31FF,
            0x3400, 0x4DBF, 0x4E00, 0xA014, 0xA016, 0xA48C, 0xA4D0, 0xA4F7,
            0xA500, 0xA60B, 0xA610, 0xA61F, 0xA62A, 0xA62B, 0xA66E, 0xA66E,
            0xA6A0, 0xA6E5, 0xA78F, 0xA78F, 0xA7F7, 0xA7F7, 0xA7FB, 0xA801,
            0xA803, 0xA805, 0xA807, 0xA80A, 0xA80C, 0xA822, 0xA840, 0xA873,
            0xA882, 0xA8B3, 0xA8F2, 0xA8F7, 0xA8FB, 0xA8FB, 0xA8FD, 0xA8FE,
            0xA90A, 0xA925, 0xA930, 0xA946, 0xA960, 0xA97C, 0xA984, 0xA9B2,
            0xA9E0, 0xA9E4, 0xA9E7, 0xA9EF, 0xA9FA, 0xA9FE, 0xAA00, 0xAA28,
            0xAA40, 0xAA42, 0xAA44, 0xAA4B, 0xAA60, 0xAA6F, 0xAA71, 0xAA76,
            0xAA7A, 0xAA7A, 0xAA7E, 0xAAAF, 0xAAB1, 0xAAB1, 0xAAB5, 0xAAB6,
            0xAAB9, 0xAABD, 0xAAC0, 0xAAC0, 0xAAC2, 0xAAC2, 0xAADB, 0xAADC,
            0xAAE0, 0xAAEA, 0xAAF2, 0xAAF2, 0xAB01, 0xAB06, 0xAB09, 0xAB0E,
            0xAB11, 0xAB16, 0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xABC0, 0xABE2,
            0xAC00, 0xD7A3, 0xD7B0, 0xD7C6, 0xD7CB, 0xD7FB, 0xF900, 0xFA6D,
            0xFA70, 0xFAD9, 0xFB1D, 0xFB1D, 0xFB1F, 0xFB28, 0xFB2A, 0xFB36,
            0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41, 0xFB43, 0xFB44,
            0xFB46, 0xFBB1, 0xFBD3, 0xFD3D, 0xFD50, 0xFD8F, 0xFD92, 0xFDC7,
            0xFDF0, 0xFDFB, 0xFE70, 0xFE74, 0xFE76, 0xFEFC, 0xFF66, 0xFF6F,
            0xFF71, 0xFF9D, 0xFFA0, 0xFFBE, 0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF,
            0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC, 0x10000, 0x1000B, 0x1000D, 0x10026,
            0x10028, 0x1003A, 0x1003C, 0x1003D, 0x1003F, 0x1004D, 0x10050, 0x1005D,
            0x10080, 0x100FA, 0x10280, 0x1029C, 0x102A0, 0x102D0, 0x10300, 0x1031F,
            0x1032D, 0x10340, 0x10342, 0x10349, 0x10350, 0x10375, 0x10380, 0x1039D,
            0x103A0, 0x103C3, 0x103C8, 0x103CF, 0x10450, 0x1049D, 0x10500, 0x10527,
            0x10530, 0x10563, 0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767,
            0x10800, 0x10805, 0x10808, 0x10808, 0x1080A, 0x10835, 0x10837, 0x10838,
            0x1083C, 0x1083C, 0x1083F, 0x10855, 0x10860, 0x10876, 0x10880, 0x1089E,
            0x108E0, 0x108F2, 0x108F4, 0x108F5, 0x10900, 0x10915, 0x10920, 0x10939,
            0x10980, 0x109B7, 0x109BE, 0x109BF, 0x10A00, 0x10A00, 0x10A10, 0x10A13,
            0x10A15, 0x10A17, 0x10A19, 0x10A35, 0x10A60, 0x10A7C, 0x10A80, 0x10A9C,
            0x10AC0, 0x10AC7, 0x10AC9, 0x10AE4, 0x10B00, 0x10B35, 0x10B40, 0x10B55,
            0x10B60, 0x10B72, 0x10B80, 0x10B91, 0x10C00, 0x10C48, 0x10D00, 0x10D23,
            0x10E80, 0x10EA9, 0x10EB0, 0x10EB1, 0x10F00, 0x10F1C, 0x10F27, 0x10F27,
            0x10F30, 0x10F45, 0x10F70, 0x10F81, 0x10FB0, 0x10FC4, 0x10FE0, 0x10FF6,
            0x11003, 0x11037, 0x11071, 0x11072, 0x11075, 0x11075, 0x11083, 0x110AF,
            0x110D0, 0x110E8, 0x11103, 0x11126, 0x11144, 0x11144, 0x11147, 0x11147,
            0x11150, 0x11172, 0x11176, 0x11176, 0x11183, 0x111B2, 0x111C1, 0x111C4,
            0x111DA, 0x111DA, 0x111DC, 0x111DC, 0x11200, 0x11211, 0x11213, 0x1122B,
            0x11280, 0x11286, 0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D,
            0x1129F, 0x112A8, 0x112B0, 0x112DE, 0x11305, 0x1130C, 0x1130F, 0x11310,
            0x11313, 0x11328, 0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339,
            0x1133D, 0x1133D, 0x11350, 0x11350, 0x1135D, 0x11361, 0x11400, 0x11434,
            0x11447, 0x1144A, 0x1145F, 0x11461, 0x11480, 0x114AF, 0x114C4, 0x114C5,
            0x114C7, 0x114C7, 0x11580, 0x115AE, 0x115D8, 0x115DB, 0x11600, 0x1162F,
            0x11644, 0x11644, 0x11680, 0x116AA, 0x116B8, 0x116B8, 0x11700, 0x1171A,
            0x11740, 0x11746, 0x11800, 0x1182B, 0x118FF, 0x11906, 0x11909, 0x11909,
            0x1190C, 0x11913, 0x11915, 0x11916, 0x11918, 0x1192F, 0x1193F, 0x1193F,
            0x11941, 0x11941, 0x119A0, 0x119A7, 0x119AA, 0x119D0, 0x119E1, 0x119E1,
            0x119E3, 0x119E3, 0x11A00, 0x11A00, 0x11A0B, 0x11A32, 0x11A3A, 0x11A3A,
            0x11A50, 0x11A50, 0x11A5C, 0x11A89, 0x11A9D, 0x11A9D, 0x11AB0, 0x11AF8,
            0x11C00, 0x11C08, 0x11C0A, 0x11C2E, 0x11C40, 0x11C40, 0x11C72, 0x11C8F,
            0x11D00, 0x11D06, 0x11D08, 0x11D09, 0x11D0B, 0x11D30, 0x11D46, 0x11D46,
            0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D89, 0x11D98, 0x11D98,
            0x11EE0, 0x11EF2, 0x11FB0, 0x11FB0, 0x12000, 0x12399, 0x12480, 0x12543,
            0x12F90, 0x12FF0, 0x13000, 0x1342E, 0x14400, 0x14646, 0x16800, 0x16A38,
            0x16A40, 0x16A5E, 0x16A70, 0x16ABE, 0x16AD0, 0x16AED, 0x16B00, 0x16B2F,
            0x16B63, 0x16B77, 0x16B7D, 0x16B8F, 0x16F00, 0x16F4A, 0x16F50, 0x16F50,
            0x17000, 0x187F7, 0x18800, 0x18CD5, 0x18D00, 0x18D08, 0x1B000, 0x1B122,
            0x1B150, 0x1B152, 0x1B164, 0x1B167, 0x1B170, 0x1B2FB, 0x1BC00, 0x1BC6A,
            0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99, 0x1DF0A, 0x1DF0A,
            0x1E100, 0x1E12C, 0x1E14E, 0x1E14E, 0x1E290, 0x1E2AD, 0x1E2C0, 0x1E2EB,
            0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE, 0x1E7F0, 0x1E7FE,
            0x1E800, 0x1E8C4, 0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22,
            0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37,
            0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47,
            0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52,
            0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B,
            0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64,
            0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C,
            0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3,
            0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB, 0x20000, 0x2A6DF, 0x2A700, 0x2B738,
            0x2B740, 0x2B81D, 0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D,
            0x30000, 0x3134A,
            //  #14 (2608+518): gc=Mark:M:Combining_Mark
            //  Mc:177 + Me:5 + Mn:336
            //  #15 (2608+177): gc=Spacing_Mark:Mc
            0x0903, 0x0903, 0x093B, 0x093B, 0x093E, 0x0940, 0x0949, 0x094C,
            0x094E, 0x094F, 0x0982, 0x0983, 0x09BE, 0x09C0, 0x09C7, 0x09C8,
            0x09CB, 0x09CC, 0x09D7, 0x09D7, 0x0A03, 0x0A03, 0x0A3E, 0x0A40,
            0x0A83, 0x0A83, 0x0ABE, 0x0AC0, 0x0AC9, 0x0AC9, 0x0ACB, 0x0ACC,
            0x0B02, 0x0B03, 0x0B3E, 0x0B3E, 0x0B40, 0x0B40, 0x0B47, 0x0B48,
            0x0B4B, 0x0B4C, 0x0B57, 0x0B57, 0x0BBE, 0x0BBF, 0x0BC1, 0x0BC2,
            0x0BC6, 0x0BC8, 0x0BCA, 0x0BCC, 0x0BD7, 0x0BD7, 0x0C01, 0x0C03,
            0x0C41, 0x0C44, 0x0C82, 0x0C83, 0x0CBE, 0x0CBE, 0x0CC0, 0x0CC4,
            0x0CC7, 0x0CC8, 0x0CCA, 0x0CCB, 0x0CD5, 0x0CD6, 0x0D02, 0x0D03,
            0x0D3E, 0x0D40, 0x0D46, 0x0D48, 0x0D4A, 0x0D4C, 0x0D57, 0x0D57,
            0x0D82, 0x0D83, 0x0DCF, 0x0DD1, 0x0DD8, 0x0DDF, 0x0DF2, 0x0DF3,
            0x0F3E, 0x0F3F, 0x0F7F, 0x0F7F, 0x102B, 0x102C, 0x1031, 0x1031,
            0x1038, 0x1038, 0x103B, 0x103C, 0x1056, 0x1057, 0x1062, 0x1064,
            0x1067, 0x106D, 0x1083, 0x1084, 0x1087, 0x108C, 0x108F, 0x108F,
            0x109A, 0x109C, 0x1715, 0x1715, 0x1734, 0x1734, 0x17B6, 0x17B6,
            0x17BE, 0x17C5, 0x17C7, 0x17C8, 0x1923, 0x1926, 0x1929, 0x192B,
            0x1930, 0x1931, 0x1933, 0x1938, 0x1A19, 0x1A1A, 0x1A55, 0x1A55,
            0x1A57, 0x1A57, 0x1A61, 0x1A61, 0x1A63, 0x1A64, 0x1A6D, 0x1A72,
            0x1B04, 0x1B04, 0x1B35, 0x1B35, 0x1B3B, 0x1B3B, 0x1B3D, 0x1B41,
            0x1B43, 0x1B44, 0x1B82, 0x1B82, 0x1BA1, 0x1BA1, 0x1BA6, 0x1BA7,
            0x1BAA, 0x1BAA, 0x1BE7, 0x1BE7, 0x1BEA, 0x1BEC, 0x1BEE, 0x1BEE,
            0x1BF2, 0x1BF3, 0x1C24, 0x1C2B, 0x1C34, 0x1C35, 0x1CE1, 0x1CE1,
            0x1CF7, 0x1CF7, 0x302E, 0x302F, 0xA823, 0xA824, 0xA827, 0xA827,
            0xA880, 0xA881, 0xA8B4, 0xA8C3, 0xA952, 0xA953, 0xA983, 0xA983,
            0xA9B4, 0xA9B5, 0xA9BA, 0xA9BB, 0xA9BE, 0xA9C0, 0xAA2F, 0xAA30,
            0xAA33, 0xAA34, 0xAA4D, 0xAA4D, 0xAA7B, 0xAA7B, 0xAA7D, 0xAA7D,
            0xAAEB, 0xAAEB, 0xAAEE, 0xAAEF, 0xAAF5, 0xAAF5, 0xABE3, 0xABE4,
            0xABE6, 0xABE7, 0xABE9, 0xABEA, 0xABEC, 0xABEC, 0x11000, 0x11000,
            0x11002, 0x11002, 0x11082, 0x11082, 0x110B0, 0x110B2, 0x110B7, 0x110B8,
            0x1112C, 0x1112C, 0x11145, 0x11146, 0x11182, 0x11182, 0x111B3, 0x111B5,
            0x111BF, 0x111C0, 0x111CE, 0x111CE, 0x1122C, 0x1122E, 0x11232, 0x11233,
            0x11235, 0x11235, 0x112E0, 0x112E2, 0x11302, 0x11303, 0x1133E, 0x1133F,
            0x11341, 0x11344, 0x11347, 0x11348, 0x1134B, 0x1134D, 0x11357, 0x11357,
            0x11362, 0x11363, 0x11435, 0x11437, 0x11440, 0x11441, 0x11445, 0x11445,
            0x114B0, 0x114B2, 0x114B9, 0x114B9, 0x114BB, 0x114BE, 0x114C1, 0x114C1,
            0x115AF, 0x115B1, 0x115B8, 0x115BB, 0x115BE, 0x115BE, 0x11630, 0x11632,
            0x1163B, 0x1163C, 0x1163E, 0x1163E, 0x116AC, 0x116AC, 0x116AE, 0x116AF,
            0x116B6, 0x116B6, 0x11720, 0x11721, 0x11726, 0x11726, 0x1182C, 0x1182E,
            0x11838, 0x11838, 0x11930, 0x11935, 0x11937, 0x11938, 0x1193D, 0x1193D,
            0x11940, 0x11940, 0x11942, 0x11942, 0x119D1, 0x119D3, 0x119DC, 0x119DF,
            0x119E4, 0x119E4, 0x11A39, 0x11A39, 0x11A57, 0x11A58, 0x11A97, 0x11A97,
            0x11C2F, 0x11C2F, 0x11C3E, 0x11C3E, 0x11CA9, 0x11CA9, 0x11CB1, 0x11CB1,
            0x11CB4, 0x11CB4, 0x11D8A, 0x11D8E, 0x11D93, 0x11D94, 0x11D96, 0x11D96,
            0x11EF5, 0x11EF6, 0x16F51, 0x16F87, 0x16FF0, 0x16FF1, 0x1D165, 0x1D166,
            0x1D16D, 0x1D172,
            //  #16 (2785+5): gc=Enclosing_Mark:Me
            0x0488, 0x0489, 0x1ABE, 0x1ABE, 0x20DD, 0x20E0, 0x20E2, 0x20E4,
            0xA670, 0xA672,
            //  #17 (2790+336): gc=Nonspacing_Mark:Mn
            0x0300, 0x036F, 0x0483, 0x0487, 0x0591, 0x05BD, 0x05BF, 0x05BF,
            0x05C1, 0x05C2, 0x05C4, 0x05C5, 0x05C7, 0x05C7, 0x0610, 0x061A,
            0x064B, 0x065F, 0x0670, 0x0670, 0x06D6, 0x06DC, 0x06DF, 0x06E4,
            0x06E7, 0x06E8, 0x06EA, 0x06ED, 0x0711, 0x0711, 0x0730, 0x074A,
            0x07A6, 0x07B0, 0x07EB, 0x07F3, 0x07FD, 0x07FD, 0x0816, 0x0819,
            0x081B, 0x0823, 0x0825, 0x0827, 0x0829, 0x082D, 0x0859, 0x085B,
            0x0898, 0x089F, 0x08CA, 0x08E1, 0x08E3, 0x0902, 0x093A, 0x093A,
            0x093C, 0x093C, 0x0941, 0x0948, 0x094D, 0x094D, 0x0951, 0x0957,
            0x0962, 0x0963, 0x0981, 0x0981, 0x09BC, 0x09BC, 0x09C1, 0x09C4,
            0x09CD, 0x09CD, 0x09E2, 0x09E3, 0x09FE, 0x09FE, 0x0A01, 0x0A02,
            0x0A3C, 0x0A3C, 0x0A41, 0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4D,
            0x0A51, 0x0A51, 0x0A70, 0x0A71, 0x0A75, 0x0A75, 0x0A81, 0x0A82,
            0x0ABC, 0x0ABC, 0x0AC1, 0x0AC5, 0x0AC7, 0x0AC8, 0x0ACD, 0x0ACD,
            0x0AE2, 0x0AE3, 0x0AFA, 0x0AFF, 0x0B01, 0x0B01, 0x0B3C, 0x0B3C,
            0x0B3F, 0x0B3F, 0x0B41, 0x0B44, 0x0B4D, 0x0B4D, 0x0B55, 0x0B56,
            0x0B62, 0x0B63, 0x0B82, 0x0B82, 0x0BC0, 0x0BC0, 0x0BCD, 0x0BCD,
            0x0C00, 0x0C00, 0x0C04, 0x0C04, 0x0C3C, 0x0C3C, 0x0C3E, 0x0C40,
            0x0C46, 0x0C48, 0x0C4A, 0x0C4D, 0x0C55, 0x0C56, 0x0C62, 0x0C63,
            0x0C81, 0x0C81, 0x0CBC, 0x0CBC, 0x0CBF, 0x0CBF, 0x0CC6, 0x0CC6,
            0x0CCC, 0x0CCD, 0x0CE2, 0x0CE3, 0x0D00, 0x0D01, 0x0D3B, 0x0D3C,
            0x0D41, 0x0D44, 0x0D4D, 0x0D4D, 0x0D62, 0x0D63, 0x0D81, 0x0D81,
            0x0DCA, 0x0DCA, 0x0DD2, 0x0DD4, 0x0DD6, 0x0DD6, 0x0E31, 0x0E31,
            0x0E34, 0x0E3A, 0x0E47, 0x0E4E, 0x0EB1, 0x0EB1, 0x0EB4, 0x0EBC,
            0x0EC8, 0x0ECD, 0x0F18, 0x0F19, 0x0F35, 0x0F35, 0x0F37, 0x0F37,
            0x0F39, 0x0F39, 0x0F71, 0x0F7E, 0x0F80, 0x0F84, 0x0F86, 0x0F87,
            0x0F8D, 0x0F97, 0x0F99, 0x0FBC, 0x0FC6, 0x0FC6, 0x102D, 0x1030,
            0x1032, 0x1037, 0x1039, 0x103A, 0x103D, 0x103E, 0x1058, 0x1059,
            0x105E, 0x1060, 0x1071, 0x1074, 0x1082, 0x1082, 0x1085, 0x1086,
            0x108D, 0x108D, 0x109D, 0x109D, 0x135D, 0x135F, 0x1712, 0x1714,
            0x1732, 0x1733, 0x1752, 0x1753, 0x1772, 0x1773, 0x17B4, 0x17B5,
            0x17B7, 0x17BD, 0x17C6, 0x17C6, 0x17C9, 0x17D3, 0x17DD, 0x17DD,
            0x180B, 0x180D, 0x180F, 0x180F, 0x1885, 0x1886, 0x18A9, 0x18A9,
            0x1920, 0x1922, 0x1927, 0x1928, 0x1932, 0x1932, 0x1939, 0x193B,
            0x1A17, 0x1A18, 0x1A1B, 0x1A1B, 0x1A56, 0x1A56, 0x1A58, 0x1A5E,
            0x1A60, 0x1A60, 0x1A62, 0x1A62, 0x1A65, 0x1A6C, 0x1A73, 0x1A7C,
            0x1A7F, 0x1A7F, 0x1AB0, 0x1ABD, 0x1ABF, 0x1ACE, 0x1B00, 0x1B03,
            0x1B34, 0x1B34, 0x1B36, 0x1B3A, 0x1B3C, 0x1B3C, 0x1B42, 0x1B42,
            0x1B6B, 0x1B73, 0x1B80, 0x1B81, 0x1BA2, 0x1BA5, 0x1BA8, 0x1BA9,
            0x1BAB, 0x1BAD, 0x1BE6, 0x1BE6, 0x1BE8, 0x1BE9, 0x1BED, 0x1BED,
            0x1BEF, 0x1BF1, 0x1C2C, 0x1C33, 0x1C36, 0x1C37, 0x1CD0, 0x1CD2,
            0x1CD4, 0x1CE0, 0x1CE2, 0x1CE8, 0x1CED, 0x1CED, 0x1CF4, 0x1CF4,
            0x1CF8, 0x1CF9, 0x1DC0, 0x1DFF, 0x20D0, 0x20DC, 0x20E1, 0x20E1,
            0x20E5, 0x20F0, 0x2CEF, 0x2CF1, 0x2D7F, 0x2D7F, 0x2DE0, 0x2DFF,
            0x302A, 0x302D, 0x3099, 0x309A, 0xA66F, 0xA66F, 0xA674, 0xA67D,
            0xA69E, 0xA69F, 0xA6F0, 0xA6F1, 0xA802, 0xA802, 0xA806, 0xA806,
            0xA80B, 0xA80B, 0xA825, 0xA826, 0xA82C, 0xA82C, 0xA8C4, 0xA8C5,
            0xA8E0, 0xA8F1, 0xA8FF, 0xA8FF, 0xA926, 0xA92D, 0xA947, 0xA951,
            0xA980, 0xA982, 0xA9B3, 0xA9B3, 0xA9B6, 0xA9B9, 0xA9BC, 0xA9BD,
            0xA9E5, 0xA9E5, 0xAA29, 0xAA2E, 0xAA31, 0xAA32, 0xAA35, 0xAA36,
            0xAA43, 0xAA43, 0xAA4C, 0xAA4C, 0xAA7C, 0xAA7C, 0xAAB0, 0xAAB0,
            0xAAB2, 0xAAB4, 0xAAB7, 0xAAB8, 0xAABE, 0xAABF, 0xAAC1, 0xAAC1,
            0xAAEC, 0xAAED, 0xAAF6, 0xAAF6, 0xABE5, 0xABE5, 0xABE8, 0xABE8,
            0xABED, 0xABED, 0xFB1E, 0xFB1E, 0xFE00, 0xFE0F, 0xFE20, 0xFE2F,
            0x101FD, 0x101FD, 0x102E0, 0x102E0, 0x10376, 0x1037A, 0x10A01, 0x10A03,
            0x10A05, 0x10A06, 0x10A0C, 0x10A0F, 0x10A38, 0x10A3A, 0x10A3F, 0x10A3F,
            0x10AE5, 0x10AE6, 0x10D24, 0x10D27, 0x10EAB, 0x10EAC, 0x10F46, 0x10F50,
            0x10F82, 0x10F85, 0x11001, 0x11001, 0x11038, 0x11046, 0x11070, 0x11070,
            0x11073, 0x11074, 0x1107F, 0x11081, 0x110B3, 0x110B6, 0x110B9, 0x110BA,
            0x110C2, 0x110C2, 0x11100, 0x11102, 0x11127, 0x1112B, 0x1112D, 0x11134,
            0x11173, 0x11173, 0x11180, 0x11181, 0x111B6, 0x111BE, 0x111C9, 0x111CC,
            0x111CF, 0x111CF, 0x1122F, 0x11231, 0x11234, 0x11234, 0x11236, 0x11237,
            0x1123E, 0x1123E, 0x112DF, 0x112DF, 0x112E3, 0x112EA, 0x11300, 0x11301,
            0x1133B, 0x1133C, 0x11340, 0x11340, 0x11366, 0x1136C, 0x11370, 0x11374,
            0x11438, 0x1143F, 0x11442, 0x11444, 0x11446, 0x11446, 0x1145E, 0x1145E,
            0x114B3, 0x114B8, 0x114BA, 0x114BA, 0x114BF, 0x114C0, 0x114C2, 0x114C3,
            0x115B2, 0x115B5, 0x115BC, 0x115BD, 0x115BF, 0x115C0, 0x115DC, 0x115DD,
            0x11633, 0x1163A, 0x1163D, 0x1163D, 0x1163F, 0x11640, 0x116AB, 0x116AB,
            0x116AD, 0x116AD, 0x116B0, 0x116B5, 0x116B7, 0x116B7, 0x1171D, 0x1171F,
            0x11722, 0x11725, 0x11727, 0x1172B, 0x1182F, 0x11837, 0x11839, 0x1183A,
            0x1193B, 0x1193C, 0x1193E, 0x1193E, 0x11943, 0x11943, 0x119D4, 0x119D7,
            0x119DA, 0x119DB, 0x119E0, 0x119E0, 0x11A01, 0x11A0A, 0x11A33, 0x11A38,
            0x11A3B, 0x11A3E, 0x11A47, 0x11A47, 0x11A51, 0x11A56, 0x11A59, 0x11A5B,
            0x11A8A, 0x11A96, 0x11A98, 0x11A99, 0x11C30, 0x11C36, 0x11C38, 0x11C3D,
            0x11C3F, 0x11C3F, 0x11C92, 0x11CA7, 0x11CAA, 0x11CB0, 0x11CB2, 0x11CB3,
            0x11CB5, 0x11CB6, 0x11D31, 0x11D36, 0x11D3A, 0x11D3A, 0x11D3C, 0x11D3D,
            0x11D3F, 0x11D45, 0x11D47, 0x11D47, 0x11D90, 0x11D91, 0x11D95, 0x11D95,
            0x11D97, 0x11D97, 0x11EF3, 0x11EF4, 0x16AF0, 0x16AF4, 0x16B30, 0x16B36,
            0x16F4F, 0x16F4F, 0x16F8F, 0x16F92, 0x16FE4, 0x16FE4, 0x1BC9D, 0x1BC9E,
            0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46, 0x1D167, 0x1D169, 0x1D17B, 0x1D182,
            0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD, 0x1D242, 0x1D244, 0x1DA00, 0x1DA36,
            0x1DA3B, 0x1DA6C, 0x1DA75, 0x1DA75, 0x1DA84, 0x1DA84, 0x1DA9B, 0x1DA9F,
            0x1DAA1, 0x1DAAF, 0x1E000, 0x1E006, 0x1E008, 0x1E018, 0x1E01B, 0x1E021,
            0x1E023, 0x1E024, 0x1E026, 0x1E02A, 0x1E130, 0x1E136, 0x1E2AE, 0x1E2AE,
            0x1E2EC, 0x1E2EF, 0x1E8D0, 0x1E8D6, 0x1E944, 0x1E94A, 0xE0100, 0xE01EF,
            //  #18 (3126+145): gc=Number:N
            //  Nd:62 + Nl:12 + No:71
            //  #19 (3126+62): gc=Decimal_Number:Nd:digit
            0x0030, 0x0039, 0x0660, 0x0669, 0x06F0, 0x06F9, 0x07C0, 0x07C9,
            0x0966, 0x096F, 0x09E6, 0x09EF, 0x0A66, 0x0A6F, 0x0AE6, 0x0AEF,
            0x0B66, 0x0B6F, 0x0BE6, 0x0BEF, 0x0C66, 0x0C6F, 0x0CE6, 0x0CEF,
            0x0D66, 0x0D6F, 0x0DE6, 0x0DEF, 0x0E50, 0x0E59, 0x0ED0, 0x0ED9,
            0x0F20, 0x0F29, 0x1040, 0x1049, 0x1090, 0x1099, 0x17E0, 0x17E9,
            0x1810, 0x1819, 0x1946, 0x194F, 0x19D0, 0x19D9, 0x1A80, 0x1A89,
            0x1A90, 0x1A99, 0x1B50, 0x1B59, 0x1BB0, 0x1BB9, 0x1C40, 0x1C49,
            0x1C50, 0x1C59, 0xA620, 0xA629, 0xA8D0, 0xA8D9, 0xA900, 0xA909,
            0xA9D0, 0xA9D9, 0xA9F0, 0xA9F9, 0xAA50, 0xAA59, 0xABF0, 0xABF9,
            0xFF10, 0xFF19, 0x104A0, 0x104A9, 0x10D30, 0x10D39, 0x11066, 0x1106F,
            0x110F0, 0x110F9, 0x11136, 0x1113F, 0x111D0, 0x111D9, 0x112F0, 0x112F9,
            0x11450, 0x11459, 0x114D0, 0x114D9, 0x11650, 0x11659, 0x116C0, 0x116C9,
            0x11730, 0x11739, 0x118E0, 0x118E9, 0x11950, 0x11959, 0x11C50, 0x11C59,
            0x11D50, 0x11D59, 0x11DA0, 0x11DA9, 0x16A60, 0x16A69, 0x16AC0, 0x16AC9,
            0x16B50, 0x16B59, 0x1D7CE, 0x1D7FF, 0x1E140, 0x1E149, 0x1E2F0, 0x1E2F9,
            0x1E950, 0x1E959, 0x1FBF0, 0x1FBF9,
            //  #20 (3188+12): gc=Letter_Number:Nl
            0x16EE, 0x16F0, 0x2160, 0x2182, 0x2185, 0x2188, 0x3007, 0x3007,
            0x3021, 0x3029, 0x3038, 0x303A, 0xA6E6, 0xA6EF, 0x10140, 0x10174,
            0x10341, 0x10341, 0x1034A, 0x1034A, 0x103D1, 0x103D5, 0x12400, 0x1246E,
            //  #21 (3200+71): gc=Other_Number:No
            0x00B2, 0x00B3, 0x00B9, 0x00B9, 0x00BC, 0x00BE, 0x09F4, 0x09F9,
            0x0B72, 0x0B77, 0x0BF0, 0x0BF2, 0x0C78, 0x0C7E, 0x0D58, 0x0D5E,
            0x0D70, 0x0D78, 0x0F2A, 0x0F33, 0x1369, 0x137C, 0x17F0, 0x17F9,
            0x19DA, 0x19DA, 0x2070, 0x2070, 0x2074, 0x2079, 0x2080, 0x2089,
            0x2150, 0x215F, 0x2189, 0x2189, 0x2460, 0x249B, 0x24EA, 0x24FF,
            0x2776, 0x2793, 0x2CFD, 0x2CFD, 0x3192, 0x3195, 0x3220, 0x3229,
            0x3248, 0x324F, 0x3251, 0x325F, 0x3280, 0x3289, 0x32B1, 0x32BF,
            0xA830, 0xA835, 0x10107, 0x10133, 0x10175, 0x10178, 0x1018A, 0x1018B,
            0x102E1, 0x102FB, 0x10320, 0x10323, 0x10858, 0x1085F, 0x10879, 0x1087F,
            0x108A7, 0x108AF, 0x108FB, 0x108FF, 0x10916, 0x1091B, 0x109BC, 0x109BD,
            0x109C0, 0x109CF, 0x109D2, 0x109FF, 0x10A40, 0x10A48, 0x10A7D, 0x10A7E,
            0x10A9D, 0x10A9F, 0x10AEB, 0x10AEF, 0x10B58, 0x10B5F, 0x10B78, 0x10B7F,
            0x10BA9, 0x10BAF, 0x10CFA, 0x10CFF, 0x10E60, 0x10E7E, 0x10F1D, 0x10F26,
            0x10F51, 0x10F54, 0x10FC5, 0x10FCB, 0x11052, 0x11065, 0x111E1, 0x111F4,
            0x1173A, 0x1173B, 0x118EA, 0x118F2, 0x11C5A, 0x11C6C, 0x11FC0, 0x11FD4,
            0x16B5B, 0x16B61, 0x16E80, 0x16E96, 0x1D2E0, 0x1D2F3, 0x1D360, 0x1D378,
            0x1E8C7, 0x1E8CF, 0x1EC71, 0x1ECAB, 0x1ECAD, 0x1ECAF, 0x1ECB1, 0x1ECB4,
            0x1ED01, 0x1ED2D, 0x1ED2F, 0x1ED3D, 0x1F100, 0x1F10C,
            //  #22 (3271+386): gc=Punctuation:P:punct
            //  Pc:6 + Pd:19 + Pe:76 + Pf:10 + Pi:11 + Po:185 + Ps:79
            //  #23 (3271+6): gc=Connector_Punctuation:Pc
            0x005F, 0x005F, 0x203F, 0x2040, 0x2054, 0x2054, 0xFE33, 0xFE34,
            0xFE4D, 0xFE4F, 0xFF3F, 0xFF3F,
            //  #24 (3277+19): gc=Dash_Punctuation:Pd
            0x002D, 0x002D, 0x058A, 0x058A, 0x05BE, 0x05BE, 0x1400, 0x1400,
            0x1806, 0x1806, 0x2010, 0x2015, 0x2E17, 0x2E17, 0x2E1A, 0x2E1A,
            0x2E3A, 0x2E3B, 0x2E40, 0x2E40, 0x2E5D, 0x2E5D, 0x301C, 0x301C,
            0x3030, 0x3030, 0x30A0, 0x30A0, 0xFE31, 0xFE32, 0xFE58, 0xFE58,
            0xFE63, 0xFE63, 0xFF0D, 0xFF0D, 0x10EAD, 0x10EAD,
            //  #25 (3296+76): gc=Close_Punctuation:Pe
            0x0029, 0x0029, 0x005D, 0x005D, 0x007D, 0x007D, 0x0F3B, 0x0F3B,
            0x0F3D, 0x0F3D, 0x169C, 0x169C, 0x2046, 0x2046, 0x207E, 0x207E,
            0x208E, 0x208E, 0x2309, 0x2309, 0x230B, 0x230B, 0x232A, 0x232A,
            0x2769, 0x2769, 0x276B, 0x276B, 0x276D, 0x276D, 0x276F, 0x276F,
            0x2771, 0x2771, 0x2773, 0x2773, 0x2775, 0x2775, 0x27C6, 0x27C6,
            0x27E7, 0x27E7, 0x27E9, 0x27E9, 0x27EB, 0x27EB, 0x27ED, 0x27ED,
            0x27EF, 0x27EF, 0x2984, 0x2984, 0x2986, 0x2986, 0x2988, 0x2988,
            0x298A, 0x298A, 0x298C, 0x298C, 0x298E, 0x298E, 0x2990, 0x2990,
            0x2992, 0x2992, 0x2994, 0x2994, 0x2996, 0x2996, 0x2998, 0x2998,
            0x29D9, 0x29D9, 0x29DB, 0x29DB, 0x29FD, 0x29FD, 0x2E23, 0x2E23,
            0x2E25, 0x2E25, 0x2E27, 0x2E27, 0x2E29, 0x2E29, 0x2E56, 0x2E56,
            0x2E58, 0x2E58, 0x2E5A, 0x2E5A, 0x2E5C, 0x2E5C, 0x3009, 0x3009,
            0x300B, 0x300B, 0x300D, 0x300D, 0x300F, 0x300F, 0x3011, 0x3011,
            0x3015, 0x3015, 0x3017, 0x3017, 0x3019, 0x3019, 0x301B, 0x301B,
            0x301E, 0x301F, 0xFD3E, 0xFD3E, 0xFE18, 0xFE18, 0xFE36, 0xFE36,
            0xFE38, 0xFE38, 0xFE3A, 0xFE3A, 0xFE3C, 0xFE3C, 0xFE3E, 0xFE3E,
            0xFE40, 0xFE40, 0xFE42, 0xFE42, 0xFE44, 0xFE44, 0xFE48, 0xFE48,
            0xFE5A, 0xFE5A, 0xFE5C, 0xFE5C, 0xFE5E, 0xFE5E, 0xFF09, 0xFF09,
            0xFF3D, 0xFF3D, 0xFF5D, 0xFF5D, 0xFF60, 0xFF60, 0xFF63, 0xFF63,
            //  #26 (3372+10): gc=Final_Punctuation:Pf
            0x00BB, 0x00BB, 0x2019, 0x2019, 0x201D, 0x201D, 0x203A, 0x203A,
            0x2E03, 0x2E03, 0x2E05, 0x2E05, 0x2E0A, 0x2E0A, 0x2E0D, 0x2E0D,
            0x2E1D, 0x2E1D, 0x2E21, 0x2E21,
            //  #27 (3382+11): gc=Initial_Punctuation:Pi
            0x00AB, 0x00AB, 0x2018, 0x2018, 0x201B, 0x201C, 0x201F, 0x201F,
            0x2039, 0x2039, 0x2E02, 0x2E02, 0x2E04, 0x2E04, 0x2E09, 0x2E09,
            0x2E0C, 0x2E0C, 0x2E1C, 0x2E1C, 0x2E20, 0x2E20,
            //  #28 (3393+185): gc=Other_Punctuation:Po
            0x0021, 0x0023, 0x0025, 0x0027, 0x002A, 0x002A, 0x002C, 0x002C,
            0x002E, 0x002F, 0x003A, 0x003B, 0x003F, 0x0040, 0x005C, 0x005C,
            0x00A1, 0x00A1, 0x00A7, 0x00A7, 0x00B6, 0x00B7, 0x00BF, 0x00BF,
            0x037E, 0x037E, 0x0387, 0x0387, 0x055A, 0x055F, 0x0589, 0x0589,
            0x05C0, 0x05C0, 0x05C3, 0x05C3, 0x05C6, 0x05C6, 0x05F3, 0x05F4,
            0x0609, 0x060A, 0x060C, 0x060D, 0x061B, 0x061B, 0x061D, 0x061F,
            0x066A, 0x066D, 0x06D4, 0x06D4, 0x0700, 0x070D, 0x07F7, 0x07F9,
            0x0830, 0x083E, 0x085E, 0x085E, 0x0964, 0x0965, 0x0970, 0x0970,
            0x09FD, 0x09FD, 0x0A76, 0x0A76, 0x0AF0, 0x0AF0, 0x0C77, 0x0C77,
            0x0C84, 0x0C84, 0x0DF4, 0x0DF4, 0x0E4F, 0x0E4F, 0x0E5A, 0x0E5B,
            0x0F04, 0x0F12, 0x0F14, 0x0F14, 0x0F85, 0x0F85, 0x0FD0, 0x0FD4,
            0x0FD9, 0x0FDA, 0x104A, 0x104F, 0x10FB, 0x10FB, 0x1360, 0x1368,
            0x166E, 0x166E, 0x16EB, 0x16ED, 0x1735, 0x1736, 0x17D4, 0x17D6,
            0x17D8, 0x17DA, 0x1800, 0x1805, 0x1807, 0x180A, 0x1944, 0x1945,
            0x1A1E, 0x1A1F, 0x1AA0, 0x1AA6, 0x1AA8, 0x1AAD, 0x1B5A, 0x1B60,
            0x1B7D, 0x1B7E, 0x1BFC, 0x1BFF, 0x1C3B, 0x1C3F, 0x1C7E, 0x1C7F,
            0x1CC0, 0x1CC7, 0x1CD3, 0x1CD3, 0x2016, 0x2017, 0x2020, 0x2027,
            0x2030, 0x2038, 0x203B, 0x203E, 0x2041, 0x2043, 0x2047, 0x2051,
            0x2053, 0x2053, 0x2055, 0x205E, 0x2CF9, 0x2CFC, 0x2CFE, 0x2CFF,
            0x2D70, 0x2D70, 0x2E00, 0x2E01, 0x2E06, 0x2E08, 0x2E0B, 0x2E0B,
            0x2E0E, 0x2E16, 0x2E18, 0x2E19, 0x2E1B, 0x2E1B, 0x2E1E, 0x2E1F,
            0x2E2A, 0x2E2E, 0x2E30, 0x2E39, 0x2E3C, 0x2E3F, 0x2E41, 0x2E41,
            0x2E43, 0x2E4F, 0x2E52, 0x2E54, 0x3001, 0x3003, 0x303D, 0x303D,
            0x30FB, 0x30FB, 0xA4FE, 0xA4FF, 0xA60D, 0xA60F, 0xA673, 0xA673,
            0xA67E, 0xA67E, 0xA6F2, 0xA6F7, 0xA874, 0xA877, 0xA8CE, 0xA8CF,
            0xA8F8, 0xA8FA, 0xA8FC, 0xA8FC, 0xA92E, 0xA92F, 0xA95F, 0xA95F,
            0xA9C1, 0xA9CD, 0xA9DE, 0xA9DF, 0xAA5C, 0xAA5F, 0xAADE, 0xAADF,
            0xAAF0, 0xAAF1, 0xABEB, 0xABEB, 0xFE10, 0xFE16, 0xFE19, 0xFE19,
            0xFE30, 0xFE30, 0xFE45, 0xFE46, 0xFE49, 0xFE4C, 0xFE50, 0xFE52,
            0xFE54, 0xFE57, 0xFE5F, 0xFE61, 0xFE68, 0xFE68, 0xFE6A, 0xFE6B,
            0xFF01, 0xFF03, 0xFF05, 0xFF07, 0xFF0A, 0xFF0A, 0xFF0C, 0xFF0C,
            0xFF0E, 0xFF0F, 0xFF1A, 0xFF1B, 0xFF1F, 0xFF20, 0xFF3C, 0xFF3C,
            0xFF61, 0xFF61, 0xFF64, 0xFF65, 0x10100, 0x10102, 0x1039F, 0x1039F,
            0x103D0, 0x103D0, 0x1056F, 0x1056F, 0x10857, 0x10857, 0x1091F, 0x1091F,
            0x1093F, 0x1093F, 0x10A50, 0x10A58, 0x10A7F, 0x10A7F, 0x10AF0, 0x10AF6,
            0x10B39, 0x10B3F, 0x10B99, 0x10B9C, 0x10F55, 0x10F59, 0x10F86, 0x10F89,
            0x11047, 0x1104D, 0x110BB, 0x110BC, 0x110BE, 0x110C1, 0x11140, 0x11143,
            0x11174, 0x11175, 0x111C5, 0x111C8, 0x111CD, 0x111CD, 0x111DB, 0x111DB,
            0x111DD, 0x111DF, 0x11238, 0x1123D, 0x112A9, 0x112A9, 0x1144B, 0x1144F,
            0x1145A, 0x1145B, 0x1145D, 0x1145D, 0x114C6, 0x114C6, 0x115C1, 0x115D7,
            0x11641, 0x11643, 0x11660, 0x1166C, 0x116B9, 0x116B9, 0x1173C, 0x1173E,
            0x1183B, 0x1183B, 0x11944, 0x11946, 0x119E2, 0x119E2, 0x11A3F, 0x11A46,
            0x11A9A, 0x11A9C, 0x11A9E, 0x11AA2, 0x11C41, 0x11C45, 0x11C70, 0x11C71,
            0x11EF7, 0x11EF8, 0x11FFF, 0x11FFF, 0x12470, 0x12474, 0x12FF1, 0x12FF2,
            0x16A6E, 0x16A6F, 0x16AF5, 0x16AF5, 0x16B37, 0x16B3B, 0x16B44, 0x16B44,
            0x16E97, 0x16E9A, 0x16FE2, 0x16FE2, 0x1BC9F, 0x1BC9F, 0x1DA87, 0x1DA8B,
            0x1E95E, 0x1E95F,
            //  #29 (3578+79): gc=Open_Punctuation:Ps
            0x0028, 0x0028, 0x005B, 0x005B, 0x007B, 0x007B, 0x0F3A, 0x0F3A,
            0x0F3C, 0x0F3C, 0x169B, 0x169B, 0x201A, 0x201A, 0x201E, 0x201E,
            0x2045, 0x2045, 0x207D, 0x207D, 0x208D, 0x208D, 0x2308, 0x2308,
            0x230A, 0x230A, 0x2329, 0x2329, 0x2768, 0x2768, 0x276A, 0x276A,
            0x276C, 0x276C, 0x276E, 0x276E, 0x2770, 0x2770, 0x2772, 0x2772,
            0x2774, 0x2774, 0x27C5, 0x27C5, 0x27E6, 0x27E6, 0x27E8, 0x27E8,
            0x27EA, 0x27EA, 0x27EC, 0x27EC, 0x27EE, 0x27EE, 0x2983, 0x2983,
            0x2985, 0x2985, 0x2987, 0x2987, 0x2989, 0x2989, 0x298B, 0x298B,
            0x298D, 0x298D, 0x298F, 0x298F, 0x2991, 0x2991, 0x2993, 0x2993,
            0x2995, 0x2995, 0x2997, 0x2997, 0x29D8, 0x29D8, 0x29DA, 0x29DA,
            0x29FC, 0x29FC, 0x2E22, 0x2E22, 0x2E24, 0x2E24, 0x2E26, 0x2E26,
            0x2E28, 0x2E28, 0x2E42, 0x2E42, 0x2E55, 0x2E55, 0x2E57, 0x2E57,
            0x2E59, 0x2E59, 0x2E5B, 0x2E5B, 0x3008, 0x3008, 0x300A, 0x300A,
            0x300C, 0x300C, 0x300E, 0x300E, 0x3010, 0x3010, 0x3014, 0x3014,
            0x3016, 0x3016, 0x3018, 0x3018, 0x301A, 0x301A, 0x301D, 0x301D,
            0xFD3F, 0xFD3F, 0xFE17, 0xFE17, 0xFE35, 0xFE35, 0xFE37, 0xFE37,
            0xFE39, 0xFE39, 0xFE3B, 0xFE3B, 0xFE3D, 0xFE3D, 0xFE3F, 0xFE3F,
            0xFE41, 0xFE41, 0xFE43, 0xFE43, 0xFE47, 0xFE47, 0xFE59, 0xFE59,
            0xFE5B, 0xFE5B, 0xFE5D, 0xFE5D, 0xFF08, 0xFF08, 0xFF3B, 0xFF3B,
            0xFF5B, 0xFF5B, 0xFF5F, 0xFF5F, 0xFF62, 0xFF62,
            //  #30 (3657+302): gc=Symbol:S
            //  Sc:21 + Sk:31 + Sm:64 + So:186
            //  #31 (3657+21): gc=Currency_Symbol:Sc
            0x0024, 0x0024, 0x00A2, 0x00A5, 0x058F, 0x058F, 0x060B, 0x060B,
            0x07FE, 0x07FF, 0x09F2, 0x09F3, 0x09FB, 0x09FB, 0x0AF1, 0x0AF1,
            0x0BF9, 0x0BF9, 0x0E3F, 0x0E3F, 0x17DB, 0x17DB, 0x20A0, 0x20C0,
            0xA838, 0xA838, 0xFDFC, 0xFDFC, 0xFE69, 0xFE69, 0xFF04, 0xFF04,
            0xFFE0, 0xFFE1, 0xFFE5, 0xFFE6, 0x11FDD, 0x11FE0, 0x1E2FF, 0x1E2FF,
            0x1ECB0, 0x1ECB0,
            //  #32 (3678+31): gc=Modifier_Symbol:Sk
            0x005E, 0x005E, 0x0060, 0x0060, 0x00A8, 0x00A8, 0x00AF, 0x00AF,
            0x00B4, 0x00B4, 0x00B8, 0x00B8, 0x02C2, 0x02C5, 0x02D2, 0x02DF,
            0x02E5, 0x02EB, 0x02ED, 0x02ED, 0x02EF, 0x02FF, 0x0375, 0x0375,
            0x0384, 0x0385, 0x0888, 0x0888, 0x1FBD, 0x1FBD, 0x1FBF, 0x1FC1,
            0x1FCD, 0x1FCF, 0x1FDD, 0x1FDF, 0x1FED, 0x1FEF, 0x1FFD, 0x1FFE,
            0x309B, 0x309C, 0xA700, 0xA716, 0xA720, 0xA721, 0xA789, 0xA78A,
            0xAB5B, 0xAB5B, 0xAB6A, 0xAB6B, 0xFBB2, 0xFBC2, 0xFF3E, 0xFF3E,
            0xFF40, 0xFF40, 0xFFE3, 0xFFE3, 0x1F3FB, 0x1F3FF,
            //  #33 (3709+64): gc=Math_Symbol:Sm
            0x002B, 0x002B, 0x003C, 0x003E, 0x007C, 0x007C, 0x007E, 0x007E,
            0x00AC, 0x00AC, 0x00B1, 0x00B1, 0x00D7, 0x00D7, 0x00F7, 0x00F7,
            0x03F6, 0x03F6, 0x0606, 0x0608, 0x2044, 0x2044, 0x2052, 0x2052,
            0x207A, 0x207C, 0x208A, 0x208C, 0x2118, 0x2118, 0x2140, 0x2144,
            0x214B, 0x214B, 0x2190, 0x2194, 0x219A, 0x219B, 0x21A0, 0x21A0,
            0x21A3, 0x21A3, 0x21A6, 0x21A6, 0x21AE, 0x21AE, 0x21CE, 0x21CF,
            0x21D2, 0x21D2, 0x21D4, 0x21D4, 0x21F4, 0x22FF, 0x2320, 0x2321,
            0x237C, 0x237C, 0x239B, 0x23B3, 0x23DC, 0x23E1, 0x25B7, 0x25B7,
            0x25C1, 0x25C1, 0x25F8, 0x25FF, 0x266F, 0x266F, 0x27C0, 0x27C4,
            0x27C7, 0x27E5, 0x27F0, 0x27FF, 0x2900, 0x2982, 0x2999, 0x29D7,
            0x29DC, 0x29FB, 0x29FE, 0x2AFF, 0x2B30, 0x2B44, 0x2B47, 0x2B4C,
            0xFB29, 0xFB29, 0xFE62, 0xFE62, 0xFE64, 0xFE66, 0xFF0B, 0xFF0B,
            0xFF1C, 0xFF1E, 0xFF5C, 0xFF5C, 0xFF5E, 0xFF5E, 0xFFE2, 0xFFE2,
            0xFFE9, 0xFFEC, 0x1D6C1, 0x1D6C1, 0x1D6DB, 0x1D6DB, 0x1D6FB, 0x1D6FB,
            0x1D715, 0x1D715, 0x1D735, 0x1D735, 0x1D74F, 0x1D74F, 0x1D76F, 0x1D76F,
            0x1D789, 0x1D789, 0x1D7A9, 0x1D7A9, 0x1D7C3, 0x1D7C3, 0x1EEF0, 0x1EEF1,
            //  #34 (3773+186): gc=Other_Symbol:So
            0x00A6, 0x00A6, 0x00A9, 0x00A9, 0x00AE, 0x00AE, 0x00B0, 0x00B0,
            0x0482, 0x0482, 0x058D, 0x058E, 0x060E, 0x060F, 0x06DE, 0x06DE,
            0x06E9, 0x06E9, 0x06FD, 0x06FE, 0x07F6, 0x07F6, 0x09FA, 0x09FA,
            0x0B70, 0x0B70, 0x0BF3, 0x0BF8, 0x0BFA, 0x0BFA, 0x0C7F, 0x0C7F,
            0x0D4F, 0x0D4F, 0x0D79, 0x0D79, 0x0F01, 0x0F03, 0x0F13, 0x0F13,
            0x0F15, 0x0F17, 0x0F1A, 0x0F1F, 0x0F34, 0x0F34, 0x0F36, 0x0F36,
            0x0F38, 0x0F38, 0x0FBE, 0x0FC5, 0x0FC7, 0x0FCC, 0x0FCE, 0x0FCF,
            0x0FD5, 0x0FD8, 0x109E, 0x109F, 0x1390, 0x1399, 0x166D, 0x166D,
            0x1940, 0x1940, 0x19DE, 0x19FF, 0x1B61, 0x1B6A, 0x1B74, 0x1B7C,
            0x2100, 0x2101, 0x2103, 0x2106, 0x2108, 0x2109, 0x2114, 0x2114,
            0x2116, 0x2117, 0x211E, 0x2123, 0x2125, 0x2125, 0x2127, 0x2127,
            0x2129, 0x2129, 0x212E, 0x212E, 0x213A, 0x213B, 0x214A, 0x214A,
            0x214C, 0x214D, 0x214F, 0x214F, 0x218A, 0x218B, 0x2195, 0x2199,
            0x219C, 0x219F, 0x21A1, 0x21A2, 0x21A4, 0x21A5, 0x21A7, 0x21AD,
            0x21AF, 0x21CD, 0x21D0, 0x21D1, 0x21D3, 0x21D3, 0x21D5, 0x21F3,
            0x2300, 0x2307, 0x230C, 0x231F, 0x2322, 0x2328, 0x232B, 0x237B,
            0x237D, 0x239A, 0x23B4, 0x23DB, 0x23E2, 0x2426, 0x2440, 0x244A,
            0x249C, 0x24E9, 0x2500, 0x25B6, 0x25B8, 0x25C0, 0x25C2, 0x25F7,
            0x2600, 0x266E, 0x2670, 0x2767, 0x2794, 0x27BF, 0x2800, 0x28FF,
            0x2B00, 0x2B2F, 0x2B45, 0x2B46, 0x2B4D, 0x2B73, 0x2B76, 0x2B95,
            0x2B97, 0x2BFF, 0x2CE5, 0x2CEA, 0x2E50, 0x2E51, 0x2E80, 0x2E99,
            0x2E9B, 0x2EF3, 0x2F00, 0x2FD5, 0x2FF0, 0x2FFB, 0x3004, 0x3004,
            0x3012, 0x3013, 0x3020, 0x3020, 0x3036, 0x3037, 0x303E, 0x303F,
            0x3190, 0x3191, 0x3196, 0x319F, 0x31C0, 0x31E3, 0x3200, 0x321E,
            0x322A, 0x3247, 0x3250, 0x3250, 0x3260, 0x327F, 0x328A, 0x32B0,
            0x32C0, 0x33FF, 0x4DC0, 0x4DFF, 0xA490, 0xA4C6, 0xA828, 0xA82B,
            0xA836, 0xA837, 0xA839, 0xA839, 0xAA77, 0xAA79, 0xFD40, 0xFD4F,
            0xFDCF, 0xFDCF, 0xFDFD, 0xFDFF, 0xFFE4, 0xFFE4, 0xFFE8, 0xFFE8,
            0xFFED, 0xFFEE, 0xFFFC, 0xFFFD, 0x10137, 0x1013F, 0x10179, 0x10189,
            0x1018C, 0x1018E, 0x10190, 0x1019C, 0x101A0, 0x101A0, 0x101D0, 0x101FC,
            0x10877, 0x10878, 0x10AC8, 0x10AC8, 0x1173F, 0x1173F, 0x11FD5, 0x11FDC,
            0x11FE1, 0x11FF1, 0x16B3C, 0x16B3F, 0x16B45, 0x16B45, 0x1BC9C, 0x1BC9C,
            0x1CF50, 0x1CFC3, 0x1D000, 0x1D0F5, 0x1D100, 0x1D126, 0x1D129, 0x1D164,
            0x1D16A, 0x1D16C, 0x1D183, 0x1D184, 0x1D18C, 0x1D1A9, 0x1D1AE, 0x1D1EA,
            0x1D200, 0x1D241, 0x1D245, 0x1D245, 0x1D300, 0x1D356, 0x1D800, 0x1D9FF,
            0x1DA37, 0x1DA3A, 0x1DA6D, 0x1DA74, 0x1DA76, 0x1DA83, 0x1DA85, 0x1DA86,
            0x1E14F, 0x1E14F, 0x1ECAC, 0x1ECAC, 0x1ED2E, 0x1ED2E, 0x1F000, 0x1F02B,
            0x1F030, 0x1F093, 0x1F0A0, 0x1F0AE, 0x1F0B1, 0x1F0BF, 0x1F0C1, 0x1F0CF,
            0x1F0D1, 0x1F0F5, 0x1F10D, 0x1F1AD, 0x1F1E6, 0x1F202, 0x1F210, 0x1F23B,
            0x1F240, 0x1F248, 0x1F250, 0x1F251, 0x1F260, 0x1F265, 0x1F300, 0x1F3FA,
            0x1F400, 0x1F6D7, 0x1F6DD, 0x1F6EC, 0x1F6F0, 0x1F6FC, 0x1F700, 0x1F773,
            0x1F780, 0x1F7D8, 0x1F7E0, 0x1F7EB, 0x1F7F0, 0x1F7F0, 0x1F800, 0x1F80B,
            0x1F810, 0x1F847, 0x1F850, 0x1F859, 0x1F860, 0x1F887, 0x1F890, 0x1F8AD,
            0x1F8B0, 0x1F8B1, 0x1F900, 0x1FA53, 0x1FA60, 0x1FA6D, 0x1FA70, 0x1FA74,
            0x1FA78, 0x1FA7C, 0x1FA80, 0x1FA86, 0x1FA90, 0x1FAAC, 0x1FAB0, 0x1FABA,
            0x1FAC0, 0x1FAC5, 0x1FAD0, 0x1FAD9, 0x1FAE0, 0x1FAE7, 0x1FAF0, 0x1FAF6,
            0x1FB00, 0x1FB92, 0x1FB94, 0x1FBCA,
            //  #35 (3959+9): gc=Separator:Z
            //  Zl:1 + Zp:1 + Zs:7
            //  #36 (3959+1): gc=Line_Separator:Zl
            0x2028, 0x2028,
            //  #37 (3960+1): gc=Paragraph_Separator:Zp
            0x2029, 0x2029,
            //  #38 (3961+7): gc=Space_Separator:Zs
            0x0020, 0x0020, 0x00A0, 0x00A0, 0x1680, 0x1680, 0x2000, 0x200A,
            0x202F, 0x202F, 0x205F, 0x205F, 0x3000, 0x3000,
            //  #39 (3968+1): bp=ASCII
            0x0000, 0x007F,
            //  #40 (3969+3): bp=ASCII_Hex_Digit:AHex
            0x0030, 0x0039, 0x0041, 0x0046, 0x0061, 0x0066,
            //  #41 (3972+722): bp=Alphabetic:Alpha
            0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00B5, 0x00B5,
            0x00BA, 0x00BA, 0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02C1,
            0x02C6, 0x02D1, 0x02E0, 0x02E4, 0x02EC, 0x02EC, 0x02EE, 0x02EE,
            0x0345, 0x0345, 0x0370, 0x0374, 0x0376, 0x0377, 0x037A, 0x037D,
            0x037F, 0x037F, 0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C,
            0x038E, 0x03A1, 0x03A3, 0x03F5, 0x03F7, 0x0481, 0x048A, 0x052F,
            0x0531, 0x0556, 0x0559, 0x0559, 0x0560, 0x0588, 0x05B0, 0x05BD,
            0x05BF, 0x05BF, 0x05C1, 0x05C2, 0x05C4, 0x05C5, 0x05C7, 0x05C7,
            0x05D0, 0x05EA, 0x05EF, 0x05F2, 0x0610, 0x061A, 0x0620, 0x0657,
            0x0659, 0x065F, 0x066E, 0x06D3, 0x06D5, 0x06DC, 0x06E1, 0x06E8,
            0x06ED, 0x06EF, 0x06FA, 0x06FC, 0x06FF, 0x06FF, 0x0710, 0x073F,
            0x074D, 0x07B1, 0x07CA, 0x07EA, 0x07F4, 0x07F5, 0x07FA, 0x07FA,
            0x0800, 0x0817, 0x081A, 0x082C, 0x0840, 0x0858, 0x0860, 0x086A,
            0x0870, 0x0887, 0x0889, 0x088E, 0x08A0, 0x08C9, 0x08D4, 0x08DF,
            0x08E3, 0x08E9, 0x08F0, 0x093B, 0x093D, 0x094C, 0x094E, 0x0950,
            0x0955, 0x0963, 0x0971, 0x0983, 0x0985, 0x098C, 0x098F, 0x0990,
            0x0993, 0x09A8, 0x09AA, 0x09B0, 0x09B2, 0x09B2, 0x09B6, 0x09B9,
            0x09BD, 0x09C4, 0x09C7, 0x09C8, 0x09CB, 0x09CC, 0x09CE, 0x09CE,
            0x09D7, 0x09D7, 0x09DC, 0x09DD, 0x09DF, 0x09E3, 0x09F0, 0x09F1,
            0x09FC, 0x09FC, 0x0A01, 0x0A03, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10,
            0x0A13, 0x0A28, 0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36,
            0x0A38, 0x0A39, 0x0A3E, 0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4C,
            0x0A51, 0x0A51, 0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A70, 0x0A75,
            0x0A81, 0x0A83, 0x0A85, 0x0A8D, 0x0A8F, 0x0A91, 0x0A93, 0x0AA8,
            0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3, 0x0AB5, 0x0AB9, 0x0ABD, 0x0AC5,
            0x0AC7, 0x0AC9, 0x0ACB, 0x0ACC, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE3,
            0x0AF9, 0x0AFC, 0x0B01, 0x0B03, 0x0B05, 0x0B0C, 0x0B0F, 0x0B10,
            0x0B13, 0x0B28, 0x0B2A, 0x0B30, 0x0B32, 0x0B33, 0x0B35, 0x0B39,
            0x0B3D, 0x0B44, 0x0B47, 0x0B48, 0x0B4B, 0x0B4C, 0x0B56, 0x0B57,
            0x0B5C, 0x0B5D, 0x0B5F, 0x0B63, 0x0B71, 0x0B71, 0x0B82, 0x0B83,
            0x0B85, 0x0B8A, 0x0B8E, 0x0B90, 0x0B92, 0x0B95, 0x0B99, 0x0B9A,
            0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA,
            0x0BAE, 0x0BB9, 0x0BBE, 0x0BC2, 0x0BC6, 0x0BC8, 0x0BCA, 0x0BCC,
            0x0BD0, 0x0BD0, 0x0BD7, 0x0BD7, 0x0C00, 0x0C03, 0x0C05, 0x0C0C,
            0x0C0E, 0x0C10, 0x0C12, 0x0C28, 0x0C2A, 0x0C39, 0x0C3D, 0x0C44,
            0x0C46, 0x0C48, 0x0C4A, 0x0C4C, 0x0C55, 0x0C56, 0x0C58, 0x0C5A,
            0x0C5D, 0x0C5D, 0x0C60, 0x0C63, 0x0C80, 0x0C83, 0x0C85, 0x0C8C,
            0x0C8E, 0x0C90, 0x0C92, 0x0CA8, 0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9,
            0x0CBD, 0x0CC4, 0x0CC6, 0x0CC8, 0x0CCA, 0x0CCC, 0x0CD5, 0x0CD6,
            0x0CDD, 0x0CDE, 0x0CE0, 0x0CE3, 0x0CF1, 0x0CF2, 0x0D00, 0x0D0C,
            0x0D0E, 0x0D10, 0x0D12, 0x0D3A, 0x0D3D, 0x0D44, 0x0D46, 0x0D48,
            0x0D4A, 0x0D4C, 0x0D4E, 0x0D4E, 0x0D54, 0x0D57, 0x0D5F, 0x0D63,
            0x0D7A, 0x0D7F, 0x0D81, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1,
            0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DCF, 0x0DD4,
            0x0DD6, 0x0DD6, 0x0DD8, 0x0DDF, 0x0DF2, 0x0DF3, 0x0E01, 0x0E3A,
            0x0E40, 0x0E46, 0x0E4D, 0x0E4D, 0x0E81, 0x0E82, 0x0E84, 0x0E84,
            0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5, 0x0EA7, 0x0EB9,
            0x0EBB, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0ECD, 0x0ECD,
            0x0EDC, 0x0EDF, 0x0F00, 0x0F00, 0x0F40, 0x0F47, 0x0F49, 0x0F6C,
            0x0F71, 0x0F81, 0x0F88, 0x0F97, 0x0F99, 0x0FBC, 0x1000, 0x1036,
            0x1038, 0x1038, 0x103B, 0x103F, 0x1050, 0x108F, 0x109A, 0x109D,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA,
            0x10FC, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258,
            0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D, 0x1290, 0x12B0,
            0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0, 0x12C2, 0x12C5,
            0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315, 0x1318, 0x135A,
            0x1380, 0x138F, 0x13A0, 0x13F5, 0x13F8, 0x13FD, 0x1401, 0x166C,
            0x166F, 0x167F, 0x1681, 0x169A, 0x16A0, 0x16EA, 0x16EE, 0x16F8,
            0x1700, 0x1713, 0x171F, 0x1733, 0x1740, 0x1753, 0x1760, 0x176C,
            0x176E, 0x1770, 0x1772, 0x1773, 0x1780, 0x17B3, 0x17B6, 0x17C8,
            0x17D7, 0x17D7, 0x17DC, 0x17DC, 0x1820, 0x1878, 0x1880, 0x18AA,
            0x18B0, 0x18F5, 0x1900, 0x191E, 0x1920, 0x192B, 0x1930, 0x1938,
            0x1950, 0x196D, 0x1970, 0x1974, 0x1980, 0x19AB, 0x19B0, 0x19C9,
            0x1A00, 0x1A1B, 0x1A20, 0x1A5E, 0x1A61, 0x1A74, 0x1AA7, 0x1AA7,
            0x1ABF, 0x1AC0, 0x1ACC, 0x1ACE, 0x1B00, 0x1B33, 0x1B35, 0x1B43,
            0x1B45, 0x1B4C, 0x1B80, 0x1BA9, 0x1BAC, 0x1BAF, 0x1BBA, 0x1BE5,
            0x1BE7, 0x1BF1, 0x1C00, 0x1C36, 0x1C4D, 0x1C4F, 0x1C5A, 0x1C7D,
            0x1C80, 0x1C88, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1CE9, 0x1CEC,
            0x1CEE, 0x1CF3, 0x1CF5, 0x1CF6, 0x1CFA, 0x1CFA, 0x1D00, 0x1DBF,
            0x1DE7, 0x1DF4, 0x1E00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45,
            0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B,
            0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FBC,
            0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC, 0x1FD0, 0x1FD3,
            0x1FD6, 0x1FDB, 0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFC,
            0x2071, 0x2071, 0x207F, 0x207F, 0x2090, 0x209C, 0x2102, 0x2102,
            0x2107, 0x2107, 0x210A, 0x2113, 0x2115, 0x2115, 0x2119, 0x211D,
            0x2124, 0x2124, 0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x212D,
            0x212F, 0x2139, 0x213C, 0x213F, 0x2145, 0x2149, 0x214E, 0x214E,
            0x2160, 0x2188, 0x24B6, 0x24E9, 0x2C00, 0x2CE4, 0x2CEB, 0x2CEE,
            0x2CF2, 0x2CF3, 0x2D00, 0x2D25, 0x2D27, 0x2D27, 0x2D2D, 0x2D2D,
            0x2D30, 0x2D67, 0x2D6F, 0x2D6F, 0x2D80, 0x2D96, 0x2DA0, 0x2DA6,
            0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6, 0x2DB8, 0x2DBE, 0x2DC0, 0x2DC6,
            0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6, 0x2DD8, 0x2DDE, 0x2DE0, 0x2DFF,
            0x2E2F, 0x2E2F, 0x3005, 0x3007, 0x3021, 0x3029, 0x3031, 0x3035,
            0x3038, 0x303C, 0x3041, 0x3096, 0x309D, 0x309F, 0x30A1, 0x30FA,
            0x30FC, 0x30FF, 0x3105, 0x312F, 0x3131, 0x318E, 0x31A0, 0x31BF,
            0x31F0, 0x31FF, 0x3400, 0x4DBF, 0x4E00, 0xA48C, 0xA4D0, 0xA4FD,
            0xA500, 0xA60C, 0xA610, 0xA61F, 0xA62A, 0xA62B, 0xA640, 0xA66E,
            0xA674, 0xA67B, 0xA67F, 0xA6EF, 0xA717, 0xA71F, 0xA722, 0xA788,
            0xA78B, 0xA7CA, 0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9,
            0xA7F2, 0xA805, 0xA807, 0xA827, 0xA840, 0xA873, 0xA880, 0xA8C3,
            0xA8C5, 0xA8C5, 0xA8F2, 0xA8F7, 0xA8FB, 0xA8FB, 0xA8FD, 0xA8FF,
            0xA90A, 0xA92A, 0xA930, 0xA952, 0xA960, 0xA97C, 0xA980, 0xA9B2,
            0xA9B4, 0xA9BF, 0xA9CF, 0xA9CF, 0xA9E0, 0xA9EF, 0xA9FA, 0xA9FE,
            0xAA00, 0xAA36, 0xAA40, 0xAA4D, 0xAA60, 0xAA76, 0xAA7A, 0xAABE,
            0xAAC0, 0xAAC0, 0xAAC2, 0xAAC2, 0xAADB, 0xAADD, 0xAAE0, 0xAAEF,
            0xAAF2, 0xAAF5, 0xAB01, 0xAB06, 0xAB09, 0xAB0E, 0xAB11, 0xAB16,
            0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xAB30, 0xAB5A, 0xAB5C, 0xAB69,
            0xAB70, 0xABEA, 0xAC00, 0xD7A3, 0xD7B0, 0xD7C6, 0xD7CB, 0xD7FB,
            0xF900, 0xFA6D, 0xFA70, 0xFAD9, 0xFB00, 0xFB06, 0xFB13, 0xFB17,
            0xFB1D, 0xFB28, 0xFB2A, 0xFB36, 0xFB38, 0xFB3C, 0xFB3E, 0xFB3E,
            0xFB40, 0xFB41, 0xFB43, 0xFB44, 0xFB46, 0xFBB1, 0xFBD3, 0xFD3D,
            0xFD50, 0xFD8F, 0xFD92, 0xFDC7, 0xFDF0, 0xFDFB, 0xFE70, 0xFE74,
            0xFE76, 0xFEFC, 0xFF21, 0xFF3A, 0xFF41, 0xFF5A, 0xFF66, 0xFFBE,
            0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC,
            0x10000, 0x1000B, 0x1000D, 0x10026, 0x10028, 0x1003A, 0x1003C, 0x1003D,
            0x1003F, 0x1004D, 0x10050, 0x1005D, 0x10080, 0x100FA, 0x10140, 0x10174,
            0x10280, 0x1029C, 0x102A0, 0x102D0, 0x10300, 0x1031F, 0x1032D, 0x1034A,
            0x10350, 0x1037A, 0x10380, 0x1039D, 0x103A0, 0x103C3, 0x103C8, 0x103CF,
            0x103D1, 0x103D5, 0x10400, 0x1049D, 0x104B0, 0x104D3, 0x104D8, 0x104FB,
            0x10500, 0x10527, 0x10530, 0x10563, 0x10570, 0x1057A, 0x1057C, 0x1058A,
            0x1058C, 0x10592, 0x10594, 0x10595, 0x10597, 0x105A1, 0x105A3, 0x105B1,
            0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10600, 0x10736, 0x10740, 0x10755,
            0x10760, 0x10767, 0x10780, 0x10785, 0x10787, 0x107B0, 0x107B2, 0x107BA,
            0x10800, 0x10805, 0x10808, 0x10808, 0x1080A, 0x10835, 0x10837, 0x10838,
            0x1083C, 0x1083C, 0x1083F, 0x10855, 0x10860, 0x10876, 0x10880, 0x1089E,
            0x108E0, 0x108F2, 0x108F4, 0x108F5, 0x10900, 0x10915, 0x10920, 0x10939,
            0x10980, 0x109B7, 0x109BE, 0x109BF, 0x10A00, 0x10A03, 0x10A05, 0x10A06,
            0x10A0C, 0x10A13, 0x10A15, 0x10A17, 0x10A19, 0x10A35, 0x10A60, 0x10A7C,
            0x10A80, 0x10A9C, 0x10AC0, 0x10AC7, 0x10AC9, 0x10AE4, 0x10B00, 0x10B35,
            0x10B40, 0x10B55, 0x10B60, 0x10B72, 0x10B80, 0x10B91, 0x10C00, 0x10C48,
            0x10C80, 0x10CB2, 0x10CC0, 0x10CF2, 0x10D00, 0x10D27, 0x10E80, 0x10EA9,
            0x10EAB, 0x10EAC, 0x10EB0, 0x10EB1, 0x10F00, 0x10F1C, 0x10F27, 0x10F27,
            0x10F30, 0x10F45, 0x10F70, 0x10F81, 0x10FB0, 0x10FC4, 0x10FE0, 0x10FF6,
            0x11000, 0x11045, 0x11071, 0x11075, 0x11082, 0x110B8, 0x110C2, 0x110C2,
            0x110D0, 0x110E8, 0x11100, 0x11132, 0x11144, 0x11147, 0x11150, 0x11172,
            0x11176, 0x11176, 0x11180, 0x111BF, 0x111C1, 0x111C4, 0x111CE, 0x111CF,
            0x111DA, 0x111DA, 0x111DC, 0x111DC, 0x11200, 0x11211, 0x11213, 0x11234,
            0x11237, 0x11237, 0x1123E, 0x1123E, 0x11280, 0x11286, 0x11288, 0x11288,
            0x1128A, 0x1128D, 0x1128F, 0x1129D, 0x1129F, 0x112A8, 0x112B0, 0x112E8,
            0x11300, 0x11303, 0x11305, 0x1130C, 0x1130F, 0x11310, 0x11313, 0x11328,
            0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339, 0x1133D, 0x11344,
            0x11347, 0x11348, 0x1134B, 0x1134C, 0x11350, 0x11350, 0x11357, 0x11357,
            0x1135D, 0x11363, 0x11400, 0x11441, 0x11443, 0x11445, 0x11447, 0x1144A,
            0x1145F, 0x11461, 0x11480, 0x114C1, 0x114C4, 0x114C5, 0x114C7, 0x114C7,
            0x11580, 0x115B5, 0x115B8, 0x115BE, 0x115D8, 0x115DD, 0x11600, 0x1163E,
            0x11640, 0x11640, 0x11644, 0x11644, 0x11680, 0x116B5, 0x116B8, 0x116B8,
            0x11700, 0x1171A, 0x1171D, 0x1172A, 0x11740, 0x11746, 0x11800, 0x11838,
            0x118A0, 0x118DF, 0x118FF, 0x11906, 0x11909, 0x11909, 0x1190C, 0x11913,
            0x11915, 0x11916, 0x11918, 0x11935, 0x11937, 0x11938, 0x1193B, 0x1193C,
            0x1193F, 0x11942, 0x119A0, 0x119A7, 0x119AA, 0x119D7, 0x119DA, 0x119DF,
            0x119E1, 0x119E1, 0x119E3, 0x119E4, 0x11A00, 0x11A32, 0x11A35, 0x11A3E,
            0x11A50, 0x11A97, 0x11A9D, 0x11A9D, 0x11AB0, 0x11AF8, 0x11C00, 0x11C08,
            0x11C0A, 0x11C36, 0x11C38, 0x11C3E, 0x11C40, 0x11C40, 0x11C72, 0x11C8F,
            0x11C92, 0x11CA7, 0x11CA9, 0x11CB6, 0x11D00, 0x11D06, 0x11D08, 0x11D09,
            0x11D0B, 0x11D36, 0x11D3A, 0x11D3A, 0x11D3C, 0x11D3D, 0x11D3F, 0x11D41,
            0x11D43, 0x11D43, 0x11D46, 0x11D47, 0x11D60, 0x11D65, 0x11D67, 0x11D68,
            0x11D6A, 0x11D8E, 0x11D90, 0x11D91, 0x11D93, 0x11D96, 0x11D98, 0x11D98,
            0x11EE0, 0x11EF6, 0x11FB0, 0x11FB0, 0x12000, 0x12399, 0x12400, 0x1246E,
            0x12480, 0x12543, 0x12F90, 0x12FF0, 0x13000, 0x1342E, 0x14400, 0x14646,
            0x16800, 0x16A38, 0x16A40, 0x16A5E, 0x16A70, 0x16ABE, 0x16AD0, 0x16AED,
            0x16B00, 0x16B2F, 0x16B40, 0x16B43, 0x16B63, 0x16B77, 0x16B7D, 0x16B8F,
            0x16E40, 0x16E7F, 0x16F00, 0x16F4A, 0x16F4F, 0x16F87, 0x16F8F, 0x16F9F,
            0x16FE0, 0x16FE1, 0x16FE3, 0x16FE3, 0x16FF0, 0x16FF1, 0x17000, 0x187F7,
            0x18800, 0x18CD5, 0x18D00, 0x18D08, 0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB,
            0x1AFFD, 0x1AFFE, 0x1B000, 0x1B122, 0x1B150, 0x1B152, 0x1B164, 0x1B167,
            0x1B170, 0x1B2FB, 0x1BC00, 0x1BC6A, 0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88,
            0x1BC90, 0x1BC99, 0x1BC9E, 0x1BC9E, 0x1D400, 0x1D454, 0x1D456, 0x1D49C,
            0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC,
            0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505,
            0x1D507, 0x1D50A, 0x1D50D, 0x1D514, 0x1D516, 0x1D51C, 0x1D51E, 0x1D539,
            0x1D53B, 0x1D53E, 0x1D540, 0x1D544, 0x1D546, 0x1D546, 0x1D54A, 0x1D550,
            0x1D552, 0x1D6A5, 0x1D6A8, 0x1D6C0, 0x1D6C2, 0x1D6DA, 0x1D6DC, 0x1D6FA,
            0x1D6FC, 0x1D714, 0x1D716, 0x1D734, 0x1D736, 0x1D74E, 0x1D750, 0x1D76E,
            0x1D770, 0x1D788, 0x1D78A, 0x1D7A8, 0x1D7AA, 0x1D7C2, 0x1D7C4, 0x1D7CB,
            0x1DF00, 0x1DF1E, 0x1E000, 0x1E006, 0x1E008, 0x1E018, 0x1E01B, 0x1E021,
            0x1E023, 0x1E024, 0x1E026, 0x1E02A, 0x1E100, 0x1E12C, 0x1E137, 0x1E13D,
            0x1E14E, 0x1E14E, 0x1E290, 0x1E2AD, 0x1E2C0, 0x1E2EB, 0x1E7E0, 0x1E7E6,
            0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE, 0x1E7F0, 0x1E7FE, 0x1E800, 0x1E8C4,
            0x1E900, 0x1E943, 0x1E947, 0x1E947, 0x1E94B, 0x1E94B, 0x1EE00, 0x1EE03,
            0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22, 0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27,
            0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37, 0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B,
            0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47, 0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B,
            0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52, 0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57,
            0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B, 0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F,
            0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64, 0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72,
            0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C, 0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89,
            0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3, 0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB,
            0x1F130, 0x1F149, 0x1F150, 0x1F169, 0x1F170, 0x1F189, 0x20000, 0x2A6DF,
            0x2A700, 0x2B738, 0x2B740, 0x2B81D, 0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0,
            0x2F800, 0x2FA1D, 0x30000, 0x3134A,
            //  #42 (4694+1): bp=Any
            0x0000, 0x10FFFF,
            //  #43 (4695+0): bp=Assigned

            //  #44 (4695+4): bp=Bidi_Control:Bidi_C
            0x061C, 0x061C, 0x200E, 0x200F, 0x202A, 0x202E, 0x2066, 0x2069,
            //  #45 (4699+114): bp=Bidi_Mirrored:Bidi_M
            0x0028, 0x0029, 0x003C, 0x003C, 0x003E, 0x003E, 0x005B, 0x005B,
            0x005D, 0x005D, 0x007B, 0x007B, 0x007D, 0x007D, 0x00AB, 0x00AB,
            0x00BB, 0x00BB, 0x0F3A, 0x0F3D, 0x169B, 0x169C, 0x2039, 0x203A,
            0x2045, 0x2046, 0x207D, 0x207E, 0x208D, 0x208E, 0x2140, 0x2140,
            0x2201, 0x2204, 0x2208, 0x220D, 0x2211, 0x2211, 0x2215, 0x2216,
            0x221A, 0x221D, 0x221F, 0x2222, 0x2224, 0x2224, 0x2226, 0x2226,
            0x222B, 0x2233, 0x2239, 0x2239, 0x223B, 0x224C, 0x2252, 0x2255,
            0x225F, 0x2260, 0x2262, 0x2262, 0x2264, 0x226B, 0x226E, 0x228C,
            0x228F, 0x2292, 0x2298, 0x2298, 0x22A2, 0x22A3, 0x22A6, 0x22B8,
            0x22BE, 0x22BF, 0x22C9, 0x22CD, 0x22D0, 0x22D1, 0x22D6, 0x22ED,
            0x22F0, 0x22FF, 0x2308, 0x230B, 0x2320, 0x2321, 0x2329, 0x232A,
            0x2768, 0x2775, 0x27C0, 0x27C0, 0x27C3, 0x27C6, 0x27C8, 0x27C9,
            0x27CB, 0x27CD, 0x27D3, 0x27D6, 0x27DC, 0x27DE, 0x27E2, 0x27EF,
            0x2983, 0x2998, 0x299B, 0x29A0, 0x29A2, 0x29AF, 0x29B8, 0x29B8,
            0x29C0, 0x29C5, 0x29C9, 0x29C9, 0x29CE, 0x29D2, 0x29D4, 0x29D5,
            0x29D8, 0x29DC, 0x29E1, 0x29E1, 0x29E3, 0x29E5, 0x29E8, 0x29E9,
            0x29F4, 0x29F9, 0x29FC, 0x29FD, 0x2A0A, 0x2A1C, 0x2A1E, 0x2A21,
            0x2A24, 0x2A24, 0x2A26, 0x2A26, 0x2A29, 0x2A29, 0x2A2B, 0x2A2E,
            0x2A34, 0x2A35, 0x2A3C, 0x2A3E, 0x2A57, 0x2A58, 0x2A64, 0x2A65,
            0x2A6A, 0x2A6D, 0x2A6F, 0x2A70, 0x2A73, 0x2A74, 0x2A79, 0x2AA3,
            0x2AA6, 0x2AAD, 0x2AAF, 0x2AD6, 0x2ADC, 0x2ADC, 0x2ADE, 0x2ADE,
            0x2AE2, 0x2AE6, 0x2AEC, 0x2AEE, 0x2AF3, 0x2AF3, 0x2AF7, 0x2AFB,
            0x2AFD, 0x2AFD, 0x2BFE, 0x2BFE, 0x2E02, 0x2E05, 0x2E09, 0x2E0A,
            0x2E0C, 0x2E0D, 0x2E1C, 0x2E1D, 0x2E20, 0x2E29, 0x2E55, 0x2E5C,
            0x3008, 0x3011, 0x3014, 0x301B, 0xFE59, 0xFE5E, 0xFE64, 0xFE65,
            0xFF08, 0xFF09, 0xFF1C, 0xFF1C, 0xFF1E, 0xFF1E, 0xFF3B, 0xFF3B,
            0xFF3D, 0xFF3D, 0xFF5B, 0xFF5B, 0xFF5D, 0xFF5D, 0xFF5F, 0xFF60,
            0xFF62, 0xFF63, 0x1D6DB, 0x1D6DB, 0x1D715, 0x1D715, 0x1D74F, 0x1D74F,
            0x1D789, 0x1D789, 0x1D7C3, 0x1D7C3,
            //  #46 (4813+427): bp=Case_Ignorable:CI
            0x0027, 0x0027, 0x002E, 0x002E, 0x003A, 0x003A, 0x005E, 0x005E,
            0x0060, 0x0060, 0x00A8, 0x00A8, 0x00AD, 0x00AD, 0x00AF, 0x00AF,
            0x00B4, 0x00B4, 0x00B7, 0x00B8, 0x02B0, 0x036F, 0x0374, 0x0375,
            0x037A, 0x037A, 0x0384, 0x0385, 0x0387, 0x0387, 0x0483, 0x0489,
            0x0559, 0x0559, 0x055F, 0x055F, 0x0591, 0x05BD, 0x05BF, 0x05BF,
            0x05C1, 0x05C2, 0x05C4, 0x05C5, 0x05C7, 0x05C7, 0x05F4, 0x05F4,
            0x0600, 0x0605, 0x0610, 0x061A, 0x061C, 0x061C, 0x0640, 0x0640,
            0x064B, 0x065F, 0x0670, 0x0670, 0x06D6, 0x06DD, 0x06DF, 0x06E8,
            0x06EA, 0x06ED, 0x070F, 0x070F, 0x0711, 0x0711, 0x0730, 0x074A,
            0x07A6, 0x07B0, 0x07EB, 0x07F5, 0x07FA, 0x07FA, 0x07FD, 0x07FD,
            0x0816, 0x082D, 0x0859, 0x085B, 0x0888, 0x0888, 0x0890, 0x0891,
            0x0898, 0x089F, 0x08C9, 0x0902, 0x093A, 0x093A, 0x093C, 0x093C,
            0x0941, 0x0948, 0x094D, 0x094D, 0x0951, 0x0957, 0x0962, 0x0963,
            0x0971, 0x0971, 0x0981, 0x0981, 0x09BC, 0x09BC, 0x09C1, 0x09C4,
            0x09CD, 0x09CD, 0x09E2, 0x09E3, 0x09FE, 0x09FE, 0x0A01, 0x0A02,
            0x0A3C, 0x0A3C, 0x0A41, 0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4D,
            0x0A51, 0x0A51, 0x0A70, 0x0A71, 0x0A75, 0x0A75, 0x0A81, 0x0A82,
            0x0ABC, 0x0ABC, 0x0AC1, 0x0AC5, 0x0AC7, 0x0AC8, 0x0ACD, 0x0ACD,
            0x0AE2, 0x0AE3, 0x0AFA, 0x0AFF, 0x0B01, 0x0B01, 0x0B3C, 0x0B3C,
            0x0B3F, 0x0B3F, 0x0B41, 0x0B44, 0x0B4D, 0x0B4D, 0x0B55, 0x0B56,
            0x0B62, 0x0B63, 0x0B82, 0x0B82, 0x0BC0, 0x0BC0, 0x0BCD, 0x0BCD,
            0x0C00, 0x0C00, 0x0C04, 0x0C04, 0x0C3C, 0x0C3C, 0x0C3E, 0x0C40,
            0x0C46, 0x0C48, 0x0C4A, 0x0C4D, 0x0C55, 0x0C56, 0x0C62, 0x0C63,
            0x0C81, 0x0C81, 0x0CBC, 0x0CBC, 0x0CBF, 0x0CBF, 0x0CC6, 0x0CC6,
            0x0CCC, 0x0CCD, 0x0CE2, 0x0CE3, 0x0D00, 0x0D01, 0x0D3B, 0x0D3C,
            0x0D41, 0x0D44, 0x0D4D, 0x0D4D, 0x0D62, 0x0D63, 0x0D81, 0x0D81,
            0x0DCA, 0x0DCA, 0x0DD2, 0x0DD4, 0x0DD6, 0x0DD6, 0x0E31, 0x0E31,
            0x0E34, 0x0E3A, 0x0E46, 0x0E4E, 0x0EB1, 0x0EB1, 0x0EB4, 0x0EBC,
            0x0EC6, 0x0EC6, 0x0EC8, 0x0ECD, 0x0F18, 0x0F19, 0x0F35, 0x0F35,
            0x0F37, 0x0F37, 0x0F39, 0x0F39, 0x0F71, 0x0F7E, 0x0F80, 0x0F84,
            0x0F86, 0x0F87, 0x0F8D, 0x0F97, 0x0F99, 0x0FBC, 0x0FC6, 0x0FC6,
            0x102D, 0x1030, 0x1032, 0x1037, 0x1039, 0x103A, 0x103D, 0x103E,
            0x1058, 0x1059, 0x105E, 0x1060, 0x1071, 0x1074, 0x1082, 0x1082,
            0x1085, 0x1086, 0x108D, 0x108D, 0x109D, 0x109D, 0x10FC, 0x10FC,
            0x135D, 0x135F, 0x1712, 0x1714, 0x1732, 0x1733, 0x1752, 0x1753,
            0x1772, 0x1773, 0x17B4, 0x17B5, 0x17B7, 0x17BD, 0x17C6, 0x17C6,
            0x17C9, 0x17D3, 0x17D7, 0x17D7, 0x17DD, 0x17DD, 0x180B, 0x180F,
            0x1843, 0x1843, 0x1885, 0x1886, 0x18A9, 0x18A9, 0x1920, 0x1922,
            0x1927, 0x1928, 0x1932, 0x1932, 0x1939, 0x193B, 0x1A17, 0x1A18,
            0x1A1B, 0x1A1B, 0x1A56, 0x1A56, 0x1A58, 0x1A5E, 0x1A60, 0x1A60,
            0x1A62, 0x1A62, 0x1A65, 0x1A6C, 0x1A73, 0x1A7C, 0x1A7F, 0x1A7F,
            0x1AA7, 0x1AA7, 0x1AB0, 0x1ACE, 0x1B00, 0x1B03, 0x1B34, 0x1B34,
            0x1B36, 0x1B3A, 0x1B3C, 0x1B3C, 0x1B42, 0x1B42, 0x1B6B, 0x1B73,
            0x1B80, 0x1B81, 0x1BA2, 0x1BA5, 0x1BA8, 0x1BA9, 0x1BAB, 0x1BAD,
            0x1BE6, 0x1BE6, 0x1BE8, 0x1BE9, 0x1BED, 0x1BED, 0x1BEF, 0x1BF1,
            0x1C2C, 0x1C33, 0x1C36, 0x1C37, 0x1C78, 0x1C7D, 0x1CD0, 0x1CD2,
            0x1CD4, 0x1CE0, 0x1CE2, 0x1CE8, 0x1CED, 0x1CED, 0x1CF4, 0x1CF4,
            0x1CF8, 0x1CF9, 0x1D2C, 0x1D6A, 0x1D78, 0x1D78, 0x1D9B, 0x1DFF,
            0x1FBD, 0x1FBD, 0x1FBF, 0x1FC1, 0x1FCD, 0x1FCF, 0x1FDD, 0x1FDF,
            0x1FED, 0x1FEF, 0x1FFD, 0x1FFE, 0x200B, 0x200F, 0x2018, 0x2019,
            0x2024, 0x2024, 0x2027, 0x2027, 0x202A, 0x202E, 0x2060, 0x2064,
            0x2066, 0x206F, 0x2071, 0x2071, 0x207F, 0x207F, 0x2090, 0x209C,
            0x20D0, 0x20F0, 0x2C7C, 0x2C7D, 0x2CEF, 0x2CF1, 0x2D6F, 0x2D6F,
            0x2D7F, 0x2D7F, 0x2DE0, 0x2DFF, 0x2E2F, 0x2E2F, 0x3005, 0x3005,
            0x302A, 0x302D, 0x3031, 0x3035, 0x303B, 0x303B, 0x3099, 0x309E,
            0x30FC, 0x30FE, 0xA015, 0xA015, 0xA4F8, 0xA4FD, 0xA60C, 0xA60C,
            0xA66F, 0xA672, 0xA674, 0xA67D, 0xA67F, 0xA67F, 0xA69C, 0xA69F,
            0xA6F0, 0xA6F1, 0xA700, 0xA721, 0xA770, 0xA770, 0xA788, 0xA78A,
            0xA7F2, 0xA7F4, 0xA7F8, 0xA7F9, 0xA802, 0xA802, 0xA806, 0xA806,
            0xA80B, 0xA80B, 0xA825, 0xA826, 0xA82C, 0xA82C, 0xA8C4, 0xA8C5,
            0xA8E0, 0xA8F1, 0xA8FF, 0xA8FF, 0xA926, 0xA92D, 0xA947, 0xA951,
            0xA980, 0xA982, 0xA9B3, 0xA9B3, 0xA9B6, 0xA9B9, 0xA9BC, 0xA9BD,
            0xA9CF, 0xA9CF, 0xA9E5, 0xA9E6, 0xAA29, 0xAA2E, 0xAA31, 0xAA32,
            0xAA35, 0xAA36, 0xAA43, 0xAA43, 0xAA4C, 0xAA4C, 0xAA70, 0xAA70,
            0xAA7C, 0xAA7C, 0xAAB0, 0xAAB0, 0xAAB2, 0xAAB4, 0xAAB7, 0xAAB8,
            0xAABE, 0xAABF, 0xAAC1, 0xAAC1, 0xAADD, 0xAADD, 0xAAEC, 0xAAED,
            0xAAF3, 0xAAF4, 0xAAF6, 0xAAF6, 0xAB5B, 0xAB5F, 0xAB69, 0xAB6B,
            0xABE5, 0xABE5, 0xABE8, 0xABE8, 0xABED, 0xABED, 0xFB1E, 0xFB1E,
            0xFBB2, 0xFBC2, 0xFE00, 0xFE0F, 0xFE13, 0xFE13, 0xFE20, 0xFE2F,
            0xFE52, 0xFE52, 0xFE55, 0xFE55, 0xFEFF, 0xFEFF, 0xFF07, 0xFF07,
            0xFF0E, 0xFF0E, 0xFF1A, 0xFF1A, 0xFF3E, 0xFF3E, 0xFF40, 0xFF40,
            0xFF70, 0xFF70, 0xFF9E, 0xFF9F, 0xFFE3, 0xFFE3, 0xFFF9, 0xFFFB,
            0x101FD, 0x101FD, 0x102E0, 0x102E0, 0x10376, 0x1037A, 0x10780, 0x10785,
            0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10A01, 0x10A03, 0x10A05, 0x10A06,
            0x10A0C, 0x10A0F, 0x10A38, 0x10A3A, 0x10A3F, 0x10A3F, 0x10AE5, 0x10AE6,
            0x10D24, 0x10D27, 0x10EAB, 0x10EAC, 0x10F46, 0x10F50, 0x10F82, 0x10F85,
            0x11001, 0x11001, 0x11038, 0x11046, 0x11070, 0x11070, 0x11073, 0x11074,
            0x1107F, 0x11081, 0x110B3, 0x110B6, 0x110B9, 0x110BA, 0x110BD, 0x110BD,
            0x110C2, 0x110C2, 0x110CD, 0x110CD, 0x11100, 0x11102, 0x11127, 0x1112B,
            0x1112D, 0x11134, 0x11173, 0x11173, 0x11180, 0x11181, 0x111B6, 0x111BE,
            0x111C9, 0x111CC, 0x111CF, 0x111CF, 0x1122F, 0x11231, 0x11234, 0x11234,
            0x11236, 0x11237, 0x1123E, 0x1123E, 0x112DF, 0x112DF, 0x112E3, 0x112EA,
            0x11300, 0x11301, 0x1133B, 0x1133C, 0x11340, 0x11340, 0x11366, 0x1136C,
            0x11370, 0x11374, 0x11438, 0x1143F, 0x11442, 0x11444, 0x11446, 0x11446,
            0x1145E, 0x1145E, 0x114B3, 0x114B8, 0x114BA, 0x114BA, 0x114BF, 0x114C0,
            0x114C2, 0x114C3, 0x115B2, 0x115B5, 0x115BC, 0x115BD, 0x115BF, 0x115C0,
            0x115DC, 0x115DD, 0x11633, 0x1163A, 0x1163D, 0x1163D, 0x1163F, 0x11640,
            0x116AB, 0x116AB, 0x116AD, 0x116AD, 0x116B0, 0x116B5, 0x116B7, 0x116B7,
            0x1171D, 0x1171F, 0x11722, 0x11725, 0x11727, 0x1172B, 0x1182F, 0x11837,
            0x11839, 0x1183A, 0x1193B, 0x1193C, 0x1193E, 0x1193E, 0x11943, 0x11943,
            0x119D4, 0x119D7, 0x119DA, 0x119DB, 0x119E0, 0x119E0, 0x11A01, 0x11A0A,
            0x11A33, 0x11A38, 0x11A3B, 0x11A3E, 0x11A47, 0x11A47, 0x11A51, 0x11A56,
            0x11A59, 0x11A5B, 0x11A8A, 0x11A96, 0x11A98, 0x11A99, 0x11C30, 0x11C36,
            0x11C38, 0x11C3D, 0x11C3F, 0x11C3F, 0x11C92, 0x11CA7, 0x11CAA, 0x11CB0,
            0x11CB2, 0x11CB3, 0x11CB5, 0x11CB6, 0x11D31, 0x11D36, 0x11D3A, 0x11D3A,
            0x11D3C, 0x11D3D, 0x11D3F, 0x11D45, 0x11D47, 0x11D47, 0x11D90, 0x11D91,
            0x11D95, 0x11D95, 0x11D97, 0x11D97, 0x11EF3, 0x11EF4, 0x13430, 0x13438,
            0x16AF0, 0x16AF4, 0x16B30, 0x16B36, 0x16B40, 0x16B43, 0x16F4F, 0x16F4F,
            0x16F8F, 0x16F9F, 0x16FE0, 0x16FE1, 0x16FE3, 0x16FE4, 0x1AFF0, 0x1AFF3,
            0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE, 0x1BC9D, 0x1BC9E, 0x1BCA0, 0x1BCA3,
            0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46, 0x1D167, 0x1D169, 0x1D173, 0x1D182,
            0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD, 0x1D242, 0x1D244, 0x1DA00, 0x1DA36,
            0x1DA3B, 0x1DA6C, 0x1DA75, 0x1DA75, 0x1DA84, 0x1DA84, 0x1DA9B, 0x1DA9F,
            0x1DAA1, 0x1DAAF, 0x1E000, 0x1E006, 0x1E008, 0x1E018, 0x1E01B, 0x1E021,
            0x1E023, 0x1E024, 0x1E026, 0x1E02A, 0x1E130, 0x1E13D, 0x1E2AE, 0x1E2AE,
            0x1E2EC, 0x1E2EF, 0x1E8D0, 0x1E8D6, 0x1E944, 0x1E94B, 0x1F3FB, 0x1F3FF,
            0xE0001, 0xE0001, 0xE0020, 0xE007F, 0xE0100, 0xE01EF,
            //  #47 (5240+155): bp=Cased
            0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00B5, 0x00B5,
            0x00BA, 0x00BA, 0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x01BA,
            0x01BC, 0x01BF, 0x01C4, 0x0293, 0x0295, 0x02B8, 0x02C0, 0x02C1,
            0x02E0, 0x02E4, 0x0345, 0x0345, 0x0370, 0x0373, 0x0376, 0x0377,
            0x037A, 0x037D, 0x037F, 0x037F, 0x0386, 0x0386, 0x0388, 0x038A,
            0x038C, 0x038C, 0x038E, 0x03A1, 0x03A3, 0x03F5, 0x03F7, 0x0481,
            0x048A, 0x052F, 0x0531, 0x0556, 0x0560, 0x0588, 0x10A0, 0x10C5,
            0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA, 0x10FD, 0x10FF,
            0x13A0, 0x13F5, 0x13F8, 0x13FD, 0x1C80, 0x1C88, 0x1C90, 0x1CBA,
            0x1CBD, 0x1CBF, 0x1D00, 0x1DBF, 0x1E00, 0x1F15, 0x1F18, 0x1F1D,
            0x1F20, 0x1F45, 0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59,
            0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4,
            0x1FB6, 0x1FBC, 0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC,
            0x1FD0, 0x1FD3, 0x1FD6, 0x1FDB, 0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4,
            0x1FF6, 0x1FFC, 0x2071, 0x2071, 0x207F, 0x207F, 0x2090, 0x209C,
            0x2102, 0x2102, 0x2107, 0x2107, 0x210A, 0x2113, 0x2115, 0x2115,
            0x2119, 0x211D, 0x2124, 0x2124, 0x2126, 0x2126, 0x2128, 0x2128,
            0x212A, 0x212D, 0x212F, 0x2134, 0x2139, 0x2139, 0x213C, 0x213F,
            0x2145, 0x2149, 0x214E, 0x214E, 0x2160, 0x217F, 0x2183, 0x2184,
            0x24B6, 0x24E9, 0x2C00, 0x2CE4, 0x2CEB, 0x2CEE, 0x2CF2, 0x2CF3,
            0x2D00, 0x2D25, 0x2D27, 0x2D27, 0x2D2D, 0x2D2D, 0xA640, 0xA66D,
            0xA680, 0xA69D, 0xA722, 0xA787, 0xA78B, 0xA78E, 0xA790, 0xA7CA,
            0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9, 0xA7F5, 0xA7F6,
            0xA7F8, 0xA7FA, 0xAB30, 0xAB5A, 0xAB5C, 0xAB68, 0xAB70, 0xABBF,
            0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFF21, 0xFF3A, 0xFF41, 0xFF5A,
            0x10400, 0x1044F, 0x104B0, 0x104D3, 0x104D8, 0x104FB, 0x10570, 0x1057A,
            0x1057C, 0x1058A, 0x1058C, 0x10592, 0x10594, 0x10595, 0x10597, 0x105A1,
            0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10780, 0x10780,
            0x10783, 0x10785, 0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10C80, 0x10CB2,
            0x10CC0, 0x10CF2, 0x118A0, 0x118DF, 0x16E40, 0x16E7F, 0x1D400, 0x1D454,
            0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6,
            0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3,
            0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514, 0x1D516, 0x1D51C,
            0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544, 0x1D546, 0x1D546,
            0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D6C0, 0x1D6C2, 0x1D6DA,
            0x1D6DC, 0x1D6FA, 0x1D6FC, 0x1D714, 0x1D716, 0x1D734, 0x1D736, 0x1D74E,
            0x1D750, 0x1D76E, 0x1D770, 0x1D788, 0x1D78A, 0x1D7A8, 0x1D7AA, 0x1D7C2,
            0x1D7C4, 0x1D7CB, 0x1DF00, 0x1DF09, 0x1DF0B, 0x1DF1E, 0x1E900, 0x1E943,
            0x1F130, 0x1F149, 0x1F150, 0x1F169, 0x1F170, 0x1F189,
            //  #48 (5395+622): bp=Changes_When_Casefolded:CWCF
            0x0041, 0x005A, 0x00B5, 0x00B5, 0x00C0, 0x00D6, 0x00D8, 0x00DF,
            0x0100, 0x0100, 0x0102, 0x0102, 0x0104, 0x0104, 0x0106, 0x0106,
            0x0108, 0x0108, 0x010A, 0x010A, 0x010C, 0x010C, 0x010E, 0x010E,
            0x0110, 0x0110, 0x0112, 0x0112, 0x0114, 0x0114, 0x0116, 0x0116,
            0x0118, 0x0118, 0x011A, 0x011A, 0x011C, 0x011C, 0x011E, 0x011E,
            0x0120, 0x0120, 0x0122, 0x0122, 0x0124, 0x0124, 0x0126, 0x0126,
            0x0128, 0x0128, 0x012A, 0x012A, 0x012C, 0x012C, 0x012E, 0x012E,
            0x0130, 0x0130, 0x0132, 0x0132, 0x0134, 0x0134, 0x0136, 0x0136,
            0x0139, 0x0139, 0x013B, 0x013B, 0x013D, 0x013D, 0x013F, 0x013F,
            0x0141, 0x0141, 0x0143, 0x0143, 0x0145, 0x0145, 0x0147, 0x0147,
            0x0149, 0x014A, 0x014C, 0x014C, 0x014E, 0x014E, 0x0150, 0x0150,
            0x0152, 0x0152, 0x0154, 0x0154, 0x0156, 0x0156, 0x0158, 0x0158,
            0x015A, 0x015A, 0x015C, 0x015C, 0x015E, 0x015E, 0x0160, 0x0160,
            0x0162, 0x0162, 0x0164, 0x0164, 0x0166, 0x0166, 0x0168, 0x0168,
            0x016A, 0x016A, 0x016C, 0x016C, 0x016E, 0x016E, 0x0170, 0x0170,
            0x0172, 0x0172, 0x0174, 0x0174, 0x0176, 0x0176, 0x0178, 0x0179,
            0x017B, 0x017B, 0x017D, 0x017D, 0x017F, 0x017F, 0x0181, 0x0182,
            0x0184, 0x0184, 0x0186, 0x0187, 0x0189, 0x018B, 0x018E, 0x0191,
            0x0193, 0x0194, 0x0196, 0x0198, 0x019C, 0x019D, 0x019F, 0x01A0,
            0x01A2, 0x01A2, 0x01A4, 0x01A4, 0x01A6, 0x01A7, 0x01A9, 0x01A9,
            0x01AC, 0x01AC, 0x01AE, 0x01AF, 0x01B1, 0x01B3, 0x01B5, 0x01B5,
            0x01B7, 0x01B8, 0x01BC, 0x01BC, 0x01C4, 0x01C5, 0x01C7, 0x01C8,
            0x01CA, 0x01CB, 0x01CD, 0x01CD, 0x01CF, 0x01CF, 0x01D1, 0x01D1,
            0x01D3, 0x01D3, 0x01D5, 0x01D5, 0x01D7, 0x01D7, 0x01D9, 0x01D9,
            0x01DB, 0x01DB, 0x01DE, 0x01DE, 0x01E0, 0x01E0, 0x01E2, 0x01E2,
            0x01E4, 0x01E4, 0x01E6, 0x01E6, 0x01E8, 0x01E8, 0x01EA, 0x01EA,
            0x01EC, 0x01EC, 0x01EE, 0x01EE, 0x01F1, 0x01F2, 0x01F4, 0x01F4,
            0x01F6, 0x01F8, 0x01FA, 0x01FA, 0x01FC, 0x01FC, 0x01FE, 0x01FE,
            0x0200, 0x0200, 0x0202, 0x0202, 0x0204, 0x0204, 0x0206, 0x0206,
            0x0208, 0x0208, 0x020A, 0x020A, 0x020C, 0x020C, 0x020E, 0x020E,
            0x0210, 0x0210, 0x0212, 0x0212, 0x0214, 0x0214, 0x0216, 0x0216,
            0x0218, 0x0218, 0x021A, 0x021A, 0x021C, 0x021C, 0x021E, 0x021E,
            0x0220, 0x0220, 0x0222, 0x0222, 0x0224, 0x0224, 0x0226, 0x0226,
            0x0228, 0x0228, 0x022A, 0x022A, 0x022C, 0x022C, 0x022E, 0x022E,
            0x0230, 0x0230, 0x0232, 0x0232, 0x023A, 0x023B, 0x023D, 0x023E,
            0x0241, 0x0241, 0x0243, 0x0246, 0x0248, 0x0248, 0x024A, 0x024A,
            0x024C, 0x024C, 0x024E, 0x024E, 0x0345, 0x0345, 0x0370, 0x0370,
            0x0372, 0x0372, 0x0376, 0x0376, 0x037F, 0x037F, 0x0386, 0x0386,
            0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x038F, 0x0391, 0x03A1,
            0x03A3, 0x03AB, 0x03C2, 0x03C2, 0x03CF, 0x03D1, 0x03D5, 0x03D6,
            0x03D8, 0x03D8, 0x03DA, 0x03DA, 0x03DC, 0x03DC, 0x03DE, 0x03DE,
            0x03E0, 0x03E0, 0x03E2, 0x03E2, 0x03E4, 0x03E4, 0x03E6, 0x03E6,
            0x03E8, 0x03E8, 0x03EA, 0x03EA, 0x03EC, 0x03EC, 0x03EE, 0x03EE,
            0x03F0, 0x03F1, 0x03F4, 0x03F5, 0x03F7, 0x03F7, 0x03F9, 0x03FA,
            0x03FD, 0x042F, 0x0460, 0x0460, 0x0462, 0x0462, 0x0464, 0x0464,
            0x0466, 0x0466, 0x0468, 0x0468, 0x046A, 0x046A, 0x046C, 0x046C,
            0x046E, 0x046E, 0x0470, 0x0470, 0x0472, 0x0472, 0x0474, 0x0474,
            0x0476, 0x0476, 0x0478, 0x0478, 0x047A, 0x047A, 0x047C, 0x047C,
            0x047E, 0x047E, 0x0480, 0x0480, 0x048A, 0x048A, 0x048C, 0x048C,
            0x048E, 0x048E, 0x0490, 0x0490, 0x0492, 0x0492, 0x0494, 0x0494,
            0x0496, 0x0496, 0x0498, 0x0498, 0x049A, 0x049A, 0x049C, 0x049C,
            0x049E, 0x049E, 0x04A0, 0x04A0, 0x04A2, 0x04A2, 0x04A4, 0x04A4,
            0x04A6, 0x04A6, 0x04A8, 0x04A8, 0x04AA, 0x04AA, 0x04AC, 0x04AC,
            0x04AE, 0x04AE, 0x04B0, 0x04B0, 0x04B2, 0x04B2, 0x04B4, 0x04B4,
            0x04B6, 0x04B6, 0x04B8, 0x04B8, 0x04BA, 0x04BA, 0x04BC, 0x04BC,
            0x04BE, 0x04BE, 0x04C0, 0x04C1, 0x04C3, 0x04C3, 0x04C5, 0x04C5,
            0x04C7, 0x04C7, 0x04C9, 0x04C9, 0x04CB, 0x04CB, 0x04CD, 0x04CD,
            0x04D0, 0x04D0, 0x04D2, 0x04D2, 0x04D4, 0x04D4, 0x04D6, 0x04D6,
            0x04D8, 0x04D8, 0x04DA, 0x04DA, 0x04DC, 0x04DC, 0x04DE, 0x04DE,
            0x04E0, 0x04E0, 0x04E2, 0x04E2, 0x04E4, 0x04E4, 0x04E6, 0x04E6,
            0x04E8, 0x04E8, 0x04EA, 0x04EA, 0x04EC, 0x04EC, 0x04EE, 0x04EE,
            0x04F0, 0x04F0, 0x04F2, 0x04F2, 0x04F4, 0x04F4, 0x04F6, 0x04F6,
            0x04F8, 0x04F8, 0x04FA, 0x04FA, 0x04FC, 0x04FC, 0x04FE, 0x04FE,
            0x0500, 0x0500, 0x0502, 0x0502, 0x0504, 0x0504, 0x0506, 0x0506,
            0x0508, 0x0508, 0x050A, 0x050A, 0x050C, 0x050C, 0x050E, 0x050E,
            0x0510, 0x0510, 0x0512, 0x0512, 0x0514, 0x0514, 0x0516, 0x0516,
            0x0518, 0x0518, 0x051A, 0x051A, 0x051C, 0x051C, 0x051E, 0x051E,
            0x0520, 0x0520, 0x0522, 0x0522, 0x0524, 0x0524, 0x0526, 0x0526,
            0x0528, 0x0528, 0x052A, 0x052A, 0x052C, 0x052C, 0x052E, 0x052E,
            0x0531, 0x0556, 0x0587, 0x0587, 0x10A0, 0x10C5, 0x10C7, 0x10C7,
            0x10CD, 0x10CD, 0x13F8, 0x13FD, 0x1C80, 0x1C88, 0x1C90, 0x1CBA,
            0x1CBD, 0x1CBF, 0x1E00, 0x1E00, 0x1E02, 0x1E02, 0x1E04, 0x1E04,
            0x1E06, 0x1E06, 0x1E08, 0x1E08, 0x1E0A, 0x1E0A, 0x1E0C, 0x1E0C,
            0x1E0E, 0x1E0E, 0x1E10, 0x1E10, 0x1E12, 0x1E12, 0x1E14, 0x1E14,
            0x1E16, 0x1E16, 0x1E18, 0x1E18, 0x1E1A, 0x1E1A, 0x1E1C, 0x1E1C,
            0x1E1E, 0x1E1E, 0x1E20, 0x1E20, 0x1E22, 0x1E22, 0x1E24, 0x1E24,
            0x1E26, 0x1E26, 0x1E28, 0x1E28, 0x1E2A, 0x1E2A, 0x1E2C, 0x1E2C,
            0x1E2E, 0x1E2E, 0x1E30, 0x1E30, 0x1E32, 0x1E32, 0x1E34, 0x1E34,
            0x1E36, 0x1E36, 0x1E38, 0x1E38, 0x1E3A, 0x1E3A, 0x1E3C, 0x1E3C,
            0x1E3E, 0x1E3E, 0x1E40, 0x1E40, 0x1E42, 0x1E42, 0x1E44, 0x1E44,
            0x1E46, 0x1E46, 0x1E48, 0x1E48, 0x1E4A, 0x1E4A, 0x1E4C, 0x1E4C,
            0x1E4E, 0x1E4E, 0x1E50, 0x1E50, 0x1E52, 0x1E52, 0x1E54, 0x1E54,
            0x1E56, 0x1E56, 0x1E58, 0x1E58, 0x1E5A, 0x1E5A, 0x1E5C, 0x1E5C,
            0x1E5E, 0x1E5E, 0x1E60, 0x1E60, 0x1E62, 0x1E62, 0x1E64, 0x1E64,
            0x1E66, 0x1E66, 0x1E68, 0x1E68, 0x1E6A, 0x1E6A, 0x1E6C, 0x1E6C,
            0x1E6E, 0x1E6E, 0x1E70, 0x1E70, 0x1E72, 0x1E72, 0x1E74, 0x1E74,
            0x1E76, 0x1E76, 0x1E78, 0x1E78, 0x1E7A, 0x1E7A, 0x1E7C, 0x1E7C,
            0x1E7E, 0x1E7E, 0x1E80, 0x1E80, 0x1E82, 0x1E82, 0x1E84, 0x1E84,
            0x1E86, 0x1E86, 0x1E88, 0x1E88, 0x1E8A, 0x1E8A, 0x1E8C, 0x1E8C,
            0x1E8E, 0x1E8E, 0x1E90, 0x1E90, 0x1E92, 0x1E92, 0x1E94, 0x1E94,
            0x1E9A, 0x1E9B, 0x1E9E, 0x1E9E, 0x1EA0, 0x1EA0, 0x1EA2, 0x1EA2,
            0x1EA4, 0x1EA4, 0x1EA6, 0x1EA6, 0x1EA8, 0x1EA8, 0x1EAA, 0x1EAA,
            0x1EAC, 0x1EAC, 0x1EAE, 0x1EAE, 0x1EB0, 0x1EB0, 0x1EB2, 0x1EB2,
            0x1EB4, 0x1EB4, 0x1EB6, 0x1EB6, 0x1EB8, 0x1EB8, 0x1EBA, 0x1EBA,
            0x1EBC, 0x1EBC, 0x1EBE, 0x1EBE, 0x1EC0, 0x1EC0, 0x1EC2, 0x1EC2,
            0x1EC4, 0x1EC4, 0x1EC6, 0x1EC6, 0x1EC8, 0x1EC8, 0x1ECA, 0x1ECA,
            0x1ECC, 0x1ECC, 0x1ECE, 0x1ECE, 0x1ED0, 0x1ED0, 0x1ED2, 0x1ED2,
            0x1ED4, 0x1ED4, 0x1ED6, 0x1ED6, 0x1ED8, 0x1ED8, 0x1EDA, 0x1EDA,
            0x1EDC, 0x1EDC, 0x1EDE, 0x1EDE, 0x1EE0, 0x1EE0, 0x1EE2, 0x1EE2,
            0x1EE4, 0x1EE4, 0x1EE6, 0x1EE6, 0x1EE8, 0x1EE8, 0x1EEA, 0x1EEA,
            0x1EEC, 0x1EEC, 0x1EEE, 0x1EEE, 0x1EF0, 0x1EF0, 0x1EF2, 0x1EF2,
            0x1EF4, 0x1EF4, 0x1EF6, 0x1EF6, 0x1EF8, 0x1EF8, 0x1EFA, 0x1EFA,
            0x1EFC, 0x1EFC, 0x1EFE, 0x1EFE, 0x1F08, 0x1F0F, 0x1F18, 0x1F1D,
            0x1F28, 0x1F2F, 0x1F38, 0x1F3F, 0x1F48, 0x1F4D, 0x1F59, 0x1F59,
            0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F5F, 0x1F68, 0x1F6F,
            0x1F80, 0x1FAF, 0x1FB2, 0x1FB4, 0x1FB7, 0x1FBC, 0x1FC2, 0x1FC4,
            0x1FC7, 0x1FCC, 0x1FD8, 0x1FDB, 0x1FE8, 0x1FEC, 0x1FF2, 0x1FF4,
            0x1FF7, 0x1FFC, 0x2126, 0x2126, 0x212A, 0x212B, 0x2132, 0x2132,
            0x2160, 0x216F, 0x2183, 0x2183, 0x24B6, 0x24CF, 0x2C00, 0x2C2F,
            0x2C60, 0x2C60, 0x2C62, 0x2C64, 0x2C67, 0x2C67, 0x2C69, 0x2C69,
            0x2C6B, 0x2C6B, 0x2C6D, 0x2C70, 0x2C72, 0x2C72, 0x2C75, 0x2C75,
            0x2C7E, 0x2C80, 0x2C82, 0x2C82, 0x2C84, 0x2C84, 0x2C86, 0x2C86,
            0x2C88, 0x2C88, 0x2C8A, 0x2C8A, 0x2C8C, 0x2C8C, 0x2C8E, 0x2C8E,
            0x2C90, 0x2C90, 0x2C92, 0x2C92, 0x2C94, 0x2C94, 0x2C96, 0x2C96,
            0x2C98, 0x2C98, 0x2C9A, 0x2C9A, 0x2C9C, 0x2C9C, 0x2C9E, 0x2C9E,
            0x2CA0, 0x2CA0, 0x2CA2, 0x2CA2, 0x2CA4, 0x2CA4, 0x2CA6, 0x2CA6,
            0x2CA8, 0x2CA8, 0x2CAA, 0x2CAA, 0x2CAC, 0x2CAC, 0x2CAE, 0x2CAE,
            0x2CB0, 0x2CB0, 0x2CB2, 0x2CB2, 0x2CB4, 0x2CB4, 0x2CB6, 0x2CB6,
            0x2CB8, 0x2CB8, 0x2CBA, 0x2CBA, 0x2CBC, 0x2CBC, 0x2CBE, 0x2CBE,
            0x2CC0, 0x2CC0, 0x2CC2, 0x2CC2, 0x2CC4, 0x2CC4, 0x2CC6, 0x2CC6,
            0x2CC8, 0x2CC8, 0x2CCA, 0x2CCA, 0x2CCC, 0x2CCC, 0x2CCE, 0x2CCE,
            0x2CD0, 0x2CD0, 0x2CD2, 0x2CD2, 0x2CD4, 0x2CD4, 0x2CD6, 0x2CD6,
            0x2CD8, 0x2CD8, 0x2CDA, 0x2CDA, 0x2CDC, 0x2CDC, 0x2CDE, 0x2CDE,
            0x2CE0, 0x2CE0, 0x2CE2, 0x2CE2, 0x2CEB, 0x2CEB, 0x2CED, 0x2CED,
            0x2CF2, 0x2CF2, 0xA640, 0xA640, 0xA642, 0xA642, 0xA644, 0xA644,
            0xA646, 0xA646, 0xA648, 0xA648, 0xA64A, 0xA64A, 0xA64C, 0xA64C,
            0xA64E, 0xA64E, 0xA650, 0xA650, 0xA652, 0xA652, 0xA654, 0xA654,
            0xA656, 0xA656, 0xA658, 0xA658, 0xA65A, 0xA65A, 0xA65C, 0xA65C,
            0xA65E, 0xA65E, 0xA660, 0xA660, 0xA662, 0xA662, 0xA664, 0xA664,
            0xA666, 0xA666, 0xA668, 0xA668, 0xA66A, 0xA66A, 0xA66C, 0xA66C,
            0xA680, 0xA680, 0xA682, 0xA682, 0xA684, 0xA684, 0xA686, 0xA686,
            0xA688, 0xA688, 0xA68A, 0xA68A, 0xA68C, 0xA68C, 0xA68E, 0xA68E,
            0xA690, 0xA690, 0xA692, 0xA692, 0xA694, 0xA694, 0xA696, 0xA696,
            0xA698, 0xA698, 0xA69A, 0xA69A, 0xA722, 0xA722, 0xA724, 0xA724,
            0xA726, 0xA726, 0xA728, 0xA728, 0xA72A, 0xA72A, 0xA72C, 0xA72C,
            0xA72E, 0xA72E, 0xA732, 0xA732, 0xA734, 0xA734, 0xA736, 0xA736,
            0xA738, 0xA738, 0xA73A, 0xA73A, 0xA73C, 0xA73C, 0xA73E, 0xA73E,
            0xA740, 0xA740, 0xA742, 0xA742, 0xA744, 0xA744, 0xA746, 0xA746,
            0xA748, 0xA748, 0xA74A, 0xA74A, 0xA74C, 0xA74C, 0xA74E, 0xA74E,
            0xA750, 0xA750, 0xA752, 0xA752, 0xA754, 0xA754, 0xA756, 0xA756,
            0xA758, 0xA758, 0xA75A, 0xA75A, 0xA75C, 0xA75C, 0xA75E, 0xA75E,
            0xA760, 0xA760, 0xA762, 0xA762, 0xA764, 0xA764, 0xA766, 0xA766,
            0xA768, 0xA768, 0xA76A, 0xA76A, 0xA76C, 0xA76C, 0xA76E, 0xA76E,
            0xA779, 0xA779, 0xA77B, 0xA77B, 0xA77D, 0xA77E, 0xA780, 0xA780,
            0xA782, 0xA782, 0xA784, 0xA784, 0xA786, 0xA786, 0xA78B, 0xA78B,
            0xA78D, 0xA78D, 0xA790, 0xA790, 0xA792, 0xA792, 0xA796, 0xA796,
            0xA798, 0xA798, 0xA79A, 0xA79A, 0xA79C, 0xA79C, 0xA79E, 0xA79E,
            0xA7A0, 0xA7A0, 0xA7A2, 0xA7A2, 0xA7A4, 0xA7A4, 0xA7A6, 0xA7A6,
            0xA7A8, 0xA7A8, 0xA7AA, 0xA7AE, 0xA7B0, 0xA7B4, 0xA7B6, 0xA7B6,
            0xA7B8, 0xA7B8, 0xA7BA, 0xA7BA, 0xA7BC, 0xA7BC, 0xA7BE, 0xA7BE,
            0xA7C0, 0xA7C0, 0xA7C2, 0xA7C2, 0xA7C4, 0xA7C7, 0xA7C9, 0xA7C9,
            0xA7D0, 0xA7D0, 0xA7D6, 0xA7D6, 0xA7D8, 0xA7D8, 0xA7F5, 0xA7F5,
            0xAB70, 0xABBF, 0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFF21, 0xFF3A,
            0x10400, 0x10427, 0x104B0, 0x104D3, 0x10570, 0x1057A, 0x1057C, 0x1058A,
            0x1058C, 0x10592, 0x10594, 0x10595, 0x10C80, 0x10CB2, 0x118A0, 0x118BF,
            0x16E40, 0x16E5F, 0x1E900, 0x1E921,
            //  #49 (6017+131): bp=Changes_When_Casemapped:CWCM
            0x0041, 0x005A, 0x0061, 0x007A, 0x00B5, 0x00B5, 0x00C0, 0x00D6,
            0x00D8, 0x00F6, 0x00F8, 0x0137, 0x0139, 0x018C, 0x018E, 0x019A,
            0x019C, 0x01A9, 0x01AC, 0x01B9, 0x01BC, 0x01BD, 0x01BF, 0x01BF,
            0x01C4, 0x0220, 0x0222, 0x0233, 0x023A, 0x0254, 0x0256, 0x0257,
            0x0259, 0x0259, 0x025B, 0x025C, 0x0260, 0x0261, 0x0263, 0x0263,
            0x0265, 0x0266, 0x0268, 0x026C, 0x026F, 0x026F, 0x0271, 0x0272,
            0x0275, 0x0275, 0x027D, 0x027D, 0x0280, 0x0280, 0x0282, 0x0283,
            0x0287, 0x028C, 0x0292, 0x0292, 0x029D, 0x029E, 0x0345, 0x0345,
            0x0370, 0x0373, 0x0376, 0x0377, 0x037B, 0x037D, 0x037F, 0x037F,
            0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x03A1,
            0x03A3, 0x03D1, 0x03D5, 0x03F5, 0x03F7, 0x03FB, 0x03FD, 0x0481,
            0x048A, 0x052F, 0x0531, 0x0556, 0x0561, 0x0587, 0x10A0, 0x10C5,
            0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA, 0x10FD, 0x10FF,
            0x13A0, 0x13F5, 0x13F8, 0x13FD, 0x1C80, 0x1C88, 0x1C90, 0x1CBA,
            0x1CBD, 0x1CBF, 0x1D79, 0x1D79, 0x1D7D, 0x1D7D, 0x1D8E, 0x1D8E,
            0x1E00, 0x1E9B, 0x1E9E, 0x1E9E, 0x1EA0, 0x1F15, 0x1F18, 0x1F1D,
            0x1F20, 0x1F45, 0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59,
            0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4,
            0x1FB6, 0x1FBC, 0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC,
            0x1FD0, 0x1FD3, 0x1FD6, 0x1FDB, 0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4,
            0x1FF6, 0x1FFC, 0x2126, 0x2126, 0x212A, 0x212B, 0x2132, 0x2132,
            0x214E, 0x214E, 0x2160, 0x217F, 0x2183, 0x2184, 0x24B6, 0x24E9,
            0x2C00, 0x2C70, 0x2C72, 0x2C73, 0x2C75, 0x2C76, 0x2C7E, 0x2CE3,
            0x2CEB, 0x2CEE, 0x2CF2, 0x2CF3, 0x2D00, 0x2D25, 0x2D27, 0x2D27,
            0x2D2D, 0x2D2D, 0xA640, 0xA66D, 0xA680, 0xA69B, 0xA722, 0xA72F,
            0xA732, 0xA76F, 0xA779, 0xA787, 0xA78B, 0xA78D, 0xA790, 0xA794,
            0xA796, 0xA7AE, 0xA7B0, 0xA7CA, 0xA7D0, 0xA7D1, 0xA7D6, 0xA7D9,
            0xA7F5, 0xA7F6, 0xAB53, 0xAB53, 0xAB70, 0xABBF, 0xFB00, 0xFB06,
            0xFB13, 0xFB17, 0xFF21, 0xFF3A, 0xFF41, 0xFF5A, 0x10400, 0x1044F,
            0x104B0, 0x104D3, 0x104D8, 0x104FB, 0x10570, 0x1057A, 0x1057C, 0x1058A,
            0x1058C, 0x10592, 0x10594, 0x10595, 0x10597, 0x105A1, 0x105A3, 0x105B1,
            0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10C80, 0x10CB2, 0x10CC0, 0x10CF2,
            0x118A0, 0x118DF, 0x16E40, 0x16E7F, 0x1E900, 0x1E943,
            //  #50 (6148+609): bp=Changes_When_Lowercased:CWL
            0x0041, 0x005A, 0x00C0, 0x00D6, 0x00D8, 0x00DE, 0x0100, 0x0100,
            0x0102, 0x0102, 0x0104, 0x0104, 0x0106, 0x0106, 0x0108, 0x0108,
            0x010A, 0x010A, 0x010C, 0x010C, 0x010E, 0x010E, 0x0110, 0x0110,
            0x0112, 0x0112, 0x0114, 0x0114, 0x0116, 0x0116, 0x0118, 0x0118,
            0x011A, 0x011A, 0x011C, 0x011C, 0x011E, 0x011E, 0x0120, 0x0120,
            0x0122, 0x0122, 0x0124, 0x0124, 0x0126, 0x0126, 0x0128, 0x0128,
            0x012A, 0x012A, 0x012C, 0x012C, 0x012E, 0x012E, 0x0130, 0x0130,
            0x0132, 0x0132, 0x0134, 0x0134, 0x0136, 0x0136, 0x0139, 0x0139,
            0x013B, 0x013B, 0x013D, 0x013D, 0x013F, 0x013F, 0x0141, 0x0141,
            0x0143, 0x0143, 0x0145, 0x0145, 0x0147, 0x0147, 0x014A, 0x014A,
            0x014C, 0x014C, 0x014E, 0x014E, 0x0150, 0x0150, 0x0152, 0x0152,
            0x0154, 0x0154, 0x0156, 0x0156, 0x0158, 0x0158, 0x015A, 0x015A,
            0x015C, 0x015C, 0x015E, 0x015E, 0x0160, 0x0160, 0x0162, 0x0162,
            0x0164, 0x0164, 0x0166, 0x0166, 0x0168, 0x0168, 0x016A, 0x016A,
            0x016C, 0x016C, 0x016E, 0x016E, 0x0170, 0x0170, 0x0172, 0x0172,
            0x0174, 0x0174, 0x0176, 0x0176, 0x0178, 0x0179, 0x017B, 0x017B,
            0x017D, 0x017D, 0x0181, 0x0182, 0x0184, 0x0184, 0x0186, 0x0187,
            0x0189, 0x018B, 0x018E, 0x0191, 0x0193, 0x0194, 0x0196, 0x0198,
            0x019C, 0x019D, 0x019F, 0x01A0, 0x01A2, 0x01A2, 0x01A4, 0x01A4,
            0x01A6, 0x01A7, 0x01A9, 0x01A9, 0x01AC, 0x01AC, 0x01AE, 0x01AF,
            0x01B1, 0x01B3, 0x01B5, 0x01B5, 0x01B7, 0x01B8, 0x01BC, 0x01BC,
            0x01C4, 0x01C5, 0x01C7, 0x01C8, 0x01CA, 0x01CB, 0x01CD, 0x01CD,
            0x01CF, 0x01CF, 0x01D1, 0x01D1, 0x01D3, 0x01D3, 0x01D5, 0x01D5,
            0x01D7, 0x01D7, 0x01D9, 0x01D9, 0x01DB, 0x01DB, 0x01DE, 0x01DE,
            0x01E0, 0x01E0, 0x01E2, 0x01E2, 0x01E4, 0x01E4, 0x01E6, 0x01E6,
            0x01E8, 0x01E8, 0x01EA, 0x01EA, 0x01EC, 0x01EC, 0x01EE, 0x01EE,
            0x01F1, 0x01F2, 0x01F4, 0x01F4, 0x01F6, 0x01F8, 0x01FA, 0x01FA,
            0x01FC, 0x01FC, 0x01FE, 0x01FE, 0x0200, 0x0200, 0x0202, 0x0202,
            0x0204, 0x0204, 0x0206, 0x0206, 0x0208, 0x0208, 0x020A, 0x020A,
            0x020C, 0x020C, 0x020E, 0x020E, 0x0210, 0x0210, 0x0212, 0x0212,
            0x0214, 0x0214, 0x0216, 0x0216, 0x0218, 0x0218, 0x021A, 0x021A,
            0x021C, 0x021C, 0x021E, 0x021E, 0x0220, 0x0220, 0x0222, 0x0222,
            0x0224, 0x0224, 0x0226, 0x0226, 0x0228, 0x0228, 0x022A, 0x022A,
            0x022C, 0x022C, 0x022E, 0x022E, 0x0230, 0x0230, 0x0232, 0x0232,
            0x023A, 0x023B, 0x023D, 0x023E, 0x0241, 0x0241, 0x0243, 0x0246,
            0x0248, 0x0248, 0x024A, 0x024A, 0x024C, 0x024C, 0x024E, 0x024E,
            0x0370, 0x0370, 0x0372, 0x0372, 0x0376, 0x0376, 0x037F, 0x037F,
            0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x038F,
            0x0391, 0x03A1, 0x03A3, 0x03AB, 0x03CF, 0x03CF, 0x03D8, 0x03D8,
            0x03DA, 0x03DA, 0x03DC, 0x03DC, 0x03DE, 0x03DE, 0x03E0, 0x03E0,
            0x03E2, 0x03E2, 0x03E4, 0x03E4, 0x03E6, 0x03E6, 0x03E8, 0x03E8,
            0x03EA, 0x03EA, 0x03EC, 0x03EC, 0x03EE, 0x03EE, 0x03F4, 0x03F4,
            0x03F7, 0x03F7, 0x03F9, 0x03FA, 0x03FD, 0x042F, 0x0460, 0x0460,
            0x0462, 0x0462, 0x0464, 0x0464, 0x0466, 0x0466, 0x0468, 0x0468,
            0x046A, 0x046A, 0x046C, 0x046C, 0x046E, 0x046E, 0x0470, 0x0470,
            0x0472, 0x0472, 0x0474, 0x0474, 0x0476, 0x0476, 0x0478, 0x0478,
            0x047A, 0x047A, 0x047C, 0x047C, 0x047E, 0x047E, 0x0480, 0x0480,
            0x048A, 0x048A, 0x048C, 0x048C, 0x048E, 0x048E, 0x0490, 0x0490,
            0x0492, 0x0492, 0x0494, 0x0494, 0x0496, 0x0496, 0x0498, 0x0498,
            0x049A, 0x049A, 0x049C, 0x049C, 0x049E, 0x049E, 0x04A0, 0x04A0,
            0x04A2, 0x04A2, 0x04A4, 0x04A4, 0x04A6, 0x04A6, 0x04A8, 0x04A8,
            0x04AA, 0x04AA, 0x04AC, 0x04AC, 0x04AE, 0x04AE, 0x04B0, 0x04B0,
            0x04B2, 0x04B2, 0x04B4, 0x04B4, 0x04B6, 0x04B6, 0x04B8, 0x04B8,
            0x04BA, 0x04BA, 0x04BC, 0x04BC, 0x04BE, 0x04BE, 0x04C0, 0x04C1,
            0x04C3, 0x04C3, 0x04C5, 0x04C5, 0x04C7, 0x04C7, 0x04C9, 0x04C9,
            0x04CB, 0x04CB, 0x04CD, 0x04CD, 0x04D0, 0x04D0, 0x04D2, 0x04D2,
            0x04D4, 0x04D4, 0x04D6, 0x04D6, 0x04D8, 0x04D8, 0x04DA, 0x04DA,
            0x04DC, 0x04DC, 0x04DE, 0x04DE, 0x04E0, 0x04E0, 0x04E2, 0x04E2,
            0x04E4, 0x04E4, 0x04E6, 0x04E6, 0x04E8, 0x04E8, 0x04EA, 0x04EA,
            0x04EC, 0x04EC, 0x04EE, 0x04EE, 0x04F0, 0x04F0, 0x04F2, 0x04F2,
            0x04F4, 0x04F4, 0x04F6, 0x04F6, 0x04F8, 0x04F8, 0x04FA, 0x04FA,
            0x04FC, 0x04FC, 0x04FE, 0x04FE, 0x0500, 0x0500, 0x0502, 0x0502,
            0x0504, 0x0504, 0x0506, 0x0506, 0x0508, 0x0508, 0x050A, 0x050A,
            0x050C, 0x050C, 0x050E, 0x050E, 0x0510, 0x0510, 0x0512, 0x0512,
            0x0514, 0x0514, 0x0516, 0x0516, 0x0518, 0x0518, 0x051A, 0x051A,
            0x051C, 0x051C, 0x051E, 0x051E, 0x0520, 0x0520, 0x0522, 0x0522,
            0x0524, 0x0524, 0x0526, 0x0526, 0x0528, 0x0528, 0x052A, 0x052A,
            0x052C, 0x052C, 0x052E, 0x052E, 0x0531, 0x0556, 0x10A0, 0x10C5,
            0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x13A0, 0x13F5, 0x1C90, 0x1CBA,
            0x1CBD, 0x1CBF, 0x1E00, 0x1E00, 0x1E02, 0x1E02, 0x1E04, 0x1E04,
            0x1E06, 0x1E06, 0x1E08, 0x1E08, 0x1E0A, 0x1E0A, 0x1E0C, 0x1E0C,
            0x1E0E, 0x1E0E, 0x1E10, 0x1E10, 0x1E12, 0x1E12, 0x1E14, 0x1E14,
            0x1E16, 0x1E16, 0x1E18, 0x1E18, 0x1E1A, 0x1E1A, 0x1E1C, 0x1E1C,
            0x1E1E, 0x1E1E, 0x1E20, 0x1E20, 0x1E22, 0x1E22, 0x1E24, 0x1E24,
            0x1E26, 0x1E26, 0x1E28, 0x1E28, 0x1E2A, 0x1E2A, 0x1E2C, 0x1E2C,
            0x1E2E, 0x1E2E, 0x1E30, 0x1E30, 0x1E32, 0x1E32, 0x1E34, 0x1E34,
            0x1E36, 0x1E36, 0x1E38, 0x1E38, 0x1E3A, 0x1E3A, 0x1E3C, 0x1E3C,
            0x1E3E, 0x1E3E, 0x1E40, 0x1E40, 0x1E42, 0x1E42, 0x1E44, 0x1E44,
            0x1E46, 0x1E46, 0x1E48, 0x1E48, 0x1E4A, 0x1E4A, 0x1E4C, 0x1E4C,
            0x1E4E, 0x1E4E, 0x1E50, 0x1E50, 0x1E52, 0x1E52, 0x1E54, 0x1E54,
            0x1E56, 0x1E56, 0x1E58, 0x1E58, 0x1E5A, 0x1E5A, 0x1E5C, 0x1E5C,
            0x1E5E, 0x1E5E, 0x1E60, 0x1E60, 0x1E62, 0x1E62, 0x1E64, 0x1E64,
            0x1E66, 0x1E66, 0x1E68, 0x1E68, 0x1E6A, 0x1E6A, 0x1E6C, 0x1E6C,
            0x1E6E, 0x1E6E, 0x1E70, 0x1E70, 0x1E72, 0x1E72, 0x1E74, 0x1E74,
            0x1E76, 0x1E76, 0x1E78, 0x1E78, 0x1E7A, 0x1E7A, 0x1E7C, 0x1E7C,
            0x1E7E, 0x1E7E, 0x1E80, 0x1E80, 0x1E82, 0x1E82, 0x1E84, 0x1E84,
            0x1E86, 0x1E86, 0x1E88, 0x1E88, 0x1E8A, 0x1E8A, 0x1E8C, 0x1E8C,
            0x1E8E, 0x1E8E, 0x1E90, 0x1E90, 0x1E92, 0x1E92, 0x1E94, 0x1E94,
            0x1E9E, 0x1E9E, 0x1EA0, 0x1EA0, 0x1EA2, 0x1EA2, 0x1EA4, 0x1EA4,
            0x1EA6, 0x1EA6, 0x1EA8, 0x1EA8, 0x1EAA, 0x1EAA, 0x1EAC, 0x1EAC,
            0x1EAE, 0x1EAE, 0x1EB0, 0x1EB0, 0x1EB2, 0x1EB2, 0x1EB4, 0x1EB4,
            0x1EB6, 0x1EB6, 0x1EB8, 0x1EB8, 0x1EBA, 0x1EBA, 0x1EBC, 0x1EBC,
            0x1EBE, 0x1EBE, 0x1EC0, 0x1EC0, 0x1EC2, 0x1EC2, 0x1EC4, 0x1EC4,
            0x1EC6, 0x1EC6, 0x1EC8, 0x1EC8, 0x1ECA, 0x1ECA, 0x1ECC, 0x1ECC,
            0x1ECE, 0x1ECE, 0x1ED0, 0x1ED0, 0x1ED2, 0x1ED2, 0x1ED4, 0x1ED4,
            0x1ED6, 0x1ED6, 0x1ED8, 0x1ED8, 0x1EDA, 0x1EDA, 0x1EDC, 0x1EDC,
            0x1EDE, 0x1EDE, 0x1EE0, 0x1EE0, 0x1EE2, 0x1EE2, 0x1EE4, 0x1EE4,
            0x1EE6, 0x1EE6, 0x1EE8, 0x1EE8, 0x1EEA, 0x1EEA, 0x1EEC, 0x1EEC,
            0x1EEE, 0x1EEE, 0x1EF0, 0x1EF0, 0x1EF2, 0x1EF2, 0x1EF4, 0x1EF4,
            0x1EF6, 0x1EF6, 0x1EF8, 0x1EF8, 0x1EFA, 0x1EFA, 0x1EFC, 0x1EFC,
            0x1EFE, 0x1EFE, 0x1F08, 0x1F0F, 0x1F18, 0x1F1D, 0x1F28, 0x1F2F,
            0x1F38, 0x1F3F, 0x1F48, 0x1F4D, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B,
            0x1F5D, 0x1F5D, 0x1F5F, 0x1F5F, 0x1F68, 0x1F6F, 0x1F88, 0x1F8F,
            0x1F98, 0x1F9F, 0x1FA8, 0x1FAF, 0x1FB8, 0x1FBC, 0x1FC8, 0x1FCC,
            0x1FD8, 0x1FDB, 0x1FE8, 0x1FEC, 0x1FF8, 0x1FFC, 0x2126, 0x2126,
            0x212A, 0x212B, 0x2132, 0x2132, 0x2160, 0x216F, 0x2183, 0x2183,
            0x24B6, 0x24CF, 0x2C00, 0x2C2F, 0x2C60, 0x2C60, 0x2C62, 0x2C64,
            0x2C67, 0x2C67, 0x2C69, 0x2C69, 0x2C6B, 0x2C6B, 0x2C6D, 0x2C70,
            0x2C72, 0x2C72, 0x2C75, 0x2C75, 0x2C7E, 0x2C80, 0x2C82, 0x2C82,
            0x2C84, 0x2C84, 0x2C86, 0x2C86, 0x2C88, 0x2C88, 0x2C8A, 0x2C8A,
            0x2C8C, 0x2C8C, 0x2C8E, 0x2C8E, 0x2C90, 0x2C90, 0x2C92, 0x2C92,
            0x2C94, 0x2C94, 0x2C96, 0x2C96, 0x2C98, 0x2C98, 0x2C9A, 0x2C9A,
            0x2C9C, 0x2C9C, 0x2C9E, 0x2C9E, 0x2CA0, 0x2CA0, 0x2CA2, 0x2CA2,
            0x2CA4, 0x2CA4, 0x2CA6, 0x2CA6, 0x2CA8, 0x2CA8, 0x2CAA, 0x2CAA,
            0x2CAC, 0x2CAC, 0x2CAE, 0x2CAE, 0x2CB0, 0x2CB0, 0x2CB2, 0x2CB2,
            0x2CB4, 0x2CB4, 0x2CB6, 0x2CB6, 0x2CB8, 0x2CB8, 0x2CBA, 0x2CBA,
            0x2CBC, 0x2CBC, 0x2CBE, 0x2CBE, 0x2CC0, 0x2CC0, 0x2CC2, 0x2CC2,
            0x2CC4, 0x2CC4, 0x2CC6, 0x2CC6, 0x2CC8, 0x2CC8, 0x2CCA, 0x2CCA,
            0x2CCC, 0x2CCC, 0x2CCE, 0x2CCE, 0x2CD0, 0x2CD0, 0x2CD2, 0x2CD2,
            0x2CD4, 0x2CD4, 0x2CD6, 0x2CD6, 0x2CD8, 0x2CD8, 0x2CDA, 0x2CDA,
            0x2CDC, 0x2CDC, 0x2CDE, 0x2CDE, 0x2CE0, 0x2CE0, 0x2CE2, 0x2CE2,
            0x2CEB, 0x2CEB, 0x2CED, 0x2CED, 0x2CF2, 0x2CF2, 0xA640, 0xA640,
            0xA642, 0xA642, 0xA644, 0xA644, 0xA646, 0xA646, 0xA648, 0xA648,
            0xA64A, 0xA64A, 0xA64C, 0xA64C, 0xA64E, 0xA64E, 0xA650, 0xA650,
            0xA652, 0xA652, 0xA654, 0xA654, 0xA656, 0xA656, 0xA658, 0xA658,
            0xA65A, 0xA65A, 0xA65C, 0xA65C, 0xA65E, 0xA65E, 0xA660, 0xA660,
            0xA662, 0xA662, 0xA664, 0xA664, 0xA666, 0xA666, 0xA668, 0xA668,
            0xA66A, 0xA66A, 0xA66C, 0xA66C, 0xA680, 0xA680, 0xA682, 0xA682,
            0xA684, 0xA684, 0xA686, 0xA686, 0xA688, 0xA688, 0xA68A, 0xA68A,
            0xA68C, 0xA68C, 0xA68E, 0xA68E, 0xA690, 0xA690, 0xA692, 0xA692,
            0xA694, 0xA694, 0xA696, 0xA696, 0xA698, 0xA698, 0xA69A, 0xA69A,
            0xA722, 0xA722, 0xA724, 0xA724, 0xA726, 0xA726, 0xA728, 0xA728,
            0xA72A, 0xA72A, 0xA72C, 0xA72C, 0xA72E, 0xA72E, 0xA732, 0xA732,
            0xA734, 0xA734, 0xA736, 0xA736, 0xA738, 0xA738, 0xA73A, 0xA73A,
            0xA73C, 0xA73C, 0xA73E, 0xA73E, 0xA740, 0xA740, 0xA742, 0xA742,
            0xA744, 0xA744, 0xA746, 0xA746, 0xA748, 0xA748, 0xA74A, 0xA74A,
            0xA74C, 0xA74C, 0xA74E, 0xA74E, 0xA750, 0xA750, 0xA752, 0xA752,
            0xA754, 0xA754, 0xA756, 0xA756, 0xA758, 0xA758, 0xA75A, 0xA75A,
            0xA75C, 0xA75C, 0xA75E, 0xA75E, 0xA760, 0xA760, 0xA762, 0xA762,
            0xA764, 0xA764, 0xA766, 0xA766, 0xA768, 0xA768, 0xA76A, 0xA76A,
            0xA76C, 0xA76C, 0xA76E, 0xA76E, 0xA779, 0xA779, 0xA77B, 0xA77B,
            0xA77D, 0xA77E, 0xA780, 0xA780, 0xA782, 0xA782, 0xA784, 0xA784,
            0xA786, 0xA786, 0xA78B, 0xA78B, 0xA78D, 0xA78D, 0xA790, 0xA790,
            0xA792, 0xA792, 0xA796, 0xA796, 0xA798, 0xA798, 0xA79A, 0xA79A,
            0xA79C, 0xA79C, 0xA79E, 0xA79E, 0xA7A0, 0xA7A0, 0xA7A2, 0xA7A2,
            0xA7A4, 0xA7A4, 0xA7A6, 0xA7A6, 0xA7A8, 0xA7A8, 0xA7AA, 0xA7AE,
            0xA7B0, 0xA7B4, 0xA7B6, 0xA7B6, 0xA7B8, 0xA7B8, 0xA7BA, 0xA7BA,
            0xA7BC, 0xA7BC, 0xA7BE, 0xA7BE, 0xA7C0, 0xA7C0, 0xA7C2, 0xA7C2,
            0xA7C4, 0xA7C7, 0xA7C9, 0xA7C9, 0xA7D0, 0xA7D0, 0xA7D6, 0xA7D6,
            0xA7D8, 0xA7D8, 0xA7F5, 0xA7F5, 0xFF21, 0xFF3A, 0x10400, 0x10427,
            0x104B0, 0x104D3, 0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592,
            0x10594, 0x10595, 0x10C80, 0x10CB2, 0x118A0, 0x118BF, 0x16E40, 0x16E5F,
            0x1E900, 0x1E921,
            //  #51 (6757+838): bp=Changes_When_NFKC_Casefolded:CWKCF
            0x0041, 0x005A, 0x00A0, 0x00A0, 0x00A8, 0x00A8, 0x00AA, 0x00AA,
            0x00AD, 0x00AD, 0x00AF, 0x00AF, 0x00B2, 0x00B5, 0x00B8, 0x00BA,
            0x00BC, 0x00BE, 0x00C0, 0x00D6, 0x00D8, 0x00DF, 0x0100, 0x0100,
            0x0102, 0x0102, 0x0104, 0x0104, 0x0106, 0x0106, 0x0108, 0x0108,
            0x010A, 0x010A, 0x010C, 0x010C, 0x010E, 0x010E, 0x0110, 0x0110,
            0x0112, 0x0112, 0x0114, 0x0114, 0x0116, 0x0116, 0x0118, 0x0118,
            0x011A, 0x011A, 0x011C, 0x011C, 0x011E, 0x011E, 0x0120, 0x0120,
            0x0122, 0x0122, 0x0124, 0x0124, 0x0126, 0x0126, 0x0128, 0x0128,
            0x012A, 0x012A, 0x012C, 0x012C, 0x012E, 0x012E, 0x0130, 0x0130,
            0x0132, 0x0134, 0x0136, 0x0136, 0x0139, 0x0139, 0x013B, 0x013B,
            0x013D, 0x013D, 0x013F, 0x0141, 0x0143, 0x0143, 0x0145, 0x0145,
            0x0147, 0x0147, 0x0149, 0x014A, 0x014C, 0x014C, 0x014E, 0x014E,
            0x0150, 0x0150, 0x0152, 0x0152, 0x0154, 0x0154, 0x0156, 0x0156,
            0x0158, 0x0158, 0x015A, 0x015A, 0x015C, 0x015C, 0x015E, 0x015E,
            0x0160, 0x0160, 0x0162, 0x0162, 0x0164, 0x0164, 0x0166, 0x0166,
            0x0168, 0x0168, 0x016A, 0x016A, 0x016C, 0x016C, 0x016E, 0x016E,
            0x0170, 0x0170, 0x0172, 0x0172, 0x0174, 0x0174, 0x0176, 0x0176,
            0x0178, 0x0179, 0x017B, 0x017B, 0x017D, 0x017D, 0x017F, 0x017F,
            0x0181, 0x0182, 0x0184, 0x0184, 0x0186, 0x0187, 0x0189, 0x018B,
            0x018E, 0x0191, 0x0193, 0x0194, 0x0196, 0x0198, 0x019C, 0x019D,
            0x019F, 0x01A0, 0x01A2, 0x01A2, 0x01A4, 0x01A4, 0x01A6, 0x01A7,
            0x01A9, 0x01A9, 0x01AC, 0x01AC, 0x01AE, 0x01AF, 0x01B1, 0x01B3,
            0x01B5, 0x01B5, 0x01B7, 0x01B8, 0x01BC, 0x01BC, 0x01C4, 0x01CD,
            0x01CF, 0x01CF, 0x01D1, 0x01D1, 0x01D3, 0x01D3, 0x01D5, 0x01D5,
            0x01D7, 0x01D7, 0x01D9, 0x01D9, 0x01DB, 0x01DB, 0x01DE, 0x01DE,
            0x01E0, 0x01E0, 0x01E2, 0x01E2, 0x01E4, 0x01E4, 0x01E6, 0x01E6,
            0x01E8, 0x01E8, 0x01EA, 0x01EA, 0x01EC, 0x01EC, 0x01EE, 0x01EE,
            0x01F1, 0x01F4, 0x01F6, 0x01F8, 0x01FA, 0x01FA, 0x01FC, 0x01FC,
            0x01FE, 0x01FE, 0x0200, 0x0200, 0x0202, 0x0202, 0x0204, 0x0204,
            0x0206, 0x0206, 0x0208, 0x0208, 0x020A, 0x020A, 0x020C, 0x020C,
            0x020E, 0x020E, 0x0210, 0x0210, 0x0212, 0x0212, 0x0214, 0x0214,
            0x0216, 0x0216, 0x0218, 0x0218, 0x021A, 0x021A, 0x021C, 0x021C,
            0x021E, 0x021E, 0x0220, 0x0220, 0x0222, 0x0222, 0x0224, 0x0224,
            0x0226, 0x0226, 0x0228, 0x0228, 0x022A, 0x022A, 0x022C, 0x022C,
            0x022E, 0x022E, 0x0230, 0x0230, 0x0232, 0x0232, 0x023A, 0x023B,
            0x023D, 0x023E, 0x0241, 0x0241, 0x0243, 0x0246, 0x0248, 0x0248,
            0x024A, 0x024A, 0x024C, 0x024C, 0x024E, 0x024E, 0x02B0, 0x02B8,
            0x02D8, 0x02DD, 0x02E0, 0x02E4, 0x0340, 0x0341, 0x0343, 0x0345,
            0x034F, 0x034F, 0x0370, 0x0370, 0x0372, 0x0372, 0x0374, 0x0374,
            0x0376, 0x0376, 0x037A, 0x037A, 0x037E, 0x037F, 0x0384, 0x038A,
            0x038C, 0x038C, 0x038E, 0x038F, 0x0391, 0x03A1, 0x03A3, 0x03AB,
            0x03C2, 0x03C2, 0x03CF, 0x03D6, 0x03D8, 0x03D8, 0x03DA, 0x03DA,
            0x03DC, 0x03DC, 0x03DE, 0x03DE, 0x03E0, 0x03E0, 0x03E2, 0x03E2,
            0x03E4, 0x03E4, 0x03E6, 0x03E6, 0x03E8, 0x03E8, 0x03EA, 0x03EA,
            0x03EC, 0x03EC, 0x03EE, 0x03EE, 0x03F0, 0x03F2, 0x03F4, 0x03F5,
            0x03F7, 0x03F7, 0x03F9, 0x03FA, 0x03FD, 0x042F, 0x0460, 0x0460,
            0x0462, 0x0462, 0x0464, 0x0464, 0x0466, 0x0466, 0x0468, 0x0468,
            0x046A, 0x046A, 0x046C, 0x046C, 0x046E, 0x046E, 0x0470, 0x0470,
            0x0472, 0x0472, 0x0474, 0x0474, 0x0476, 0x0476, 0x0478, 0x0478,
            0x047A, 0x047A, 0x047C, 0x047C, 0x047E, 0x047E, 0x0480, 0x0480,
            0x048A, 0x048A, 0x048C, 0x048C, 0x048E, 0x048E, 0x0490, 0x0490,
            0x0492, 0x0492, 0x0494, 0x0494, 0x0496, 0x0496, 0x0498, 0x0498,
            0x049A, 0x049A, 0x049C, 0x049C, 0x049E, 0x049E, 0x04A0, 0x04A0,
            0x04A2, 0x04A2, 0x04A4, 0x04A4, 0x04A6, 0x04A6, 0x04A8, 0x04A8,
            0x04AA, 0x04AA, 0x04AC, 0x04AC, 0x04AE, 0x04AE, 0x04B0, 0x04B0,
            0x04B2, 0x04B2, 0x04B4, 0x04B4, 0x04B6, 0x04B6, 0x04B8, 0x04B8,
            0x04BA, 0x04BA, 0x04BC, 0x04BC, 0x04BE, 0x04BE, 0x04C0, 0x04C1,
            0x04C3, 0x04C3, 0x04C5, 0x04C5, 0x04C7, 0x04C7, 0x04C9, 0x04C9,
            0x04CB, 0x04CB, 0x04CD, 0x04CD, 0x04D0, 0x04D0, 0x04D2, 0x04D2,
            0x04D4, 0x04D4, 0x04D6, 0x04D6, 0x04D8, 0x04D8, 0x04DA, 0x04DA,
            0x04DC, 0x04DC, 0x04DE, 0x04DE, 0x04E0, 0x04E0, 0x04E2, 0x04E2,
            0x04E4, 0x04E4, 0x04E6, 0x04E6, 0x04E8, 0x04E8, 0x04EA, 0x04EA,
            0x04EC, 0x04EC, 0x04EE, 0x04EE, 0x04F0, 0x04F0, 0x04F2, 0x04F2,
            0x04F4, 0x04F4, 0x04F6, 0x04F6, 0x04F8, 0x04F8, 0x04FA, 0x04FA,
            0x04FC, 0x04FC, 0x04FE, 0x04FE, 0x0500, 0x0500, 0x0502, 0x0502,
            0x0504, 0x0504, 0x0506, 0x0506, 0x0508, 0x0508, 0x050A, 0x050A,
            0x050C, 0x050C, 0x050E, 0x050E, 0x0510, 0x0510, 0x0512, 0x0512,
            0x0514, 0x0514, 0x0516, 0x0516, 0x0518, 0x0518, 0x051A, 0x051A,
            0x051C, 0x051C, 0x051E, 0x051E, 0x0520, 0x0520, 0x0522, 0x0522,
            0x0524, 0x0524, 0x0526, 0x0526, 0x0528, 0x0528, 0x052A, 0x052A,
            0x052C, 0x052C, 0x052E, 0x052E, 0x0531, 0x0556, 0x0587, 0x0587,
            0x061C, 0x061C, 0x0675, 0x0678, 0x0958, 0x095F, 0x09DC, 0x09DD,
            0x09DF, 0x09DF, 0x0A33, 0x0A33, 0x0A36, 0x0A36, 0x0A59, 0x0A5B,
            0x0A5E, 0x0A5E, 0x0B5C, 0x0B5D, 0x0E33, 0x0E33, 0x0EB3, 0x0EB3,
            0x0EDC, 0x0EDD, 0x0F0C, 0x0F0C, 0x0F43, 0x0F43, 0x0F4D, 0x0F4D,
            0x0F52, 0x0F52, 0x0F57, 0x0F57, 0x0F5C, 0x0F5C, 0x0F69, 0x0F69,
            0x0F73, 0x0F73, 0x0F75, 0x0F79, 0x0F81, 0x0F81, 0x0F93, 0x0F93,
            0x0F9D, 0x0F9D, 0x0FA2, 0x0FA2, 0x0FA7, 0x0FA7, 0x0FAC, 0x0FAC,
            0x0FB9, 0x0FB9, 0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD,
            0x10FC, 0x10FC, 0x115F, 0x1160, 0x13F8, 0x13FD, 0x17B4, 0x17B5,
            0x180B, 0x180F, 0x1C80, 0x1C88, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF,
            0x1D2C, 0x1D2E, 0x1D30, 0x1D3A, 0x1D3C, 0x1D4D, 0x1D4F, 0x1D6A,
            0x1D78, 0x1D78, 0x1D9B, 0x1DBF, 0x1E00, 0x1E00, 0x1E02, 0x1E02,
            0x1E04, 0x1E04, 0x1E06, 0x1E06, 0x1E08, 0x1E08, 0x1E0A, 0x1E0A,
            0x1E0C, 0x1E0C, 0x1E0E, 0x1E0E, 0x1E10, 0x1E10, 0x1E12, 0x1E12,
            0x1E14, 0x1E14, 0x1E16, 0x1E16, 0x1E18, 0x1E18, 0x1E1A, 0x1E1A,
            0x1E1C, 0x1E1C, 0x1E1E, 0x1E1E, 0x1E20, 0x1E20, 0x1E22, 0x1E22,
            0x1E24, 0x1E24, 0x1E26, 0x1E26, 0x1E28, 0x1E28, 0x1E2A, 0x1E2A,
            0x1E2C, 0x1E2C, 0x1E2E, 0x1E2E, 0x1E30, 0x1E30, 0x1E32, 0x1E32,
            0x1E34, 0x1E34, 0x1E36, 0x1E36, 0x1E38, 0x1E38, 0x1E3A, 0x1E3A,
            0x1E3C, 0x1E3C, 0x1E3E, 0x1E3E, 0x1E40, 0x1E40, 0x1E42, 0x1E42,
            0x1E44, 0x1E44, 0x1E46, 0x1E46, 0x1E48, 0x1E48, 0x1E4A, 0x1E4A,
            0x1E4C, 0x1E4C, 0x1E4E, 0x1E4E, 0x1E50, 0x1E50, 0x1E52, 0x1E52,
            0x1E54, 0x1E54, 0x1E56, 0x1E56, 0x1E58, 0x1E58, 0x1E5A, 0x1E5A,
            0x1E5C, 0x1E5C, 0x1E5E, 0x1E5E, 0x1E60, 0x1E60, 0x1E62, 0x1E62,
            0x1E64, 0x1E64, 0x1E66, 0x1E66, 0x1E68, 0x1E68, 0x1E6A, 0x1E6A,
            0x1E6C, 0x1E6C, 0x1E6E, 0x1E6E, 0x1E70, 0x1E70, 0x1E72, 0x1E72,
            0x1E74, 0x1E74, 0x1E76, 0x1E76, 0x1E78, 0x1E78, 0x1E7A, 0x1E7A,
            0x1E7C, 0x1E7C, 0x1E7E, 0x1E7E, 0x1E80, 0x1E80, 0x1E82, 0x1E82,
            0x1E84, 0x1E84, 0x1E86, 0x1E86, 0x1E88, 0x1E88, 0x1E8A, 0x1E8A,
            0x1E8C, 0x1E8C, 0x1E8E, 0x1E8E, 0x1E90, 0x1E90, 0x1E92, 0x1E92,
            0x1E94, 0x1E94, 0x1E9A, 0x1E9B, 0x1E9E, 0x1E9E, 0x1EA0, 0x1EA0,
            0x1EA2, 0x1EA2, 0x1EA4, 0x1EA4, 0x1EA6, 0x1EA6, 0x1EA8, 0x1EA8,
            0x1EAA, 0x1EAA, 0x1EAC, 0x1EAC, 0x1EAE, 0x1EAE, 0x1EB0, 0x1EB0,
            0x1EB2, 0x1EB2, 0x1EB4, 0x1EB4, 0x1EB6, 0x1EB6, 0x1EB8, 0x1EB8,
            0x1EBA, 0x1EBA, 0x1EBC, 0x1EBC, 0x1EBE, 0x1EBE, 0x1EC0, 0x1EC0,
            0x1EC2, 0x1EC2, 0x1EC4, 0x1EC4, 0x1EC6, 0x1EC6, 0x1EC8, 0x1EC8,
            0x1ECA, 0x1ECA, 0x1ECC, 0x1ECC, 0x1ECE, 0x1ECE, 0x1ED0, 0x1ED0,
            0x1ED2, 0x1ED2, 0x1ED4, 0x1ED4, 0x1ED6, 0x1ED6, 0x1ED8, 0x1ED8,
            0x1EDA, 0x1EDA, 0x1EDC, 0x1EDC, 0x1EDE, 0x1EDE, 0x1EE0, 0x1EE0,
            0x1EE2, 0x1EE2, 0x1EE4, 0x1EE4, 0x1EE6, 0x1EE6, 0x1EE8, 0x1EE8,
            0x1EEA, 0x1EEA, 0x1EEC, 0x1EEC, 0x1EEE, 0x1EEE, 0x1EF0, 0x1EF0,
            0x1EF2, 0x1EF2, 0x1EF4, 0x1EF4, 0x1EF6, 0x1EF6, 0x1EF8, 0x1EF8,
            0x1EFA, 0x1EFA, 0x1EFC, 0x1EFC, 0x1EFE, 0x1EFE, 0x1F08, 0x1F0F,
            0x1F18, 0x1F1D, 0x1F28, 0x1F2F, 0x1F38, 0x1F3F, 0x1F48, 0x1F4D,
            0x1F59, 0x1F59, 0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F5F,
            0x1F68, 0x1F6F, 0x1F71, 0x1F71, 0x1F73, 0x1F73, 0x1F75, 0x1F75,
            0x1F77, 0x1F77, 0x1F79, 0x1F79, 0x1F7B, 0x1F7B, 0x1F7D, 0x1F7D,
            0x1F80, 0x1FAF, 0x1FB2, 0x1FB4, 0x1FB7, 0x1FC4, 0x1FC7, 0x1FCF,
            0x1FD3, 0x1FD3, 0x1FD8, 0x1FDB, 0x1FDD, 0x1FDF, 0x1FE3, 0x1FE3,
            0x1FE8, 0x1FEF, 0x1FF2, 0x1FF4, 0x1FF7, 0x1FFE, 0x2000, 0x200F,
            0x2011, 0x2011, 0x2017, 0x2017, 0x2024, 0x2026, 0x202A, 0x202F,
            0x2033, 0x2034, 0x2036, 0x2037, 0x203C, 0x203C, 0x203E, 0x203E,
            0x2047, 0x2049, 0x2057, 0x2057, 0x205F, 0x2071, 0x2074, 0x208E,
            0x2090, 0x209C, 0x20A8, 0x20A8, 0x2100, 0x2103, 0x2105, 0x2107,
            0x2109, 0x2113, 0x2115, 0x2116, 0x2119, 0x211D, 0x2120, 0x2122,
            0x2124, 0x2124, 0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x212D,
            0x212F, 0x2139, 0x213B, 0x2140, 0x2145, 0x2149, 0x2150, 0x217F,
            0x2183, 0x2183, 0x2189, 0x2189, 0x222C, 0x222D, 0x222F, 0x2230,
            0x2329, 0x232A, 0x2460, 0x24EA, 0x2A0C, 0x2A0C, 0x2A74, 0x2A76,
            0x2ADC, 0x2ADC, 0x2C00, 0x2C2F, 0x2C60, 0x2C60, 0x2C62, 0x2C64,
            0x2C67, 0x2C67, 0x2C69, 0x2C69, 0x2C6B, 0x2C6B, 0x2C6D, 0x2C70,
            0x2C72, 0x2C72, 0x2C75, 0x2C75, 0x2C7C, 0x2C80, 0x2C82, 0x2C82,
            0x2C84, 0x2C84, 0x2C86, 0x2C86, 0x2C88, 0x2C88, 0x2C8A, 0x2C8A,
            0x2C8C, 0x2C8C, 0x2C8E, 0x2C8E, 0x2C90, 0x2C90, 0x2C92, 0x2C92,
            0x2C94, 0x2C94, 0x2C96, 0x2C96, 0x2C98, 0x2C98, 0x2C9A, 0x2C9A,
            0x2C9C, 0x2C9C, 0x2C9E, 0x2C9E, 0x2CA0, 0x2CA0, 0x2CA2, 0x2CA2,
            0x2CA4, 0x2CA4, 0x2CA6, 0x2CA6, 0x2CA8, 0x2CA8, 0x2CAA, 0x2CAA,
            0x2CAC, 0x2CAC, 0x2CAE, 0x2CAE, 0x2CB0, 0x2CB0, 0x2CB2, 0x2CB2,
            0x2CB4, 0x2CB4, 0x2CB6, 0x2CB6, 0x2CB8, 0x2CB8, 0x2CBA, 0x2CBA,
            0x2CBC, 0x2CBC, 0x2CBE, 0x2CBE, 0x2CC0, 0x2CC0, 0x2CC2, 0x2CC2,
            0x2CC4, 0x2CC4, 0x2CC6, 0x2CC6, 0x2CC8, 0x2CC8, 0x2CCA, 0x2CCA,
            0x2CCC, 0x2CCC, 0x2CCE, 0x2CCE, 0x2CD0, 0x2CD0, 0x2CD2, 0x2CD2,
            0x2CD4, 0x2CD4, 0x2CD6, 0x2CD6, 0x2CD8, 0x2CD8, 0x2CDA, 0x2CDA,
            0x2CDC, 0x2CDC, 0x2CDE, 0x2CDE, 0x2CE0, 0x2CE0, 0x2CE2, 0x2CE2,
            0x2CEB, 0x2CEB, 0x2CED, 0x2CED, 0x2CF2, 0x2CF2, 0x2D6F, 0x2D6F,
            0x2E9F, 0x2E9F, 0x2EF3, 0x2EF3, 0x2F00, 0x2FD5, 0x3000, 0x3000,
            0x3036, 0x3036, 0x3038, 0x303A, 0x309B, 0x309C, 0x309F, 0x309F,
            0x30FF, 0x30FF, 0x3131, 0x318E, 0x3192, 0x319F, 0x3200, 0x321E,
            0x3220, 0x3247, 0x3250, 0x327E, 0x3280, 0x33FF, 0xA640, 0xA640,
            0xA642, 0xA642, 0xA644, 0xA644, 0xA646, 0xA646, 0xA648, 0xA648,
            0xA64A, 0xA64A, 0xA64C, 0xA64C, 0xA64E, 0xA64E, 0xA650, 0xA650,
            0xA652, 0xA652, 0xA654, 0xA654, 0xA656, 0xA656, 0xA658, 0xA658,
            0xA65A, 0xA65A, 0xA65C, 0xA65C, 0xA65E, 0xA65E, 0xA660, 0xA660,
            0xA662, 0xA662, 0xA664, 0xA664, 0xA666, 0xA666, 0xA668, 0xA668,
            0xA66A, 0xA66A, 0xA66C, 0xA66C, 0xA680, 0xA680, 0xA682, 0xA682,
            0xA684, 0xA684, 0xA686, 0xA686, 0xA688, 0xA688, 0xA68A, 0xA68A,
            0xA68C, 0xA68C, 0xA68E, 0xA68E, 0xA690, 0xA690, 0xA692, 0xA692,
            0xA694, 0xA694, 0xA696, 0xA696, 0xA698, 0xA698, 0xA69A, 0xA69A,
            0xA69C, 0xA69D, 0xA722, 0xA722, 0xA724, 0xA724, 0xA726, 0xA726,
            0xA728, 0xA728, 0xA72A, 0xA72A, 0xA72C, 0xA72C, 0xA72E, 0xA72E,
            0xA732, 0xA732, 0xA734, 0xA734, 0xA736, 0xA736, 0xA738, 0xA738,
            0xA73A, 0xA73A, 0xA73C, 0xA73C, 0xA73E, 0xA73E, 0xA740, 0xA740,
            0xA742, 0xA742, 0xA744, 0xA744, 0xA746, 0xA746, 0xA748, 0xA748,
            0xA74A, 0xA74A, 0xA74C, 0xA74C, 0xA74E, 0xA74E, 0xA750, 0xA750,
            0xA752, 0xA752, 0xA754, 0xA754, 0xA756, 0xA756, 0xA758, 0xA758,
            0xA75A, 0xA75A, 0xA75C, 0xA75C, 0xA75E, 0xA75E, 0xA760, 0xA760,
            0xA762, 0xA762, 0xA764, 0xA764, 0xA766, 0xA766, 0xA768, 0xA768,
            0xA76A, 0xA76A, 0xA76C, 0xA76C, 0xA76E, 0xA76E, 0xA770, 0xA770,
            0xA779, 0xA779, 0xA77B, 0xA77B, 0xA77D, 0xA77E, 0xA780, 0xA780,
            0xA782, 0xA782, 0xA784, 0xA784, 0xA786, 0xA786, 0xA78B, 0xA78B,
            0xA78D, 0xA78D, 0xA790, 0xA790, 0xA792, 0xA792, 0xA796, 0xA796,
            0xA798, 0xA798, 0xA79A, 0xA79A, 0xA79C, 0xA79C, 0xA79E, 0xA79E,
            0xA7A0, 0xA7A0, 0xA7A2, 0xA7A2, 0xA7A4, 0xA7A4, 0xA7A6, 0xA7A6,
            0xA7A8, 0xA7A8, 0xA7AA, 0xA7AE, 0xA7B0, 0xA7B4, 0xA7B6, 0xA7B6,
            0xA7B8, 0xA7B8, 0xA7BA, 0xA7BA, 0xA7BC, 0xA7BC, 0xA7BE, 0xA7BE,
            0xA7C0, 0xA7C0, 0xA7C2, 0xA7C2, 0xA7C4, 0xA7C7, 0xA7C9, 0xA7C9,
            0xA7D0, 0xA7D0, 0xA7D6, 0xA7D6, 0xA7D8, 0xA7D8, 0xA7F2, 0xA7F5,
            0xA7F8, 0xA7F9, 0xAB5C, 0xAB5F, 0xAB69, 0xAB69, 0xAB70, 0xABBF,
            0xF900, 0xFA0D, 0xFA10, 0xFA10, 0xFA12, 0xFA12, 0xFA15, 0xFA1E,
            0xFA20, 0xFA20, 0xFA22, 0xFA22, 0xFA25, 0xFA26, 0xFA2A, 0xFA6D,
            0xFA70, 0xFAD9, 0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFB1D, 0xFB1D,
            0xFB1F, 0xFB36, 0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41,
            0xFB43, 0xFB44, 0xFB46, 0xFBB1, 0xFBD3, 0xFD3D, 0xFD50, 0xFD8F,
            0xFD92, 0xFDC7, 0xFDF0, 0xFDFC, 0xFE00, 0xFE19, 0xFE30, 0xFE44,
            0xFE47, 0xFE52, 0xFE54, 0xFE66, 0xFE68, 0xFE6B, 0xFE70, 0xFE72,
            0xFE74, 0xFE74, 0xFE76, 0xFEFC, 0xFEFF, 0xFEFF, 0xFF01, 0xFFBE,
            0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC,
            0xFFE0, 0xFFE6, 0xFFE8, 0xFFEE, 0xFFF0, 0xFFF8, 0x10400, 0x10427,
            0x104B0, 0x104D3, 0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592,
            0x10594, 0x10595, 0x10781, 0x10785, 0x10787, 0x107B0, 0x107B2, 0x107BA,
            0x10C80, 0x10CB2, 0x118A0, 0x118BF, 0x16E40, 0x16E5F, 0x1BCA0, 0x1BCA3,
            0x1D15E, 0x1D164, 0x1D173, 0x1D17A, 0x1D1BB, 0x1D1C0, 0x1D400, 0x1D454,
            0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6,
            0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3,
            0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514, 0x1D516, 0x1D51C,
            0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544, 0x1D546, 0x1D546,
            0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D7CB, 0x1D7CE, 0x1D7FF,
            0x1E900, 0x1E921, 0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22,
            0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37,
            0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47,
            0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52,
            0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B,
            0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64,
            0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C,
            0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3,
            0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB, 0x1F100, 0x1F10A, 0x1F110, 0x1F12E,
            0x1F130, 0x1F14F, 0x1F16A, 0x1F16C, 0x1F190, 0x1F190, 0x1F200, 0x1F202,
            0x1F210, 0x1F23B, 0x1F240, 0x1F248, 0x1F250, 0x1F251, 0x1FBF0, 0x1FBF9,
            0x2F800, 0x2FA1D, 0xE0000, 0xE0FFF,
            //  #52 (7595+626): bp=Changes_When_Titlecased:CWT
            0x0061, 0x007A, 0x00B5, 0x00B5, 0x00DF, 0x00F6, 0x00F8, 0x00FF,
            0x0101, 0x0101, 0x0103, 0x0103, 0x0105, 0x0105, 0x0107, 0x0107,
            0x0109, 0x0109, 0x010B, 0x010B, 0x010D, 0x010D, 0x010F, 0x010F,
            0x0111, 0x0111, 0x0113, 0x0113, 0x0115, 0x0115, 0x0117, 0x0117,
            0x0119, 0x0119, 0x011B, 0x011B, 0x011D, 0x011D, 0x011F, 0x011F,
            0x0121, 0x0121, 0x0123, 0x0123, 0x0125, 0x0125, 0x0127, 0x0127,
            0x0129, 0x0129, 0x012B, 0x012B, 0x012D, 0x012D, 0x012F, 0x012F,
            0x0131, 0x0131, 0x0133, 0x0133, 0x0135, 0x0135, 0x0137, 0x0137,
            0x013A, 0x013A, 0x013C, 0x013C, 0x013E, 0x013E, 0x0140, 0x0140,
            0x0142, 0x0142, 0x0144, 0x0144, 0x0146, 0x0146, 0x0148, 0x0149,
            0x014B, 0x014B, 0x014D, 0x014D, 0x014F, 0x014F, 0x0151, 0x0151,
            0x0153, 0x0153, 0x0155, 0x0155, 0x0157, 0x0157, 0x0159, 0x0159,
            0x015B, 0x015B, 0x015D, 0x015D, 0x015F, 0x015F, 0x0161, 0x0161,
            0x0163, 0x0163, 0x0165, 0x0165, 0x0167, 0x0167, 0x0169, 0x0169,
            0x016B, 0x016B, 0x016D, 0x016D, 0x016F, 0x016F, 0x0171, 0x0171,
            0x0173, 0x0173, 0x0175, 0x0175, 0x0177, 0x0177, 0x017A, 0x017A,
            0x017C, 0x017C, 0x017E, 0x0180, 0x0183, 0x0183, 0x0185, 0x0185,
            0x0188, 0x0188, 0x018C, 0x018C, 0x0192, 0x0192, 0x0195, 0x0195,
            0x0199, 0x019A, 0x019E, 0x019E, 0x01A1, 0x01A1, 0x01A3, 0x01A3,
            0x01A5, 0x01A5, 0x01A8, 0x01A8, 0x01AD, 0x01AD, 0x01B0, 0x01B0,
            0x01B4, 0x01B4, 0x01B6, 0x01B6, 0x01B9, 0x01B9, 0x01BD, 0x01BD,
            0x01BF, 0x01BF, 0x01C4, 0x01C4, 0x01C6, 0x01C7, 0x01C9, 0x01CA,
            0x01CC, 0x01CC, 0x01CE, 0x01CE, 0x01D0, 0x01D0, 0x01D2, 0x01D2,
            0x01D4, 0x01D4, 0x01D6, 0x01D6, 0x01D8, 0x01D8, 0x01DA, 0x01DA,
            0x01DC, 0x01DD, 0x01DF, 0x01DF, 0x01E1, 0x01E1, 0x01E3, 0x01E3,
            0x01E5, 0x01E5, 0x01E7, 0x01E7, 0x01E9, 0x01E9, 0x01EB, 0x01EB,
            0x01ED, 0x01ED, 0x01EF, 0x01F1, 0x01F3, 0x01F3, 0x01F5, 0x01F5,
            0x01F9, 0x01F9, 0x01FB, 0x01FB, 0x01FD, 0x01FD, 0x01FF, 0x01FF,
            0x0201, 0x0201, 0x0203, 0x0203, 0x0205, 0x0205, 0x0207, 0x0207,
            0x0209, 0x0209, 0x020B, 0x020B, 0x020D, 0x020D, 0x020F, 0x020F,
            0x0211, 0x0211, 0x0213, 0x0213, 0x0215, 0x0215, 0x0217, 0x0217,
            0x0219, 0x0219, 0x021B, 0x021B, 0x021D, 0x021D, 0x021F, 0x021F,
            0x0223, 0x0223, 0x0225, 0x0225, 0x0227, 0x0227, 0x0229, 0x0229,
            0x022B, 0x022B, 0x022D, 0x022D, 0x022F, 0x022F, 0x0231, 0x0231,
            0x0233, 0x0233, 0x023C, 0x023C, 0x023F, 0x0240, 0x0242, 0x0242,
            0x0247, 0x0247, 0x0249, 0x0249, 0x024B, 0x024B, 0x024D, 0x024D,
            0x024F, 0x0254, 0x0256, 0x0257, 0x0259, 0x0259, 0x025B, 0x025C,
            0x0260, 0x0261, 0x0263, 0x0263, 0x0265, 0x0266, 0x0268, 0x026C,
            0x026F, 0x026F, 0x0271, 0x0272, 0x0275, 0x0275, 0x027D, 0x027D,
            0x0280, 0x0280, 0x0282, 0x0283, 0x0287, 0x028C, 0x0292, 0x0292,
            0x029D, 0x029E, 0x0345, 0x0345, 0x0371, 0x0371, 0x0373, 0x0373,
            0x0377, 0x0377, 0x037B, 0x037D, 0x0390, 0x0390, 0x03AC, 0x03CE,
            0x03D0, 0x03D1, 0x03D5, 0x03D7, 0x03D9, 0x03D9, 0x03DB, 0x03DB,
            0x03DD, 0x03DD, 0x03DF, 0x03DF, 0x03E1, 0x03E1, 0x03E3, 0x03E3,
            0x03E5, 0x03E5, 0x03E7, 0x03E7, 0x03E9, 0x03E9, 0x03EB, 0x03EB,
            0x03ED, 0x03ED, 0x03EF, 0x03F3, 0x03F5, 0x03F5, 0x03F8, 0x03F8,
            0x03FB, 0x03FB, 0x0430, 0x045F, 0x0461, 0x0461, 0x0463, 0x0463,
            0x0465, 0x0465, 0x0467, 0x0467, 0x0469, 0x0469, 0x046B, 0x046B,
            0x046D, 0x046D, 0x046F, 0x046F, 0x0471, 0x0471, 0x0473, 0x0473,
            0x0475, 0x0475, 0x0477, 0x0477, 0x0479, 0x0479, 0x047B, 0x047B,
            0x047D, 0x047D, 0x047F, 0x047F, 0x0481, 0x0481, 0x048B, 0x048B,
            0x048D, 0x048D, 0x048F, 0x048F, 0x0491, 0x0491, 0x0493, 0x0493,
            0x0495, 0x0495, 0x0497, 0x0497, 0x0499, 0x0499, 0x049B, 0x049B,
            0x049D, 0x049D, 0x049F, 0x049F, 0x04A1, 0x04A1, 0x04A3, 0x04A3,
            0x04A5, 0x04A5, 0x04A7, 0x04A7, 0x04A9, 0x04A9, 0x04AB, 0x04AB,
            0x04AD, 0x04AD, 0x04AF, 0x04AF, 0x04B1, 0x04B1, 0x04B3, 0x04B3,
            0x04B5, 0x04B5, 0x04B7, 0x04B7, 0x04B9, 0x04B9, 0x04BB, 0x04BB,
            0x04BD, 0x04BD, 0x04BF, 0x04BF, 0x04C2, 0x04C2, 0x04C4, 0x04C4,
            0x04C6, 0x04C6, 0x04C8, 0x04C8, 0x04CA, 0x04CA, 0x04CC, 0x04CC,
            0x04CE, 0x04CF, 0x04D1, 0x04D1, 0x04D3, 0x04D3, 0x04D5, 0x04D5,
            0x04D7, 0x04D7, 0x04D9, 0x04D9, 0x04DB, 0x04DB, 0x04DD, 0x04DD,
            0x04DF, 0x04DF, 0x04E1, 0x04E1, 0x04E3, 0x04E3, 0x04E5, 0x04E5,
            0x04E7, 0x04E7, 0x04E9, 0x04E9, 0x04EB, 0x04EB, 0x04ED, 0x04ED,
            0x04EF, 0x04EF, 0x04F1, 0x04F1, 0x04F3, 0x04F3, 0x04F5, 0x04F5,
            0x04F7, 0x04F7, 0x04F9, 0x04F9, 0x04FB, 0x04FB, 0x04FD, 0x04FD,
            0x04FF, 0x04FF, 0x0501, 0x0501, 0x0503, 0x0503, 0x0505, 0x0505,
            0x0507, 0x0507, 0x0509, 0x0509, 0x050B, 0x050B, 0x050D, 0x050D,
            0x050F, 0x050F, 0x0511, 0x0511, 0x0513, 0x0513, 0x0515, 0x0515,
            0x0517, 0x0517, 0x0519, 0x0519, 0x051B, 0x051B, 0x051D, 0x051D,
            0x051F, 0x051F, 0x0521, 0x0521, 0x0523, 0x0523, 0x0525, 0x0525,
            0x0527, 0x0527, 0x0529, 0x0529, 0x052B, 0x052B, 0x052D, 0x052D,
            0x052F, 0x052F, 0x0561, 0x0587, 0x13F8, 0x13FD, 0x1C80, 0x1C88,
            0x1D79, 0x1D79, 0x1D7D, 0x1D7D, 0x1D8E, 0x1D8E, 0x1E01, 0x1E01,
            0x1E03, 0x1E03, 0x1E05, 0x1E05, 0x1E07, 0x1E07, 0x1E09, 0x1E09,
            0x1E0B, 0x1E0B, 0x1E0D, 0x1E0D, 0x1E0F, 0x1E0F, 0x1E11, 0x1E11,
            0x1E13, 0x1E13, 0x1E15, 0x1E15, 0x1E17, 0x1E17, 0x1E19, 0x1E19,
            0x1E1B, 0x1E1B, 0x1E1D, 0x1E1D, 0x1E1F, 0x1E1F, 0x1E21, 0x1E21,
            0x1E23, 0x1E23, 0x1E25, 0x1E25, 0x1E27, 0x1E27, 0x1E29, 0x1E29,
            0x1E2B, 0x1E2B, 0x1E2D, 0x1E2D, 0x1E2F, 0x1E2F, 0x1E31, 0x1E31,
            0x1E33, 0x1E33, 0x1E35, 0x1E35, 0x1E37, 0x1E37, 0x1E39, 0x1E39,
            0x1E3B, 0x1E3B, 0x1E3D, 0x1E3D, 0x1E3F, 0x1E3F, 0x1E41, 0x1E41,
            0x1E43, 0x1E43, 0x1E45, 0x1E45, 0x1E47, 0x1E47, 0x1E49, 0x1E49,
            0x1E4B, 0x1E4B, 0x1E4D, 0x1E4D, 0x1E4F, 0x1E4F, 0x1E51, 0x1E51,
            0x1E53, 0x1E53, 0x1E55, 0x1E55, 0x1E57, 0x1E57, 0x1E59, 0x1E59,
            0x1E5B, 0x1E5B, 0x1E5D, 0x1E5D, 0x1E5F, 0x1E5F, 0x1E61, 0x1E61,
            0x1E63, 0x1E63, 0x1E65, 0x1E65, 0x1E67, 0x1E67, 0x1E69, 0x1E69,
            0x1E6B, 0x1E6B, 0x1E6D, 0x1E6D, 0x1E6F, 0x1E6F, 0x1E71, 0x1E71,
            0x1E73, 0x1E73, 0x1E75, 0x1E75, 0x1E77, 0x1E77, 0x1E79, 0x1E79,
            0x1E7B, 0x1E7B, 0x1E7D, 0x1E7D, 0x1E7F, 0x1E7F, 0x1E81, 0x1E81,
            0x1E83, 0x1E83, 0x1E85, 0x1E85, 0x1E87, 0x1E87, 0x1E89, 0x1E89,
            0x1E8B, 0x1E8B, 0x1E8D, 0x1E8D, 0x1E8F, 0x1E8F, 0x1E91, 0x1E91,
            0x1E93, 0x1E93, 0x1E95, 0x1E9B, 0x1EA1, 0x1EA1, 0x1EA3, 0x1EA3,
            0x1EA5, 0x1EA5, 0x1EA7, 0x1EA7, 0x1EA9, 0x1EA9, 0x1EAB, 0x1EAB,
            0x1EAD, 0x1EAD, 0x1EAF, 0x1EAF, 0x1EB1, 0x1EB1, 0x1EB3, 0x1EB3,
            0x1EB5, 0x1EB5, 0x1EB7, 0x1EB7, 0x1EB9, 0x1EB9, 0x1EBB, 0x1EBB,
            0x1EBD, 0x1EBD, 0x1EBF, 0x1EBF, 0x1EC1, 0x1EC1, 0x1EC3, 0x1EC3,
            0x1EC5, 0x1EC5, 0x1EC7, 0x1EC7, 0x1EC9, 0x1EC9, 0x1ECB, 0x1ECB,
            0x1ECD, 0x1ECD, 0x1ECF, 0x1ECF, 0x1ED1, 0x1ED1, 0x1ED3, 0x1ED3,
            0x1ED5, 0x1ED5, 0x1ED7, 0x1ED7, 0x1ED9, 0x1ED9, 0x1EDB, 0x1EDB,
            0x1EDD, 0x1EDD, 0x1EDF, 0x1EDF, 0x1EE1, 0x1EE1, 0x1EE3, 0x1EE3,
            0x1EE5, 0x1EE5, 0x1EE7, 0x1EE7, 0x1EE9, 0x1EE9, 0x1EEB, 0x1EEB,
            0x1EED, 0x1EED, 0x1EEF, 0x1EEF, 0x1EF1, 0x1EF1, 0x1EF3, 0x1EF3,
            0x1EF5, 0x1EF5, 0x1EF7, 0x1EF7, 0x1EF9, 0x1EF9, 0x1EFB, 0x1EFB,
            0x1EFD, 0x1EFD, 0x1EFF, 0x1F07, 0x1F10, 0x1F15, 0x1F20, 0x1F27,
            0x1F30, 0x1F37, 0x1F40, 0x1F45, 0x1F50, 0x1F57, 0x1F60, 0x1F67,
            0x1F70, 0x1F7D, 0x1F80, 0x1F87, 0x1F90, 0x1F97, 0x1FA0, 0x1FA7,
            0x1FB0, 0x1FB4, 0x1FB6, 0x1FB7, 0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4,
            0x1FC6, 0x1FC7, 0x1FD0, 0x1FD3, 0x1FD6, 0x1FD7, 0x1FE0, 0x1FE7,
            0x1FF2, 0x1FF4, 0x1FF6, 0x1FF7, 0x214E, 0x214E, 0x2170, 0x217F,
            0x2184, 0x2184, 0x24D0, 0x24E9, 0x2C30, 0x2C5F, 0x2C61, 0x2C61,
            0x2C65, 0x2C66, 0x2C68, 0x2C68, 0x2C6A, 0x2C6A, 0x2C6C, 0x2C6C,
            0x2C73, 0x2C73, 0x2C76, 0x2C76, 0x2C81, 0x2C81, 0x2C83, 0x2C83,
            0x2C85, 0x2C85, 0x2C87, 0x2C87, 0x2C89, 0x2C89, 0x2C8B, 0x2C8B,
            0x2C8D, 0x2C8D, 0x2C8F, 0x2C8F, 0x2C91, 0x2C91, 0x2C93, 0x2C93,
            0x2C95, 0x2C95, 0x2C97, 0x2C97, 0x2C99, 0x2C99, 0x2C9B, 0x2C9B,
            0x2C9D, 0x2C9D, 0x2C9F, 0x2C9F, 0x2CA1, 0x2CA1, 0x2CA3, 0x2CA3,
            0x2CA5, 0x2CA5, 0x2CA7, 0x2CA7, 0x2CA9, 0x2CA9, 0x2CAB, 0x2CAB,
            0x2CAD, 0x2CAD, 0x2CAF, 0x2CAF, 0x2CB1, 0x2CB1, 0x2CB3, 0x2CB3,
            0x2CB5, 0x2CB5, 0x2CB7, 0x2CB7, 0x2CB9, 0x2CB9, 0x2CBB, 0x2CBB,
            0x2CBD, 0x2CBD, 0x2CBF, 0x2CBF, 0x2CC1, 0x2CC1, 0x2CC3, 0x2CC3,
            0x2CC5, 0x2CC5, 0x2CC7, 0x2CC7, 0x2CC9, 0x2CC9, 0x2CCB, 0x2CCB,
            0x2CCD, 0x2CCD, 0x2CCF, 0x2CCF, 0x2CD1, 0x2CD1, 0x2CD3, 0x2CD3,
            0x2CD5, 0x2CD5, 0x2CD7, 0x2CD7, 0x2CD9, 0x2CD9, 0x2CDB, 0x2CDB,
            0x2CDD, 0x2CDD, 0x2CDF, 0x2CDF, 0x2CE1, 0x2CE1, 0x2CE3, 0x2CE3,
            0x2CEC, 0x2CEC, 0x2CEE, 0x2CEE, 0x2CF3, 0x2CF3, 0x2D00, 0x2D25,
            0x2D27, 0x2D27, 0x2D2D, 0x2D2D, 0xA641, 0xA641, 0xA643, 0xA643,
            0xA645, 0xA645, 0xA647, 0xA647, 0xA649, 0xA649, 0xA64B, 0xA64B,
            0xA64D, 0xA64D, 0xA64F, 0xA64F, 0xA651, 0xA651, 0xA653, 0xA653,
            0xA655, 0xA655, 0xA657, 0xA657, 0xA659, 0xA659, 0xA65B, 0xA65B,
            0xA65D, 0xA65D, 0xA65F, 0xA65F, 0xA661, 0xA661, 0xA663, 0xA663,
            0xA665, 0xA665, 0xA667, 0xA667, 0xA669, 0xA669, 0xA66B, 0xA66B,
            0xA66D, 0xA66D, 0xA681, 0xA681, 0xA683, 0xA683, 0xA685, 0xA685,
            0xA687, 0xA687, 0xA689, 0xA689, 0xA68B, 0xA68B, 0xA68D, 0xA68D,
            0xA68F, 0xA68F, 0xA691, 0xA691, 0xA693, 0xA693, 0xA695, 0xA695,
            0xA697, 0xA697, 0xA699, 0xA699, 0xA69B, 0xA69B, 0xA723, 0xA723,
            0xA725, 0xA725, 0xA727, 0xA727, 0xA729, 0xA729, 0xA72B, 0xA72B,
            0xA72D, 0xA72D, 0xA72F, 0xA72F, 0xA733, 0xA733, 0xA735, 0xA735,
            0xA737, 0xA737, 0xA739, 0xA739, 0xA73B, 0xA73B, 0xA73D, 0xA73D,
            0xA73F, 0xA73F, 0xA741, 0xA741, 0xA743, 0xA743, 0xA745, 0xA745,
            0xA747, 0xA747, 0xA749, 0xA749, 0xA74B, 0xA74B, 0xA74D, 0xA74D,
            0xA74F, 0xA74F, 0xA751, 0xA751, 0xA753, 0xA753, 0xA755, 0xA755,
            0xA757, 0xA757, 0xA759, 0xA759, 0xA75B, 0xA75B, 0xA75D, 0xA75D,
            0xA75F, 0xA75F, 0xA761, 0xA761, 0xA763, 0xA763, 0xA765, 0xA765,
            0xA767, 0xA767, 0xA769, 0xA769, 0xA76B, 0xA76B, 0xA76D, 0xA76D,
            0xA76F, 0xA76F, 0xA77A, 0xA77A, 0xA77C, 0xA77C, 0xA77F, 0xA77F,
            0xA781, 0xA781, 0xA783, 0xA783, 0xA785, 0xA785, 0xA787, 0xA787,
            0xA78C, 0xA78C, 0xA791, 0xA791, 0xA793, 0xA794, 0xA797, 0xA797,
            0xA799, 0xA799, 0xA79B, 0xA79B, 0xA79D, 0xA79D, 0xA79F, 0xA79F,
            0xA7A1, 0xA7A1, 0xA7A3, 0xA7A3, 0xA7A5, 0xA7A5, 0xA7A7, 0xA7A7,
            0xA7A9, 0xA7A9, 0xA7B5, 0xA7B5, 0xA7B7, 0xA7B7, 0xA7B9, 0xA7B9,
            0xA7BB, 0xA7BB, 0xA7BD, 0xA7BD, 0xA7BF, 0xA7BF, 0xA7C1, 0xA7C1,
            0xA7C3, 0xA7C3, 0xA7C8, 0xA7C8, 0xA7CA, 0xA7CA, 0xA7D1, 0xA7D1,
            0xA7D7, 0xA7D7, 0xA7D9, 0xA7D9, 0xA7F6, 0xA7F6, 0xAB53, 0xAB53,
            0xAB70, 0xABBF, 0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFF41, 0xFF5A,
            0x10428, 0x1044F, 0x104D8, 0x104FB, 0x10597, 0x105A1, 0x105A3, 0x105B1,
            0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10CC0, 0x10CF2, 0x118C0, 0x118DF,
            0x16E60, 0x16E7F, 0x1E922, 0x1E943,
            //  #53 (8221+627): bp=Changes_When_Uppercased:CWU
            0x0061, 0x007A, 0x00B5, 0x00B5, 0x00DF, 0x00F6, 0x00F8, 0x00FF,
            0x0101, 0x0101, 0x0103, 0x0103, 0x0105, 0x0105, 0x0107, 0x0107,
            0x0109, 0x0109, 0x010B, 0x010B, 0x010D, 0x010D, 0x010F, 0x010F,
            0x0111, 0x0111, 0x0113, 0x0113, 0x0115, 0x0115, 0x0117, 0x0117,
            0x0119, 0x0119, 0x011B, 0x011B, 0x011D, 0x011D, 0x011F, 0x011F,
            0x0121, 0x0121, 0x0123, 0x0123, 0x0125, 0x0125, 0x0127, 0x0127,
            0x0129, 0x0129, 0x012B, 0x012B, 0x012D, 0x012D, 0x012F, 0x012F,
            0x0131, 0x0131, 0x0133, 0x0133, 0x0135, 0x0135, 0x0137, 0x0137,
            0x013A, 0x013A, 0x013C, 0x013C, 0x013E, 0x013E, 0x0140, 0x0140,
            0x0142, 0x0142, 0x0144, 0x0144, 0x0146, 0x0146, 0x0148, 0x0149,
            0x014B, 0x014B, 0x014D, 0x014D, 0x014F, 0x014F, 0x0151, 0x0151,
            0x0153, 0x0153, 0x0155, 0x0155, 0x0157, 0x0157, 0x0159, 0x0159,
            0x015B, 0x015B, 0x015D, 0x015D, 0x015F, 0x015F, 0x0161, 0x0161,
            0x0163, 0x0163, 0x0165, 0x0165, 0x0167, 0x0167, 0x0169, 0x0169,
            0x016B, 0x016B, 0x016D, 0x016D, 0x016F, 0x016F, 0x0171, 0x0171,
            0x0173, 0x0173, 0x0175, 0x0175, 0x0177, 0x0177, 0x017A, 0x017A,
            0x017C, 0x017C, 0x017E, 0x0180, 0x0183, 0x0183, 0x0185, 0x0185,
            0x0188, 0x0188, 0x018C, 0x018C, 0x0192, 0x0192, 0x0195, 0x0195,
            0x0199, 0x019A, 0x019E, 0x019E, 0x01A1, 0x01A1, 0x01A3, 0x01A3,
            0x01A5, 0x01A5, 0x01A8, 0x01A8, 0x01AD, 0x01AD, 0x01B0, 0x01B0,
            0x01B4, 0x01B4, 0x01B6, 0x01B6, 0x01B9, 0x01B9, 0x01BD, 0x01BD,
            0x01BF, 0x01BF, 0x01C5, 0x01C6, 0x01C8, 0x01C9, 0x01CB, 0x01CC,
            0x01CE, 0x01CE, 0x01D0, 0x01D0, 0x01D2, 0x01D2, 0x01D4, 0x01D4,
            0x01D6, 0x01D6, 0x01D8, 0x01D8, 0x01DA, 0x01DA, 0x01DC, 0x01DD,
            0x01DF, 0x01DF, 0x01E1, 0x01E1, 0x01E3, 0x01E3, 0x01E5, 0x01E5,
            0x01E7, 0x01E7, 0x01E9, 0x01E9, 0x01EB, 0x01EB, 0x01ED, 0x01ED,
            0x01EF, 0x01F0, 0x01F2, 0x01F3, 0x01F5, 0x01F5, 0x01F9, 0x01F9,
            0x01FB, 0x01FB, 0x01FD, 0x01FD, 0x01FF, 0x01FF, 0x0201, 0x0201,
            0x0203, 0x0203, 0x0205, 0x0205, 0x0207, 0x0207, 0x0209, 0x0209,
            0x020B, 0x020B, 0x020D, 0x020D, 0x020F, 0x020F, 0x0211, 0x0211,
            0x0213, 0x0213, 0x0215, 0x0215, 0x0217, 0x0217, 0x0219, 0x0219,
            0x021B, 0x021B, 0x021D, 0x021D, 0x021F, 0x021F, 0x0223, 0x0223,
            0x0225, 0x0225, 0x0227, 0x0227, 0x0229, 0x0229, 0x022B, 0x022B,
            0x022D, 0x022D, 0x022F, 0x022F, 0x0231, 0x0231, 0x0233, 0x0233,
            0x023C, 0x023C, 0x023F, 0x0240, 0x0242, 0x0242, 0x0247, 0x0247,
            0x0249, 0x0249, 0x024B, 0x024B, 0x024D, 0x024D, 0x024F, 0x0254,
            0x0256, 0x0257, 0x0259, 0x0259, 0x025B, 0x025C, 0x0260, 0x0261,
            0x0263, 0x0263, 0x0265, 0x0266, 0x0268, 0x026C, 0x026F, 0x026F,
            0x0271, 0x0272, 0x0275, 0x0275, 0x027D, 0x027D, 0x0280, 0x0280,
            0x0282, 0x0283, 0x0287, 0x028C, 0x0292, 0x0292, 0x029D, 0x029E,
            0x0345, 0x0345, 0x0371, 0x0371, 0x0373, 0x0373, 0x0377, 0x0377,
            0x037B, 0x037D, 0x0390, 0x0390, 0x03AC, 0x03CE, 0x03D0, 0x03D1,
            0x03D5, 0x03D7, 0x03D9, 0x03D9, 0x03DB, 0x03DB, 0x03DD, 0x03DD,
            0x03DF, 0x03DF, 0x03E1, 0x03E1, 0x03E3, 0x03E3, 0x03E5, 0x03E5,
            0x03E7, 0x03E7, 0x03E9, 0x03E9, 0x03EB, 0x03EB, 0x03ED, 0x03ED,
            0x03EF, 0x03F3, 0x03F5, 0x03F5, 0x03F8, 0x03F8, 0x03FB, 0x03FB,
            0x0430, 0x045F, 0x0461, 0x0461, 0x0463, 0x0463, 0x0465, 0x0465,
            0x0467, 0x0467, 0x0469, 0x0469, 0x046B, 0x046B, 0x046D, 0x046D,
            0x046F, 0x046F, 0x0471, 0x0471, 0x0473, 0x0473, 0x0475, 0x0475,
            0x0477, 0x0477, 0x0479, 0x0479, 0x047B, 0x047B, 0x047D, 0x047D,
            0x047F, 0x047F, 0x0481, 0x0481, 0x048B, 0x048B, 0x048D, 0x048D,
            0x048F, 0x048F, 0x0491, 0x0491, 0x0493, 0x0493, 0x0495, 0x0495,
            0x0497, 0x0497, 0x0499, 0x0499, 0x049B, 0x049B, 0x049D, 0x049D,
            0x049F, 0x049F, 0x04A1, 0x04A1, 0x04A3, 0x04A3, 0x04A5, 0x04A5,
            0x04A7, 0x04A7, 0x04A9, 0x04A9, 0x04AB, 0x04AB, 0x04AD, 0x04AD,
            0x04AF, 0x04AF, 0x04B1, 0x04B1, 0x04B3, 0x04B3, 0x04B5, 0x04B5,
            0x04B7, 0x04B7, 0x04B9, 0x04B9, 0x04BB, 0x04BB, 0x04BD, 0x04BD,
            0x04BF, 0x04BF, 0x04C2, 0x04C2, 0x04C4, 0x04C4, 0x04C6, 0x04C6,
            0x04C8, 0x04C8, 0x04CA, 0x04CA, 0x04CC, 0x04CC, 0x04CE, 0x04CF,
            0x04D1, 0x04D1, 0x04D3, 0x04D3, 0x04D5, 0x04D5, 0x04D7, 0x04D7,
            0x04D9, 0x04D9, 0x04DB, 0x04DB, 0x04DD, 0x04DD, 0x04DF, 0x04DF,
            0x04E1, 0x04E1, 0x04E3, 0x04E3, 0x04E5, 0x04E5, 0x04E7, 0x04E7,
            0x04E9, 0x04E9, 0x04EB, 0x04EB, 0x04ED, 0x04ED, 0x04EF, 0x04EF,
            0x04F1, 0x04F1, 0x04F3, 0x04F3, 0x04F5, 0x04F5, 0x04F7, 0x04F7,
            0x04F9, 0x04F9, 0x04FB, 0x04FB, 0x04FD, 0x04FD, 0x04FF, 0x04FF,
            0x0501, 0x0501, 0x0503, 0x0503, 0x0505, 0x0505, 0x0507, 0x0507,
            0x0509, 0x0509, 0x050B, 0x050B, 0x050D, 0x050D, 0x050F, 0x050F,
            0x0511, 0x0511, 0x0513, 0x0513, 0x0515, 0x0515, 0x0517, 0x0517,
            0x0519, 0x0519, 0x051B, 0x051B, 0x051D, 0x051D, 0x051F, 0x051F,
            0x0521, 0x0521, 0x0523, 0x0523, 0x0525, 0x0525, 0x0527, 0x0527,
            0x0529, 0x0529, 0x052B, 0x052B, 0x052D, 0x052D, 0x052F, 0x052F,
            0x0561, 0x0587, 0x10D0, 0x10FA, 0x10FD, 0x10FF, 0x13F8, 0x13FD,
            0x1C80, 0x1C88, 0x1D79, 0x1D79, 0x1D7D, 0x1D7D, 0x1D8E, 0x1D8E,
            0x1E01, 0x1E01, 0x1E03, 0x1E03, 0x1E05, 0x1E05, 0x1E07, 0x1E07,
            0x1E09, 0x1E09, 0x1E0B, 0x1E0B, 0x1E0D, 0x1E0D, 0x1E0F, 0x1E0F,
            0x1E11, 0x1E11, 0x1E13, 0x1E13, 0x1E15, 0x1E15, 0x1E17, 0x1E17,
            0x1E19, 0x1E19, 0x1E1B, 0x1E1B, 0x1E1D, 0x1E1D, 0x1E1F, 0x1E1F,
            0x1E21, 0x1E21, 0x1E23, 0x1E23, 0x1E25, 0x1E25, 0x1E27, 0x1E27,
            0x1E29, 0x1E29, 0x1E2B, 0x1E2B, 0x1E2D, 0x1E2D, 0x1E2F, 0x1E2F,
            0x1E31, 0x1E31, 0x1E33, 0x1E33, 0x1E35, 0x1E35, 0x1E37, 0x1E37,
            0x1E39, 0x1E39, 0x1E3B, 0x1E3B, 0x1E3D, 0x1E3D, 0x1E3F, 0x1E3F,
            0x1E41, 0x1E41, 0x1E43, 0x1E43, 0x1E45, 0x1E45, 0x1E47, 0x1E47,
            0x1E49, 0x1E49, 0x1E4B, 0x1E4B, 0x1E4D, 0x1E4D, 0x1E4F, 0x1E4F,
            0x1E51, 0x1E51, 0x1E53, 0x1E53, 0x1E55, 0x1E55, 0x1E57, 0x1E57,
            0x1E59, 0x1E59, 0x1E5B, 0x1E5B, 0x1E5D, 0x1E5D, 0x1E5F, 0x1E5F,
            0x1E61, 0x1E61, 0x1E63, 0x1E63, 0x1E65, 0x1E65, 0x1E67, 0x1E67,
            0x1E69, 0x1E69, 0x1E6B, 0x1E6B, 0x1E6D, 0x1E6D, 0x1E6F, 0x1E6F,
            0x1E71, 0x1E71, 0x1E73, 0x1E73, 0x1E75, 0x1E75, 0x1E77, 0x1E77,
            0x1E79, 0x1E79, 0x1E7B, 0x1E7B, 0x1E7D, 0x1E7D, 0x1E7F, 0x1E7F,
            0x1E81, 0x1E81, 0x1E83, 0x1E83, 0x1E85, 0x1E85, 0x1E87, 0x1E87,
            0x1E89, 0x1E89, 0x1E8B, 0x1E8B, 0x1E8D, 0x1E8D, 0x1E8F, 0x1E8F,
            0x1E91, 0x1E91, 0x1E93, 0x1E93, 0x1E95, 0x1E9B, 0x1EA1, 0x1EA1,
            0x1EA3, 0x1EA3, 0x1EA5, 0x1EA5, 0x1EA7, 0x1EA7, 0x1EA9, 0x1EA9,
            0x1EAB, 0x1EAB, 0x1EAD, 0x1EAD, 0x1EAF, 0x1EAF, 0x1EB1, 0x1EB1,
            0x1EB3, 0x1EB3, 0x1EB5, 0x1EB5, 0x1EB7, 0x1EB7, 0x1EB9, 0x1EB9,
            0x1EBB, 0x1EBB, 0x1EBD, 0x1EBD, 0x1EBF, 0x1EBF, 0x1EC1, 0x1EC1,
            0x1EC3, 0x1EC3, 0x1EC5, 0x1EC5, 0x1EC7, 0x1EC7, 0x1EC9, 0x1EC9,
            0x1ECB, 0x1ECB, 0x1ECD, 0x1ECD, 0x1ECF, 0x1ECF, 0x1ED1, 0x1ED1,
            0x1ED3, 0x1ED3, 0x1ED5, 0x1ED5, 0x1ED7, 0x1ED7, 0x1ED9, 0x1ED9,
            0x1EDB, 0x1EDB, 0x1EDD, 0x1EDD, 0x1EDF, 0x1EDF, 0x1EE1, 0x1EE1,
            0x1EE3, 0x1EE3, 0x1EE5, 0x1EE5, 0x1EE7, 0x1EE7, 0x1EE9, 0x1EE9,
            0x1EEB, 0x1EEB, 0x1EED, 0x1EED, 0x1EEF, 0x1EEF, 0x1EF1, 0x1EF1,
            0x1EF3, 0x1EF3, 0x1EF5, 0x1EF5, 0x1EF7, 0x1EF7, 0x1EF9, 0x1EF9,
            0x1EFB, 0x1EFB, 0x1EFD, 0x1EFD, 0x1EFF, 0x1F07, 0x1F10, 0x1F15,
            0x1F20, 0x1F27, 0x1F30, 0x1F37, 0x1F40, 0x1F45, 0x1F50, 0x1F57,
            0x1F60, 0x1F67, 0x1F70, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FB7,
            0x1FBC, 0x1FBC, 0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FC7,
            0x1FCC, 0x1FCC, 0x1FD0, 0x1FD3, 0x1FD6, 0x1FD7, 0x1FE0, 0x1FE7,
            0x1FF2, 0x1FF4, 0x1FF6, 0x1FF7, 0x1FFC, 0x1FFC, 0x214E, 0x214E,
            0x2170, 0x217F, 0x2184, 0x2184, 0x24D0, 0x24E9, 0x2C30, 0x2C5F,
            0x2C61, 0x2C61, 0x2C65, 0x2C66, 0x2C68, 0x2C68, 0x2C6A, 0x2C6A,
            0x2C6C, 0x2C6C, 0x2C73, 0x2C73, 0x2C76, 0x2C76, 0x2C81, 0x2C81,
            0x2C83, 0x2C83, 0x2C85, 0x2C85, 0x2C87, 0x2C87, 0x2C89, 0x2C89,
            0x2C8B, 0x2C8B, 0x2C8D, 0x2C8D, 0x2C8F, 0x2C8F, 0x2C91, 0x2C91,
            0x2C93, 0x2C93, 0x2C95, 0x2C95, 0x2C97, 0x2C97, 0x2C99, 0x2C99,
            0x2C9B, 0x2C9B, 0x2C9D, 0x2C9D, 0x2C9F, 0x2C9F, 0x2CA1, 0x2CA1,
            0x2CA3, 0x2CA3, 0x2CA5, 0x2CA5, 0x2CA7, 0x2CA7, 0x2CA9, 0x2CA9,
            0x2CAB, 0x2CAB, 0x2CAD, 0x2CAD, 0x2CAF, 0x2CAF, 0x2CB1, 0x2CB1,
            0x2CB3, 0x2CB3, 0x2CB5, 0x2CB5, 0x2CB7, 0x2CB7, 0x2CB9, 0x2CB9,
            0x2CBB, 0x2CBB, 0x2CBD, 0x2CBD, 0x2CBF, 0x2CBF, 0x2CC1, 0x2CC1,
            0x2CC3, 0x2CC3, 0x2CC5, 0x2CC5, 0x2CC7, 0x2CC7, 0x2CC9, 0x2CC9,
            0x2CCB, 0x2CCB, 0x2CCD, 0x2CCD, 0x2CCF, 0x2CCF, 0x2CD1, 0x2CD1,
            0x2CD3, 0x2CD3, 0x2CD5, 0x2CD5, 0x2CD7, 0x2CD7, 0x2CD9, 0x2CD9,
            0x2CDB, 0x2CDB, 0x2CDD, 0x2CDD, 0x2CDF, 0x2CDF, 0x2CE1, 0x2CE1,
            0x2CE3, 0x2CE3, 0x2CEC, 0x2CEC, 0x2CEE, 0x2CEE, 0x2CF3, 0x2CF3,
            0x2D00, 0x2D25, 0x2D27, 0x2D27, 0x2D2D, 0x2D2D, 0xA641, 0xA641,
            0xA643, 0xA643, 0xA645, 0xA645, 0xA647, 0xA647, 0xA649, 0xA649,
            0xA64B, 0xA64B, 0xA64D, 0xA64D, 0xA64F, 0xA64F, 0xA651, 0xA651,
            0xA653, 0xA653, 0xA655, 0xA655, 0xA657, 0xA657, 0xA659, 0xA659,
            0xA65B, 0xA65B, 0xA65D, 0xA65D, 0xA65F, 0xA65F, 0xA661, 0xA661,
            0xA663, 0xA663, 0xA665, 0xA665, 0xA667, 0xA667, 0xA669, 0xA669,
            0xA66B, 0xA66B, 0xA66D, 0xA66D, 0xA681, 0xA681, 0xA683, 0xA683,
            0xA685, 0xA685, 0xA687, 0xA687, 0xA689, 0xA689, 0xA68B, 0xA68B,
            0xA68D, 0xA68D, 0xA68F, 0xA68F, 0xA691, 0xA691, 0xA693, 0xA693,
            0xA695, 0xA695, 0xA697, 0xA697, 0xA699, 0xA699, 0xA69B, 0xA69B,
            0xA723, 0xA723, 0xA725, 0xA725, 0xA727, 0xA727, 0xA729, 0xA729,
            0xA72B, 0xA72B, 0xA72D, 0xA72D, 0xA72F, 0xA72F, 0xA733, 0xA733,
            0xA735, 0xA735, 0xA737, 0xA737, 0xA739, 0xA739, 0xA73B, 0xA73B,
            0xA73D, 0xA73D, 0xA73F, 0xA73F, 0xA741, 0xA741, 0xA743, 0xA743,
            0xA745, 0xA745, 0xA747, 0xA747, 0xA749, 0xA749, 0xA74B, 0xA74B,
            0xA74D, 0xA74D, 0xA74F, 0xA74F, 0xA751, 0xA751, 0xA753, 0xA753,
            0xA755, 0xA755, 0xA757, 0xA757, 0xA759, 0xA759, 0xA75B, 0xA75B,
            0xA75D, 0xA75D, 0xA75F, 0xA75F, 0xA761, 0xA761, 0xA763, 0xA763,
            0xA765, 0xA765, 0xA767, 0xA767, 0xA769, 0xA769, 0xA76B, 0xA76B,
            0xA76D, 0xA76D, 0xA76F, 0xA76F, 0xA77A, 0xA77A, 0xA77C, 0xA77C,
            0xA77F, 0xA77F, 0xA781, 0xA781, 0xA783, 0xA783, 0xA785, 0xA785,
            0xA787, 0xA787, 0xA78C, 0xA78C, 0xA791, 0xA791, 0xA793, 0xA794,
            0xA797, 0xA797, 0xA799, 0xA799, 0xA79B, 0xA79B, 0xA79D, 0xA79D,
            0xA79F, 0xA79F, 0xA7A1, 0xA7A1, 0xA7A3, 0xA7A3, 0xA7A5, 0xA7A5,
            0xA7A7, 0xA7A7, 0xA7A9, 0xA7A9, 0xA7B5, 0xA7B5, 0xA7B7, 0xA7B7,
            0xA7B9, 0xA7B9, 0xA7BB, 0xA7BB, 0xA7BD, 0xA7BD, 0xA7BF, 0xA7BF,
            0xA7C1, 0xA7C1, 0xA7C3, 0xA7C3, 0xA7C8, 0xA7C8, 0xA7CA, 0xA7CA,
            0xA7D1, 0xA7D1, 0xA7D7, 0xA7D7, 0xA7D9, 0xA7D9, 0xA7F6, 0xA7F6,
            0xAB53, 0xAB53, 0xAB70, 0xABBF, 0xFB00, 0xFB06, 0xFB13, 0xFB17,
            0xFF41, 0xFF5A, 0x10428, 0x1044F, 0x104D8, 0x104FB, 0x10597, 0x105A1,
            0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10CC0, 0x10CF2,
            0x118C0, 0x118DF, 0x16E60, 0x16E7F, 0x1E922, 0x1E943,
            //  #54 (8848+23): bp=Dash
            0x002D, 0x002D, 0x058A, 0x058A, 0x05BE, 0x05BE, 0x1400, 0x1400,
            0x1806, 0x1806, 0x2010, 0x2015, 0x2053, 0x2053, 0x207B, 0x207B,
            0x208B, 0x208B, 0x2212, 0x2212, 0x2E17, 0x2E17, 0x2E1A, 0x2E1A,
            0x2E3A, 0x2E3B, 0x2E40, 0x2E40, 0x2E5D, 0x2E5D, 0x301C, 0x301C,
            0x3030, 0x3030, 0x30A0, 0x30A0, 0xFE31, 0xFE32, 0xFE58, 0xFE58,
            0xFE63, 0xFE63, 0xFF0D, 0xFF0D, 0x10EAD, 0x10EAD,
            //  #55 (8871+17): bp=Default_Ignorable_Code_Point:DI
            0x00AD, 0x00AD, 0x034F, 0x034F, 0x061C, 0x061C, 0x115F, 0x1160,
            0x17B4, 0x17B5, 0x180B, 0x180F, 0x200B, 0x200F, 0x202A, 0x202E,
            0x2060, 0x206F, 0x3164, 0x3164, 0xFE00, 0xFE0F, 0xFEFF, 0xFEFF,
            0xFFA0, 0xFFA0, 0xFFF0, 0xFFF8, 0x1BCA0, 0x1BCA3, 0x1D173, 0x1D17A,
            0xE0000, 0xE0FFF,
            //  #56 (8888+8): bp=Deprecated:Dep
            0x0149, 0x0149, 0x0673, 0x0673, 0x0F77, 0x0F77, 0x0F79, 0x0F79,
            0x17A3, 0x17A4, 0x206A, 0x206F, 0x2329, 0x232A, 0xE0001, 0xE0001,
            //  #57 (8896+192): bp=Diacritic:Dia
            0x005E, 0x005E, 0x0060, 0x0060, 0x00A8, 0x00A8, 0x00AF, 0x00AF,
            0x00B4, 0x00B4, 0x00B7, 0x00B8, 0x02B0, 0x034E, 0x0350, 0x0357,
            0x035D, 0x0362, 0x0374, 0x0375, 0x037A, 0x037A, 0x0384, 0x0385,
            0x0483, 0x0487, 0x0559, 0x0559, 0x0591, 0x05A1, 0x05A3, 0x05BD,
            0x05BF, 0x05BF, 0x05C1, 0x05C2, 0x05C4, 0x05C4, 0x064B, 0x0652,
            0x0657, 0x0658, 0x06DF, 0x06E0, 0x06E5, 0x06E6, 0x06EA, 0x06EC,
            0x0730, 0x074A, 0x07A6, 0x07B0, 0x07EB, 0x07F5, 0x0818, 0x0819,
            0x0898, 0x089F, 0x08C9, 0x08D2, 0x08E3, 0x08FE, 0x093C, 0x093C,
            0x094D, 0x094D, 0x0951, 0x0954, 0x0971, 0x0971, 0x09BC, 0x09BC,
            0x09CD, 0x09CD, 0x0A3C, 0x0A3C, 0x0A4D, 0x0A4D, 0x0ABC, 0x0ABC,
            0x0ACD, 0x0ACD, 0x0AFD, 0x0AFF, 0x0B3C, 0x0B3C, 0x0B4D, 0x0B4D,
            0x0B55, 0x0B55, 0x0BCD, 0x0BCD, 0x0C3C, 0x0C3C, 0x0C4D, 0x0C4D,
            0x0CBC, 0x0CBC, 0x0CCD, 0x0CCD, 0x0D3B, 0x0D3C, 0x0D4D, 0x0D4D,
            0x0DCA, 0x0DCA, 0x0E47, 0x0E4C, 0x0E4E, 0x0E4E, 0x0EBA, 0x0EBA,
            0x0EC8, 0x0ECC, 0x0F18, 0x0F19, 0x0F35, 0x0F35, 0x0F37, 0x0F37,
            0x0F39, 0x0F39, 0x0F3E, 0x0F3F, 0x0F82, 0x0F84, 0x0F86, 0x0F87,
            0x0FC6, 0x0FC6, 0x1037, 0x1037, 0x1039, 0x103A, 0x1063, 0x1064,
            0x1069, 0x106D, 0x1087, 0x108D, 0x108F, 0x108F, 0x109A, 0x109B,
            0x135D, 0x135F, 0x1714, 0x1715, 0x17C9, 0x17D3, 0x17DD, 0x17DD,
            0x1939, 0x193B, 0x1A75, 0x1A7C, 0x1A7F, 0x1A7F, 0x1AB0, 0x1ABE,
            0x1AC1, 0x1ACB, 0x1B34, 0x1B34, 0x1B44, 0x1B44, 0x1B6B, 0x1B73,
            0x1BAA, 0x1BAB, 0x1C36, 0x1C37, 0x1C78, 0x1C7D, 0x1CD0, 0x1CE8,
            0x1CED, 0x1CED, 0x1CF4, 0x1CF4, 0x1CF7, 0x1CF9, 0x1D2C, 0x1D6A,
            0x1DC4, 0x1DCF, 0x1DF5, 0x1DFF, 0x1FBD, 0x1FBD, 0x1FBF, 0x1FC1,
            0x1FCD, 0x1FCF, 0x1FDD, 0x1FDF, 0x1FED, 0x1FEF, 0x1FFD, 0x1FFE,
            0x2CEF, 0x2CF1, 0x2E2F, 0x2E2F, 0x302A, 0x302F, 0x3099, 0x309C,
            0x30FC, 0x30FC, 0xA66F, 0xA66F, 0xA67C, 0xA67D, 0xA67F, 0xA67F,
            0xA69C, 0xA69D, 0xA6F0, 0xA6F1, 0xA700, 0xA721, 0xA788, 0xA78A,
            0xA7F8, 0xA7F9, 0xA8C4, 0xA8C4, 0xA8E0, 0xA8F1, 0xA92B, 0xA92E,
            0xA953, 0xA953, 0xA9B3, 0xA9B3, 0xA9C0, 0xA9C0, 0xA9E5, 0xA9E5,
            0xAA7B, 0xAA7D, 0xAABF, 0xAAC2, 0xAAF6, 0xAAF6, 0xAB5B, 0xAB5F,
            0xAB69, 0xAB6B, 0xABEC, 0xABED, 0xFB1E, 0xFB1E, 0xFE20, 0xFE2F,
            0xFF3E, 0xFF3E, 0xFF40, 0xFF40, 0xFF70, 0xFF70, 0xFF9E, 0xFF9F,
            0xFFE3, 0xFFE3, 0x102E0, 0x102E0, 0x10780, 0x10785, 0x10787, 0x107B0,
            0x107B2, 0x107BA, 0x10AE5, 0x10AE6, 0x10D22, 0x10D27, 0x10F46, 0x10F50,
            0x10F82, 0x10F85, 0x11046, 0x11046, 0x11070, 0x11070, 0x110B9, 0x110BA,
            0x11133, 0x11134, 0x11173, 0x11173, 0x111C0, 0x111C0, 0x111CA, 0x111CC,
            0x11235, 0x11236, 0x112E9, 0x112EA, 0x1133C, 0x1133C, 0x1134D, 0x1134D,
            0x11366, 0x1136C, 0x11370, 0x11374, 0x11442, 0x11442, 0x11446, 0x11446,
            0x114C2, 0x114C3, 0x115BF, 0x115C0, 0x1163F, 0x1163F, 0x116B6, 0x116B7,
            0x1172B, 0x1172B, 0x11839, 0x1183A, 0x1193D, 0x1193E, 0x11943, 0x11943,
            0x119E0, 0x119E0, 0x11A34, 0x11A34, 0x11A47, 0x11A47, 0x11A99, 0x11A99,
            0x11C3F, 0x11C3F, 0x11D42, 0x11D42, 0x11D44, 0x11D45, 0x11D97, 0x11D97,
            0x16AF0, 0x16AF4, 0x16B30, 0x16B36, 0x16F8F, 0x16F9F, 0x16FF0, 0x16FF1,
            0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE, 0x1CF00, 0x1CF2D,
            0x1CF30, 0x1CF46, 0x1D167, 0x1D169, 0x1D16D, 0x1D172, 0x1D17B, 0x1D182,
            0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD, 0x1E130, 0x1E136, 0x1E2AE, 0x1E2AE,
            0x1E2EC, 0x1E2EF, 0x1E8D0, 0x1E8D6, 0x1E944, 0x1E946, 0x1E948, 0x1E94A,
            //  #58 (9088+153): bp=Emoji
            0x0023, 0x0023, 0x002A, 0x002A, 0x0030, 0x0039, 0x00A9, 0x00A9,
            0x00AE, 0x00AE, 0x203C, 0x203C, 0x2049, 0x2049, 0x2122, 0x2122,
            0x2139, 0x2139, 0x2194, 0x2199, 0x21A9, 0x21AA, 0x231A, 0x231B,
            0x2328, 0x2328, 0x23CF, 0x23CF, 0x23E9, 0x23F3, 0x23F8, 0x23FA,
            0x24C2, 0x24C2, 0x25AA, 0x25AB, 0x25B6, 0x25B6, 0x25C0, 0x25C0,
            0x25FB, 0x25FE, 0x2600, 0x2604, 0x260E, 0x260E, 0x2611, 0x2611,
            0x2614, 0x2615, 0x2618, 0x2618, 0x261D, 0x261D, 0x2620, 0x2620,
            0x2622, 0x2623, 0x2626, 0x2626, 0x262A, 0x262A, 0x262E, 0x262F,
            0x2638, 0x263A, 0x2640, 0x2640, 0x2642, 0x2642, 0x2648, 0x2653,
            0x265F, 0x2660, 0x2663, 0x2663, 0x2665, 0x2666, 0x2668, 0x2668,
            0x267B, 0x267B, 0x267E, 0x267F, 0x2692, 0x2697, 0x2699, 0x2699,
            0x269B, 0x269C, 0x26A0, 0x26A1, 0x26A7, 0x26A7, 0x26AA, 0x26AB,
            0x26B0, 0x26B1, 0x26BD, 0x26BE, 0x26C4, 0x26C5, 0x26C8, 0x26C8,
            0x26CE, 0x26CF, 0x26D1, 0x26D1, 0x26D3, 0x26D4, 0x26E9, 0x26EA,
            0x26F0, 0x26F5, 0x26F7, 0x26FA, 0x26FD, 0x26FD, 0x2702, 0x2702,
            0x2705, 0x2705, 0x2708, 0x270D, 0x270F, 0x270F, 0x2712, 0x2712,
            0x2714, 0x2714, 0x2716, 0x2716, 0x271D, 0x271D, 0x2721, 0x2721,
            0x2728, 0x2728, 0x2733, 0x2734, 0x2744, 0x2744, 0x2747, 0x2747,
            0x274C, 0x274C, 0x274E, 0x274E, 0x2753, 0x2755, 0x2757, 0x2757,
            0x2763, 0x2764, 0x2795, 0x2797, 0x27A1, 0x27A1, 0x27B0, 0x27B0,
            0x27BF, 0x27BF, 0x2934, 0x2935, 0x2B05, 0x2B07, 0x2B1B, 0x2B1C,
            0x2B50, 0x2B50, 0x2B55, 0x2B55, 0x3030, 0x3030, 0x303D, 0x303D,
            0x3297, 0x3297, 0x3299, 0x3299, 0x1F004, 0x1F004, 0x1F0CF, 0x1F0CF,
            0x1F170, 0x1F171, 0x1F17E, 0x1F17F, 0x1F18E, 0x1F18E, 0x1F191, 0x1F19A,
            0x1F1E6, 0x1F1FF, 0x1F201, 0x1F202, 0x1F21A, 0x1F21A, 0x1F22F, 0x1F22F,
            0x1F232, 0x1F23A, 0x1F250, 0x1F251, 0x1F300, 0x1F321, 0x1F324, 0x1F393,
            0x1F396, 0x1F397, 0x1F399, 0x1F39B, 0x1F39E, 0x1F3F0, 0x1F3F3, 0x1F3F5,
            0x1F3F7, 0x1F4FD, 0x1F4FF, 0x1F53D, 0x1F549, 0x1F54E, 0x1F550, 0x1F567,
            0x1F56F, 0x1F570, 0x1F573, 0x1F57A, 0x1F587, 0x1F587, 0x1F58A, 0x1F58D,
            0x1F590, 0x1F590, 0x1F595, 0x1F596, 0x1F5A4, 0x1F5A5, 0x1F5A8, 0x1F5A8,
            0x1F5B1, 0x1F5B2, 0x1F5BC, 0x1F5BC, 0x1F5C2, 0x1F5C4, 0x1F5D1, 0x1F5D3,
            0x1F5DC, 0x1F5DE, 0x1F5E1, 0x1F5E1, 0x1F5E3, 0x1F5E3, 0x1F5E8, 0x1F5E8,
            0x1F5EF, 0x1F5EF, 0x1F5F3, 0x1F5F3, 0x1F5FA, 0x1F64F, 0x1F680, 0x1F6C5,
            0x1F6CB, 0x1F6D2, 0x1F6D5, 0x1F6D7, 0x1F6DD, 0x1F6E5, 0x1F6E9, 0x1F6E9,
            0x1F6EB, 0x1F6EC, 0x1F6F0, 0x1F6F0, 0x1F6F3, 0x1F6FC, 0x1F7E0, 0x1F7EB,
            0x1F7F0, 0x1F7F0, 0x1F90C, 0x1F93A, 0x1F93C, 0x1F945, 0x1F947, 0x1F9FF,
            0x1FA70, 0x1FA74, 0x1FA78, 0x1FA7C, 0x1FA80, 0x1FA86, 0x1FA90, 0x1FAAC,
            0x1FAB0, 0x1FABA, 0x1FAC0, 0x1FAC5, 0x1FAD0, 0x1FAD9, 0x1FAE0, 0x1FAE7,
            0x1FAF0, 0x1FAF6,
            //  #59 (9241+10): bp=Emoji_Component:EComp
            0x0023, 0x0023, 0x002A, 0x002A, 0x0030, 0x0039, 0x200D, 0x200D,
            0x20E3, 0x20E3, 0xFE0F, 0xFE0F, 0x1F1E6, 0x1F1FF, 0x1F3FB, 0x1F3FF,
            0x1F9B0, 0x1F9B3, 0xE0020, 0xE007F,
            //  #60 (9251+1): bp=Emoji_Modifier:EMod
            0x1F3FB, 0x1F3FF,
            //  #61 (9252+40): bp=Emoji_Modifier_Base:EBase
            0x261D, 0x261D, 0x26F9, 0x26F9, 0x270A, 0x270D, 0x1F385, 0x1F385,
            0x1F3C2, 0x1F3C4, 0x1F3C7, 0x1F3C7, 0x1F3CA, 0x1F3CC, 0x1F442, 0x1F443,
            0x1F446, 0x1F450, 0x1F466, 0x1F478, 0x1F47C, 0x1F47C, 0x1F481, 0x1F483,
            0x1F485, 0x1F487, 0x1F48F, 0x1F48F, 0x1F491, 0x1F491, 0x1F4AA, 0x1F4AA,
            0x1F574, 0x1F575, 0x1F57A, 0x1F57A, 0x1F590, 0x1F590, 0x1F595, 0x1F596,
            0x1F645, 0x1F647, 0x1F64B, 0x1F64F, 0x1F6A3, 0x1F6A3, 0x1F6B4, 0x1F6B6,
            0x1F6C0, 0x1F6C0, 0x1F6CC, 0x1F6CC, 0x1F90C, 0x1F90C, 0x1F90F, 0x1F90F,
            0x1F918, 0x1F91F, 0x1F926, 0x1F926, 0x1F930, 0x1F939, 0x1F93C, 0x1F93E,
            0x1F977, 0x1F977, 0x1F9B5, 0x1F9B6, 0x1F9B8, 0x1F9B9, 0x1F9BB, 0x1F9BB,
            0x1F9CD, 0x1F9CF, 0x1F9D1, 0x1F9DD, 0x1FAC3, 0x1FAC5, 0x1FAF0, 0x1FAF6,
            //  #62 (9292+83): bp=Emoji_Presentation:EPres
            0x231A, 0x231B, 0x23E9, 0x23EC, 0x23F0, 0x23F0, 0x23F3, 0x23F3,
            0x25FD, 0x25FE, 0x2614, 0x2615, 0x2648, 0x2653, 0x267F, 0x267F,
            0x2693, 0x2693, 0x26A1, 0x26A1, 0x26AA, 0x26AB, 0x26BD, 0x26BE,
            0x26C4, 0x26C5, 0x26CE, 0x26CE, 0x26D4, 0x26D4, 0x26EA, 0x26EA,
            0x26F2, 0x26F3, 0x26F5, 0x26F5, 0x26FA, 0x26FA, 0x26FD, 0x26FD,
            0x2705, 0x2705, 0x270A, 0x270B, 0x2728, 0x2728, 0x274C, 0x274C,
            0x274E, 0x274E, 0x2753, 0x2755, 0x2757, 0x2757, 0x2795, 0x2797,
            0x27B0, 0x27B0, 0x27BF, 0x27BF, 0x2B1B, 0x2B1C, 0x2B50, 0x2B50,
            0x2B55, 0x2B55, 0x1F004, 0x1F004, 0x1F0CF, 0x1F0CF, 0x1F18E, 0x1F18E,
            0x1F191, 0x1F19A, 0x1F1E6, 0x1F1FF, 0x1F201, 0x1F201, 0x1F21A, 0x1F21A,
            0x1F22F, 0x1F22F, 0x1F232, 0x1F236, 0x1F238, 0x1F23A, 0x1F250, 0x1F251,
            0x1F300, 0x1F320, 0x1F32D, 0x1F335, 0x1F337, 0x1F37C, 0x1F37E, 0x1F393,
            0x1F3A0, 0x1F3CA, 0x1F3CF, 0x1F3D3, 0x1F3E0, 0x1F3F0, 0x1F3F4, 0x1F3F4,
            0x1F3F8, 0x1F43E, 0x1F440, 0x1F440, 0x1F442, 0x1F4FC, 0x1F4FF, 0x1F53D,
            0x1F54B, 0x1F54E, 0x1F550, 0x1F567, 0x1F57A, 0x1F57A, 0x1F595, 0x1F596,
            0x1F5A4, 0x1F5A4, 0x1F5FB, 0x1F64F, 0x1F680, 0x1F6C5, 0x1F6CC, 0x1F6CC,
            0x1F6D0, 0x1F6D2, 0x1F6D5, 0x1F6D7, 0x1F6DD, 0x1F6DF, 0x1F6EB, 0x1F6EC,
            0x1F6F4, 0x1F6FC, 0x1F7E0, 0x1F7EB, 0x1F7F0, 0x1F7F0, 0x1F90C, 0x1F93A,
            0x1F93C, 0x1F945, 0x1F947, 0x1F9FF, 0x1FA70, 0x1FA74, 0x1FA78, 0x1FA7C,
            0x1FA80, 0x1FA86, 0x1FA90, 0x1FAAC, 0x1FAB0, 0x1FABA, 0x1FAC0, 0x1FAC5,
            0x1FAD0, 0x1FAD9, 0x1FAE0, 0x1FAE7, 0x1FAF0, 0x1FAF6,
            //  #63 (9375+78): bp=Extended_Pictographic:ExtPict
            0x00A9, 0x00A9, 0x00AE, 0x00AE, 0x203C, 0x203C, 0x2049, 0x2049,
            0x2122, 0x2122, 0x2139, 0x2139, 0x2194, 0x2199, 0x21A9, 0x21AA,
            0x231A, 0x231B, 0x2328, 0x2328, 0x2388, 0x2388, 0x23CF, 0x23CF,
            0x23E9, 0x23F3, 0x23F8, 0x23FA, 0x24C2, 0x24C2, 0x25AA, 0x25AB,
            0x25B6, 0x25B6, 0x25C0, 0x25C0, 0x25FB, 0x25FE, 0x2600, 0x2605,
            0x2607, 0x2612, 0x2614, 0x2685, 0x2690, 0x2705, 0x2708, 0x2712,
            0x2714, 0x2714, 0x2716, 0x2716, 0x271D, 0x271D, 0x2721, 0x2721,
            0x2728, 0x2728, 0x2733, 0x2734, 0x2744, 0x2744, 0x2747, 0x2747,
            0x274C, 0x274C, 0x274E, 0x274E, 0x2753, 0x2755, 0x2757, 0x2757,
            0x2763, 0x2767, 0x2795, 0x2797, 0x27A1, 0x27A1, 0x27B0, 0x27B0,
            0x27BF, 0x27BF, 0x2934, 0x2935, 0x2B05, 0x2B07, 0x2B1B, 0x2B1C,
            0x2B50, 0x2B50, 0x2B55, 0x2B55, 0x3030, 0x3030, 0x303D, 0x303D,
            0x3297, 0x3297, 0x3299, 0x3299, 0x1F000, 0x1F0FF, 0x1F10D, 0x1F10F,
            0x1F12F, 0x1F12F, 0x1F16C, 0x1F171, 0x1F17E, 0x1F17F, 0x1F18E, 0x1F18E,
            0x1F191, 0x1F19A, 0x1F1AD, 0x1F1E5, 0x1F201, 0x1F20F, 0x1F21A, 0x1F21A,
            0x1F22F, 0x1F22F, 0x1F232, 0x1F23A, 0x1F23C, 0x1F23F, 0x1F249, 0x1F3FA,
            0x1F400, 0x1F53D, 0x1F546, 0x1F64F, 0x1F680, 0x1F6FF, 0x1F774, 0x1F77F,
            0x1F7D5, 0x1F7FF, 0x1F80C, 0x1F80F, 0x1F848, 0x1F84F, 0x1F85A, 0x1F85F,
            0x1F888, 0x1F88F, 0x1F8AE, 0x1F8FF, 0x1F90C, 0x1F93A, 0x1F93C, 0x1F945,
            0x1F947, 0x1FAFF, 0x1FC00, 0x1FFFD,
            //  #64 (9453+33): bp=Extender:Ext
            0x00B7, 0x00B7, 0x02D0, 0x02D1, 0x0640, 0x0640, 0x07FA, 0x07FA,
            0x0B55, 0x0B55, 0x0E46, 0x0E46, 0x0EC6, 0x0EC6, 0x180A, 0x180A,
            0x1843, 0x1843, 0x1AA7, 0x1AA7, 0x1C36, 0x1C36, 0x1C7B, 0x1C7B,
            0x3005, 0x3005, 0x3031, 0x3035, 0x309D, 0x309E, 0x30FC, 0x30FE,
            0xA015, 0xA015, 0xA60C, 0xA60C, 0xA9CF, 0xA9CF, 0xA9E6, 0xA9E6,
            0xAA70, 0xAA70, 0xAADD, 0xAADD, 0xAAF3, 0xAAF4, 0xFF70, 0xFF70,
            0x10781, 0x10782, 0x1135D, 0x1135D, 0x115C6, 0x115C8, 0x11A98, 0x11A98,
            0x16B42, 0x16B43, 0x16FE0, 0x16FE1, 0x16FE3, 0x16FE3, 0x1E13C, 0x1E13D,
            0x1E944, 0x1E946,
            //  #65 (9486+861): bp=Grapheme_Base:Gr_Base
            0x0020, 0x007E, 0x00A0, 0x00AC, 0x00AE, 0x02FF, 0x0370, 0x0377,
            0x037A, 0x037F, 0x0384, 0x038A, 0x038C, 0x038C, 0x038E, 0x03A1,
            0x03A3, 0x0482, 0x048A, 0x052F, 0x0531, 0x0556, 0x0559, 0x058A,
            0x058D, 0x058F, 0x05BE, 0x05BE, 0x05C0, 0x05C0, 0x05C3, 0x05C3,
            0x05C6, 0x05C6, 0x05D0, 0x05EA, 0x05EF, 0x05F4, 0x0606, 0x060F,
            0x061B, 0x061B, 0x061D, 0x064A, 0x0660, 0x066F, 0x0671, 0x06D5,
            0x06DE, 0x06DE, 0x06E5, 0x06E6, 0x06E9, 0x06E9, 0x06EE, 0x070D,
            0x0710, 0x0710, 0x0712, 0x072F, 0x074D, 0x07A5, 0x07B1, 0x07B1,
            0x07C0, 0x07EA, 0x07F4, 0x07FA, 0x07FE, 0x0815, 0x081A, 0x081A,
            0x0824, 0x0824, 0x0828, 0x0828, 0x0830, 0x083E, 0x0840, 0x0858,
            0x085E, 0x085E, 0x0860, 0x086A, 0x0870, 0x088E, 0x08A0, 0x08C9,
            0x0903, 0x0939, 0x093B, 0x093B, 0x093D, 0x0940, 0x0949, 0x094C,
            0x094E, 0x0950, 0x0958, 0x0961, 0x0964, 0x0980, 0x0982, 0x0983,
            0x0985, 0x098C, 0x098F, 0x0990, 0x0993, 0x09A8, 0x09AA, 0x09B0,
            0x09B2, 0x09B2, 0x09B6, 0x09B9, 0x09BD, 0x09BD, 0x09BF, 0x09C0,
            0x09C7, 0x09C8, 0x09CB, 0x09CC, 0x09CE, 0x09CE, 0x09DC, 0x09DD,
            0x09DF, 0x09E1, 0x09E6, 0x09FD, 0x0A03, 0x0A03, 0x0A05, 0x0A0A,
            0x0A0F, 0x0A10, 0x0A13, 0x0A28, 0x0A2A, 0x0A30, 0x0A32, 0x0A33,
            0x0A35, 0x0A36, 0x0A38, 0x0A39, 0x0A3E, 0x0A40, 0x0A59, 0x0A5C,
            0x0A5E, 0x0A5E, 0x0A66, 0x0A6F, 0x0A72, 0x0A74, 0x0A76, 0x0A76,
            0x0A83, 0x0A83, 0x0A85, 0x0A8D, 0x0A8F, 0x0A91, 0x0A93, 0x0AA8,
            0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3, 0x0AB5, 0x0AB9, 0x0ABD, 0x0AC0,
            0x0AC9, 0x0AC9, 0x0ACB, 0x0ACC, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE1,
            0x0AE6, 0x0AF1, 0x0AF9, 0x0AF9, 0x0B02, 0x0B03, 0x0B05, 0x0B0C,
            0x0B0F, 0x0B10, 0x0B13, 0x0B28, 0x0B2A, 0x0B30, 0x0B32, 0x0B33,
            0x0B35, 0x0B39, 0x0B3D, 0x0B3D, 0x0B40, 0x0B40, 0x0B47, 0x0B48,
            0x0B4B, 0x0B4C, 0x0B5C, 0x0B5D, 0x0B5F, 0x0B61, 0x0B66, 0x0B77,
            0x0B83, 0x0B83, 0x0B85, 0x0B8A, 0x0B8E, 0x0B90, 0x0B92, 0x0B95,
            0x0B99, 0x0B9A, 0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4,
            0x0BA8, 0x0BAA, 0x0BAE, 0x0BB9, 0x0BBF, 0x0BBF, 0x0BC1, 0x0BC2,
            0x0BC6, 0x0BC8, 0x0BCA, 0x0BCC, 0x0BD0, 0x0BD0, 0x0BE6, 0x0BFA,
            0x0C01, 0x0C03, 0x0C05, 0x0C0C, 0x0C0E, 0x0C10, 0x0C12, 0x0C28,
            0x0C2A, 0x0C39, 0x0C3D, 0x0C3D, 0x0C41, 0x0C44, 0x0C58, 0x0C5A,
            0x0C5D, 0x0C5D, 0x0C60, 0x0C61, 0x0C66, 0x0C6F, 0x0C77, 0x0C80,
            0x0C82, 0x0C8C, 0x0C8E, 0x0C90, 0x0C92, 0x0CA8, 0x0CAA, 0x0CB3,
            0x0CB5, 0x0CB9, 0x0CBD, 0x0CBE, 0x0CC0, 0x0CC1, 0x0CC3, 0x0CC4,
            0x0CC7, 0x0CC8, 0x0CCA, 0x0CCB, 0x0CDD, 0x0CDE, 0x0CE0, 0x0CE1,
            0x0CE6, 0x0CEF, 0x0CF1, 0x0CF2, 0x0D02, 0x0D0C, 0x0D0E, 0x0D10,
            0x0D12, 0x0D3A, 0x0D3D, 0x0D3D, 0x0D3F, 0x0D40, 0x0D46, 0x0D48,
            0x0D4A, 0x0D4C, 0x0D4E, 0x0D4F, 0x0D54, 0x0D56, 0x0D58, 0x0D61,
            0x0D66, 0x0D7F, 0x0D82, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1,
            0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DD0, 0x0DD1,
            0x0DD8, 0x0DDE, 0x0DE6, 0x0DEF, 0x0DF2, 0x0DF4, 0x0E01, 0x0E30,
            0x0E32, 0x0E33, 0x0E3F, 0x0E46, 0x0E4F, 0x0E5B, 0x0E81, 0x0E82,
            0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5,
            0x0EA7, 0x0EB0, 0x0EB2, 0x0EB3, 0x0EBD, 0x0EBD, 0x0EC0, 0x0EC4,
            0x0EC6, 0x0EC6, 0x0ED0, 0x0ED9, 0x0EDC, 0x0EDF, 0x0F00, 0x0F17,
            0x0F1A, 0x0F34, 0x0F36, 0x0F36, 0x0F38, 0x0F38, 0x0F3A, 0x0F47,
            0x0F49, 0x0F6C, 0x0F7F, 0x0F7F, 0x0F85, 0x0F85, 0x0F88, 0x0F8C,
            0x0FBE, 0x0FC5, 0x0FC7, 0x0FCC, 0x0FCE, 0x0FDA, 0x1000, 0x102C,
            0x1031, 0x1031, 0x1038, 0x1038, 0x103B, 0x103C, 0x103F, 0x1057,
            0x105A, 0x105D, 0x1061, 0x1070, 0x1075, 0x1081, 0x1083, 0x1084,
            0x1087, 0x108C, 0x108E, 0x109C, 0x109E, 0x10C5, 0x10C7, 0x10C7,
            0x10CD, 0x10CD, 0x10D0, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256,
            0x1258, 0x1258, 0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D,
            0x1290, 0x12B0, 0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0,
            0x12C2, 0x12C5, 0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315,
            0x1318, 0x135A, 0x1360, 0x137C, 0x1380, 0x1399, 0x13A0, 0x13F5,
            0x13F8, 0x13FD, 0x1400, 0x169C, 0x16A0, 0x16F8, 0x1700, 0x1711,
            0x1715, 0x1715, 0x171F, 0x1731, 0x1734, 0x1736, 0x1740, 0x1751,
            0x1760, 0x176C, 0x176E, 0x1770, 0x1780, 0x17B3, 0x17B6, 0x17B6,
            0x17BE, 0x17C5, 0x17C7, 0x17C8, 0x17D4, 0x17DC, 0x17E0, 0x17E9,
            0x17F0, 0x17F9, 0x1800, 0x180A, 0x1810, 0x1819, 0x1820, 0x1878,
            0x1880, 0x1884, 0x1887, 0x18A8, 0x18AA, 0x18AA, 0x18B0, 0x18F5,
            0x1900, 0x191E, 0x1923, 0x1926, 0x1929, 0x192B, 0x1930, 0x1931,
            0x1933, 0x1938, 0x1940, 0x1940, 0x1944, 0x196D, 0x1970, 0x1974,
            0x1980, 0x19AB, 0x19B0, 0x19C9, 0x19D0, 0x19DA, 0x19DE, 0x1A16,
            0x1A19, 0x1A1A, 0x1A1E, 0x1A55, 0x1A57, 0x1A57, 0x1A61, 0x1A61,
            0x1A63, 0x1A64, 0x1A6D, 0x1A72, 0x1A80, 0x1A89, 0x1A90, 0x1A99,
            0x1AA0, 0x1AAD, 0x1B04, 0x1B33, 0x1B3B, 0x1B3B, 0x1B3D, 0x1B41,
            0x1B43, 0x1B4C, 0x1B50, 0x1B6A, 0x1B74, 0x1B7E, 0x1B82, 0x1BA1,
            0x1BA6, 0x1BA7, 0x1BAA, 0x1BAA, 0x1BAE, 0x1BE5, 0x1BE7, 0x1BE7,
            0x1BEA, 0x1BEC, 0x1BEE, 0x1BEE, 0x1BF2, 0x1BF3, 0x1BFC, 0x1C2B,
            0x1C34, 0x1C35, 0x1C3B, 0x1C49, 0x1C4D, 0x1C88, 0x1C90, 0x1CBA,
            0x1CBD, 0x1CC7, 0x1CD3, 0x1CD3, 0x1CE1, 0x1CE1, 0x1CE9, 0x1CEC,
            0x1CEE, 0x1CF3, 0x1CF5, 0x1CF7, 0x1CFA, 0x1CFA, 0x1D00, 0x1DBF,
            0x1E00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45, 0x1F48, 0x1F4D,
            0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D,
            0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FC4, 0x1FC6, 0x1FD3,
            0x1FD6, 0x1FDB, 0x1FDD, 0x1FEF, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFE,
            0x2000, 0x200A, 0x2010, 0x2027, 0x202F, 0x205F, 0x2070, 0x2071,
            0x2074, 0x208E, 0x2090, 0x209C, 0x20A0, 0x20C0, 0x2100, 0x218B,
            0x2190, 0x2426, 0x2440, 0x244A, 0x2460, 0x2B73, 0x2B76, 0x2B95,
            0x2B97, 0x2CEE, 0x2CF2, 0x2CF3, 0x2CF9, 0x2D25, 0x2D27, 0x2D27,
            0x2D2D, 0x2D2D, 0x2D30, 0x2D67, 0x2D6F, 0x2D70, 0x2D80, 0x2D96,
            0x2DA0, 0x2DA6, 0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6, 0x2DB8, 0x2DBE,
            0x2DC0, 0x2DC6, 0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6, 0x2DD8, 0x2DDE,
            0x2E00, 0x2E5D, 0x2E80, 0x2E99, 0x2E9B, 0x2EF3, 0x2F00, 0x2FD5,
            0x2FF0, 0x2FFB, 0x3000, 0x3029, 0x3030, 0x303F, 0x3041, 0x3096,
            0x309B, 0x30FF, 0x3105, 0x312F, 0x3131, 0x318E, 0x3190, 0x31E3,
            0x31F0, 0x321E, 0x3220, 0xA48C, 0xA490, 0xA4C6, 0xA4D0, 0xA62B,
            0xA640, 0xA66E, 0xA673, 0xA673, 0xA67E, 0xA69D, 0xA6A0, 0xA6EF,
            0xA6F2, 0xA6F7, 0xA700, 0xA7CA, 0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3,
            0xA7D5, 0xA7D9, 0xA7F2, 0xA801, 0xA803, 0xA805, 0xA807, 0xA80A,
            0xA80C, 0xA824, 0xA827, 0xA82B, 0xA830, 0xA839, 0xA840, 0xA877,
            0xA880, 0xA8C3, 0xA8CE, 0xA8D9, 0xA8F2, 0xA8FE, 0xA900, 0xA925,
            0xA92E, 0xA946, 0xA952, 0xA953, 0xA95F, 0xA97C, 0xA983, 0xA9B2,
            0xA9B4, 0xA9B5, 0xA9BA, 0xA9BB, 0xA9BE, 0xA9CD, 0xA9CF, 0xA9D9,
            0xA9DE, 0xA9E4, 0xA9E6, 0xA9FE, 0xAA00, 0xAA28, 0xAA2F, 0xAA30,
            0xAA33, 0xAA34, 0xAA40, 0xAA42, 0xAA44, 0xAA4B, 0xAA4D, 0xAA4D,
            0xAA50, 0xAA59, 0xAA5C, 0xAA7B, 0xAA7D, 0xAAAF, 0xAAB1, 0xAAB1,
            0xAAB5, 0xAAB6, 0xAAB9, 0xAABD, 0xAAC0, 0xAAC0, 0xAAC2, 0xAAC2,
            0xAADB, 0xAAEB, 0xAAEE, 0xAAF5, 0xAB01, 0xAB06, 0xAB09, 0xAB0E,
            0xAB11, 0xAB16, 0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xAB30, 0xAB6B,
            0xAB70, 0xABE4, 0xABE6, 0xABE7, 0xABE9, 0xABEC, 0xABF0, 0xABF9,
            0xAC00, 0xD7A3, 0xD7B0, 0xD7C6, 0xD7CB, 0xD7FB, 0xF900, 0xFA6D,
            0xFA70, 0xFAD9, 0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFB1D, 0xFB1D,
            0xFB1F, 0xFB36, 0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41,
            0xFB43, 0xFB44, 0xFB46, 0xFBC2, 0xFBD3, 0xFD8F, 0xFD92, 0xFDC7,
            0xFDCF, 0xFDCF, 0xFDF0, 0xFDFF, 0xFE10, 0xFE19, 0xFE30, 0xFE52,
            0xFE54, 0xFE66, 0xFE68, 0xFE6B, 0xFE70, 0xFE74, 0xFE76, 0xFEFC,
            0xFF01, 0xFF9D, 0xFFA0, 0xFFBE, 0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF,
            0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC, 0xFFE0, 0xFFE6, 0xFFE8, 0xFFEE,
            0xFFFC, 0xFFFD, 0x10000, 0x1000B, 0x1000D, 0x10026, 0x10028, 0x1003A,
            0x1003C, 0x1003D, 0x1003F, 0x1004D, 0x10050, 0x1005D, 0x10080, 0x100FA,
            0x10100, 0x10102, 0x10107, 0x10133, 0x10137, 0x1018E, 0x10190, 0x1019C,
            0x101A0, 0x101A0, 0x101D0, 0x101FC, 0x10280, 0x1029C, 0x102A0, 0x102D0,
            0x102E1, 0x102FB, 0x10300, 0x10323, 0x1032D, 0x1034A, 0x10350, 0x10375,
            0x10380, 0x1039D, 0x1039F, 0x103C3, 0x103C8, 0x103D5, 0x10400, 0x1049D,
            0x104A0, 0x104A9, 0x104B0, 0x104D3, 0x104D8, 0x104FB, 0x10500, 0x10527,
            0x10530, 0x10563, 0x1056F, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592,
            0x10594, 0x10595, 0x10597, 0x105A1, 0x105A3, 0x105B1, 0x105B3, 0x105B9,
            0x105BB, 0x105BC, 0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767,
            0x10780, 0x10785, 0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10800, 0x10805,
            0x10808, 0x10808, 0x1080A, 0x10835, 0x10837, 0x10838, 0x1083C, 0x1083C,
            0x1083F, 0x10855, 0x10857, 0x1089E, 0x108A7, 0x108AF, 0x108E0, 0x108F2,
            0x108F4, 0x108F5, 0x108FB, 0x1091B, 0x1091F, 0x10939, 0x1093F, 0x1093F,
            0x10980, 0x109B7, 0x109BC, 0x109CF, 0x109D2, 0x10A00, 0x10A10, 0x10A13,
            0x10A15, 0x10A17, 0x10A19, 0x10A35, 0x10A40, 0x10A48, 0x10A50, 0x10A58,
            0x10A60, 0x10A9F, 0x10AC0, 0x10AE4, 0x10AEB, 0x10AF6, 0x10B00, 0x10B35,
            0x10B39, 0x10B55, 0x10B58, 0x10B72, 0x10B78, 0x10B91, 0x10B99, 0x10B9C,
            0x10BA9, 0x10BAF, 0x10C00, 0x10C48, 0x10C80, 0x10CB2, 0x10CC0, 0x10CF2,
            0x10CFA, 0x10D23, 0x10D30, 0x10D39, 0x10E60, 0x10E7E, 0x10E80, 0x10EA9,
            0x10EAD, 0x10EAD, 0x10EB0, 0x10EB1, 0x10F00, 0x10F27, 0x10F30, 0x10F45,
            0x10F51, 0x10F59, 0x10F70, 0x10F81, 0x10F86, 0x10F89, 0x10FB0, 0x10FCB,
            0x10FE0, 0x10FF6, 0x11000, 0x11000, 0x11002, 0x11037, 0x11047, 0x1104D,
            0x11052, 0x1106F, 0x11071, 0x11072, 0x11075, 0x11075, 0x11082, 0x110B2,
            0x110B7, 0x110B8, 0x110BB, 0x110BC, 0x110BE, 0x110C1, 0x110D0, 0x110E8,
            0x110F0, 0x110F9, 0x11103, 0x11126, 0x1112C, 0x1112C, 0x11136, 0x11147,
            0x11150, 0x11172, 0x11174, 0x11176, 0x11182, 0x111B5, 0x111BF, 0x111C8,
            0x111CD, 0x111CE, 0x111D0, 0x111DF, 0x111E1, 0x111F4, 0x11200, 0x11211,
            0x11213, 0x1122E, 0x11232, 0x11233, 0x11235, 0x11235, 0x11238, 0x1123D,
            0x11280, 0x11286, 0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D,
            0x1129F, 0x112A9, 0x112B0, 0x112DE, 0x112E0, 0x112E2, 0x112F0, 0x112F9,
            0x11302, 0x11303, 0x11305, 0x1130C, 0x1130F, 0x11310, 0x11313, 0x11328,
            0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339, 0x1133D, 0x1133D,
            0x1133F, 0x1133F, 0x11341, 0x11344, 0x11347, 0x11348, 0x1134B, 0x1134D,
            0x11350, 0x11350, 0x1135D, 0x11363, 0x11400, 0x11437, 0x11440, 0x11441,
            0x11445, 0x11445, 0x11447, 0x1145B, 0x1145D, 0x1145D, 0x1145F, 0x11461,
            0x11480, 0x114AF, 0x114B1, 0x114B2, 0x114B9, 0x114B9, 0x114BB, 0x114BC,
            0x114BE, 0x114BE, 0x114C1, 0x114C1, 0x114C4, 0x114C7, 0x114D0, 0x114D9,
            0x11580, 0x115AE, 0x115B0, 0x115B1, 0x115B8, 0x115BB, 0x115BE, 0x115BE,
            0x115C1, 0x115DB, 0x11600, 0x11632, 0x1163B, 0x1163C, 0x1163E, 0x1163E,
            0x11641, 0x11644, 0x11650, 0x11659, 0x11660, 0x1166C, 0x11680, 0x116AA,
            0x116AC, 0x116AC, 0x116AE, 0x116AF, 0x116B6, 0x116B6, 0x116B8, 0x116B9,
            0x116C0, 0x116C9, 0x11700, 0x1171A, 0x11720, 0x11721, 0x11726, 0x11726,
            0x11730, 0x11746, 0x11800, 0x1182E, 0x11838, 0x11838, 0x1183B, 0x1183B,
            0x118A0, 0x118F2, 0x118FF, 0x11906, 0x11909, 0x11909, 0x1190C, 0x11913,
            0x11915, 0x11916, 0x11918, 0x1192F, 0x11931, 0x11935, 0x11937, 0x11938,
            0x1193D, 0x1193D, 0x1193F, 0x11942, 0x11944, 0x11946, 0x11950, 0x11959,
            0x119A0, 0x119A7, 0x119AA, 0x119D3, 0x119DC, 0x119DF, 0x119E1, 0x119E4,
            0x11A00, 0x11A00, 0x11A0B, 0x11A32, 0x11A39, 0x11A3A, 0x11A3F, 0x11A46,
            0x11A50, 0x11A50, 0x11A57, 0x11A58, 0x11A5C, 0x11A89, 0x11A97, 0x11A97,
            0x11A9A, 0x11AA2, 0x11AB0, 0x11AF8, 0x11C00, 0x11C08, 0x11C0A, 0x11C2F,
            0x11C3E, 0x11C3E, 0x11C40, 0x11C45, 0x11C50, 0x11C6C, 0x11C70, 0x11C8F,
            0x11CA9, 0x11CA9, 0x11CB1, 0x11CB1, 0x11CB4, 0x11CB4, 0x11D00, 0x11D06,
            0x11D08, 0x11D09, 0x11D0B, 0x11D30, 0x11D46, 0x11D46, 0x11D50, 0x11D59,
            0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D8E, 0x11D93, 0x11D94,
            0x11D96, 0x11D96, 0x11D98, 0x11D98, 0x11DA0, 0x11DA9, 0x11EE0, 0x11EF2,
            0x11EF5, 0x11EF8, 0x11FB0, 0x11FB0, 0x11FC0, 0x11FF1, 0x11FFF, 0x12399,
            0x12400, 0x1246E, 0x12470, 0x12474, 0x12480, 0x12543, 0x12F90, 0x12FF2,
            0x13000, 0x1342E, 0x14400, 0x14646, 0x16800, 0x16A38, 0x16A40, 0x16A5E,
            0x16A60, 0x16A69, 0x16A6E, 0x16ABE, 0x16AC0, 0x16AC9, 0x16AD0, 0x16AED,
            0x16AF5, 0x16AF5, 0x16B00, 0x16B2F, 0x16B37, 0x16B45, 0x16B50, 0x16B59,
            0x16B5B, 0x16B61, 0x16B63, 0x16B77, 0x16B7D, 0x16B8F, 0x16E40, 0x16E9A,
            0x16F00, 0x16F4A, 0x16F50, 0x16F87, 0x16F93, 0x16F9F, 0x16FE0, 0x16FE3,
            0x16FF0, 0x16FF1, 0x17000, 0x187F7, 0x18800, 0x18CD5, 0x18D00, 0x18D08,
            0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE, 0x1B000, 0x1B122,
            0x1B150, 0x1B152, 0x1B164, 0x1B167, 0x1B170, 0x1B2FB, 0x1BC00, 0x1BC6A,
            0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99, 0x1BC9C, 0x1BC9C,
            0x1BC9F, 0x1BC9F, 0x1CF50, 0x1CFC3, 0x1D000, 0x1D0F5, 0x1D100, 0x1D126,
            0x1D129, 0x1D164, 0x1D166, 0x1D166, 0x1D16A, 0x1D16D, 0x1D183, 0x1D184,
            0x1D18C, 0x1D1A9, 0x1D1AE, 0x1D1EA, 0x1D200, 0x1D241, 0x1D245, 0x1D245,
            0x1D2E0, 0x1D2F3, 0x1D300, 0x1D356, 0x1D360, 0x1D378, 0x1D400, 0x1D454,
            0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6,
            0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3,
            0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514, 0x1D516, 0x1D51C,
            0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544, 0x1D546, 0x1D546,
            0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D7CB, 0x1D7CE, 0x1D9FF,
            0x1DA37, 0x1DA3A, 0x1DA6D, 0x1DA74, 0x1DA76, 0x1DA83, 0x1DA85, 0x1DA8B,
            0x1DF00, 0x1DF1E, 0x1E100, 0x1E12C, 0x1E137, 0x1E13D, 0x1E140, 0x1E149,
            0x1E14E, 0x1E14F, 0x1E290, 0x1E2AD, 0x1E2C0, 0x1E2EB, 0x1E2F0, 0x1E2F9,
            0x1E2FF, 0x1E2FF, 0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE,
            0x1E7F0, 0x1E7FE, 0x1E800, 0x1E8C4, 0x1E8C7, 0x1E8CF, 0x1E900, 0x1E943,
            0x1E94B, 0x1E94B, 0x1E950, 0x1E959, 0x1E95E, 0x1E95F, 0x1EC71, 0x1ECB4,
            0x1ED01, 0x1ED3D, 0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22,
            0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37,
            0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47,
            0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52,
            0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B,
            0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64,
            0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C,
            0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3,
            0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB, 0x1EEF0, 0x1EEF1, 0x1F000, 0x1F02B,
            0x1F030, 0x1F093, 0x1F0A0, 0x1F0AE, 0x1F0B1, 0x1F0BF, 0x1F0C1, 0x1F0CF,
            0x1F0D1, 0x1F0F5, 0x1F100, 0x1F1AD, 0x1F1E6, 0x1F202, 0x1F210, 0x1F23B,
            0x1F240, 0x1F248, 0x1F250, 0x1F251, 0x1F260, 0x1F265, 0x1F300, 0x1F6D7,
            0x1F6DD, 0x1F6EC, 0x1F6F0, 0x1F6FC, 0x1F700, 0x1F773, 0x1F780, 0x1F7D8,
            0x1F7E0, 0x1F7EB, 0x1F7F0, 0x1F7F0, 0x1F800, 0x1F80B, 0x1F810, 0x1F847,
            0x1F850, 0x1F859, 0x1F860, 0x1F887, 0x1F890, 0x1F8AD, 0x1F8B0, 0x1F8B1,
            0x1F900, 0x1FA53, 0x1FA60, 0x1FA6D, 0x1FA70, 0x1FA74, 0x1FA78, 0x1FA7C,
            0x1FA80, 0x1FA86, 0x1FA90, 0x1FAAC, 0x1FAB0, 0x1FABA, 0x1FAC0, 0x1FAC5,
            0x1FAD0, 0x1FAD9, 0x1FAE0, 0x1FAE7, 0x1FAF0, 0x1FAF6, 0x1FB00, 0x1FB92,
            0x1FB94, 0x1FBCA, 0x1FBF0, 0x1FBF9, 0x20000, 0x2A6DF, 0x2A700, 0x2B738,
            0x2B740, 0x2B81D, 0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D,
            0x30000, 0x3134A,
            //  #66 (10347+353): bp=Grapheme_Extend:Gr_Ext
            0x0300, 0x036F, 0x0483, 0x0489, 0x0591, 0x05BD, 0x05BF, 0x05BF,
            0x05C1, 0x05C2, 0x05C4, 0x05C5, 0x05C7, 0x05C7, 0x0610, 0x061A,
            0x064B, 0x065F, 0x0670, 0x0670, 0x06D6, 0x06DC, 0x06DF, 0x06E4,
            0x06E7, 0x06E8, 0x06EA, 0x06ED, 0x0711, 0x0711, 0x0730, 0x074A,
            0x07A6, 0x07B0, 0x07EB, 0x07F3, 0x07FD, 0x07FD, 0x0816, 0x0819,
            0x081B, 0x0823, 0x0825, 0x0827, 0x0829, 0x082D, 0x0859, 0x085B,
            0x0898, 0x089F, 0x08CA, 0x08E1, 0x08E3, 0x0902, 0x093A, 0x093A,
            0x093C, 0x093C, 0x0941, 0x0948, 0x094D, 0x094D, 0x0951, 0x0957,
            0x0962, 0x0963, 0x0981, 0x0981, 0x09BC, 0x09BC, 0x09BE, 0x09BE,
            0x09C1, 0x09C4, 0x09CD, 0x09CD, 0x09D7, 0x09D7, 0x09E2, 0x09E3,
            0x09FE, 0x09FE, 0x0A01, 0x0A02, 0x0A3C, 0x0A3C, 0x0A41, 0x0A42,
            0x0A47, 0x0A48, 0x0A4B, 0x0A4D, 0x0A51, 0x0A51, 0x0A70, 0x0A71,
            0x0A75, 0x0A75, 0x0A81, 0x0A82, 0x0ABC, 0x0ABC, 0x0AC1, 0x0AC5,
            0x0AC7, 0x0AC8, 0x0ACD, 0x0ACD, 0x0AE2, 0x0AE3, 0x0AFA, 0x0AFF,
            0x0B01, 0x0B01, 0x0B3C, 0x0B3C, 0x0B3E, 0x0B3F, 0x0B41, 0x0B44,
            0x0B4D, 0x0B4D, 0x0B55, 0x0B57, 0x0B62, 0x0B63, 0x0B82, 0x0B82,
            0x0BBE, 0x0BBE, 0x0BC0, 0x0BC0, 0x0BCD, 0x0BCD, 0x0BD7, 0x0BD7,
            0x0C00, 0x0C00, 0x0C04, 0x0C04, 0x0C3C, 0x0C3C, 0x0C3E, 0x0C40,
            0x0C46, 0x0C48, 0x0C4A, 0x0C4D, 0x0C55, 0x0C56, 0x0C62, 0x0C63,
            0x0C81, 0x0C81, 0x0CBC, 0x0CBC, 0x0CBF, 0x0CBF, 0x0CC2, 0x0CC2,
            0x0CC6, 0x0CC6, 0x0CCC, 0x0CCD, 0x0CD5, 0x0CD6, 0x0CE2, 0x0CE3,
            0x0D00, 0x0D01, 0x0D3B, 0x0D3C, 0x0D3E, 0x0D3E, 0x0D41, 0x0D44,
            0x0D4D, 0x0D4D, 0x0D57, 0x0D57, 0x0D62, 0x0D63, 0x0D81, 0x0D81,
            0x0DCA, 0x0DCA, 0x0DCF, 0x0DCF, 0x0DD2, 0x0DD4, 0x0DD6, 0x0DD6,
            0x0DDF, 0x0DDF, 0x0E31, 0x0E31, 0x0E34, 0x0E3A, 0x0E47, 0x0E4E,
            0x0EB1, 0x0EB1, 0x0EB4, 0x0EBC, 0x0EC8, 0x0ECD, 0x0F18, 0x0F19,
            0x0F35, 0x0F35, 0x0F37, 0x0F37, 0x0F39, 0x0F39, 0x0F71, 0x0F7E,
            0x0F80, 0x0F84, 0x0F86, 0x0F87, 0x0F8D, 0x0F97, 0x0F99, 0x0FBC,
            0x0FC6, 0x0FC6, 0x102D, 0x1030, 0x1032, 0x1037, 0x1039, 0x103A,
            0x103D, 0x103E, 0x1058, 0x1059, 0x105E, 0x1060, 0x1071, 0x1074,
            0x1082, 0x1082, 0x1085, 0x1086, 0x108D, 0x108D, 0x109D, 0x109D,
            0x135D, 0x135F, 0x1712, 0x1714, 0x1732, 0x1733, 0x1752, 0x1753,
            0x1772, 0x1773, 0x17B4, 0x17B5, 0x17B7, 0x17BD, 0x17C6, 0x17C6,
            0x17C9, 0x17D3, 0x17DD, 0x17DD, 0x180B, 0x180D, 0x180F, 0x180F,
            0x1885, 0x1886, 0x18A9, 0x18A9, 0x1920, 0x1922, 0x1927, 0x1928,
            0x1932, 0x1932, 0x1939, 0x193B, 0x1A17, 0x1A18, 0x1A1B, 0x1A1B,
            0x1A56, 0x1A56, 0x1A58, 0x1A5E, 0x1A60, 0x1A60, 0x1A62, 0x1A62,
            0x1A65, 0x1A6C, 0x1A73, 0x1A7C, 0x1A7F, 0x1A7F, 0x1AB0, 0x1ACE,
            0x1B00, 0x1B03, 0x1B34, 0x1B3A, 0x1B3C, 0x1B3C, 0x1B42, 0x1B42,
            0x1B6B, 0x1B73, 0x1B80, 0x1B81, 0x1BA2, 0x1BA5, 0x1BA8, 0x1BA9,
            0x1BAB, 0x1BAD, 0x1BE6, 0x1BE6, 0x1BE8, 0x1BE9, 0x1BED, 0x1BED,
            0x1BEF, 0x1BF1, 0x1C2C, 0x1C33, 0x1C36, 0x1C37, 0x1CD0, 0x1CD2,
            0x1CD4, 0x1CE0, 0x1CE2, 0x1CE8, 0x1CED, 0x1CED, 0x1CF4, 0x1CF4,
            0x1CF8, 0x1CF9, 0x1DC0, 0x1DFF, 0x200C, 0x200C, 0x20D0, 0x20F0,
            0x2CEF, 0x2CF1, 0x2D7F, 0x2D7F, 0x2DE0, 0x2DFF, 0x302A, 0x302F,
            0x3099, 0x309A, 0xA66F, 0xA672, 0xA674, 0xA67D, 0xA69E, 0xA69F,
            0xA6F0, 0xA6F1, 0xA802, 0xA802, 0xA806, 0xA806, 0xA80B, 0xA80B,
            0xA825, 0xA826, 0xA82C, 0xA82C, 0xA8C4, 0xA8C5, 0xA8E0, 0xA8F1,
            0xA8FF, 0xA8FF, 0xA926, 0xA92D, 0xA947, 0xA951, 0xA980, 0xA982,
            0xA9B3, 0xA9B3, 0xA9B6, 0xA9B9, 0xA9BC, 0xA9BD, 0xA9E5, 0xA9E5,
            0xAA29, 0xAA2E, 0xAA31, 0xAA32, 0xAA35, 0xAA36, 0xAA43, 0xAA43,
            0xAA4C, 0xAA4C, 0xAA7C, 0xAA7C, 0xAAB0, 0xAAB0, 0xAAB2, 0xAAB4,
            0xAAB7, 0xAAB8, 0xAABE, 0xAABF, 0xAAC1, 0xAAC1, 0xAAEC, 0xAAED,
            0xAAF6, 0xAAF6, 0xABE5, 0xABE5, 0xABE8, 0xABE8, 0xABED, 0xABED,
            0xFB1E, 0xFB1E, 0xFE00, 0xFE0F, 0xFE20, 0xFE2F, 0xFF9E, 0xFF9F,
            0x101FD, 0x101FD, 0x102E0, 0x102E0, 0x10376, 0x1037A, 0x10A01, 0x10A03,
            0x10A05, 0x10A06, 0x10A0C, 0x10A0F, 0x10A38, 0x10A3A, 0x10A3F, 0x10A3F,
            0x10AE5, 0x10AE6, 0x10D24, 0x10D27, 0x10EAB, 0x10EAC, 0x10F46, 0x10F50,
            0x10F82, 0x10F85, 0x11001, 0x11001, 0x11038, 0x11046, 0x11070, 0x11070,
            0x11073, 0x11074, 0x1107F, 0x11081, 0x110B3, 0x110B6, 0x110B9, 0x110BA,
            0x110C2, 0x110C2, 0x11100, 0x11102, 0x11127, 0x1112B, 0x1112D, 0x11134,
            0x11173, 0x11173, 0x11180, 0x11181, 0x111B6, 0x111BE, 0x111C9, 0x111CC,
            0x111CF, 0x111CF, 0x1122F, 0x11231, 0x11234, 0x11234, 0x11236, 0x11237,
            0x1123E, 0x1123E, 0x112DF, 0x112DF, 0x112E3, 0x112EA, 0x11300, 0x11301,
            0x1133B, 0x1133C, 0x1133E, 0x1133E, 0x11340, 0x11340, 0x11357, 0x11357,
            0x11366, 0x1136C, 0x11370, 0x11374, 0x11438, 0x1143F, 0x11442, 0x11444,
            0x11446, 0x11446, 0x1145E, 0x1145E, 0x114B0, 0x114B0, 0x114B3, 0x114B8,
            0x114BA, 0x114BA, 0x114BD, 0x114BD, 0x114BF, 0x114C0, 0x114C2, 0x114C3,
            0x115AF, 0x115AF, 0x115B2, 0x115B5, 0x115BC, 0x115BD, 0x115BF, 0x115C0,
            0x115DC, 0x115DD, 0x11633, 0x1163A, 0x1163D, 0x1163D, 0x1163F, 0x11640,
            0x116AB, 0x116AB, 0x116AD, 0x116AD, 0x116B0, 0x116B5, 0x116B7, 0x116B7,
            0x1171D, 0x1171F, 0x11722, 0x11725, 0x11727, 0x1172B, 0x1182F, 0x11837,
            0x11839, 0x1183A, 0x11930, 0x11930, 0x1193B, 0x1193C, 0x1193E, 0x1193E,
            0x11943, 0x11943, 0x119D4, 0x119D7, 0x119DA, 0x119DB, 0x119E0, 0x119E0,
            0x11A01, 0x11A0A, 0x11A33, 0x11A38, 0x11A3B, 0x11A3E, 0x11A47, 0x11A47,
            0x11A51, 0x11A56, 0x11A59, 0x11A5B, 0x11A8A, 0x11A96, 0x11A98, 0x11A99,
            0x11C30, 0x11C36, 0x11C38, 0x11C3D, 0x11C3F, 0x11C3F, 0x11C92, 0x11CA7,
            0x11CAA, 0x11CB0, 0x11CB2, 0x11CB3, 0x11CB5, 0x11CB6, 0x11D31, 0x11D36,
            0x11D3A, 0x11D3A, 0x11D3C, 0x11D3D, 0x11D3F, 0x11D45, 0x11D47, 0x11D47,
            0x11D90, 0x11D91, 0x11D95, 0x11D95, 0x11D97, 0x11D97, 0x11EF3, 0x11EF4,
            0x16AF0, 0x16AF4, 0x16B30, 0x16B36, 0x16F4F, 0x16F4F, 0x16F8F, 0x16F92,
            0x16FE4, 0x16FE4, 0x1BC9D, 0x1BC9E, 0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46,
            0x1D165, 0x1D165, 0x1D167, 0x1D169, 0x1D16E, 0x1D172, 0x1D17B, 0x1D182,
            0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD, 0x1D242, 0x1D244, 0x1DA00, 0x1DA36,
            0x1DA3B, 0x1DA6C, 0x1DA75, 0x1DA75, 0x1DA84, 0x1DA84, 0x1DA9B, 0x1DA9F,
            0x1DAA1, 0x1DAAF, 0x1E000, 0x1E006, 0x1E008, 0x1E018, 0x1E01B, 0x1E021,
            0x1E023, 0x1E024, 0x1E026, 0x1E02A, 0x1E130, 0x1E136, 0x1E2AE, 0x1E2AE,
            0x1E2EC, 0x1E2EF, 0x1E8D0, 0x1E8D6, 0x1E944, 0x1E94A, 0xE0020, 0xE007F,
            0xE0100, 0xE01EF,
            //  #67 (10700+6): bp=Hex_Digit:Hex
            0x0030, 0x0039, 0x0041, 0x0046, 0x0061, 0x0066, 0xFF10, 0xFF19,
            0xFF21, 0xFF26, 0xFF41, 0xFF46,
            //  #68 (10706+2): bp=IDS_Binary_Operator:IDSB
            0x2FF0, 0x2FF1, 0x2FF4, 0x2FFB,
            //  #69 (10708+1): bp=IDS_Trinary_Operator:IDST
            0x2FF2, 0x2FF3,
            //  #70 (10709+756): bp=ID_Continue:IDC
            0x0030, 0x0039, 0x0041, 0x005A, 0x005F, 0x005F, 0x0061, 0x007A,
            0x00AA, 0x00AA, 0x00B5, 0x00B5, 0x00B7, 0x00B7, 0x00BA, 0x00BA,
            0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02C1, 0x02C6, 0x02D1,
            0x02E0, 0x02E4, 0x02EC, 0x02EC, 0x02EE, 0x02EE, 0x0300, 0x0374,
            0x0376, 0x0377, 0x037A, 0x037D, 0x037F, 0x037F, 0x0386, 0x038A,
            0x038C, 0x038C, 0x038E, 0x03A1, 0x03A3, 0x03F5, 0x03F7, 0x0481,
            0x0483, 0x0487, 0x048A, 0x052F, 0x0531, 0x0556, 0x0559, 0x0559,
            0x0560, 0x0588, 0x0591, 0x05BD, 0x05BF, 0x05BF, 0x05C1, 0x05C2,
            0x05C4, 0x05C5, 0x05C7, 0x05C7, 0x05D0, 0x05EA, 0x05EF, 0x05F2,
            0x0610, 0x061A, 0x0620, 0x0669, 0x066E, 0x06D3, 0x06D5, 0x06DC,
            0x06DF, 0x06E8, 0x06EA, 0x06FC, 0x06FF, 0x06FF, 0x0710, 0x074A,
            0x074D, 0x07B1, 0x07C0, 0x07F5, 0x07FA, 0x07FA, 0x07FD, 0x07FD,
            0x0800, 0x082D, 0x0840, 0x085B, 0x0860, 0x086A, 0x0870, 0x0887,
            0x0889, 0x088E, 0x0898, 0x08E1, 0x08E3, 0x0963, 0x0966, 0x096F,
            0x0971, 0x0983, 0x0985, 0x098C, 0x098F, 0x0990, 0x0993, 0x09A8,
            0x09AA, 0x09B0, 0x09B2, 0x09B2, 0x09B6, 0x09B9, 0x09BC, 0x09C4,
            0x09C7, 0x09C8, 0x09CB, 0x09CE, 0x09D7, 0x09D7, 0x09DC, 0x09DD,
            0x09DF, 0x09E3, 0x09E6, 0x09F1, 0x09FC, 0x09FC, 0x09FE, 0x09FE,
            0x0A01, 0x0A03, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10, 0x0A13, 0x0A28,
            0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36, 0x0A38, 0x0A39,
            0x0A3C, 0x0A3C, 0x0A3E, 0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4D,
            0x0A51, 0x0A51, 0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A66, 0x0A75,
            0x0A81, 0x0A83, 0x0A85, 0x0A8D, 0x0A8F, 0x0A91, 0x0A93, 0x0AA8,
            0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3, 0x0AB5, 0x0AB9, 0x0ABC, 0x0AC5,
            0x0AC7, 0x0AC9, 0x0ACB, 0x0ACD, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE3,
            0x0AE6, 0x0AEF, 0x0AF9, 0x0AFF, 0x0B01, 0x0B03, 0x0B05, 0x0B0C,
            0x0B0F, 0x0B10, 0x0B13, 0x0B28, 0x0B2A, 0x0B30, 0x0B32, 0x0B33,
            0x0B35, 0x0B39, 0x0B3C, 0x0B44, 0x0B47, 0x0B48, 0x0B4B, 0x0B4D,
            0x0B55, 0x0B57, 0x0B5C, 0x0B5D, 0x0B5F, 0x0B63, 0x0B66, 0x0B6F,
            0x0B71, 0x0B71, 0x0B82, 0x0B83, 0x0B85, 0x0B8A, 0x0B8E, 0x0B90,
            0x0B92, 0x0B95, 0x0B99, 0x0B9A, 0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F,
            0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA, 0x0BAE, 0x0BB9, 0x0BBE, 0x0BC2,
            0x0BC6, 0x0BC8, 0x0BCA, 0x0BCD, 0x0BD0, 0x0BD0, 0x0BD7, 0x0BD7,
            0x0BE6, 0x0BEF, 0x0C00, 0x0C0C, 0x0C0E, 0x0C10, 0x0C12, 0x0C28,
            0x0C2A, 0x0C39, 0x0C3C, 0x0C44, 0x0C46, 0x0C48, 0x0C4A, 0x0C4D,
            0x0C55, 0x0C56, 0x0C58, 0x0C5A, 0x0C5D, 0x0C5D, 0x0C60, 0x0C63,
            0x0C66, 0x0C6F, 0x0C80, 0x0C83, 0x0C85, 0x0C8C, 0x0C8E, 0x0C90,
            0x0C92, 0x0CA8, 0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9, 0x0CBC, 0x0CC4,
            0x0CC6, 0x0CC8, 0x0CCA, 0x0CCD, 0x0CD5, 0x0CD6, 0x0CDD, 0x0CDE,
            0x0CE0, 0x0CE3, 0x0CE6, 0x0CEF, 0x0CF1, 0x0CF2, 0x0D00, 0x0D0C,
            0x0D0E, 0x0D10, 0x0D12, 0x0D44, 0x0D46, 0x0D48, 0x0D4A, 0x0D4E,
            0x0D54, 0x0D57, 0x0D5F, 0x0D63, 0x0D66, 0x0D6F, 0x0D7A, 0x0D7F,
            0x0D81, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1, 0x0DB3, 0x0DBB,
            0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DCA, 0x0DCA, 0x0DCF, 0x0DD4,
            0x0DD6, 0x0DD6, 0x0DD8, 0x0DDF, 0x0DE6, 0x0DEF, 0x0DF2, 0x0DF3,
            0x0E01, 0x0E3A, 0x0E40, 0x0E4E, 0x0E50, 0x0E59, 0x0E81, 0x0E82,
            0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5,
            0x0EA7, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0EC8, 0x0ECD,
            0x0ED0, 0x0ED9, 0x0EDC, 0x0EDF, 0x0F00, 0x0F00, 0x0F18, 0x0F19,
            0x0F20, 0x0F29, 0x0F35, 0x0F35, 0x0F37, 0x0F37, 0x0F39, 0x0F39,
            0x0F3E, 0x0F47, 0x0F49, 0x0F6C, 0x0F71, 0x0F84, 0x0F86, 0x0F97,
            0x0F99, 0x0FBC, 0x0FC6, 0x0FC6, 0x1000, 0x1049, 0x1050, 0x109D,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA,
            0x10FC, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258,
            0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D, 0x1290, 0x12B0,
            0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0, 0x12C2, 0x12C5,
            0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315, 0x1318, 0x135A,
            0x135D, 0x135F, 0x1369, 0x1371, 0x1380, 0x138F, 0x13A0, 0x13F5,
            0x13F8, 0x13FD, 0x1401, 0x166C, 0x166F, 0x167F, 0x1681, 0x169A,
            0x16A0, 0x16EA, 0x16EE, 0x16F8, 0x1700, 0x1715, 0x171F, 0x1734,
            0x1740, 0x1753, 0x1760, 0x176C, 0x176E, 0x1770, 0x1772, 0x1773,
            0x1780, 0x17D3, 0x17D7, 0x17D7, 0x17DC, 0x17DD, 0x17E0, 0x17E9,
            0x180B, 0x180D, 0x180F, 0x1819, 0x1820, 0x1878, 0x1880, 0x18AA,
            0x18B0, 0x18F5, 0x1900, 0x191E, 0x1920, 0x192B, 0x1930, 0x193B,
            0x1946, 0x196D, 0x1970, 0x1974, 0x1980, 0x19AB, 0x19B0, 0x19C9,
            0x19D0, 0x19DA, 0x1A00, 0x1A1B, 0x1A20, 0x1A5E, 0x1A60, 0x1A7C,
            0x1A7F, 0x1A89, 0x1A90, 0x1A99, 0x1AA7, 0x1AA7, 0x1AB0, 0x1ABD,
            0x1ABF, 0x1ACE, 0x1B00, 0x1B4C, 0x1B50, 0x1B59, 0x1B6B, 0x1B73,
            0x1B80, 0x1BF3, 0x1C00, 0x1C37, 0x1C40, 0x1C49, 0x1C4D, 0x1C7D,
            0x1C80, 0x1C88, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1CD0, 0x1CD2,
            0x1CD4, 0x1CFA, 0x1D00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45,
            0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B,
            0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FBC,
            0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC, 0x1FD0, 0x1FD3,
            0x1FD6, 0x1FDB, 0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFC,
            0x203F, 0x2040, 0x2054, 0x2054, 0x2071, 0x2071, 0x207F, 0x207F,
            0x2090, 0x209C, 0x20D0, 0x20DC, 0x20E1, 0x20E1, 0x20E5, 0x20F0,
            0x2102, 0x2102, 0x2107, 0x2107, 0x210A, 0x2113, 0x2115, 0x2115,
            0x2118, 0x211D, 0x2124, 0x2124, 0x2126, 0x2126, 0x2128, 0x2128,
            0x212A, 0x2139, 0x213C, 0x213F, 0x2145, 0x2149, 0x214E, 0x214E,
            0x2160, 0x2188, 0x2C00, 0x2CE4, 0x2CEB, 0x2CF3, 0x2D00, 0x2D25,
            0x2D27, 0x2D27, 0x2D2D, 0x2D2D, 0x2D30, 0x2D67, 0x2D6F, 0x2D6F,
            0x2D7F, 0x2D96, 0x2DA0, 0x2DA6, 0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6,
            0x2DB8, 0x2DBE, 0x2DC0, 0x2DC6, 0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6,
            0x2DD8, 0x2DDE, 0x2DE0, 0x2DFF, 0x3005, 0x3007, 0x3021, 0x302F,
            0x3031, 0x3035, 0x3038, 0x303C, 0x3041, 0x3096, 0x3099, 0x309F,
            0x30A1, 0x30FA, 0x30FC, 0x30FF, 0x3105, 0x312F, 0x3131, 0x318E,
            0x31A0, 0x31BF, 0x31F0, 0x31FF, 0x3400, 0x4DBF, 0x4E00, 0xA48C,
            0xA4D0, 0xA4FD, 0xA500, 0xA60C, 0xA610, 0xA62B, 0xA640, 0xA66F,
            0xA674, 0xA67D, 0xA67F, 0xA6F1, 0xA717, 0xA71F, 0xA722, 0xA788,
            0xA78B, 0xA7CA, 0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9,
            0xA7F2, 0xA827, 0xA82C, 0xA82C, 0xA840, 0xA873, 0xA880, 0xA8C5,
            0xA8D0, 0xA8D9, 0xA8E0, 0xA8F7, 0xA8FB, 0xA8FB, 0xA8FD, 0xA92D,
            0xA930, 0xA953, 0xA960, 0xA97C, 0xA980, 0xA9C0, 0xA9CF, 0xA9D9,
            0xA9E0, 0xA9FE, 0xAA00, 0xAA36, 0xAA40, 0xAA4D, 0xAA50, 0xAA59,
            0xAA60, 0xAA76, 0xAA7A, 0xAAC2, 0xAADB, 0xAADD, 0xAAE0, 0xAAEF,
            0xAAF2, 0xAAF6, 0xAB01, 0xAB06, 0xAB09, 0xAB0E, 0xAB11, 0xAB16,
            0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xAB30, 0xAB5A, 0xAB5C, 0xAB69,
            0xAB70, 0xABEA, 0xABEC, 0xABED, 0xABF0, 0xABF9, 0xAC00, 0xD7A3,
            0xD7B0, 0xD7C6, 0xD7CB, 0xD7FB, 0xF900, 0xFA6D, 0xFA70, 0xFAD9,
            0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFB1D, 0xFB28, 0xFB2A, 0xFB36,
            0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41, 0xFB43, 0xFB44,
            0xFB46, 0xFBB1, 0xFBD3, 0xFD3D, 0xFD50, 0xFD8F, 0xFD92, 0xFDC7,
            0xFDF0, 0xFDFB, 0xFE00, 0xFE0F, 0xFE20, 0xFE2F, 0xFE33, 0xFE34,
            0xFE4D, 0xFE4F, 0xFE70, 0xFE74, 0xFE76, 0xFEFC, 0xFF10, 0xFF19,
            0xFF21, 0xFF3A, 0xFF3F, 0xFF3F, 0xFF41, 0xFF5A, 0xFF66, 0xFFBE,
            0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC,
            0x10000, 0x1000B, 0x1000D, 0x10026, 0x10028, 0x1003A, 0x1003C, 0x1003D,
            0x1003F, 0x1004D, 0x10050, 0x1005D, 0x10080, 0x100FA, 0x10140, 0x10174,
            0x101FD, 0x101FD, 0x10280, 0x1029C, 0x102A0, 0x102D0, 0x102E0, 0x102E0,
            0x10300, 0x1031F, 0x1032D, 0x1034A, 0x10350, 0x1037A, 0x10380, 0x1039D,
            0x103A0, 0x103C3, 0x103C8, 0x103CF, 0x103D1, 0x103D5, 0x10400, 0x1049D,
            0x104A0, 0x104A9, 0x104B0, 0x104D3, 0x104D8, 0x104FB, 0x10500, 0x10527,
            0x10530, 0x10563, 0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592,
            0x10594, 0x10595, 0x10597, 0x105A1, 0x105A3, 0x105B1, 0x105B3, 0x105B9,
            0x105BB, 0x105BC, 0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767,
            0x10780, 0x10785, 0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10800, 0x10805,
            0x10808, 0x10808, 0x1080A, 0x10835, 0x10837, 0x10838, 0x1083C, 0x1083C,
            0x1083F, 0x10855, 0x10860, 0x10876, 0x10880, 0x1089E, 0x108E0, 0x108F2,
            0x108F4, 0x108F5, 0x10900, 0x10915, 0x10920, 0x10939, 0x10980, 0x109B7,
            0x109BE, 0x109BF, 0x10A00, 0x10A03, 0x10A05, 0x10A06, 0x10A0C, 0x10A13,
            0x10A15, 0x10A17, 0x10A19, 0x10A35, 0x10A38, 0x10A3A, 0x10A3F, 0x10A3F,
            0x10A60, 0x10A7C, 0x10A80, 0x10A9C, 0x10AC0, 0x10AC7, 0x10AC9, 0x10AE6,
            0x10B00, 0x10B35, 0x10B40, 0x10B55, 0x10B60, 0x10B72, 0x10B80, 0x10B91,
            0x10C00, 0x10C48, 0x10C80, 0x10CB2, 0x10CC0, 0x10CF2, 0x10D00, 0x10D27,
            0x10D30, 0x10D39, 0x10E80, 0x10EA9, 0x10EAB, 0x10EAC, 0x10EB0, 0x10EB1,
            0x10F00, 0x10F1C, 0x10F27, 0x10F27, 0x10F30, 0x10F50, 0x10F70, 0x10F85,
            0x10FB0, 0x10FC4, 0x10FE0, 0x10FF6, 0x11000, 0x11046, 0x11066, 0x11075,
            0x1107F, 0x110BA, 0x110C2, 0x110C2, 0x110D0, 0x110E8, 0x110F0, 0x110F9,
            0x11100, 0x11134, 0x11136, 0x1113F, 0x11144, 0x11147, 0x11150, 0x11173,
            0x11176, 0x11176, 0x11180, 0x111C4, 0x111C9, 0x111CC, 0x111CE, 0x111DA,
            0x111DC, 0x111DC, 0x11200, 0x11211, 0x11213, 0x11237, 0x1123E, 0x1123E,
            0x11280, 0x11286, 0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D,
            0x1129F, 0x112A8, 0x112B0, 0x112EA, 0x112F0, 0x112F9, 0x11300, 0x11303,
            0x11305, 0x1130C, 0x1130F, 0x11310, 0x11313, 0x11328, 0x1132A, 0x11330,
            0x11332, 0x11333, 0x11335, 0x11339, 0x1133B, 0x11344, 0x11347, 0x11348,
            0x1134B, 0x1134D, 0x11350, 0x11350, 0x11357, 0x11357, 0x1135D, 0x11363,
            0x11366, 0x1136C, 0x11370, 0x11374, 0x11400, 0x1144A, 0x11450, 0x11459,
            0x1145E, 0x11461, 0x11480, 0x114C5, 0x114C7, 0x114C7, 0x114D0, 0x114D9,
            0x11580, 0x115B5, 0x115B8, 0x115C0, 0x115D8, 0x115DD, 0x11600, 0x11640,
            0x11644, 0x11644, 0x11650, 0x11659, 0x11680, 0x116B8, 0x116C0, 0x116C9,
            0x11700, 0x1171A, 0x1171D, 0x1172B, 0x11730, 0x11739, 0x11740, 0x11746,
            0x11800, 0x1183A, 0x118A0, 0x118E9, 0x118FF, 0x11906, 0x11909, 0x11909,
            0x1190C, 0x11913, 0x11915, 0x11916, 0x11918, 0x11935, 0x11937, 0x11938,
            0x1193B, 0x11943, 0x11950, 0x11959, 0x119A0, 0x119A7, 0x119AA, 0x119D7,
            0x119DA, 0x119E1, 0x119E3, 0x119E4, 0x11A00, 0x11A3E, 0x11A47, 0x11A47,
            0x11A50, 0x11A99, 0x11A9D, 0x11A9D, 0x11AB0, 0x11AF8, 0x11C00, 0x11C08,
            0x11C0A, 0x11C36, 0x11C38, 0x11C40, 0x11C50, 0x11C59, 0x11C72, 0x11C8F,
            0x11C92, 0x11CA7, 0x11CA9, 0x11CB6, 0x11D00, 0x11D06, 0x11D08, 0x11D09,
            0x11D0B, 0x11D36, 0x11D3A, 0x11D3A, 0x11D3C, 0x11D3D, 0x11D3F, 0x11D47,
            0x11D50, 0x11D59, 0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D8E,
            0x11D90, 0x11D91, 0x11D93, 0x11D98, 0x11DA0, 0x11DA9, 0x11EE0, 0x11EF6,
            0x11FB0, 0x11FB0, 0x12000, 0x12399, 0x12400, 0x1246E, 0x12480, 0x12543,
            0x12F90, 0x12FF0, 0x13000, 0x1342E, 0x14400, 0x14646, 0x16800, 0x16A38,
            0x16A40, 0x16A5E, 0x16A60, 0x16A69, 0x16A70, 0x16ABE, 0x16AC0, 0x16AC9,
            0x16AD0, 0x16AED, 0x16AF0, 0x16AF4, 0x16B00, 0x16B36, 0x16B40, 0x16B43,
            0x16B50, 0x16B59, 0x16B63, 0x16B77, 0x16B7D, 0x16B8F, 0x16E40, 0x16E7F,
            0x16F00, 0x16F4A, 0x16F4F, 0x16F87, 0x16F8F, 0x16F9F, 0x16FE0, 0x16FE1,
            0x16FE3, 0x16FE4, 0x16FF0, 0x16FF1, 0x17000, 0x187F7, 0x18800, 0x18CD5,
            0x18D00, 0x18D08, 0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE,
            0x1B000, 0x1B122, 0x1B150, 0x1B152, 0x1B164, 0x1B167, 0x1B170, 0x1B2FB,
            0x1BC00, 0x1BC6A, 0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99,
            0x1BC9D, 0x1BC9E, 0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46, 0x1D165, 0x1D169,
            0x1D16D, 0x1D172, 0x1D17B, 0x1D182, 0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD,
            0x1D242, 0x1D244, 0x1D400, 0x1D454, 0x1D456, 0x1D49C, 0x1D49E, 0x1D49F,
            0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9,
            0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A,
            0x1D50D, 0x1D514, 0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E,
            0x1D540, 0x1D544, 0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A5,
            0x1D6A8, 0x1D6C0, 0x1D6C2, 0x1D6DA, 0x1D6DC, 0x1D6FA, 0x1D6FC, 0x1D714,
            0x1D716, 0x1D734, 0x1D736, 0x1D74E, 0x1D750, 0x1D76E, 0x1D770, 0x1D788,
            0x1D78A, 0x1D7A8, 0x1D7AA, 0x1D7C2, 0x1D7C4, 0x1D7CB, 0x1D7CE, 0x1D7FF,
            0x1DA00, 0x1DA36, 0x1DA3B, 0x1DA6C, 0x1DA75, 0x1DA75, 0x1DA84, 0x1DA84,
            0x1DA9B, 0x1DA9F, 0x1DAA1, 0x1DAAF, 0x1DF00, 0x1DF1E, 0x1E000, 0x1E006,
            0x1E008, 0x1E018, 0x1E01B, 0x1E021, 0x1E023, 0x1E024, 0x1E026, 0x1E02A,
            0x1E100, 0x1E12C, 0x1E130, 0x1E13D, 0x1E140, 0x1E149, 0x1E14E, 0x1E14E,
            0x1E290, 0x1E2AE, 0x1E2C0, 0x1E2F9, 0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB,
            0x1E7ED, 0x1E7EE, 0x1E7F0, 0x1E7FE, 0x1E800, 0x1E8C4, 0x1E8D0, 0x1E8D6,
            0x1E900, 0x1E94B, 0x1E950, 0x1E959, 0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F,
            0x1EE21, 0x1EE22, 0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32,
            0x1EE34, 0x1EE37, 0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42,
            0x1EE47, 0x1EE47, 0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F,
            0x1EE51, 0x1EE52, 0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59,
            0x1EE5B, 0x1EE5B, 0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62,
            0x1EE64, 0x1EE64, 0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77,
            0x1EE79, 0x1EE7C, 0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B,
            0x1EEA1, 0x1EEA3, 0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB, 0x1FBF0, 0x1FBF9,
            0x20000, 0x2A6DF, 0x2A700, 0x2B738, 0x2B740, 0x2B81D, 0x2B820, 0x2CEA1,
            0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D, 0x30000, 0x3134A, 0xE0100, 0xE01EF,
            //  #71 (11465+648): bp=ID_Start:IDS
            0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00B5, 0x00B5,
            0x00BA, 0x00BA, 0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02C1,
            0x02C6, 0x02D1, 0x02E0, 0x02E4, 0x02EC, 0x02EC, 0x02EE, 0x02EE,
            0x0370, 0x0374, 0x0376, 0x0377, 0x037A, 0x037D, 0x037F, 0x037F,
            0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x03A1,
            0x03A3, 0x03F5, 0x03F7, 0x0481, 0x048A, 0x052F, 0x0531, 0x0556,
            0x0559, 0x0559, 0x0560, 0x0588, 0x05D0, 0x05EA, 0x05EF, 0x05F2,
            0x0620, 0x064A, 0x066E, 0x066F, 0x0671, 0x06D3, 0x06D5, 0x06D5,
            0x06E5, 0x06E6, 0x06EE, 0x06EF, 0x06FA, 0x06FC, 0x06FF, 0x06FF,
            0x0710, 0x0710, 0x0712, 0x072F, 0x074D, 0x07A5, 0x07B1, 0x07B1,
            0x07CA, 0x07EA, 0x07F4, 0x07F5, 0x07FA, 0x07FA, 0x0800, 0x0815,
            0x081A, 0x081A, 0x0824, 0x0824, 0x0828, 0x0828, 0x0840, 0x0858,
            0x0860, 0x086A, 0x0870, 0x0887, 0x0889, 0x088E, 0x08A0, 0x08C9,
            0x0904, 0x0939, 0x093D, 0x093D, 0x0950, 0x0950, 0x0958, 0x0961,
            0x0971, 0x0980, 0x0985, 0x098C, 0x098F, 0x0990, 0x0993, 0x09A8,
            0x09AA, 0x09B0, 0x09B2, 0x09B2, 0x09B6, 0x09B9, 0x09BD, 0x09BD,
            0x09CE, 0x09CE, 0x09DC, 0x09DD, 0x09DF, 0x09E1, 0x09F0, 0x09F1,
            0x09FC, 0x09FC, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10, 0x0A13, 0x0A28,
            0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36, 0x0A38, 0x0A39,
            0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A72, 0x0A74, 0x0A85, 0x0A8D,
            0x0A8F, 0x0A91, 0x0A93, 0x0AA8, 0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3,
            0x0AB5, 0x0AB9, 0x0ABD, 0x0ABD, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE1,
            0x0AF9, 0x0AF9, 0x0B05, 0x0B0C, 0x0B0F, 0x0B10, 0x0B13, 0x0B28,
            0x0B2A, 0x0B30, 0x0B32, 0x0B33, 0x0B35, 0x0B39, 0x0B3D, 0x0B3D,
            0x0B5C, 0x0B5D, 0x0B5F, 0x0B61, 0x0B71, 0x0B71, 0x0B83, 0x0B83,
            0x0B85, 0x0B8A, 0x0B8E, 0x0B90, 0x0B92, 0x0B95, 0x0B99, 0x0B9A,
            0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA,
            0x0BAE, 0x0BB9, 0x0BD0, 0x0BD0, 0x0C05, 0x0C0C, 0x0C0E, 0x0C10,
            0x0C12, 0x0C28, 0x0C2A, 0x0C39, 0x0C3D, 0x0C3D, 0x0C58, 0x0C5A,
            0x0C5D, 0x0C5D, 0x0C60, 0x0C61, 0x0C80, 0x0C80, 0x0C85, 0x0C8C,
            0x0C8E, 0x0C90, 0x0C92, 0x0CA8, 0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9,
            0x0CBD, 0x0CBD, 0x0CDD, 0x0CDE, 0x0CE0, 0x0CE1, 0x0CF1, 0x0CF2,
            0x0D04, 0x0D0C, 0x0D0E, 0x0D10, 0x0D12, 0x0D3A, 0x0D3D, 0x0D3D,
            0x0D4E, 0x0D4E, 0x0D54, 0x0D56, 0x0D5F, 0x0D61, 0x0D7A, 0x0D7F,
            0x0D85, 0x0D96, 0x0D9A, 0x0DB1, 0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD,
            0x0DC0, 0x0DC6, 0x0E01, 0x0E30, 0x0E32, 0x0E33, 0x0E40, 0x0E46,
            0x0E81, 0x0E82, 0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3,
            0x0EA5, 0x0EA5, 0x0EA7, 0x0EB0, 0x0EB2, 0x0EB3, 0x0EBD, 0x0EBD,
            0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0EDC, 0x0EDF, 0x0F00, 0x0F00,
            0x0F40, 0x0F47, 0x0F49, 0x0F6C, 0x0F88, 0x0F8C, 0x1000, 0x102A,
            0x103F, 0x103F, 0x1050, 0x1055, 0x105A, 0x105D, 0x1061, 0x1061,
            0x1065, 0x1066, 0x106E, 0x1070, 0x1075, 0x1081, 0x108E, 0x108E,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA,
            0x10FC, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258,
            0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D, 0x1290, 0x12B0,
            0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0, 0x12C2, 0x12C5,
            0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315, 0x1318, 0x135A,
            0x1380, 0x138F, 0x13A0, 0x13F5, 0x13F8, 0x13FD, 0x1401, 0x166C,
            0x166F, 0x167F, 0x1681, 0x169A, 0x16A0, 0x16EA, 0x16EE, 0x16F8,
            0x1700, 0x1711, 0x171F, 0x1731, 0x1740, 0x1751, 0x1760, 0x176C,
            0x176E, 0x1770, 0x1780, 0x17B3, 0x17D7, 0x17D7, 0x17DC, 0x17DC,
            0x1820, 0x1878, 0x1880, 0x18A8, 0x18AA, 0x18AA, 0x18B0, 0x18F5,
            0x1900, 0x191E, 0x1950, 0x196D, 0x1970, 0x1974, 0x1980, 0x19AB,
            0x19B0, 0x19C9, 0x1A00, 0x1A16, 0x1A20, 0x1A54, 0x1AA7, 0x1AA7,
            0x1B05, 0x1B33, 0x1B45, 0x1B4C, 0x1B83, 0x1BA0, 0x1BAE, 0x1BAF,
            0x1BBA, 0x1BE5, 0x1C00, 0x1C23, 0x1C4D, 0x1C4F, 0x1C5A, 0x1C7D,
            0x1C80, 0x1C88, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1CE9, 0x1CEC,
            0x1CEE, 0x1CF3, 0x1CF5, 0x1CF6, 0x1CFA, 0x1CFA, 0x1D00, 0x1DBF,
            0x1E00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45, 0x1F48, 0x1F4D,
            0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D,
            0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FBC, 0x1FBE, 0x1FBE,
            0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC, 0x1FD0, 0x1FD3, 0x1FD6, 0x1FDB,
            0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFC, 0x2071, 0x2071,
            0x207F, 0x207F, 0x2090, 0x209C, 0x2102, 0x2102, 0x2107, 0x2107,
            0x210A, 0x2113, 0x2115, 0x2115, 0x2118, 0x211D, 0x2124, 0x2124,
            0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x2139, 0x213C, 0x213F,
            0x2145, 0x2149, 0x214E, 0x214E, 0x2160, 0x2188, 0x2C00, 0x2CE4,
            0x2CEB, 0x2CEE, 0x2CF2, 0x2CF3, 0x2D00, 0x2D25, 0x2D27, 0x2D27,
            0x2D2D, 0x2D2D, 0x2D30, 0x2D67, 0x2D6F, 0x2D6F, 0x2D80, 0x2D96,
            0x2DA0, 0x2DA6, 0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6, 0x2DB8, 0x2DBE,
            0x2DC0, 0x2DC6, 0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6, 0x2DD8, 0x2DDE,
            0x3005, 0x3007, 0x3021, 0x3029, 0x3031, 0x3035, 0x3038, 0x303C,
            0x3041, 0x3096, 0x309B, 0x309F, 0x30A1, 0x30FA, 0x30FC, 0x30FF,
            0x3105, 0x312F, 0x3131, 0x318E, 0x31A0, 0x31BF, 0x31F0, 0x31FF,
            0x3400, 0x4DBF, 0x4E00, 0xA48C, 0xA4D0, 0xA4FD, 0xA500, 0xA60C,
            0xA610, 0xA61F, 0xA62A, 0xA62B, 0xA640, 0xA66E, 0xA67F, 0xA69D,
            0xA6A0, 0xA6EF, 0xA717, 0xA71F, 0xA722, 0xA788, 0xA78B, 0xA7CA,
            0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9, 0xA7F2, 0xA801,
            0xA803, 0xA805, 0xA807, 0xA80A, 0xA80C, 0xA822, 0xA840, 0xA873,
            0xA882, 0xA8B3, 0xA8F2, 0xA8F7, 0xA8FB, 0xA8FB, 0xA8FD, 0xA8FE,
            0xA90A, 0xA925, 0xA930, 0xA946, 0xA960, 0xA97C, 0xA984, 0xA9B2,
            0xA9CF, 0xA9CF, 0xA9E0, 0xA9E4, 0xA9E6, 0xA9EF, 0xA9FA, 0xA9FE,
            0xAA00, 0xAA28, 0xAA40, 0xAA42, 0xAA44, 0xAA4B, 0xAA60, 0xAA76,
            0xAA7A, 0xAA7A, 0xAA7E, 0xAAAF, 0xAAB1, 0xAAB1, 0xAAB5, 0xAAB6,
            0xAAB9, 0xAABD, 0xAAC0, 0xAAC0, 0xAAC2, 0xAAC2, 0xAADB, 0xAADD,
            0xAAE0, 0xAAEA, 0xAAF2, 0xAAF4, 0xAB01, 0xAB06, 0xAB09, 0xAB0E,
            0xAB11, 0xAB16, 0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xAB30, 0xAB5A,
            0xAB5C, 0xAB69, 0xAB70, 0xABE2, 0xAC00, 0xD7A3, 0xD7B0, 0xD7C6,
            0xD7CB, 0xD7FB, 0xF900, 0xFA6D, 0xFA70, 0xFAD9, 0xFB00, 0xFB06,
            0xFB13, 0xFB17, 0xFB1D, 0xFB1D, 0xFB1F, 0xFB28, 0xFB2A, 0xFB36,
            0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41, 0xFB43, 0xFB44,
            0xFB46, 0xFBB1, 0xFBD3, 0xFD3D, 0xFD50, 0xFD8F, 0xFD92, 0xFDC7,
            0xFDF0, 0xFDFB, 0xFE70, 0xFE74, 0xFE76, 0xFEFC, 0xFF21, 0xFF3A,
            0xFF41, 0xFF5A, 0xFF66, 0xFFBE, 0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF,
            0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC, 0x10000, 0x1000B, 0x1000D, 0x10026,
            0x10028, 0x1003A, 0x1003C, 0x1003D, 0x1003F, 0x1004D, 0x10050, 0x1005D,
            0x10080, 0x100FA, 0x10140, 0x10174, 0x10280, 0x1029C, 0x102A0, 0x102D0,
            0x10300, 0x1031F, 0x1032D, 0x1034A, 0x10350, 0x10375, 0x10380, 0x1039D,
            0x103A0, 0x103C3, 0x103C8, 0x103CF, 0x103D1, 0x103D5, 0x10400, 0x1049D,
            0x104B0, 0x104D3, 0x104D8, 0x104FB, 0x10500, 0x10527, 0x10530, 0x10563,
            0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592, 0x10594, 0x10595,
            0x10597, 0x105A1, 0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC,
            0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767, 0x10780, 0x10785,
            0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10800, 0x10805, 0x10808, 0x10808,
            0x1080A, 0x10835, 0x10837, 0x10838, 0x1083C, 0x1083C, 0x1083F, 0x10855,
            0x10860, 0x10876, 0x10880, 0x1089E, 0x108E0, 0x108F2, 0x108F4, 0x108F5,
            0x10900, 0x10915, 0x10920, 0x10939, 0x10980, 0x109B7, 0x109BE, 0x109BF,
            0x10A00, 0x10A00, 0x10A10, 0x10A13, 0x10A15, 0x10A17, 0x10A19, 0x10A35,
            0x10A60, 0x10A7C, 0x10A80, 0x10A9C, 0x10AC0, 0x10AC7, 0x10AC9, 0x10AE4,
            0x10B00, 0x10B35, 0x10B40, 0x10B55, 0x10B60, 0x10B72, 0x10B80, 0x10B91,
            0x10C00, 0x10C48, 0x10C80, 0x10CB2, 0x10CC0, 0x10CF2, 0x10D00, 0x10D23,
            0x10E80, 0x10EA9, 0x10EB0, 0x10EB1, 0x10F00, 0x10F1C, 0x10F27, 0x10F27,
            0x10F30, 0x10F45, 0x10F70, 0x10F81, 0x10FB0, 0x10FC4, 0x10FE0, 0x10FF6,
            0x11003, 0x11037, 0x11071, 0x11072, 0x11075, 0x11075, 0x11083, 0x110AF,
            0x110D0, 0x110E8, 0x11103, 0x11126, 0x11144, 0x11144, 0x11147, 0x11147,
            0x11150, 0x11172, 0x11176, 0x11176, 0x11183, 0x111B2, 0x111C1, 0x111C4,
            0x111DA, 0x111DA, 0x111DC, 0x111DC, 0x11200, 0x11211, 0x11213, 0x1122B,
            0x11280, 0x11286, 0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D,
            0x1129F, 0x112A8, 0x112B0, 0x112DE, 0x11305, 0x1130C, 0x1130F, 0x11310,
            0x11313, 0x11328, 0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339,
            0x1133D, 0x1133D, 0x11350, 0x11350, 0x1135D, 0x11361, 0x11400, 0x11434,
            0x11447, 0x1144A, 0x1145F, 0x11461, 0x11480, 0x114AF, 0x114C4, 0x114C5,
            0x114C7, 0x114C7, 0x11580, 0x115AE, 0x115D8, 0x115DB, 0x11600, 0x1162F,
            0x11644, 0x11644, 0x11680, 0x116AA, 0x116B8, 0x116B8, 0x11700, 0x1171A,
            0x11740, 0x11746, 0x11800, 0x1182B, 0x118A0, 0x118DF, 0x118FF, 0x11906,
            0x11909, 0x11909, 0x1190C, 0x11913, 0x11915, 0x11916, 0x11918, 0x1192F,
            0x1193F, 0x1193F, 0x11941, 0x11941, 0x119A0, 0x119A7, 0x119AA, 0x119D0,
            0x119E1, 0x119E1, 0x119E3, 0x119E3, 0x11A00, 0x11A00, 0x11A0B, 0x11A32,
            0x11A3A, 0x11A3A, 0x11A50, 0x11A50, 0x11A5C, 0x11A89, 0x11A9D, 0x11A9D,
            0x11AB0, 0x11AF8, 0x11C00, 0x11C08, 0x11C0A, 0x11C2E, 0x11C40, 0x11C40,
            0x11C72, 0x11C8F, 0x11D00, 0x11D06, 0x11D08, 0x11D09, 0x11D0B, 0x11D30,
            0x11D46, 0x11D46, 0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D89,
            0x11D98, 0x11D98, 0x11EE0, 0x11EF2, 0x11FB0, 0x11FB0, 0x12000, 0x12399,
            0x12400, 0x1246E, 0x12480, 0x12543, 0x12F90, 0x12FF0, 0x13000, 0x1342E,
            0x14400, 0x14646, 0x16800, 0x16A38, 0x16A40, 0x16A5E, 0x16A70, 0x16ABE,
            0x16AD0, 0x16AED, 0x16B00, 0x16B2F, 0x16B40, 0x16B43, 0x16B63, 0x16B77,
            0x16B7D, 0x16B8F, 0x16E40, 0x16E7F, 0x16F00, 0x16F4A, 0x16F50, 0x16F50,
            0x16F93, 0x16F9F, 0x16FE0, 0x16FE1, 0x16FE3, 0x16FE3, 0x17000, 0x187F7,
            0x18800, 0x18CD5, 0x18D00, 0x18D08, 0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB,
            0x1AFFD, 0x1AFFE, 0x1B000, 0x1B122, 0x1B150, 0x1B152, 0x1B164, 0x1B167,
            0x1B170, 0x1B2FB, 0x1BC00, 0x1BC6A, 0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88,
            0x1BC90, 0x1BC99, 0x1D400, 0x1D454, 0x1D456, 0x1D49C, 0x1D49E, 0x1D49F,
            0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9,
            0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A,
            0x1D50D, 0x1D514, 0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E,
            0x1D540, 0x1D544, 0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A5,
            0x1D6A8, 0x1D6C0, 0x1D6C2, 0x1D6DA, 0x1D6DC, 0x1D6FA, 0x1D6FC, 0x1D714,
            0x1D716, 0x1D734, 0x1D736, 0x1D74E, 0x1D750, 0x1D76E, 0x1D770, 0x1D788,
            0x1D78A, 0x1D7A8, 0x1D7AA, 0x1D7C2, 0x1D7C4, 0x1D7CB, 0x1DF00, 0x1DF1E,
            0x1E100, 0x1E12C, 0x1E137, 0x1E13D, 0x1E14E, 0x1E14E, 0x1E290, 0x1E2AD,
            0x1E2C0, 0x1E2EB, 0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE,
            0x1E7F0, 0x1E7FE, 0x1E800, 0x1E8C4, 0x1E900, 0x1E943, 0x1E94B, 0x1E94B,
            0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22, 0x1EE24, 0x1EE24,
            0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37, 0x1EE39, 0x1EE39,
            0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47, 0x1EE49, 0x1EE49,
            0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52, 0x1EE54, 0x1EE54,
            0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B, 0x1EE5D, 0x1EE5D,
            0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64, 0x1EE67, 0x1EE6A,
            0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C, 0x1EE7E, 0x1EE7E,
            0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3, 0x1EEA5, 0x1EEA9,
            0x1EEAB, 0x1EEBB, 0x20000, 0x2A6DF, 0x2A700, 0x2B738, 0x2B740, 0x2B81D,
            0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D, 0x30000, 0x3134A,
            //  #72 (12113+19): bp=Ideographic:Ideo
            0x3006, 0x3007, 0x3021, 0x3029, 0x3038, 0x303A, 0x3400, 0x4DBF,
            0x4E00, 0x9FFF, 0xF900, 0xFA6D, 0xFA70, 0xFAD9, 0x16FE4, 0x16FE4,
            0x17000, 0x187F7, 0x18800, 0x18CD5, 0x18D00, 0x18D08, 0x1B170, 0x1B2FB,
            0x20000, 0x2A6DF, 0x2A700, 0x2B738, 0x2B740, 0x2B81D, 0x2B820, 0x2CEA1,
            0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D, 0x30000, 0x3134A,
            //  #73 (12132+1): bp=Join_Control:Join_C
            0x200C, 0x200D,
            //  #74 (12133+7): bp=Logical_Order_Exception:LOE
            0x0E40, 0x0E44, 0x0EC0, 0x0EC4, 0x19B5, 0x19B7, 0x19BA, 0x19BA,
            0xAAB5, 0xAAB6, 0xAAB9, 0xAAB9, 0xAABB, 0xAABC,
            //  #75 (12140+668): bp=Lowercase:Lower
            0x0061, 0x007A, 0x00AA, 0x00AA, 0x00B5, 0x00B5, 0x00BA, 0x00BA,
            0x00DF, 0x00F6, 0x00F8, 0x00FF, 0x0101, 0x0101, 0x0103, 0x0103,
            0x0105, 0x0105, 0x0107, 0x0107, 0x0109, 0x0109, 0x010B, 0x010B,
            0x010D, 0x010D, 0x010F, 0x010F, 0x0111, 0x0111, 0x0113, 0x0113,
            0x0115, 0x0115, 0x0117, 0x0117, 0x0119, 0x0119, 0x011B, 0x011B,
            0x011D, 0x011D, 0x011F, 0x011F, 0x0121, 0x0121, 0x0123, 0x0123,
            0x0125, 0x0125, 0x0127, 0x0127, 0x0129, 0x0129, 0x012B, 0x012B,
            0x012D, 0x012D, 0x012F, 0x012F, 0x0131, 0x0131, 0x0133, 0x0133,
            0x0135, 0x0135, 0x0137, 0x0138, 0x013A, 0x013A, 0x013C, 0x013C,
            0x013E, 0x013E, 0x0140, 0x0140, 0x0142, 0x0142, 0x0144, 0x0144,
            0x0146, 0x0146, 0x0148, 0x0149, 0x014B, 0x014B, 0x014D, 0x014D,
            0x014F, 0x014F, 0x0151, 0x0151, 0x0153, 0x0153, 0x0155, 0x0155,
            0x0157, 0x0157, 0x0159, 0x0159, 0x015B, 0x015B, 0x015D, 0x015D,
            0x015F, 0x015F, 0x0161, 0x0161, 0x0163, 0x0163, 0x0165, 0x0165,
            0x0167, 0x0167, 0x0169, 0x0169, 0x016B, 0x016B, 0x016D, 0x016D,
            0x016F, 0x016F, 0x0171, 0x0171, 0x0173, 0x0173, 0x0175, 0x0175,
            0x0177, 0x0177, 0x017A, 0x017A, 0x017C, 0x017C, 0x017E, 0x0180,
            0x0183, 0x0183, 0x0185, 0x0185, 0x0188, 0x0188, 0x018C, 0x018D,
            0x0192, 0x0192, 0x0195, 0x0195, 0x0199, 0x019B, 0x019E, 0x019E,
            0x01A1, 0x01A1, 0x01A3, 0x01A3, 0x01A5, 0x01A5, 0x01A8, 0x01A8,
            0x01AA, 0x01AB, 0x01AD, 0x01AD, 0x01B0, 0x01B0, 0x01B4, 0x01B4,
            0x01B6, 0x01B6, 0x01B9, 0x01BA, 0x01BD, 0x01BF, 0x01C6, 0x01C6,
            0x01C9, 0x01C9, 0x01CC, 0x01CC, 0x01CE, 0x01CE, 0x01D0, 0x01D0,
            0x01D2, 0x01D2, 0x01D4, 0x01D4, 0x01D6, 0x01D6, 0x01D8, 0x01D8,
            0x01DA, 0x01DA, 0x01DC, 0x01DD, 0x01DF, 0x01DF, 0x01E1, 0x01E1,
            0x01E3, 0x01E3, 0x01E5, 0x01E5, 0x01E7, 0x01E7, 0x01E9, 0x01E9,
            0x01EB, 0x01EB, 0x01ED, 0x01ED, 0x01EF, 0x01F0, 0x01F3, 0x01F3,
            0x01F5, 0x01F5, 0x01F9, 0x01F9, 0x01FB, 0x01FB, 0x01FD, 0x01FD,
            0x01FF, 0x01FF, 0x0201, 0x0201, 0x0203, 0x0203, 0x0205, 0x0205,
            0x0207, 0x0207, 0x0209, 0x0209, 0x020B, 0x020B, 0x020D, 0x020D,
            0x020F, 0x020F, 0x0211, 0x0211, 0x0213, 0x0213, 0x0215, 0x0215,
            0x0217, 0x0217, 0x0219, 0x0219, 0x021B, 0x021B, 0x021D, 0x021D,
            0x021F, 0x021F, 0x0221, 0x0221, 0x0223, 0x0223, 0x0225, 0x0225,
            0x0227, 0x0227, 0x0229, 0x0229, 0x022B, 0x022B, 0x022D, 0x022D,
            0x022F, 0x022F, 0x0231, 0x0231, 0x0233, 0x0239, 0x023C, 0x023C,
            0x023F, 0x0240, 0x0242, 0x0242, 0x0247, 0x0247, 0x0249, 0x0249,
            0x024B, 0x024B, 0x024D, 0x024D, 0x024F, 0x0293, 0x0295, 0x02B8,
            0x02C0, 0x02C1, 0x02E0, 0x02E4, 0x0345, 0x0345, 0x0371, 0x0371,
            0x0373, 0x0373, 0x0377, 0x0377, 0x037A, 0x037D, 0x0390, 0x0390,
            0x03AC, 0x03CE, 0x03D0, 0x03D1, 0x03D5, 0x03D7, 0x03D9, 0x03D9,
            0x03DB, 0x03DB, 0x03DD, 0x03DD, 0x03DF, 0x03DF, 0x03E1, 0x03E1,
            0x03E3, 0x03E3, 0x03E5, 0x03E5, 0x03E7, 0x03E7, 0x03E9, 0x03E9,
            0x03EB, 0x03EB, 0x03ED, 0x03ED, 0x03EF, 0x03F3, 0x03F5, 0x03F5,
            0x03F8, 0x03F8, 0x03FB, 0x03FC, 0x0430, 0x045F, 0x0461, 0x0461,
            0x0463, 0x0463, 0x0465, 0x0465, 0x0467, 0x0467, 0x0469, 0x0469,
            0x046B, 0x046B, 0x046D, 0x046D, 0x046F, 0x046F, 0x0471, 0x0471,
            0x0473, 0x0473, 0x0475, 0x0475, 0x0477, 0x0477, 0x0479, 0x0479,
            0x047B, 0x047B, 0x047D, 0x047D, 0x047F, 0x047F, 0x0481, 0x0481,
            0x048B, 0x048B, 0x048D, 0x048D, 0x048F, 0x048F, 0x0491, 0x0491,
            0x0493, 0x0493, 0x0495, 0x0495, 0x0497, 0x0497, 0x0499, 0x0499,
            0x049B, 0x049B, 0x049D, 0x049D, 0x049F, 0x049F, 0x04A1, 0x04A1,
            0x04A3, 0x04A3, 0x04A5, 0x04A5, 0x04A7, 0x04A7, 0x04A9, 0x04A9,
            0x04AB, 0x04AB, 0x04AD, 0x04AD, 0x04AF, 0x04AF, 0x04B1, 0x04B1,
            0x04B3, 0x04B3, 0x04B5, 0x04B5, 0x04B7, 0x04B7, 0x04B9, 0x04B9,
            0x04BB, 0x04BB, 0x04BD, 0x04BD, 0x04BF, 0x04BF, 0x04C2, 0x04C2,
            0x04C4, 0x04C4, 0x04C6, 0x04C6, 0x04C8, 0x04C8, 0x04CA, 0x04CA,
            0x04CC, 0x04CC, 0x04CE, 0x04CF, 0x04D1, 0x04D1, 0x04D3, 0x04D3,
            0x04D5, 0x04D5, 0x04D7, 0x04D7, 0x04D9, 0x04D9, 0x04DB, 0x04DB,
            0x04DD, 0x04DD, 0x04DF, 0x04DF, 0x04E1, 0x04E1, 0x04E3, 0x04E3,
            0x04E5, 0x04E5, 0x04E7, 0x04E7, 0x04E9, 0x04E9, 0x04EB, 0x04EB,
            0x04ED, 0x04ED, 0x04EF, 0x04EF, 0x04F1, 0x04F1, 0x04F3, 0x04F3,
            0x04F5, 0x04F5, 0x04F7, 0x04F7, 0x04F9, 0x04F9, 0x04FB, 0x04FB,
            0x04FD, 0x04FD, 0x04FF, 0x04FF, 0x0501, 0x0501, 0x0503, 0x0503,
            0x0505, 0x0505, 0x0507, 0x0507, 0x0509, 0x0509, 0x050B, 0x050B,
            0x050D, 0x050D, 0x050F, 0x050F, 0x0511, 0x0511, 0x0513, 0x0513,
            0x0515, 0x0515, 0x0517, 0x0517, 0x0519, 0x0519, 0x051B, 0x051B,
            0x051D, 0x051D, 0x051F, 0x051F, 0x0521, 0x0521, 0x0523, 0x0523,
            0x0525, 0x0525, 0x0527, 0x0527, 0x0529, 0x0529, 0x052B, 0x052B,
            0x052D, 0x052D, 0x052F, 0x052F, 0x0560, 0x0588, 0x10D0, 0x10FA,
            0x10FD, 0x10FF, 0x13F8, 0x13FD, 0x1C80, 0x1C88, 0x1D00, 0x1DBF,
            0x1E01, 0x1E01, 0x1E03, 0x1E03, 0x1E05, 0x1E05, 0x1E07, 0x1E07,
            0x1E09, 0x1E09, 0x1E0B, 0x1E0B, 0x1E0D, 0x1E0D, 0x1E0F, 0x1E0F,
            0x1E11, 0x1E11, 0x1E13, 0x1E13, 0x1E15, 0x1E15, 0x1E17, 0x1E17,
            0x1E19, 0x1E19, 0x1E1B, 0x1E1B, 0x1E1D, 0x1E1D, 0x1E1F, 0x1E1F,
            0x1E21, 0x1E21, 0x1E23, 0x1E23, 0x1E25, 0x1E25, 0x1E27, 0x1E27,
            0x1E29, 0x1E29, 0x1E2B, 0x1E2B, 0x1E2D, 0x1E2D, 0x1E2F, 0x1E2F,
            0x1E31, 0x1E31, 0x1E33, 0x1E33, 0x1E35, 0x1E35, 0x1E37, 0x1E37,
            0x1E39, 0x1E39, 0x1E3B, 0x1E3B, 0x1E3D, 0x1E3D, 0x1E3F, 0x1E3F,
            0x1E41, 0x1E41, 0x1E43, 0x1E43, 0x1E45, 0x1E45, 0x1E47, 0x1E47,
            0x1E49, 0x1E49, 0x1E4B, 0x1E4B, 0x1E4D, 0x1E4D, 0x1E4F, 0x1E4F,
            0x1E51, 0x1E51, 0x1E53, 0x1E53, 0x1E55, 0x1E55, 0x1E57, 0x1E57,
            0x1E59, 0x1E59, 0x1E5B, 0x1E5B, 0x1E5D, 0x1E5D, 0x1E5F, 0x1E5F,
            0x1E61, 0x1E61, 0x1E63, 0x1E63, 0x1E65, 0x1E65, 0x1E67, 0x1E67,
            0x1E69, 0x1E69, 0x1E6B, 0x1E6B, 0x1E6D, 0x1E6D, 0x1E6F, 0x1E6F,
            0x1E71, 0x1E71, 0x1E73, 0x1E73, 0x1E75, 0x1E75, 0x1E77, 0x1E77,
            0x1E79, 0x1E79, 0x1E7B, 0x1E7B, 0x1E7D, 0x1E7D, 0x1E7F, 0x1E7F,
            0x1E81, 0x1E81, 0x1E83, 0x1E83, 0x1E85, 0x1E85, 0x1E87, 0x1E87,
            0x1E89, 0x1E89, 0x1E8B, 0x1E8B, 0x1E8D, 0x1E8D, 0x1E8F, 0x1E8F,
            0x1E91, 0x1E91, 0x1E93, 0x1E93, 0x1E95, 0x1E9D, 0x1E9F, 0x1E9F,
            0x1EA1, 0x1EA1, 0x1EA3, 0x1EA3, 0x1EA5, 0x1EA5, 0x1EA7, 0x1EA7,
            0x1EA9, 0x1EA9, 0x1EAB, 0x1EAB, 0x1EAD, 0x1EAD, 0x1EAF, 0x1EAF,
            0x1EB1, 0x1EB1, 0x1EB3, 0x1EB3, 0x1EB5, 0x1EB5, 0x1EB7, 0x1EB7,
            0x1EB9, 0x1EB9, 0x1EBB, 0x1EBB, 0x1EBD, 0x1EBD, 0x1EBF, 0x1EBF,
            0x1EC1, 0x1EC1, 0x1EC3, 0x1EC3, 0x1EC5, 0x1EC5, 0x1EC7, 0x1EC7,
            0x1EC9, 0x1EC9, 0x1ECB, 0x1ECB, 0x1ECD, 0x1ECD, 0x1ECF, 0x1ECF,
            0x1ED1, 0x1ED1, 0x1ED3, 0x1ED3, 0x1ED5, 0x1ED5, 0x1ED7, 0x1ED7,
            0x1ED9, 0x1ED9, 0x1EDB, 0x1EDB, 0x1EDD, 0x1EDD, 0x1EDF, 0x1EDF,
            0x1EE1, 0x1EE1, 0x1EE3, 0x1EE3, 0x1EE5, 0x1EE5, 0x1EE7, 0x1EE7,
            0x1EE9, 0x1EE9, 0x1EEB, 0x1EEB, 0x1EED, 0x1EED, 0x1EEF, 0x1EEF,
            0x1EF1, 0x1EF1, 0x1EF3, 0x1EF3, 0x1EF5, 0x1EF5, 0x1EF7, 0x1EF7,
            0x1EF9, 0x1EF9, 0x1EFB, 0x1EFB, 0x1EFD, 0x1EFD, 0x1EFF, 0x1F07,
            0x1F10, 0x1F15, 0x1F20, 0x1F27, 0x1F30, 0x1F37, 0x1F40, 0x1F45,
            0x1F50, 0x1F57, 0x1F60, 0x1F67, 0x1F70, 0x1F7D, 0x1F80, 0x1F87,
            0x1F90, 0x1F97, 0x1FA0, 0x1FA7, 0x1FB0, 0x1FB4, 0x1FB6, 0x1FB7,
            0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FC7, 0x1FD0, 0x1FD3,
            0x1FD6, 0x1FD7, 0x1FE0, 0x1FE7, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FF7,
            0x2071, 0x2071, 0x207F, 0x207F, 0x2090, 0x209C, 0x210A, 0x210A,
            0x210E, 0x210F, 0x2113, 0x2113, 0x212F, 0x212F, 0x2134, 0x2134,
            0x2139, 0x2139, 0x213C, 0x213D, 0x2146, 0x2149, 0x214E, 0x214E,
            0x2170, 0x217F, 0x2184, 0x2184, 0x24D0, 0x24E9, 0x2C30, 0x2C5F,
            0x2C61, 0x2C61, 0x2C65, 0x2C66, 0x2C68, 0x2C68, 0x2C6A, 0x2C6A,
            0x2C6C, 0x2C6C, 0x2C71, 0x2C71, 0x2C73, 0x2C74, 0x2C76, 0x2C7D,
            0x2C81, 0x2C81, 0x2C83, 0x2C83, 0x2C85, 0x2C85, 0x2C87, 0x2C87,
            0x2C89, 0x2C89, 0x2C8B, 0x2C8B, 0x2C8D, 0x2C8D, 0x2C8F, 0x2C8F,
            0x2C91, 0x2C91, 0x2C93, 0x2C93, 0x2C95, 0x2C95, 0x2C97, 0x2C97,
            0x2C99, 0x2C99, 0x2C9B, 0x2C9B, 0x2C9D, 0x2C9D, 0x2C9F, 0x2C9F,
            0x2CA1, 0x2CA1, 0x2CA3, 0x2CA3, 0x2CA5, 0x2CA5, 0x2CA7, 0x2CA7,
            0x2CA9, 0x2CA9, 0x2CAB, 0x2CAB, 0x2CAD, 0x2CAD, 0x2CAF, 0x2CAF,
            0x2CB1, 0x2CB1, 0x2CB3, 0x2CB3, 0x2CB5, 0x2CB5, 0x2CB7, 0x2CB7,
            0x2CB9, 0x2CB9, 0x2CBB, 0x2CBB, 0x2CBD, 0x2CBD, 0x2CBF, 0x2CBF,
            0x2CC1, 0x2CC1, 0x2CC3, 0x2CC3, 0x2CC5, 0x2CC5, 0x2CC7, 0x2CC7,
            0x2CC9, 0x2CC9, 0x2CCB, 0x2CCB, 0x2CCD, 0x2CCD, 0x2CCF, 0x2CCF,
            0x2CD1, 0x2CD1, 0x2CD3, 0x2CD3, 0x2CD5, 0x2CD5, 0x2CD7, 0x2CD7,
            0x2CD9, 0x2CD9, 0x2CDB, 0x2CDB, 0x2CDD, 0x2CDD, 0x2CDF, 0x2CDF,
            0x2CE1, 0x2CE1, 0x2CE3, 0x2CE4, 0x2CEC, 0x2CEC, 0x2CEE, 0x2CEE,
            0x2CF3, 0x2CF3, 0x2D00, 0x2D25, 0x2D27, 0x2D27, 0x2D2D, 0x2D2D,
            0xA641, 0xA641, 0xA643, 0xA643, 0xA645, 0xA645, 0xA647, 0xA647,
            0xA649, 0xA649, 0xA64B, 0xA64B, 0xA64D, 0xA64D, 0xA64F, 0xA64F,
            0xA651, 0xA651, 0xA653, 0xA653, 0xA655, 0xA655, 0xA657, 0xA657,
            0xA659, 0xA659, 0xA65B, 0xA65B, 0xA65D, 0xA65D, 0xA65F, 0xA65F,
            0xA661, 0xA661, 0xA663, 0xA663, 0xA665, 0xA665, 0xA667, 0xA667,
            0xA669, 0xA669, 0xA66B, 0xA66B, 0xA66D, 0xA66D, 0xA681, 0xA681,
            0xA683, 0xA683, 0xA685, 0xA685, 0xA687, 0xA687, 0xA689, 0xA689,
            0xA68B, 0xA68B, 0xA68D, 0xA68D, 0xA68F, 0xA68F, 0xA691, 0xA691,
            0xA693, 0xA693, 0xA695, 0xA695, 0xA697, 0xA697, 0xA699, 0xA699,
            0xA69B, 0xA69D, 0xA723, 0xA723, 0xA725, 0xA725, 0xA727, 0xA727,
            0xA729, 0xA729, 0xA72B, 0xA72B, 0xA72D, 0xA72D, 0xA72F, 0xA731,
            0xA733, 0xA733, 0xA735, 0xA735, 0xA737, 0xA737, 0xA739, 0xA739,
            0xA73B, 0xA73B, 0xA73D, 0xA73D, 0xA73F, 0xA73F, 0xA741, 0xA741,
            0xA743, 0xA743, 0xA745, 0xA745, 0xA747, 0xA747, 0xA749, 0xA749,
            0xA74B, 0xA74B, 0xA74D, 0xA74D, 0xA74F, 0xA74F, 0xA751, 0xA751,
            0xA753, 0xA753, 0xA755, 0xA755, 0xA757, 0xA757, 0xA759, 0xA759,
            0xA75B, 0xA75B, 0xA75D, 0xA75D, 0xA75F, 0xA75F, 0xA761, 0xA761,
            0xA763, 0xA763, 0xA765, 0xA765, 0xA767, 0xA767, 0xA769, 0xA769,
            0xA76B, 0xA76B, 0xA76D, 0xA76D, 0xA76F, 0xA778, 0xA77A, 0xA77A,
            0xA77C, 0xA77C, 0xA77F, 0xA77F, 0xA781, 0xA781, 0xA783, 0xA783,
            0xA785, 0xA785, 0xA787, 0xA787, 0xA78C, 0xA78C, 0xA78E, 0xA78E,
            0xA791, 0xA791, 0xA793, 0xA795, 0xA797, 0xA797, 0xA799, 0xA799,
            0xA79B, 0xA79B, 0xA79D, 0xA79D, 0xA79F, 0xA79F, 0xA7A1, 0xA7A1,
            0xA7A3, 0xA7A3, 0xA7A5, 0xA7A5, 0xA7A7, 0xA7A7, 0xA7A9, 0xA7A9,
            0xA7AF, 0xA7AF, 0xA7B5, 0xA7B5, 0xA7B7, 0xA7B7, 0xA7B9, 0xA7B9,
            0xA7BB, 0xA7BB, 0xA7BD, 0xA7BD, 0xA7BF, 0xA7BF, 0xA7C1, 0xA7C1,
            0xA7C3, 0xA7C3, 0xA7C8, 0xA7C8, 0xA7CA, 0xA7CA, 0xA7D1, 0xA7D1,
            0xA7D3, 0xA7D3, 0xA7D5, 0xA7D5, 0xA7D7, 0xA7D7, 0xA7D9, 0xA7D9,
            0xA7F6, 0xA7F6, 0xA7F8, 0xA7FA, 0xAB30, 0xAB5A, 0xAB5C, 0xAB68,
            0xAB70, 0xABBF, 0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFF41, 0xFF5A,
            0x10428, 0x1044F, 0x104D8, 0x104FB, 0x10597, 0x105A1, 0x105A3, 0x105B1,
            0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10780, 0x10780, 0x10783, 0x10785,
            0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10CC0, 0x10CF2, 0x118C0, 0x118DF,
            0x16E60, 0x16E7F, 0x1D41A, 0x1D433, 0x1D44E, 0x1D454, 0x1D456, 0x1D467,
            0x1D482, 0x1D49B, 0x1D4B6, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3,
            0x1D4C5, 0x1D4CF, 0x1D4EA, 0x1D503, 0x1D51E, 0x1D537, 0x1D552, 0x1D56B,
            0x1D586, 0x1D59F, 0x1D5BA, 0x1D5D3, 0x1D5EE, 0x1D607, 0x1D622, 0x1D63B,
            0x1D656, 0x1D66F, 0x1D68A, 0x1D6A5, 0x1D6C2, 0x1D6DA, 0x1D6DC, 0x1D6E1,
            0x1D6FC, 0x1D714, 0x1D716, 0x1D71B, 0x1D736, 0x1D74E, 0x1D750, 0x1D755,
            0x1D770, 0x1D788, 0x1D78A, 0x1D78F, 0x1D7AA, 0x1D7C2, 0x1D7C4, 0x1D7C9,
            0x1D7CB, 0x1D7CB, 0x1DF00, 0x1DF09, 0x1DF0B, 0x1DF1E, 0x1E922, 0x1E943,
            //  #76 (12808+138): bp=Math
            0x002B, 0x002B, 0x003C, 0x003E, 0x005E, 0x005E, 0x007C, 0x007C,
            0x007E, 0x007E, 0x00AC, 0x00AC, 0x00B1, 0x00B1, 0x00D7, 0x00D7,
            0x00F7, 0x00F7, 0x03D0, 0x03D2, 0x03D5, 0x03D5, 0x03F0, 0x03F1,
            0x03F4, 0x03F6, 0x0606, 0x0608, 0x2016, 0x2016, 0x2032, 0x2034,
            0x2040, 0x2040, 0x2044, 0x2044, 0x2052, 0x2052, 0x2061, 0x2064,
            0x207A, 0x207E, 0x208A, 0x208E, 0x20D0, 0x20DC, 0x20E1, 0x20E1,
            0x20E5, 0x20E6, 0x20EB, 0x20EF, 0x2102, 0x2102, 0x2107, 0x2107,
            0x210A, 0x2113, 0x2115, 0x2115, 0x2118, 0x211D, 0x2124, 0x2124,
            0x2128, 0x2129, 0x212C, 0x212D, 0x212F, 0x2131, 0x2133, 0x2138,
            0x213C, 0x2149, 0x214B, 0x214B, 0x2190, 0x21A7, 0x21A9, 0x21AE,
            0x21B0, 0x21B1, 0x21B6, 0x21B7, 0x21BC, 0x21DB, 0x21DD, 0x21DD,
            0x21E4, 0x21E5, 0x21F4, 0x22FF, 0x2308, 0x230B, 0x2320, 0x2321,
            0x237C, 0x237C, 0x239B, 0x23B5, 0x23B7, 0x23B7, 0x23D0, 0x23D0,
            0x23DC, 0x23E2, 0x25A0, 0x25A1, 0x25AE, 0x25B7, 0x25BC, 0x25C1,
            0x25C6, 0x25C7, 0x25CA, 0x25CB, 0x25CF, 0x25D3, 0x25E2, 0x25E2,
            0x25E4, 0x25E4, 0x25E7, 0x25EC, 0x25F8, 0x25FF, 0x2605, 0x2606,
            0x2640, 0x2640, 0x2642, 0x2642, 0x2660, 0x2663, 0x266D, 0x266F,
            0x27C0, 0x27FF, 0x2900, 0x2AFF, 0x2B30, 0x2B44, 0x2B47, 0x2B4C,
            0xFB29, 0xFB29, 0xFE61, 0xFE66, 0xFE68, 0xFE68, 0xFF0B, 0xFF0B,
            0xFF1C, 0xFF1E, 0xFF3C, 0xFF3C, 0xFF3E, 0xFF3E, 0xFF5C, 0xFF5C,
            0xFF5E, 0xFF5E, 0xFFE2, 0xFFE2, 0xFFE9, 0xFFEC, 0x1D400, 0x1D454,
            0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6,
            0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3,
            0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514, 0x1D516, 0x1D51C,
            0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544, 0x1D546, 0x1D546,
            0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D7CB, 0x1D7CE, 0x1D7FF,
            0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22, 0x1EE24, 0x1EE24,
            0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37, 0x1EE39, 0x1EE39,
            0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47, 0x1EE49, 0x1EE49,
            0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52, 0x1EE54, 0x1EE54,
            0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B, 0x1EE5D, 0x1EE5D,
            0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64, 0x1EE67, 0x1EE6A,
            0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C, 0x1EE7E, 0x1EE7E,
            0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3, 0x1EEA5, 0x1EEA9,
            0x1EEAB, 0x1EEBB, 0x1EEF0, 0x1EEF1,
            //  #77 (12946+18): bp=Noncharacter_Code_Point:NChar
            0xFDD0, 0xFDEF, 0xFFFE, 0xFFFF, 0x1FFFE, 0x1FFFF, 0x2FFFE, 0x2FFFF,
            0x3FFFE, 0x3FFFF, 0x4FFFE, 0x4FFFF, 0x5FFFE, 0x5FFFF, 0x6FFFE, 0x6FFFF,
            0x7FFFE, 0x7FFFF, 0x8FFFE, 0x8FFFF, 0x9FFFE, 0x9FFFF, 0xAFFFE, 0xAFFFF,
            0xBFFFE, 0xBFFFF, 0xCFFFE, 0xCFFFF, 0xDFFFE, 0xDFFFF, 0xEFFFE, 0xEFFFF,
            0xFFFFE, 0xFFFFF, 0x10FFFE, 0x10FFFF,
            //  #78 (12964+28): bp=Pattern_Syntax:Pat_Syn
            0x0021, 0x002F, 0x003A, 0x0040, 0x005B, 0x005E, 0x0060, 0x0060,
            0x007B, 0x007E, 0x00A1, 0x00A7, 0x00A9, 0x00A9, 0x00AB, 0x00AC,
            0x00AE, 0x00AE, 0x00B0, 0x00B1, 0x00B6, 0x00B6, 0x00BB, 0x00BB,
            0x00BF, 0x00BF, 0x00D7, 0x00D7, 0x00F7, 0x00F7, 0x2010, 0x2027,
            0x2030, 0x203E, 0x2041, 0x2053, 0x2055, 0x205E, 0x2190, 0x245F,
            0x2500, 0x2775, 0x2794, 0x2BFF, 0x2E00, 0x2E7F, 0x3001, 0x3003,
            0x3008, 0x3020, 0x3030, 0x3030, 0xFD3E, 0xFD3F, 0xFE45, 0xFE46,
            //  #79 (12992+5): bp=Pattern_White_Space:Pat_WS
            0x0009, 0x000D, 0x0020, 0x0020, 0x0085, 0x0085, 0x200E, 0x200F,
            0x2028, 0x2029,
            //  #80 (12997+13): bp=Quotation_Mark:QMark
            0x0022, 0x0022, 0x0027, 0x0027, 0x00AB, 0x00AB, 0x00BB, 0x00BB,
            0x2018, 0x201F, 0x2039, 0x203A, 0x2E42, 0x2E42, 0x300C, 0x300F,
            0x301D, 0x301F, 0xFE41, 0xFE44, 0xFF02, 0xFF02, 0xFF07, 0xFF07,
            0xFF62, 0xFF63,
            //  #81 (13010+3): bp=Radical
            0x2E80, 0x2E99, 0x2E9B, 0x2EF3, 0x2F00, 0x2FD5,
            //  #82 (13013+1): bp=Regional_Indicator:RI
            0x1F1E6, 0x1F1FF,
            //  #83 (13014+79): bp=Sentence_Terminal:STerm
            0x0021, 0x0021, 0x002E, 0x002E, 0x003F, 0x003F, 0x0589, 0x0589,
            0x061D, 0x061F, 0x06D4, 0x06D4, 0x0700, 0x0702, 0x07F9, 0x07F9,
            0x0837, 0x0837, 0x0839, 0x0839, 0x083D, 0x083E, 0x0964, 0x0965,
            0x104A, 0x104B, 0x1362, 0x1362, 0x1367, 0x1368, 0x166E, 0x166E,
            0x1735, 0x1736, 0x1803, 0x1803, 0x1809, 0x1809, 0x1944, 0x1945,
            0x1AA8, 0x1AAB, 0x1B5A, 0x1B5B, 0x1B5E, 0x1B5F, 0x1B7D, 0x1B7E,
            0x1C3B, 0x1C3C, 0x1C7E, 0x1C7F, 0x203C, 0x203D, 0x2047, 0x2049,
            0x2E2E, 0x2E2E, 0x2E3C, 0x2E3C, 0x2E53, 0x2E54, 0x3002, 0x3002,
            0xA4FF, 0xA4FF, 0xA60E, 0xA60F, 0xA6F3, 0xA6F3, 0xA6F7, 0xA6F7,
            0xA876, 0xA877, 0xA8CE, 0xA8CF, 0xA92F, 0xA92F, 0xA9C8, 0xA9C9,
            0xAA5D, 0xAA5F, 0xAAF0, 0xAAF1, 0xABEB, 0xABEB, 0xFE52, 0xFE52,
            0xFE56, 0xFE57, 0xFF01, 0xFF01, 0xFF0E, 0xFF0E, 0xFF1F, 0xFF1F,
            0xFF61, 0xFF61, 0x10A56, 0x10A57, 0x10F55, 0x10F59, 0x10F86, 0x10F89,
            0x11047, 0x11048, 0x110BE, 0x110C1, 0x11141, 0x11143, 0x111C5, 0x111C6,
            0x111CD, 0x111CD, 0x111DE, 0x111DF, 0x11238, 0x11239, 0x1123B, 0x1123C,
            0x112A9, 0x112A9, 0x1144B, 0x1144C, 0x115C2, 0x115C3, 0x115C9, 0x115D7,
            0x11641, 0x11642, 0x1173C, 0x1173E, 0x11944, 0x11944, 0x11946, 0x11946,
            0x11A42, 0x11A43, 0x11A9B, 0x11A9C, 0x11C41, 0x11C42, 0x11EF7, 0x11EF8,
            0x16A6E, 0x16A6F, 0x16AF5, 0x16AF5, 0x16B37, 0x16B38, 0x16B44, 0x16B44,
            0x16E98, 0x16E98, 0x1BC9F, 0x1BC9F, 0x1DA88, 0x1DA88,
            //  #84 (13093+32): bp=Soft_Dotted:SD
            0x0069, 0x006A, 0x012F, 0x012F, 0x0249, 0x0249, 0x0268, 0x0268,
            0x029D, 0x029D, 0x02B2, 0x02B2, 0x03F3, 0x03F3, 0x0456, 0x0456,
            0x0458, 0x0458, 0x1D62, 0x1D62, 0x1D96, 0x1D96, 0x1DA4, 0x1DA4,
            0x1DA8, 0x1DA8, 0x1E2D, 0x1E2D, 0x1ECB, 0x1ECB, 0x2071, 0x2071,
            0x2148, 0x2149, 0x2C7C, 0x2C7C, 0x1D422, 0x1D423, 0x1D456, 0x1D457,
            0x1D48A, 0x1D48B, 0x1D4BE, 0x1D4BF, 0x1D4F2, 0x1D4F3, 0x1D526, 0x1D527,
            0x1D55A, 0x1D55B, 0x1D58E, 0x1D58F, 0x1D5C2, 0x1D5C3, 0x1D5F6, 0x1D5F7,
            0x1D62A, 0x1D62B, 0x1D65E, 0x1D65F, 0x1D692, 0x1D693, 0x1DF1A, 0x1DF1A,
            //  #85 (13125+107): bp=Terminal_Punctuation:Term
            0x0021, 0x0021, 0x002C, 0x002C, 0x002E, 0x002E, 0x003A, 0x003B,
            0x003F, 0x003F, 0x037E, 0x037E, 0x0387, 0x0387, 0x0589, 0x0589,
            0x05C3, 0x05C3, 0x060C, 0x060C, 0x061B, 0x061B, 0x061D, 0x061F,
            0x06D4, 0x06D4, 0x0700, 0x070A, 0x070C, 0x070C, 0x07F8, 0x07F9,
            0x0830, 0x083E, 0x085E, 0x085E, 0x0964, 0x0965, 0x0E5A, 0x0E5B,
            0x0F08, 0x0F08, 0x0F0D, 0x0F12, 0x104A, 0x104B, 0x1361, 0x1368,
            0x166E, 0x166E, 0x16EB, 0x16ED, 0x1735, 0x1736, 0x17D4, 0x17D6,
            0x17DA, 0x17DA, 0x1802, 0x1805, 0x1808, 0x1809, 0x1944, 0x1945,
            0x1AA8, 0x1AAB, 0x1B5A, 0x1B5B, 0x1B5D, 0x1B5F, 0x1B7D, 0x1B7E,
            0x1C3B, 0x1C3F, 0x1C7E, 0x1C7F, 0x203C, 0x203D, 0x2047, 0x2049,
            0x2E2E, 0x2E2E, 0x2E3C, 0x2E3C, 0x2E41, 0x2E41, 0x2E4C, 0x2E4C,
            0x2E4E, 0x2E4F, 0x2E53, 0x2E54, 0x3001, 0x3002, 0xA4FE, 0xA4FF,
            0xA60D, 0xA60F, 0xA6F3, 0xA6F7, 0xA876, 0xA877, 0xA8CE, 0xA8CF,
            0xA92F, 0xA92F, 0xA9C7, 0xA9C9, 0xAA5D, 0xAA5F, 0xAADF, 0xAADF,
            0xAAF0, 0xAAF1, 0xABEB, 0xABEB, 0xFE50, 0xFE52, 0xFE54, 0xFE57,
            0xFF01, 0xFF01, 0xFF0C, 0xFF0C, 0xFF0E, 0xFF0E, 0xFF1A, 0xFF1B,
            0xFF1F, 0xFF1F, 0xFF61, 0xFF61, 0xFF64, 0xFF64, 0x1039F, 0x1039F,
            0x103D0, 0x103D0, 0x10857, 0x10857, 0x1091F, 0x1091F, 0x10A56, 0x10A57,
            0x10AF0, 0x10AF5, 0x10B3A, 0x10B3F, 0x10B99, 0x10B9C, 0x10F55, 0x10F59,
            0x10F86, 0x10F89, 0x11047, 0x1104D, 0x110BE, 0x110C1, 0x11141, 0x11143,
            0x111C5, 0x111C6, 0x111CD, 0x111CD, 0x111DE, 0x111DF, 0x11238, 0x1123C,
            0x112A9, 0x112A9, 0x1144B, 0x1144D, 0x1145A, 0x1145B, 0x115C2, 0x115C5,
            0x115C9, 0x115D7, 0x11641, 0x11642, 0x1173C, 0x1173E, 0x11944, 0x11944,
            0x11946, 0x11946, 0x11A42, 0x11A43, 0x11A9B, 0x11A9C, 0x11AA1, 0x11AA2,
            0x11C41, 0x11C43, 0x11C71, 0x11C71, 0x11EF7, 0x11EF8, 0x12470, 0x12474,
            0x16A6E, 0x16A6F, 0x16AF5, 0x16AF5, 0x16B37, 0x16B39, 0x16B44, 0x16B44,
            0x16E97, 0x16E98, 0x1BC9F, 0x1BC9F, 0x1DA87, 0x1DA8A,
            //  #86 (13232+15): bp=Unified_Ideograph:UIdeo
            0x3400, 0x4DBF, 0x4E00, 0x9FFF, 0xFA0E, 0xFA0F, 0xFA11, 0xFA11,
            0xFA13, 0xFA14, 0xFA1F, 0xFA1F, 0xFA21, 0xFA21, 0xFA23, 0xFA24,
            0xFA27, 0xFA29, 0x20000, 0x2A6DF, 0x2A700, 0x2B738, 0x2B740, 0x2B81D,
            0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0, 0x30000, 0x3134A,
            //  #87 (13247+651): bp=Uppercase:Upper
            0x0041, 0x005A, 0x00C0, 0x00D6, 0x00D8, 0x00DE, 0x0100, 0x0100,
            0x0102, 0x0102, 0x0104, 0x0104, 0x0106, 0x0106, 0x0108, 0x0108,
            0x010A, 0x010A, 0x010C, 0x010C, 0x010E, 0x010E, 0x0110, 0x0110,
            0x0112, 0x0112, 0x0114, 0x0114, 0x0116, 0x0116, 0x0118, 0x0118,
            0x011A, 0x011A, 0x011C, 0x011C, 0x011E, 0x011E, 0x0120, 0x0120,
            0x0122, 0x0122, 0x0124, 0x0124, 0x0126, 0x0126, 0x0128, 0x0128,
            0x012A, 0x012A, 0x012C, 0x012C, 0x012E, 0x012E, 0x0130, 0x0130,
            0x0132, 0x0132, 0x0134, 0x0134, 0x0136, 0x0136, 0x0139, 0x0139,
            0x013B, 0x013B, 0x013D, 0x013D, 0x013F, 0x013F, 0x0141, 0x0141,
            0x0143, 0x0143, 0x0145, 0x0145, 0x0147, 0x0147, 0x014A, 0x014A,
            0x014C, 0x014C, 0x014E, 0x014E, 0x0150, 0x0150, 0x0152, 0x0152,
            0x0154, 0x0154, 0x0156, 0x0156, 0x0158, 0x0158, 0x015A, 0x015A,
            0x015C, 0x015C, 0x015E, 0x015E, 0x0160, 0x0160, 0x0162, 0x0162,
            0x0164, 0x0164, 0x0166, 0x0166, 0x0168, 0x0168, 0x016A, 0x016A,
            0x016C, 0x016C, 0x016E, 0x016E, 0x0170, 0x0170, 0x0172, 0x0172,
            0x0174, 0x0174, 0x0176, 0x0176, 0x0178, 0x0179, 0x017B, 0x017B,
            0x017D, 0x017D, 0x0181, 0x0182, 0x0184, 0x0184, 0x0186, 0x0187,
            0x0189, 0x018B, 0x018E, 0x0191, 0x0193, 0x0194, 0x0196, 0x0198,
            0x019C, 0x019D, 0x019F, 0x01A0, 0x01A2, 0x01A2, 0x01A4, 0x01A4,
            0x01A6, 0x01A7, 0x01A9, 0x01A9, 0x01AC, 0x01AC, 0x01AE, 0x01AF,
            0x01B1, 0x01B3, 0x01B5, 0x01B5, 0x01B7, 0x01B8, 0x01BC, 0x01BC,
            0x01C4, 0x01C4, 0x01C7, 0x01C7, 0x01CA, 0x01CA, 0x01CD, 0x01CD,
            0x01CF, 0x01CF, 0x01D1, 0x01D1, 0x01D3, 0x01D3, 0x01D5, 0x01D5,
            0x01D7, 0x01D7, 0x01D9, 0x01D9, 0x01DB, 0x01DB, 0x01DE, 0x01DE,
            0x01E0, 0x01E0, 0x01E2, 0x01E2, 0x01E4, 0x01E4, 0x01E6, 0x01E6,
            0x01E8, 0x01E8, 0x01EA, 0x01EA, 0x01EC, 0x01EC, 0x01EE, 0x01EE,
            0x01F1, 0x01F1, 0x01F4, 0x01F4, 0x01F6, 0x01F8, 0x01FA, 0x01FA,
            0x01FC, 0x01FC, 0x01FE, 0x01FE, 0x0200, 0x0200, 0x0202, 0x0202,
            0x0204, 0x0204, 0x0206, 0x0206, 0x0208, 0x0208, 0x020A, 0x020A,
            0x020C, 0x020C, 0x020E, 0x020E, 0x0210, 0x0210, 0x0212, 0x0212,
            0x0214, 0x0214, 0x0216, 0x0216, 0x0218, 0x0218, 0x021A, 0x021A,
            0x021C, 0x021C, 0x021E, 0x021E, 0x0220, 0x0220, 0x0222, 0x0222,
            0x0224, 0x0224, 0x0226, 0x0226, 0x0228, 0x0228, 0x022A, 0x022A,
            0x022C, 0x022C, 0x022E, 0x022E, 0x0230, 0x0230, 0x0232, 0x0232,
            0x023A, 0x023B, 0x023D, 0x023E, 0x0241, 0x0241, 0x0243, 0x0246,
            0x0248, 0x0248, 0x024A, 0x024A, 0x024C, 0x024C, 0x024E, 0x024E,
            0x0370, 0x0370, 0x0372, 0x0372, 0x0376, 0x0376, 0x037F, 0x037F,
            0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x038F,
            0x0391, 0x03A1, 0x03A3, 0x03AB, 0x03CF, 0x03CF, 0x03D2, 0x03D4,
            0x03D8, 0x03D8, 0x03DA, 0x03DA, 0x03DC, 0x03DC, 0x03DE, 0x03DE,
            0x03E0, 0x03E0, 0x03E2, 0x03E2, 0x03E4, 0x03E4, 0x03E6, 0x03E6,
            0x03E8, 0x03E8, 0x03EA, 0x03EA, 0x03EC, 0x03EC, 0x03EE, 0x03EE,
            0x03F4, 0x03F4, 0x03F7, 0x03F7, 0x03F9, 0x03FA, 0x03FD, 0x042F,
            0x0460, 0x0460, 0x0462, 0x0462, 0x0464, 0x0464, 0x0466, 0x0466,
            0x0468, 0x0468, 0x046A, 0x046A, 0x046C, 0x046C, 0x046E, 0x046E,
            0x0470, 0x0470, 0x0472, 0x0472, 0x0474, 0x0474, 0x0476, 0x0476,
            0x0478, 0x0478, 0x047A, 0x047A, 0x047C, 0x047C, 0x047E, 0x047E,
            0x0480, 0x0480, 0x048A, 0x048A, 0x048C, 0x048C, 0x048E, 0x048E,
            0x0490, 0x0490, 0x0492, 0x0492, 0x0494, 0x0494, 0x0496, 0x0496,
            0x0498, 0x0498, 0x049A, 0x049A, 0x049C, 0x049C, 0x049E, 0x049E,
            0x04A0, 0x04A0, 0x04A2, 0x04A2, 0x04A4, 0x04A4, 0x04A6, 0x04A6,
            0x04A8, 0x04A8, 0x04AA, 0x04AA, 0x04AC, 0x04AC, 0x04AE, 0x04AE,
            0x04B0, 0x04B0, 0x04B2, 0x04B2, 0x04B4, 0x04B4, 0x04B6, 0x04B6,
            0x04B8, 0x04B8, 0x04BA, 0x04BA, 0x04BC, 0x04BC, 0x04BE, 0x04BE,
            0x04C0, 0x04C1, 0x04C3, 0x04C3, 0x04C5, 0x04C5, 0x04C7, 0x04C7,
            0x04C9, 0x04C9, 0x04CB, 0x04CB, 0x04CD, 0x04CD, 0x04D0, 0x04D0,
            0x04D2, 0x04D2, 0x04D4, 0x04D4, 0x04D6, 0x04D6, 0x04D8, 0x04D8,
            0x04DA, 0x04DA, 0x04DC, 0x04DC, 0x04DE, 0x04DE, 0x04E0, 0x04E0,
            0x04E2, 0x04E2, 0x04E4, 0x04E4, 0x04E6, 0x04E6, 0x04E8, 0x04E8,
            0x04EA, 0x04EA, 0x04EC, 0x04EC, 0x04EE, 0x04EE, 0x04F0, 0x04F0,
            0x04F2, 0x04F2, 0x04F4, 0x04F4, 0x04F6, 0x04F6, 0x04F8, 0x04F8,
            0x04FA, 0x04FA, 0x04FC, 0x04FC, 0x04FE, 0x04FE, 0x0500, 0x0500,
            0x0502, 0x0502, 0x0504, 0x0504, 0x0506, 0x0506, 0x0508, 0x0508,
            0x050A, 0x050A, 0x050C, 0x050C, 0x050E, 0x050E, 0x0510, 0x0510,
            0x0512, 0x0512, 0x0514, 0x0514, 0x0516, 0x0516, 0x0518, 0x0518,
            0x051A, 0x051A, 0x051C, 0x051C, 0x051E, 0x051E, 0x0520, 0x0520,
            0x0522, 0x0522, 0x0524, 0x0524, 0x0526, 0x0526, 0x0528, 0x0528,
            0x052A, 0x052A, 0x052C, 0x052C, 0x052E, 0x052E, 0x0531, 0x0556,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x13A0, 0x13F5,
            0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1E00, 0x1E00, 0x1E02, 0x1E02,
            0x1E04, 0x1E04, 0x1E06, 0x1E06, 0x1E08, 0x1E08, 0x1E0A, 0x1E0A,
            0x1E0C, 0x1E0C, 0x1E0E, 0x1E0E, 0x1E10, 0x1E10, 0x1E12, 0x1E12,
            0x1E14, 0x1E14, 0x1E16, 0x1E16, 0x1E18, 0x1E18, 0x1E1A, 0x1E1A,
            0x1E1C, 0x1E1C, 0x1E1E, 0x1E1E, 0x1E20, 0x1E20, 0x1E22, 0x1E22,
            0x1E24, 0x1E24, 0x1E26, 0x1E26, 0x1E28, 0x1E28, 0x1E2A, 0x1E2A,
            0x1E2C, 0x1E2C, 0x1E2E, 0x1E2E, 0x1E30, 0x1E30, 0x1E32, 0x1E32,
            0x1E34, 0x1E34, 0x1E36, 0x1E36, 0x1E38, 0x1E38, 0x1E3A, 0x1E3A,
            0x1E3C, 0x1E3C, 0x1E3E, 0x1E3E, 0x1E40, 0x1E40, 0x1E42, 0x1E42,
            0x1E44, 0x1E44, 0x1E46, 0x1E46, 0x1E48, 0x1E48, 0x1E4A, 0x1E4A,
            0x1E4C, 0x1E4C, 0x1E4E, 0x1E4E, 0x1E50, 0x1E50, 0x1E52, 0x1E52,
            0x1E54, 0x1E54, 0x1E56, 0x1E56, 0x1E58, 0x1E58, 0x1E5A, 0x1E5A,
            0x1E5C, 0x1E5C, 0x1E5E, 0x1E5E, 0x1E60, 0x1E60, 0x1E62, 0x1E62,
            0x1E64, 0x1E64, 0x1E66, 0x1E66, 0x1E68, 0x1E68, 0x1E6A, 0x1E6A,
            0x1E6C, 0x1E6C, 0x1E6E, 0x1E6E, 0x1E70, 0x1E70, 0x1E72, 0x1E72,
            0x1E74, 0x1E74, 0x1E76, 0x1E76, 0x1E78, 0x1E78, 0x1E7A, 0x1E7A,
            0x1E7C, 0x1E7C, 0x1E7E, 0x1E7E, 0x1E80, 0x1E80, 0x1E82, 0x1E82,
            0x1E84, 0x1E84, 0x1E86, 0x1E86, 0x1E88, 0x1E88, 0x1E8A, 0x1E8A,
            0x1E8C, 0x1E8C, 0x1E8E, 0x1E8E, 0x1E90, 0x1E90, 0x1E92, 0x1E92,
            0x1E94, 0x1E94, 0x1E9E, 0x1E9E, 0x1EA0, 0x1EA0, 0x1EA2, 0x1EA2,
            0x1EA4, 0x1EA4, 0x1EA6, 0x1EA6, 0x1EA8, 0x1EA8, 0x1EAA, 0x1EAA,
            0x1EAC, 0x1EAC, 0x1EAE, 0x1EAE, 0x1EB0, 0x1EB0, 0x1EB2, 0x1EB2,
            0x1EB4, 0x1EB4, 0x1EB6, 0x1EB6, 0x1EB8, 0x1EB8, 0x1EBA, 0x1EBA,
            0x1EBC, 0x1EBC, 0x1EBE, 0x1EBE, 0x1EC0, 0x1EC0, 0x1EC2, 0x1EC2,
            0x1EC4, 0x1EC4, 0x1EC6, 0x1EC6, 0x1EC8, 0x1EC8, 0x1ECA, 0x1ECA,
            0x1ECC, 0x1ECC, 0x1ECE, 0x1ECE, 0x1ED0, 0x1ED0, 0x1ED2, 0x1ED2,
            0x1ED4, 0x1ED4, 0x1ED6, 0x1ED6, 0x1ED8, 0x1ED8, 0x1EDA, 0x1EDA,
            0x1EDC, 0x1EDC, 0x1EDE, 0x1EDE, 0x1EE0, 0x1EE0, 0x1EE2, 0x1EE2,
            0x1EE4, 0x1EE4, 0x1EE6, 0x1EE6, 0x1EE8, 0x1EE8, 0x1EEA, 0x1EEA,
            0x1EEC, 0x1EEC, 0x1EEE, 0x1EEE, 0x1EF0, 0x1EF0, 0x1EF2, 0x1EF2,
            0x1EF4, 0x1EF4, 0x1EF6, 0x1EF6, 0x1EF8, 0x1EF8, 0x1EFA, 0x1EFA,
            0x1EFC, 0x1EFC, 0x1EFE, 0x1EFE, 0x1F08, 0x1F0F, 0x1F18, 0x1F1D,
            0x1F28, 0x1F2F, 0x1F38, 0x1F3F, 0x1F48, 0x1F4D, 0x1F59, 0x1F59,
            0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F5F, 0x1F68, 0x1F6F,
            0x1FB8, 0x1FBB, 0x1FC8, 0x1FCB, 0x1FD8, 0x1FDB, 0x1FE8, 0x1FEC,
            0x1FF8, 0x1FFB, 0x2102, 0x2102, 0x2107, 0x2107, 0x210B, 0x210D,
            0x2110, 0x2112, 0x2115, 0x2115, 0x2119, 0x211D, 0x2124, 0x2124,
            0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x212D, 0x2130, 0x2133,
            0x213E, 0x213F, 0x2145, 0x2145, 0x2160, 0x216F, 0x2183, 0x2183,
            0x24B6, 0x24CF, 0x2C00, 0x2C2F, 0x2C60, 0x2C60, 0x2C62, 0x2C64,
            0x2C67, 0x2C67, 0x2C69, 0x2C69, 0x2C6B, 0x2C6B, 0x2C6D, 0x2C70,
            0x2C72, 0x2C72, 0x2C75, 0x2C75, 0x2C7E, 0x2C80, 0x2C82, 0x2C82,
            0x2C84, 0x2C84, 0x2C86, 0x2C86, 0x2C88, 0x2C88, 0x2C8A, 0x2C8A,
            0x2C8C, 0x2C8C, 0x2C8E, 0x2C8E, 0x2C90, 0x2C90, 0x2C92, 0x2C92,
            0x2C94, 0x2C94, 0x2C96, 0x2C96, 0x2C98, 0x2C98, 0x2C9A, 0x2C9A,
            0x2C9C, 0x2C9C, 0x2C9E, 0x2C9E, 0x2CA0, 0x2CA0, 0x2CA2, 0x2CA2,
            0x2CA4, 0x2CA4, 0x2CA6, 0x2CA6, 0x2CA8, 0x2CA8, 0x2CAA, 0x2CAA,
            0x2CAC, 0x2CAC, 0x2CAE, 0x2CAE, 0x2CB0, 0x2CB0, 0x2CB2, 0x2CB2,
            0x2CB4, 0x2CB4, 0x2CB6, 0x2CB6, 0x2CB8, 0x2CB8, 0x2CBA, 0x2CBA,
            0x2CBC, 0x2CBC, 0x2CBE, 0x2CBE, 0x2CC0, 0x2CC0, 0x2CC2, 0x2CC2,
            0x2CC4, 0x2CC4, 0x2CC6, 0x2CC6, 0x2CC8, 0x2CC8, 0x2CCA, 0x2CCA,
            0x2CCC, 0x2CCC, 0x2CCE, 0x2CCE, 0x2CD0, 0x2CD0, 0x2CD2, 0x2CD2,
            0x2CD4, 0x2CD4, 0x2CD6, 0x2CD6, 0x2CD8, 0x2CD8, 0x2CDA, 0x2CDA,
            0x2CDC, 0x2CDC, 0x2CDE, 0x2CDE, 0x2CE0, 0x2CE0, 0x2CE2, 0x2CE2,
            0x2CEB, 0x2CEB, 0x2CED, 0x2CED, 0x2CF2, 0x2CF2, 0xA640, 0xA640,
            0xA642, 0xA642, 0xA644, 0xA644, 0xA646, 0xA646, 0xA648, 0xA648,
            0xA64A, 0xA64A, 0xA64C, 0xA64C, 0xA64E, 0xA64E, 0xA650, 0xA650,
            0xA652, 0xA652, 0xA654, 0xA654, 0xA656, 0xA656, 0xA658, 0xA658,
            0xA65A, 0xA65A, 0xA65C, 0xA65C, 0xA65E, 0xA65E, 0xA660, 0xA660,
            0xA662, 0xA662, 0xA664, 0xA664, 0xA666, 0xA666, 0xA668, 0xA668,
            0xA66A, 0xA66A, 0xA66C, 0xA66C, 0xA680, 0xA680, 0xA682, 0xA682,
            0xA684, 0xA684, 0xA686, 0xA686, 0xA688, 0xA688, 0xA68A, 0xA68A,
            0xA68C, 0xA68C, 0xA68E, 0xA68E, 0xA690, 0xA690, 0xA692, 0xA692,
            0xA694, 0xA694, 0xA696, 0xA696, 0xA698, 0xA698, 0xA69A, 0xA69A,
            0xA722, 0xA722, 0xA724, 0xA724, 0xA726, 0xA726, 0xA728, 0xA728,
            0xA72A, 0xA72A, 0xA72C, 0xA72C, 0xA72E, 0xA72E, 0xA732, 0xA732,
            0xA734, 0xA734, 0xA736, 0xA736, 0xA738, 0xA738, 0xA73A, 0xA73A,
            0xA73C, 0xA73C, 0xA73E, 0xA73E, 0xA740, 0xA740, 0xA742, 0xA742,
            0xA744, 0xA744, 0xA746, 0xA746, 0xA748, 0xA748, 0xA74A, 0xA74A,
            0xA74C, 0xA74C, 0xA74E, 0xA74E, 0xA750, 0xA750, 0xA752, 0xA752,
            0xA754, 0xA754, 0xA756, 0xA756, 0xA758, 0xA758, 0xA75A, 0xA75A,
            0xA75C, 0xA75C, 0xA75E, 0xA75E, 0xA760, 0xA760, 0xA762, 0xA762,
            0xA764, 0xA764, 0xA766, 0xA766, 0xA768, 0xA768, 0xA76A, 0xA76A,
            0xA76C, 0xA76C, 0xA76E, 0xA76E, 0xA779, 0xA779, 0xA77B, 0xA77B,
            0xA77D, 0xA77E, 0xA780, 0xA780, 0xA782, 0xA782, 0xA784, 0xA784,
            0xA786, 0xA786, 0xA78B, 0xA78B, 0xA78D, 0xA78D, 0xA790, 0xA790,
            0xA792, 0xA792, 0xA796, 0xA796, 0xA798, 0xA798, 0xA79A, 0xA79A,
            0xA79C, 0xA79C, 0xA79E, 0xA79E, 0xA7A0, 0xA7A0, 0xA7A2, 0xA7A2,
            0xA7A4, 0xA7A4, 0xA7A6, 0xA7A6, 0xA7A8, 0xA7A8, 0xA7AA, 0xA7AE,
            0xA7B0, 0xA7B4, 0xA7B6, 0xA7B6, 0xA7B8, 0xA7B8, 0xA7BA, 0xA7BA,
            0xA7BC, 0xA7BC, 0xA7BE, 0xA7BE, 0xA7C0, 0xA7C0, 0xA7C2, 0xA7C2,
            0xA7C4, 0xA7C7, 0xA7C9, 0xA7C9, 0xA7D0, 0xA7D0, 0xA7D6, 0xA7D6,
            0xA7D8, 0xA7D8, 0xA7F5, 0xA7F5, 0xFF21, 0xFF3A, 0x10400, 0x10427,
            0x104B0, 0x104D3, 0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592,
            0x10594, 0x10595, 0x10C80, 0x10CB2, 0x118A0, 0x118BF, 0x16E40, 0x16E5F,
            0x1D400, 0x1D419, 0x1D434, 0x1D44D, 0x1D468, 0x1D481, 0x1D49C, 0x1D49C,
            0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC,
            0x1D4AE, 0x1D4B5, 0x1D4D0, 0x1D4E9, 0x1D504, 0x1D505, 0x1D507, 0x1D50A,
            0x1D50D, 0x1D514, 0x1D516, 0x1D51C, 0x1D538, 0x1D539, 0x1D53B, 0x1D53E,
            0x1D540, 0x1D544, 0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D56C, 0x1D585,
            0x1D5A0, 0x1D5B9, 0x1D5D4, 0x1D5ED, 0x1D608, 0x1D621, 0x1D63C, 0x1D655,
            0x1D670, 0x1D689, 0x1D6A8, 0x1D6C0, 0x1D6E2, 0x1D6FA, 0x1D71C, 0x1D734,
            0x1D756, 0x1D76E, 0x1D790, 0x1D7A8, 0x1D7CA, 0x1D7CA, 0x1E900, 0x1E921,
            0x1F130, 0x1F149, 0x1F150, 0x1F169, 0x1F170, 0x1F189,
            //  #88 (13898+4): bp=Variation_Selector:VS
            0x180B, 0x180D, 0x180F, 0x180F, 0xFE00, 0xFE0F, 0xE0100, 0xE01EF,
            //  #89 (13902+10): bp=White_Space:space
            0x0009, 0x000D, 0x0020, 0x0020, 0x0085, 0x0085, 0x00A0, 0x00A0,
            0x1680, 0x1680, 0x2000, 0x200A, 0x2028, 0x2029, 0x202F, 0x202F,
            0x205F, 0x205F, 0x3000, 0x3000,
            //  #90 (13912+763): bp=XID_Continue:XIDC
            0x0030, 0x0039, 0x0041, 0x005A, 0x005F, 0x005F, 0x0061, 0x007A,
            0x00AA, 0x00AA, 0x00B5, 0x00B5, 0x00B7, 0x00B7, 0x00BA, 0x00BA,
            0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02C1, 0x02C6, 0x02D1,
            0x02E0, 0x02E4, 0x02EC, 0x02EC, 0x02EE, 0x02EE, 0x0300, 0x0374,
            0x0376, 0x0377, 0x037B, 0x037D, 0x037F, 0x037F, 0x0386, 0x038A,
            0x038C, 0x038C, 0x038E, 0x03A1, 0x03A3, 0x03F5, 0x03F7, 0x0481,
            0x0483, 0x0487, 0x048A, 0x052F, 0x0531, 0x0556, 0x0559, 0x0559,
            0x0560, 0x0588, 0x0591, 0x05BD, 0x05BF, 0x05BF, 0x05C1, 0x05C2,
            0x05C4, 0x05C5, 0x05C7, 0x05C7, 0x05D0, 0x05EA, 0x05EF, 0x05F2,
            0x0610, 0x061A, 0x0620, 0x0669, 0x066E, 0x06D3, 0x06D5, 0x06DC,
            0x06DF, 0x06E8, 0x06EA, 0x06FC, 0x06FF, 0x06FF, 0x0710, 0x074A,
            0x074D, 0x07B1, 0x07C0, 0x07F5, 0x07FA, 0x07FA, 0x07FD, 0x07FD,
            0x0800, 0x082D, 0x0840, 0x085B, 0x0860, 0x086A, 0x0870, 0x0887,
            0x0889, 0x088E, 0x0898, 0x08E1, 0x08E3, 0x0963, 0x0966, 0x096F,
            0x0971, 0x0983, 0x0985, 0x098C, 0x098F, 0x0990, 0x0993, 0x09A8,
            0x09AA, 0x09B0, 0x09B2, 0x09B2, 0x09B6, 0x09B9, 0x09BC, 0x09C4,
            0x09C7, 0x09C8, 0x09CB, 0x09CE, 0x09D7, 0x09D7, 0x09DC, 0x09DD,
            0x09DF, 0x09E3, 0x09E6, 0x09F1, 0x09FC, 0x09FC, 0x09FE, 0x09FE,
            0x0A01, 0x0A03, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10, 0x0A13, 0x0A28,
            0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36, 0x0A38, 0x0A39,
            0x0A3C, 0x0A3C, 0x0A3E, 0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4D,
            0x0A51, 0x0A51, 0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A66, 0x0A75,
            0x0A81, 0x0A83, 0x0A85, 0x0A8D, 0x0A8F, 0x0A91, 0x0A93, 0x0AA8,
            0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3, 0x0AB5, 0x0AB9, 0x0ABC, 0x0AC5,
            0x0AC7, 0x0AC9, 0x0ACB, 0x0ACD, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE3,
            0x0AE6, 0x0AEF, 0x0AF9, 0x0AFF, 0x0B01, 0x0B03, 0x0B05, 0x0B0C,
            0x0B0F, 0x0B10, 0x0B13, 0x0B28, 0x0B2A, 0x0B30, 0x0B32, 0x0B33,
            0x0B35, 0x0B39, 0x0B3C, 0x0B44, 0x0B47, 0x0B48, 0x0B4B, 0x0B4D,
            0x0B55, 0x0B57, 0x0B5C, 0x0B5D, 0x0B5F, 0x0B63, 0x0B66, 0x0B6F,
            0x0B71, 0x0B71, 0x0B82, 0x0B83, 0x0B85, 0x0B8A, 0x0B8E, 0x0B90,
            0x0B92, 0x0B95, 0x0B99, 0x0B9A, 0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F,
            0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA, 0x0BAE, 0x0BB9, 0x0BBE, 0x0BC2,
            0x0BC6, 0x0BC8, 0x0BCA, 0x0BCD, 0x0BD0, 0x0BD0, 0x0BD7, 0x0BD7,
            0x0BE6, 0x0BEF, 0x0C00, 0x0C0C, 0x0C0E, 0x0C10, 0x0C12, 0x0C28,
            0x0C2A, 0x0C39, 0x0C3C, 0x0C44, 0x0C46, 0x0C48, 0x0C4A, 0x0C4D,
            0x0C55, 0x0C56, 0x0C58, 0x0C5A, 0x0C5D, 0x0C5D, 0x0C60, 0x0C63,
            0x0C66, 0x0C6F, 0x0C80, 0x0C83, 0x0C85, 0x0C8C, 0x0C8E, 0x0C90,
            0x0C92, 0x0CA8, 0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9, 0x0CBC, 0x0CC4,
            0x0CC6, 0x0CC8, 0x0CCA, 0x0CCD, 0x0CD5, 0x0CD6, 0x0CDD, 0x0CDE,
            0x0CE0, 0x0CE3, 0x0CE6, 0x0CEF, 0x0CF1, 0x0CF2, 0x0D00, 0x0D0C,
            0x0D0E, 0x0D10, 0x0D12, 0x0D44, 0x0D46, 0x0D48, 0x0D4A, 0x0D4E,
            0x0D54, 0x0D57, 0x0D5F, 0x0D63, 0x0D66, 0x0D6F, 0x0D7A, 0x0D7F,
            0x0D81, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1, 0x0DB3, 0x0DBB,
            0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DCA, 0x0DCA, 0x0DCF, 0x0DD4,
            0x0DD6, 0x0DD6, 0x0DD8, 0x0DDF, 0x0DE6, 0x0DEF, 0x0DF2, 0x0DF3,
            0x0E01, 0x0E3A, 0x0E40, 0x0E4E, 0x0E50, 0x0E59, 0x0E81, 0x0E82,
            0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5,
            0x0EA7, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0EC8, 0x0ECD,
            0x0ED0, 0x0ED9, 0x0EDC, 0x0EDF, 0x0F00, 0x0F00, 0x0F18, 0x0F19,
            0x0F20, 0x0F29, 0x0F35, 0x0F35, 0x0F37, 0x0F37, 0x0F39, 0x0F39,
            0x0F3E, 0x0F47, 0x0F49, 0x0F6C, 0x0F71, 0x0F84, 0x0F86, 0x0F97,
            0x0F99, 0x0FBC, 0x0FC6, 0x0FC6, 0x1000, 0x1049, 0x1050, 0x109D,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA,
            0x10FC, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258,
            0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D, 0x1290, 0x12B0,
            0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0, 0x12C2, 0x12C5,
            0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315, 0x1318, 0x135A,
            0x135D, 0x135F, 0x1369, 0x1371, 0x1380, 0x138F, 0x13A0, 0x13F5,
            0x13F8, 0x13FD, 0x1401, 0x166C, 0x166F, 0x167F, 0x1681, 0x169A,
            0x16A0, 0x16EA, 0x16EE, 0x16F8, 0x1700, 0x1715, 0x171F, 0x1734,
            0x1740, 0x1753, 0x1760, 0x176C, 0x176E, 0x1770, 0x1772, 0x1773,
            0x1780, 0x17D3, 0x17D7, 0x17D7, 0x17DC, 0x17DD, 0x17E0, 0x17E9,
            0x180B, 0x180D, 0x180F, 0x1819, 0x1820, 0x1878, 0x1880, 0x18AA,
            0x18B0, 0x18F5, 0x1900, 0x191E, 0x1920, 0x192B, 0x1930, 0x193B,
            0x1946, 0x196D, 0x1970, 0x1974, 0x1980, 0x19AB, 0x19B0, 0x19C9,
            0x19D0, 0x19DA, 0x1A00, 0x1A1B, 0x1A20, 0x1A5E, 0x1A60, 0x1A7C,
            0x1A7F, 0x1A89, 0x1A90, 0x1A99, 0x1AA7, 0x1AA7, 0x1AB0, 0x1ABD,
            0x1ABF, 0x1ACE, 0x1B00, 0x1B4C, 0x1B50, 0x1B59, 0x1B6B, 0x1B73,
            0x1B80, 0x1BF3, 0x1C00, 0x1C37, 0x1C40, 0x1C49, 0x1C4D, 0x1C7D,
            0x1C80, 0x1C88, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1CD0, 0x1CD2,
            0x1CD4, 0x1CFA, 0x1D00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45,
            0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B,
            0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FBC,
            0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC, 0x1FD0, 0x1FD3,
            0x1FD6, 0x1FDB, 0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFC,
            0x203F, 0x2040, 0x2054, 0x2054, 0x2071, 0x2071, 0x207F, 0x207F,
            0x2090, 0x209C, 0x20D0, 0x20DC, 0x20E1, 0x20E1, 0x20E5, 0x20F0,
            0x2102, 0x2102, 0x2107, 0x2107, 0x210A, 0x2113, 0x2115, 0x2115,
            0x2118, 0x211D, 0x2124, 0x2124, 0x2126, 0x2126, 0x2128, 0x2128,
            0x212A, 0x2139, 0x213C, 0x213F, 0x2145, 0x2149, 0x214E, 0x214E,
            0x2160, 0x2188, 0x2C00, 0x2CE4, 0x2CEB, 0x2CF3, 0x2D00, 0x2D25,
            0x2D27, 0x2D27, 0x2D2D, 0x2D2D, 0x2D30, 0x2D67, 0x2D6F, 0x2D6F,
            0x2D7F, 0x2D96, 0x2DA0, 0x2DA6, 0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6,
            0x2DB8, 0x2DBE, 0x2DC0, 0x2DC6, 0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6,
            0x2DD8, 0x2DDE, 0x2DE0, 0x2DFF, 0x3005, 0x3007, 0x3021, 0x302F,
            0x3031, 0x3035, 0x3038, 0x303C, 0x3041, 0x3096, 0x3099, 0x309A,
            0x309D, 0x309F, 0x30A1, 0x30FA, 0x30FC, 0x30FF, 0x3105, 0x312F,
            0x3131, 0x318E, 0x31A0, 0x31BF, 0x31F0, 0x31FF, 0x3400, 0x4DBF,
            0x4E00, 0xA48C, 0xA4D0, 0xA4FD, 0xA500, 0xA60C, 0xA610, 0xA62B,
            0xA640, 0xA66F, 0xA674, 0xA67D, 0xA67F, 0xA6F1, 0xA717, 0xA71F,
            0xA722, 0xA788, 0xA78B, 0xA7CA, 0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3,
            0xA7D5, 0xA7D9, 0xA7F2, 0xA827, 0xA82C, 0xA82C, 0xA840, 0xA873,
            0xA880, 0xA8C5, 0xA8D0, 0xA8D9, 0xA8E0, 0xA8F7, 0xA8FB, 0xA8FB,
            0xA8FD, 0xA92D, 0xA930, 0xA953, 0xA960, 0xA97C, 0xA980, 0xA9C0,
            0xA9CF, 0xA9D9, 0xA9E0, 0xA9FE, 0xAA00, 0xAA36, 0xAA40, 0xAA4D,
            0xAA50, 0xAA59, 0xAA60, 0xAA76, 0xAA7A, 0xAAC2, 0xAADB, 0xAADD,
            0xAAE0, 0xAAEF, 0xAAF2, 0xAAF6, 0xAB01, 0xAB06, 0xAB09, 0xAB0E,
            0xAB11, 0xAB16, 0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xAB30, 0xAB5A,
            0xAB5C, 0xAB69, 0xAB70, 0xABEA, 0xABEC, 0xABED, 0xABF0, 0xABF9,
            0xAC00, 0xD7A3, 0xD7B0, 0xD7C6, 0xD7CB, 0xD7FB, 0xF900, 0xFA6D,
            0xFA70, 0xFAD9, 0xFB00, 0xFB06, 0xFB13, 0xFB17, 0xFB1D, 0xFB28,
            0xFB2A, 0xFB36, 0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41,
            0xFB43, 0xFB44, 0xFB46, 0xFBB1, 0xFBD3, 0xFC5D, 0xFC64, 0xFD3D,
            0xFD50, 0xFD8F, 0xFD92, 0xFDC7, 0xFDF0, 0xFDF9, 0xFE00, 0xFE0F,
            0xFE20, 0xFE2F, 0xFE33, 0xFE34, 0xFE4D, 0xFE4F, 0xFE71, 0xFE71,
            0xFE73, 0xFE73, 0xFE77, 0xFE77, 0xFE79, 0xFE79, 0xFE7B, 0xFE7B,
            0xFE7D, 0xFE7D, 0xFE7F, 0xFEFC, 0xFF10, 0xFF19, 0xFF21, 0xFF3A,
            0xFF3F, 0xFF3F, 0xFF41, 0xFF5A, 0xFF66, 0xFFBE, 0xFFC2, 0xFFC7,
            0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC, 0x10000, 0x1000B,
            0x1000D, 0x10026, 0x10028, 0x1003A, 0x1003C, 0x1003D, 0x1003F, 0x1004D,
            0x10050, 0x1005D, 0x10080, 0x100FA, 0x10140, 0x10174, 0x101FD, 0x101FD,
            0x10280, 0x1029C, 0x102A0, 0x102D0, 0x102E0, 0x102E0, 0x10300, 0x1031F,
            0x1032D, 0x1034A, 0x10350, 0x1037A, 0x10380, 0x1039D, 0x103A0, 0x103C3,
            0x103C8, 0x103CF, 0x103D1, 0x103D5, 0x10400, 0x1049D, 0x104A0, 0x104A9,
            0x104B0, 0x104D3, 0x104D8, 0x104FB, 0x10500, 0x10527, 0x10530, 0x10563,
            0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592, 0x10594, 0x10595,
            0x10597, 0x105A1, 0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC,
            0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767, 0x10780, 0x10785,
            0x10787, 0x107B0, 0x107B2, 0x107BA, 0x10800, 0x10805, 0x10808, 0x10808,
            0x1080A, 0x10835, 0x10837, 0x10838, 0x1083C, 0x1083C, 0x1083F, 0x10855,
            0x10860, 0x10876, 0x10880, 0x1089E, 0x108E0, 0x108F2, 0x108F4, 0x108F5,
            0x10900, 0x10915, 0x10920, 0x10939, 0x10980, 0x109B7, 0x109BE, 0x109BF,
            0x10A00, 0x10A03, 0x10A05, 0x10A06, 0x10A0C, 0x10A13, 0x10A15, 0x10A17,
            0x10A19, 0x10A35, 0x10A38, 0x10A3A, 0x10A3F, 0x10A3F, 0x10A60, 0x10A7C,
            0x10A80, 0x10A9C, 0x10AC0, 0x10AC7, 0x10AC9, 0x10AE6, 0x10B00, 0x10B35,
            0x10B40, 0x10B55, 0x10B60, 0x10B72, 0x10B80, 0x10B91, 0x10C00, 0x10C48,
            0x10C80, 0x10CB2, 0x10CC0, 0x10CF2, 0x10D00, 0x10D27, 0x10D30, 0x10D39,
            0x10E80, 0x10EA9, 0x10EAB, 0x10EAC, 0x10EB0, 0x10EB1, 0x10F00, 0x10F1C,
            0x10F27, 0x10F27, 0x10F30, 0x10F50, 0x10F70, 0x10F85, 0x10FB0, 0x10FC4,
            0x10FE0, 0x10FF6, 0x11000, 0x11046, 0x11066, 0x11075, 0x1107F, 0x110BA,
            0x110C2, 0x110C2, 0x110D0, 0x110E8, 0x110F0, 0x110F9, 0x11100, 0x11134,
            0x11136, 0x1113F, 0x11144, 0x11147, 0x11150, 0x11173, 0x11176, 0x11176,
            0x11180, 0x111C4, 0x111C9, 0x111CC, 0x111CE, 0x111DA, 0x111DC, 0x111DC,
            0x11200, 0x11211, 0x11213, 0x11237, 0x1123E, 0x1123E, 0x11280, 0x11286,
            0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D, 0x1129F, 0x112A8,
            0x112B0, 0x112EA, 0x112F0, 0x112F9, 0x11300, 0x11303, 0x11305, 0x1130C,
            0x1130F, 0x11310, 0x11313, 0x11328, 0x1132A, 0x11330, 0x11332, 0x11333,
            0x11335, 0x11339, 0x1133B, 0x11344, 0x11347, 0x11348, 0x1134B, 0x1134D,
            0x11350, 0x11350, 0x11357, 0x11357, 0x1135D, 0x11363, 0x11366, 0x1136C,
            0x11370, 0x11374, 0x11400, 0x1144A, 0x11450, 0x11459, 0x1145E, 0x11461,
            0x11480, 0x114C5, 0x114C7, 0x114C7, 0x114D0, 0x114D9, 0x11580, 0x115B5,
            0x115B8, 0x115C0, 0x115D8, 0x115DD, 0x11600, 0x11640, 0x11644, 0x11644,
            0x11650, 0x11659, 0x11680, 0x116B8, 0x116C0, 0x116C9, 0x11700, 0x1171A,
            0x1171D, 0x1172B, 0x11730, 0x11739, 0x11740, 0x11746, 0x11800, 0x1183A,
            0x118A0, 0x118E9, 0x118FF, 0x11906, 0x11909, 0x11909, 0x1190C, 0x11913,
            0x11915, 0x11916, 0x11918, 0x11935, 0x11937, 0x11938, 0x1193B, 0x11943,
            0x11950, 0x11959, 0x119A0, 0x119A7, 0x119AA, 0x119D7, 0x119DA, 0x119E1,
            0x119E3, 0x119E4, 0x11A00, 0x11A3E, 0x11A47, 0x11A47, 0x11A50, 0x11A99,
            0x11A9D, 0x11A9D, 0x11AB0, 0x11AF8, 0x11C00, 0x11C08, 0x11C0A, 0x11C36,
            0x11C38, 0x11C40, 0x11C50, 0x11C59, 0x11C72, 0x11C8F, 0x11C92, 0x11CA7,
            0x11CA9, 0x11CB6, 0x11D00, 0x11D06, 0x11D08, 0x11D09, 0x11D0B, 0x11D36,
            0x11D3A, 0x11D3A, 0x11D3C, 0x11D3D, 0x11D3F, 0x11D47, 0x11D50, 0x11D59,
            0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D8E, 0x11D90, 0x11D91,
            0x11D93, 0x11D98, 0x11DA0, 0x11DA9, 0x11EE0, 0x11EF6, 0x11FB0, 0x11FB0,
            0x12000, 0x12399, 0x12400, 0x1246E, 0x12480, 0x12543, 0x12F90, 0x12FF0,
            0x13000, 0x1342E, 0x14400, 0x14646, 0x16800, 0x16A38, 0x16A40, 0x16A5E,
            0x16A60, 0x16A69, 0x16A70, 0x16ABE, 0x16AC0, 0x16AC9, 0x16AD0, 0x16AED,
            0x16AF0, 0x16AF4, 0x16B00, 0x16B36, 0x16B40, 0x16B43, 0x16B50, 0x16B59,
            0x16B63, 0x16B77, 0x16B7D, 0x16B8F, 0x16E40, 0x16E7F, 0x16F00, 0x16F4A,
            0x16F4F, 0x16F87, 0x16F8F, 0x16F9F, 0x16FE0, 0x16FE1, 0x16FE3, 0x16FE4,
            0x16FF0, 0x16FF1, 0x17000, 0x187F7, 0x18800, 0x18CD5, 0x18D00, 0x18D08,
            0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE, 0x1B000, 0x1B122,
            0x1B150, 0x1B152, 0x1B164, 0x1B167, 0x1B170, 0x1B2FB, 0x1BC00, 0x1BC6A,
            0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99, 0x1BC9D, 0x1BC9E,
            0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46, 0x1D165, 0x1D169, 0x1D16D, 0x1D172,
            0x1D17B, 0x1D182, 0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD, 0x1D242, 0x1D244,
            0x1D400, 0x1D454, 0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2,
            0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB,
            0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514,
            0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544,
            0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D6C0,
            0x1D6C2, 0x1D6DA, 0x1D6DC, 0x1D6FA, 0x1D6FC, 0x1D714, 0x1D716, 0x1D734,
            0x1D736, 0x1D74E, 0x1D750, 0x1D76E, 0x1D770, 0x1D788, 0x1D78A, 0x1D7A8,
            0x1D7AA, 0x1D7C2, 0x1D7C4, 0x1D7CB, 0x1D7CE, 0x1D7FF, 0x1DA00, 0x1DA36,
            0x1DA3B, 0x1DA6C, 0x1DA75, 0x1DA75, 0x1DA84, 0x1DA84, 0x1DA9B, 0x1DA9F,
            0x1DAA1, 0x1DAAF, 0x1DF00, 0x1DF1E, 0x1E000, 0x1E006, 0x1E008, 0x1E018,
            0x1E01B, 0x1E021, 0x1E023, 0x1E024, 0x1E026, 0x1E02A, 0x1E100, 0x1E12C,
            0x1E130, 0x1E13D, 0x1E140, 0x1E149, 0x1E14E, 0x1E14E, 0x1E290, 0x1E2AE,
            0x1E2C0, 0x1E2F9, 0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE,
            0x1E7F0, 0x1E7FE, 0x1E800, 0x1E8C4, 0x1E8D0, 0x1E8D6, 0x1E900, 0x1E94B,
            0x1E950, 0x1E959, 0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22,
            0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37,
            0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47,
            0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52,
            0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B,
            0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64,
            0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C,
            0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3,
            0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB, 0x1FBF0, 0x1FBF9, 0x20000, 0x2A6DF,
            0x2A700, 0x2B738, 0x2B740, 0x2B81D, 0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0,
            0x2F800, 0x2FA1D, 0x30000, 0x3134A, 0xE0100, 0xE01EF,
            //  #91 (14675+655): bp=XID_Start:XIDS
            0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00B5, 0x00B5,
            0x00BA, 0x00BA, 0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02C1,
            0x02C6, 0x02D1, 0x02E0, 0x02E4, 0x02EC, 0x02EC, 0x02EE, 0x02EE,
            0x0370, 0x0374, 0x0376, 0x0377, 0x037B, 0x037D, 0x037F, 0x037F,
            0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x03A1,
            0x03A3, 0x03F5, 0x03F7, 0x0481, 0x048A, 0x052F, 0x0531, 0x0556,
            0x0559, 0x0559, 0x0560, 0x0588, 0x05D0, 0x05EA, 0x05EF, 0x05F2,
            0x0620, 0x064A, 0x066E, 0x066F, 0x0671, 0x06D3, 0x06D5, 0x06D5,
            0x06E5, 0x06E6, 0x06EE, 0x06EF, 0x06FA, 0x06FC, 0x06FF, 0x06FF,
            0x0710, 0x0710, 0x0712, 0x072F, 0x074D, 0x07A5, 0x07B1, 0x07B1,
            0x07CA, 0x07EA, 0x07F4, 0x07F5, 0x07FA, 0x07FA, 0x0800, 0x0815,
            0x081A, 0x081A, 0x0824, 0x0824, 0x0828, 0x0828, 0x0840, 0x0858,
            0x0860, 0x086A, 0x0870, 0x0887, 0x0889, 0x088E, 0x08A0, 0x08C9,
            0x0904, 0x0939, 0x093D, 0x093D, 0x0950, 0x0950, 0x0958, 0x0961,
            0x0971, 0x0980, 0x0985, 0x098C, 0x098F, 0x0990, 0x0993, 0x09A8,
            0x09AA, 0x09B0, 0x09B2, 0x09B2, 0x09B6, 0x09B9, 0x09BD, 0x09BD,
            0x09CE, 0x09CE, 0x09DC, 0x09DD, 0x09DF, 0x09E1, 0x09F0, 0x09F1,
            0x09FC, 0x09FC, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10, 0x0A13, 0x0A28,
            0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36, 0x0A38, 0x0A39,
            0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A72, 0x0A74, 0x0A85, 0x0A8D,
            0x0A8F, 0x0A91, 0x0A93, 0x0AA8, 0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3,
            0x0AB5, 0x0AB9, 0x0ABD, 0x0ABD, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE1,
            0x0AF9, 0x0AF9, 0x0B05, 0x0B0C, 0x0B0F, 0x0B10, 0x0B13, 0x0B28,
            0x0B2A, 0x0B30, 0x0B32, 0x0B33, 0x0B35, 0x0B39, 0x0B3D, 0x0B3D,
            0x0B5C, 0x0B5D, 0x0B5F, 0x0B61, 0x0B71, 0x0B71, 0x0B83, 0x0B83,
            0x0B85, 0x0B8A, 0x0B8E, 0x0B90, 0x0B92, 0x0B95, 0x0B99, 0x0B9A,
            0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA,
            0x0BAE, 0x0BB9, 0x0BD0, 0x0BD0, 0x0C05, 0x0C0C, 0x0C0E, 0x0C10,
            0x0C12, 0x0C28, 0x0C2A, 0x0C39, 0x0C3D, 0x0C3D, 0x0C58, 0x0C5A,
            0x0C5D, 0x0C5D, 0x0C60, 0x0C61, 0x0C80, 0x0C80, 0x0C85, 0x0C8C,
            0x0C8E, 0x0C90, 0x0C92, 0x0CA8, 0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9,
            0x0CBD, 0x0CBD, 0x0CDD, 0x0CDE, 0x0CE0, 0x0CE1, 0x0CF1, 0x0CF2,
            0x0D04, 0x0D0C, 0x0D0E, 0x0D10, 0x0D12, 0x0D3A, 0x0D3D, 0x0D3D,
            0x0D4E, 0x0D4E, 0x0D54, 0x0D56, 0x0D5F, 0x0D61, 0x0D7A, 0x0D7F,
            0x0D85, 0x0D96, 0x0D9A, 0x0DB1, 0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD,
            0x0DC0, 0x0DC6, 0x0E01, 0x0E30, 0x0E32, 0x0E32, 0x0E40, 0x0E46,
            0x0E81, 0x0E82, 0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3,
            0x0EA5, 0x0EA5, 0x0EA7, 0x0EB0, 0x0EB2, 0x0EB2, 0x0EBD, 0x0EBD,
            0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0EDC, 0x0EDF, 0x0F00, 0x0F00,
            0x0F40, 0x0F47, 0x0F49, 0x0F6C, 0x0F88, 0x0F8C, 0x1000, 0x102A,
            0x103F, 0x103F, 0x1050, 0x1055, 0x105A, 0x105D, 0x1061, 0x1061,
            0x1065, 0x1066, 0x106E, 0x1070, 0x1075, 0x1081, 0x108E, 0x108E,
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA,
            0x10FC, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258,
            0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D, 0x1290, 0x12B0,
            0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0, 0x12C2, 0x12C5,
            0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315, 0x1318, 0x135A,
            0x1380, 0x138F, 0x13A0, 0x13F5, 0x13F8, 0x13FD, 0x1401, 0x166C,
            0x166F, 0x167F, 0x1681, 0x169A, 0x16A0, 0x16EA, 0x16EE, 0x16F8,
            0x1700, 0x1711, 0x171F, 0x1731, 0x1740, 0x1751, 0x1760, 0x176C,
            0x176E, 0x1770, 0x1780, 0x17B3, 0x17D7, 0x17D7, 0x17DC, 0x17DC,
            0x1820, 0x1878, 0x1880, 0x18A8, 0x18AA, 0x18AA, 0x18B0, 0x18F5,
            0x1900, 0x191E, 0x1950, 0x196D, 0x1970, 0x1974, 0x1980, 0x19AB,
            0x19B0, 0x19C9, 0x1A00, 0x1A16, 0x1A20, 0x1A54, 0x1AA7, 0x1AA7,
            0x1B05, 0x1B33, 0x1B45, 0x1B4C, 0x1B83, 0x1BA0, 0x1BAE, 0x1BAF,
            0x1BBA, 0x1BE5, 0x1C00, 0x1C23, 0x1C4D, 0x1C4F, 0x1C5A, 0x1C7D,
            0x1C80, 0x1C88, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x1CE9, 0x1CEC,
            0x1CEE, 0x1CF3, 0x1CF5, 0x1CF6, 0x1CFA, 0x1CFA, 0x1D00, 0x1DBF,
            0x1E00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45, 0x1F48, 0x1F4D,
            0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D,
            0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FBC, 0x1FBE, 0x1FBE,
            0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC, 0x1FD0, 0x1FD3, 0x1FD6, 0x1FDB,
            0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFC, 0x2071, 0x2071,
            0x207F, 0x207F, 0x2090, 0x209C, 0x2102, 0x2102, 0x2107, 0x2107,
            0x210A, 0x2113, 0x2115, 0x2115, 0x2118, 0x211D, 0x2124, 0x2124,
            0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x2139, 0x213C, 0x213F,
            0x2145, 0x2149, 0x214E, 0x214E, 0x2160, 0x2188, 0x2C00, 0x2CE4,
            0x2CEB, 0x2CEE, 0x2CF2, 0x2CF3, 0x2D00, 0x2D25, 0x2D27, 0x2D27,
            0x2D2D, 0x2D2D, 0x2D30, 0x2D67, 0x2D6F, 0x2D6F, 0x2D80, 0x2D96,
            0x2DA0, 0x2DA6, 0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6, 0x2DB8, 0x2DBE,
            0x2DC0, 0x2DC6, 0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6, 0x2DD8, 0x2DDE,
            0x3005, 0x3007, 0x3021, 0x3029, 0x3031, 0x3035, 0x3038, 0x303C,
            0x3041, 0x3096, 0x309D, 0x309F, 0x30A1, 0x30FA, 0x30FC, 0x30FF,
            0x3105, 0x312F, 0x3131, 0x318E, 0x31A0, 0x31BF, 0x31F0, 0x31FF,
            0x3400, 0x4DBF, 0x4E00, 0xA48C, 0xA4D0, 0xA4FD, 0xA500, 0xA60C,
            0xA610, 0xA61F, 0xA62A, 0xA62B, 0xA640, 0xA66E, 0xA67F, 0xA69D,
            0xA6A0, 0xA6EF, 0xA717, 0xA71F, 0xA722, 0xA788, 0xA78B, 0xA7CA,
            0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9, 0xA7F2, 0xA801,
            0xA803, 0xA805, 0xA807, 0xA80A, 0xA80C, 0xA822, 0xA840, 0xA873,
            0xA882, 0xA8B3, 0xA8F2, 0xA8F7, 0xA8FB, 0xA8FB, 0xA8FD, 0xA8FE,
            0xA90A, 0xA925, 0xA930, 0xA946, 0xA960, 0xA97C, 0xA984, 0xA9B2,
            0xA9CF, 0xA9CF, 0xA9E0, 0xA9E4, 0xA9E6, 0xA9EF, 0xA9FA, 0xA9FE,
            0xAA00, 0xAA28, 0xAA40, 0xAA42, 0xAA44, 0xAA4B, 0xAA60, 0xAA76,
            0xAA7A, 0xAA7A, 0xAA7E, 0xAAAF, 0xAAB1, 0xAAB1, 0xAAB5, 0xAAB6,
            0xAAB9, 0xAABD, 0xAAC0, 0xAAC0, 0xAAC2, 0xAAC2, 0xAADB, 0xAADD,
            0xAAE0, 0xAAEA, 0xAAF2, 0xAAF4, 0xAB01, 0xAB06, 0xAB09, 0xAB0E,
            0xAB11, 0xAB16, 0xAB20, 0xAB26, 0xAB28, 0xAB2E, 0xAB30, 0xAB5A,
            0xAB5C, 0xAB69, 0xAB70, 0xABE2, 0xAC00, 0xD7A3, 0xD7B0, 0xD7C6,
            0xD7CB, 0xD7FB, 0xF900, 0xFA6D, 0xFA70, 0xFAD9, 0xFB00, 0xFB06,
            0xFB13, 0xFB17, 0xFB1D, 0xFB1D, 0xFB1F, 0xFB28, 0xFB2A, 0xFB36,
            0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41, 0xFB43, 0xFB44,
            0xFB46, 0xFBB1, 0xFBD3, 0xFC5D, 0xFC64, 0xFD3D, 0xFD50, 0xFD8F,
            0xFD92, 0xFDC7, 0xFDF0, 0xFDF9, 0xFE71, 0xFE71, 0xFE73, 0xFE73,
            0xFE77, 0xFE77, 0xFE79, 0xFE79, 0xFE7B, 0xFE7B, 0xFE7D, 0xFE7D,
            0xFE7F, 0xFEFC, 0xFF21, 0xFF3A, 0xFF41, 0xFF5A, 0xFF66, 0xFF9D,
            0xFFA0, 0xFFBE, 0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7,
            0xFFDA, 0xFFDC, 0x10000, 0x1000B, 0x1000D, 0x10026, 0x10028, 0x1003A,
            0x1003C, 0x1003D, 0x1003F, 0x1004D, 0x10050, 0x1005D, 0x10080, 0x100FA,
            0x10140, 0x10174, 0x10280, 0x1029C, 0x102A0, 0x102D0, 0x10300, 0x1031F,
            0x1032D, 0x1034A, 0x10350, 0x10375, 0x10380, 0x1039D, 0x103A0, 0x103C3,
            0x103C8, 0x103CF, 0x103D1, 0x103D5, 0x10400, 0x1049D, 0x104B0, 0x104D3,
            0x104D8, 0x104FB, 0x10500, 0x10527, 0x10530, 0x10563, 0x10570, 0x1057A,
            0x1057C, 0x1058A, 0x1058C, 0x10592, 0x10594, 0x10595, 0x10597, 0x105A1,
            0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC, 0x10600, 0x10736,
            0x10740, 0x10755, 0x10760, 0x10767, 0x10780, 0x10785, 0x10787, 0x107B0,
            0x107B2, 0x107BA, 0x10800, 0x10805, 0x10808, 0x10808, 0x1080A, 0x10835,
            0x10837, 0x10838, 0x1083C, 0x1083C, 0x1083F, 0x10855, 0x10860, 0x10876,
            0x10880, 0x1089E, 0x108E0, 0x108F2, 0x108F4, 0x108F5, 0x10900, 0x10915,
            0x10920, 0x10939, 0x10980, 0x109B7, 0x109BE, 0x109BF, 0x10A00, 0x10A00,
            0x10A10, 0x10A13, 0x10A15, 0x10A17, 0x10A19, 0x10A35, 0x10A60, 0x10A7C,
            0x10A80, 0x10A9C, 0x10AC0, 0x10AC7, 0x10AC9, 0x10AE4, 0x10B00, 0x10B35,
            0x10B40, 0x10B55, 0x10B60, 0x10B72, 0x10B80, 0x10B91, 0x10C00, 0x10C48,
            0x10C80, 0x10CB2, 0x10CC0, 0x10CF2, 0x10D00, 0x10D23, 0x10E80, 0x10EA9,
            0x10EB0, 0x10EB1, 0x10F00, 0x10F1C, 0x10F27, 0x10F27, 0x10F30, 0x10F45,
            0x10F70, 0x10F81, 0x10FB0, 0x10FC4, 0x10FE0, 0x10FF6, 0x11003, 0x11037,
            0x11071, 0x11072, 0x11075, 0x11075, 0x11083, 0x110AF, 0x110D0, 0x110E8,
            0x11103, 0x11126, 0x11144, 0x11144, 0x11147, 0x11147, 0x11150, 0x11172,
            0x11176, 0x11176, 0x11183, 0x111B2, 0x111C1, 0x111C4, 0x111DA, 0x111DA,
            0x111DC, 0x111DC, 0x11200, 0x11211, 0x11213, 0x1122B, 0x11280, 0x11286,
            0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D, 0x1129F, 0x112A8,
            0x112B0, 0x112DE, 0x11305, 0x1130C, 0x1130F, 0x11310, 0x11313, 0x11328,
            0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339, 0x1133D, 0x1133D,
            0x11350, 0x11350, 0x1135D, 0x11361, 0x11400, 0x11434, 0x11447, 0x1144A,
            0x1145F, 0x11461, 0x11480, 0x114AF, 0x114C4, 0x114C5, 0x114C7, 0x114C7,
            0x11580, 0x115AE, 0x115D8, 0x115DB, 0x11600, 0x1162F, 0x11644, 0x11644,
            0x11680, 0x116AA, 0x116B8, 0x116B8, 0x11700, 0x1171A, 0x11740, 0x11746,
            0x11800, 0x1182B, 0x118A0, 0x118DF, 0x118FF, 0x11906, 0x11909, 0x11909,
            0x1190C, 0x11913, 0x11915, 0x11916, 0x11918, 0x1192F, 0x1193F, 0x1193F,
            0x11941, 0x11941, 0x119A0, 0x119A7, 0x119AA, 0x119D0, 0x119E1, 0x119E1,
            0x119E3, 0x119E3, 0x11A00, 0x11A00, 0x11A0B, 0x11A32, 0x11A3A, 0x11A3A,
            0x11A50, 0x11A50, 0x11A5C, 0x11A89, 0x11A9D, 0x11A9D, 0x11AB0, 0x11AF8,
            0x11C00, 0x11C08, 0x11C0A, 0x11C2E, 0x11C40, 0x11C40, 0x11C72, 0x11C8F,
            0x11D00, 0x11D06, 0x11D08, 0x11D09, 0x11D0B, 0x11D30, 0x11D46, 0x11D46,
            0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D89, 0x11D98, 0x11D98,
            0x11EE0, 0x11EF2, 0x11FB0, 0x11FB0, 0x12000, 0x12399, 0x12400, 0x1246E,
            0x12480, 0x12543, 0x12F90, 0x12FF0, 0x13000, 0x1342E, 0x14400, 0x14646,
            0x16800, 0x16A38, 0x16A40, 0x16A5E, 0x16A70, 0x16ABE, 0x16AD0, 0x16AED,
            0x16B00, 0x16B2F, 0x16B40, 0x16B43, 0x16B63, 0x16B77, 0x16B7D, 0x16B8F,
            0x16E40, 0x16E7F, 0x16F00, 0x16F4A, 0x16F50, 0x16F50, 0x16F93, 0x16F9F,
            0x16FE0, 0x16FE1, 0x16FE3, 0x16FE3, 0x17000, 0x187F7, 0x18800, 0x18CD5,
            0x18D00, 0x18D08, 0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE,
            0x1B000, 0x1B122, 0x1B150, 0x1B152, 0x1B164, 0x1B167, 0x1B170, 0x1B2FB,
            0x1BC00, 0x1BC6A, 0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99,
            0x1D400, 0x1D454, 0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2,
            0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB,
            0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514,
            0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544,
            0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D6C0,
            0x1D6C2, 0x1D6DA, 0x1D6DC, 0x1D6FA, 0x1D6FC, 0x1D714, 0x1D716, 0x1D734,
            0x1D736, 0x1D74E, 0x1D750, 0x1D76E, 0x1D770, 0x1D788, 0x1D78A, 0x1D7A8,
            0x1D7AA, 0x1D7C2, 0x1D7C4, 0x1D7CB, 0x1DF00, 0x1DF1E, 0x1E100, 0x1E12C,
            0x1E137, 0x1E13D, 0x1E14E, 0x1E14E, 0x1E290, 0x1E2AD, 0x1E2C0, 0x1E2EB,
            0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE, 0x1E7F0, 0x1E7FE,
            0x1E800, 0x1E8C4, 0x1E900, 0x1E943, 0x1E94B, 0x1E94B, 0x1EE00, 0x1EE03,
            0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22, 0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27,
            0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37, 0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B,
            0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47, 0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B,
            0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52, 0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57,
            0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B, 0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F,
            0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64, 0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72,
            0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C, 0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89,
            0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3, 0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB,
            0x20000, 0x2A6DF, 0x2A700, 0x2B738, 0x2B740, 0x2B81D, 0x2B820, 0x2CEA1,
            0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D, 0x30000, 0x3134A,
            //  #92 (15330+3): sc=Adlam:Adlm
            0x1E900, 0x1E94B, 0x1E950, 0x1E959, 0x1E95E, 0x1E95F,
            //  #93 (15333+3): sc=Ahom:Ahom scx=Ahom:Ahom
            0x11700, 0x1171A, 0x1171D, 0x1172B, 0x11730, 0x11746,
            //  #94 (15336+1): sc=Anatolian_Hieroglyphs:Hluw scx=Anatolian_Hieroglyphs:Hluw
            0x14400, 0x14646,
            //  #95 (15337+57): sc=Arabic:Arab
            0x0600, 0x0604, 0x0606, 0x060B, 0x060D, 0x061A, 0x061C, 0x061E,
            0x0620, 0x063F, 0x0641, 0x064A, 0x0656, 0x066F, 0x0671, 0x06DC,
            0x06DE, 0x06FF, 0x0750, 0x077F, 0x0870, 0x088E, 0x0890, 0x0891,
            0x0898, 0x08E1, 0x08E3, 0x08FF, 0xFB50, 0xFBC2, 0xFBD3, 0xFD3D,
            0xFD40, 0xFD8F, 0xFD92, 0xFDC7, 0xFDCF, 0xFDCF, 0xFDF0, 0xFDFF,
            0xFE70, 0xFE74, 0xFE76, 0xFEFC, 0x10E60, 0x10E7E, 0x1EE00, 0x1EE03,
            0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22, 0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27,
            0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37, 0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B,
            0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47, 0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B,
            0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52, 0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57,
            0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B, 0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F,
            0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64, 0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72,
            0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C, 0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89,
            0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3, 0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB,
            0x1EEF0, 0x1EEF1,
            //  #96 (15394+4): sc=Armenian:Armn scx=Armenian:Armn
            0x0531, 0x0556, 0x0559, 0x058A, 0x058D, 0x058F, 0xFB13, 0xFB17,
            //  #97 (15398+2): sc=Avestan:Avst scx=Avestan:Avst
            0x10B00, 0x10B35, 0x10B39, 0x10B3F,
            //  #98 (15400+2): sc=Balinese:Bali scx=Balinese:Bali
            0x1B00, 0x1B4C, 0x1B50, 0x1B7E,
            //  #99 (15402+2): sc=Bamum:Bamu scx=Bamum:Bamu
            0xA6A0, 0xA6F7, 0x16800, 0x16A38,
            //  #100 (15404+2): sc=Bassa_Vah:Bass scx=Bassa_Vah:Bass
            0x16AD0, 0x16AED, 0x16AF0, 0x16AF5,
            //  #101 (15406+2): sc=Batak:Batk scx=Batak:Batk
            0x1BC0, 0x1BF3, 0x1BFC, 0x1BFF,
            //  #102 (15408+14): sc=Bengali:Beng
            0x0980, 0x0983, 0x0985, 0x098C, 0x098F, 0x0990, 0x0993, 0x09A8,
            0x09AA, 0x09B0, 0x09B2, 0x09B2, 0x09B6, 0x09B9, 0x09BC, 0x09C4,
            0x09C7, 0x09C8, 0x09CB, 0x09CE, 0x09D7, 0x09D7, 0x09DC, 0x09DD,
            0x09DF, 0x09E3, 0x09E6, 0x09FE,
            //  #103 (15422+4): sc=Bhaiksuki:Bhks scx=Bhaiksuki:Bhks
            0x11C00, 0x11C08, 0x11C0A, 0x11C36, 0x11C38, 0x11C45, 0x11C50, 0x11C6C,
            //  #104 (15426+3): sc=Bopomofo:Bopo
            0x02EA, 0x02EB, 0x3105, 0x312F, 0x31A0, 0x31BF,
            //  #105 (15429+3): sc=Brahmi:Brah scx=Brahmi:Brah
            0x11000, 0x1104D, 0x11052, 0x11075, 0x1107F, 0x1107F,
            //  #106 (15432+1): sc=Braille:Brai scx=Braille:Brai
            0x2800, 0x28FF,
            //  #107 (15433+2): sc=Buginese:Bugi
            0x1A00, 0x1A1B, 0x1A1E, 0x1A1F,
            //  #108 (15435+1): sc=Buhid:Buhd
            0x1740, 0x1753,
            //  #109 (15436+3): sc=Canadian_Aboriginal:Cans scx=Canadian_Aboriginal:Cans
            0x1400, 0x167F, 0x18B0, 0x18F5, 0x11AB0, 0x11ABF,
            //  #110 (15439+1): sc=Carian:Cari scx=Carian:Cari
            0x102A0, 0x102D0,
            //  #111 (15440+2): sc=Caucasian_Albanian:Aghb scx=Caucasian_Albanian:Aghb
            0x10530, 0x10563, 0x1056F, 0x1056F,
            //  #112 (15442+2): sc=Chakma:Cakm
            0x11100, 0x11134, 0x11136, 0x11147,
            //  #113 (15444+4): sc=Cham:Cham scx=Cham:Cham
            0xAA00, 0xAA36, 0xAA40, 0xAA4D, 0xAA50, 0xAA59, 0xAA5C, 0xAA5F,
            //  #114 (15448+3): sc=Cherokee:Cher scx=Cherokee:Cher
            0x13A0, 0x13F5, 0x13F8, 0x13FD, 0xAB70, 0xABBF,
            //  #115 (15451+1): sc=Chorasmian:Chrs scx=Chorasmian:Chrs
            0x10FB0, 0x10FCB,
            //  #116 (15452+174): sc=Common:Zyyy
            0x0000, 0x0040, 0x005B, 0x0060, 0x007B, 0x00A9, 0x00AB, 0x00B9,
            0x00BB, 0x00BF, 0x00D7, 0x00D7, 0x00F7, 0x00F7, 0x02B9, 0x02DF,
            0x02E5, 0x02E9, 0x02EC, 0x02FF, 0x0374, 0x0374, 0x037E, 0x037E,
            0x0385, 0x0385, 0x0387, 0x0387, 0x0605, 0x0605, 0x060C, 0x060C,
            0x061B, 0x061B, 0x061F, 0x061F, 0x0640, 0x0640, 0x06DD, 0x06DD,
            0x08E2, 0x08E2, 0x0964, 0x0965, 0x0E3F, 0x0E3F, 0x0FD5, 0x0FD8,
            0x10FB, 0x10FB, 0x16EB, 0x16ED, 0x1735, 0x1736, 0x1802, 0x1803,
            0x1805, 0x1805, 0x1CD3, 0x1CD3, 0x1CE1, 0x1CE1, 0x1CE9, 0x1CEC,
            0x1CEE, 0x1CF3, 0x1CF5, 0x1CF7, 0x1CFA, 0x1CFA, 0x2000, 0x200B,
            0x200E, 0x2064, 0x2066, 0x2070, 0x2074, 0x207E, 0x2080, 0x208E,
            0x20A0, 0x20C0, 0x2100, 0x2125, 0x2127, 0x2129, 0x212C, 0x2131,
            0x2133, 0x214D, 0x214F, 0x215F, 0x2189, 0x218B, 0x2190, 0x2426,
            0x2440, 0x244A, 0x2460, 0x27FF, 0x2900, 0x2B73, 0x2B76, 0x2B95,
            0x2B97, 0x2BFF, 0x2E00, 0x2E5D, 0x2FF0, 0x2FFB, 0x3000, 0x3004,
            0x3006, 0x3006, 0x3008, 0x3020, 0x3030, 0x3037, 0x303C, 0x303F,
            0x309B, 0x309C, 0x30A0, 0x30A0, 0x30FB, 0x30FC, 0x3190, 0x319F,
            0x31C0, 0x31E3, 0x3220, 0x325F, 0x327F, 0x32CF, 0x32FF, 0x32FF,
            0x3358, 0x33FF, 0x4DC0, 0x4DFF, 0xA700, 0xA721, 0xA788, 0xA78A,
            0xA830, 0xA839, 0xA92E, 0xA92E, 0xA9CF, 0xA9CF, 0xAB5B, 0xAB5B,
            0xAB6A, 0xAB6B, 0xFD3E, 0xFD3F, 0xFE10, 0xFE19, 0xFE30, 0xFE52,
            0xFE54, 0xFE66, 0xFE68, 0xFE6B, 0xFEFF, 0xFEFF, 0xFF01, 0xFF20,
            0xFF3B, 0xFF40, 0xFF5B, 0xFF65, 0xFF70, 0xFF70, 0xFF9E, 0xFF9F,
            0xFFE0, 0xFFE6, 0xFFE8, 0xFFEE, 0xFFF9, 0xFFFD, 0x10100, 0x10102,
            0x10107, 0x10133, 0x10137, 0x1013F, 0x10190, 0x1019C, 0x101D0, 0x101FC,
            0x102E1, 0x102FB, 0x1BCA0, 0x1BCA3, 0x1CF50, 0x1CFC3, 0x1D000, 0x1D0F5,
            0x1D100, 0x1D126, 0x1D129, 0x1D166, 0x1D16A, 0x1D17A, 0x1D183, 0x1D184,
            0x1D18C, 0x1D1A9, 0x1D1AE, 0x1D1EA, 0x1D2E0, 0x1D2F3, 0x1D300, 0x1D356,
            0x1D360, 0x1D378, 0x1D400, 0x1D454, 0x1D456, 0x1D49C, 0x1D49E, 0x1D49F,
            0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9,
            0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A,
            0x1D50D, 0x1D514, 0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E,
            0x1D540, 0x1D544, 0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A5,
            0x1D6A8, 0x1D7CB, 0x1D7CE, 0x1D7FF, 0x1EC71, 0x1ECB4, 0x1ED01, 0x1ED3D,
            0x1F000, 0x1F02B, 0x1F030, 0x1F093, 0x1F0A0, 0x1F0AE, 0x1F0B1, 0x1F0BF,
            0x1F0C1, 0x1F0CF, 0x1F0D1, 0x1F0F5, 0x1F100, 0x1F1AD, 0x1F1E6, 0x1F1FF,
            0x1F201, 0x1F202, 0x1F210, 0x1F23B, 0x1F240, 0x1F248, 0x1F250, 0x1F251,
            0x1F260, 0x1F265, 0x1F300, 0x1F6D7, 0x1F6DD, 0x1F6EC, 0x1F6F0, 0x1F6FC,
            0x1F700, 0x1F773, 0x1F780, 0x1F7D8, 0x1F7E0, 0x1F7EB, 0x1F7F0, 0x1F7F0,
            0x1F800, 0x1F80B, 0x1F810, 0x1F847, 0x1F850, 0x1F859, 0x1F860, 0x1F887,
            0x1F890, 0x1F8AD, 0x1F8B0, 0x1F8B1, 0x1F900, 0x1FA53, 0x1FA60, 0x1FA6D,
            0x1FA70, 0x1FA74, 0x1FA78, 0x1FA7C, 0x1FA80, 0x1FA86, 0x1FA90, 0x1FAAC,
            0x1FAB0, 0x1FABA, 0x1FAC0, 0x1FAC5, 0x1FAD0, 0x1FAD9, 0x1FAE0, 0x1FAE7,
            0x1FAF0, 0x1FAF6, 0x1FB00, 0x1FB92, 0x1FB94, 0x1FBCA, 0x1FBF0, 0x1FBF9,
            0xE0001, 0xE0001, 0xE0020, 0xE007F,
            //  #117 (15626+3): sc=Coptic:Copt:Qaac
            0x03E2, 0x03EF, 0x2C80, 0x2CF3, 0x2CF9, 0x2CFF,
            //  #118 (15629+1): sc=Cypro_Minoan:Cpmn
            0x12F90, 0x12FF2,
            //  #119 (15630+4): sc=Cuneiform:Xsux scx=Cuneiform:Xsux
            0x12000, 0x12399, 0x12400, 0x1246E, 0x12470, 0x12474, 0x12480, 0x12543,
            //  #120 (15634+6): sc=Cypriot:Cprt
            0x10800, 0x10805, 0x10808, 0x10808, 0x1080A, 0x10835, 0x10837, 0x10838,
            0x1083C, 0x1083C, 0x1083F, 0x1083F,
            //  #121 (15640+8): sc=Cyrillic:Cyrl
            0x0400, 0x0484, 0x0487, 0x052F, 0x1C80, 0x1C88, 0x1D2B, 0x1D2B,
            0x1D78, 0x1D78, 0x2DE0, 0x2DFF, 0xA640, 0xA69F, 0xFE2E, 0xFE2F,
            //  #122 (15648+1): sc=Deseret:Dsrt scx=Deseret:Dsrt
            0x10400, 0x1044F,
            //  #123 (15649+4): sc=Devanagari:Deva
            0x0900, 0x0950, 0x0955, 0x0963, 0x0966, 0x097F, 0xA8E0, 0xA8FF,
            //  #124 (15653+8): sc=Dives_Akuru:Diak scx=Dives_Akuru:Diak
            0x11900, 0x11906, 0x11909, 0x11909, 0x1190C, 0x11913, 0x11915, 0x11916,
            0x11918, 0x11935, 0x11937, 0x11938, 0x1193B, 0x11946, 0x11950, 0x11959,
            //  #125 (15661+1): sc=Dogra:Dogr
            0x11800, 0x1183B,
            //  #126 (15662+5): sc=Duployan:Dupl
            0x1BC00, 0x1BC6A, 0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99,
            0x1BC9C, 0x1BC9F,
            //  #127 (15667+2): sc=Egyptian_Hieroglyphs:Egyp scx=Egyptian_Hieroglyphs:Egyp
            0x13000, 0x1342E, 0x13430, 0x13438,
            //  #128 (15669+1): sc=Elbasan:Elba scx=Elbasan:Elba
            0x10500, 0x10527,
            //  #129 (15670+1): sc=Elymaic:Elym scx=Elymaic:Elym
            0x10FE0, 0x10FF6,
            //  #130 (15671+36): sc=Ethiopic:Ethi scx=Ethiopic:Ethi
            0x1200, 0x1248, 0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258,
            0x125A, 0x125D, 0x1260, 0x1288, 0x128A, 0x128D, 0x1290, 0x12B0,
            0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0, 0x12C2, 0x12C5,
            0x12C8, 0x12D6, 0x12D8, 0x1310, 0x1312, 0x1315, 0x1318, 0x135A,
            0x135D, 0x137C, 0x1380, 0x1399, 0x2D80, 0x2D96, 0x2DA0, 0x2DA6,
            0x2DA8, 0x2DAE, 0x2DB0, 0x2DB6, 0x2DB8, 0x2DBE, 0x2DC0, 0x2DC6,
            0x2DC8, 0x2DCE, 0x2DD0, 0x2DD6, 0x2DD8, 0x2DDE, 0xAB01, 0xAB06,
            0xAB09, 0xAB0E, 0xAB11, 0xAB16, 0xAB20, 0xAB26, 0xAB28, 0xAB2E,
            0x1E7E0, 0x1E7E6, 0x1E7E8, 0x1E7EB, 0x1E7ED, 0x1E7EE, 0x1E7F0, 0x1E7FE,
            //  #131 (15707+10): sc=Georgian:Geor
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FA,
            0x10FC, 0x10FF, 0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x2D00, 0x2D25,
            0x2D27, 0x2D27, 0x2D2D, 0x2D2D,
            //  #132 (15717+6): sc=Glagolitic:Glag
            0x2C00, 0x2C5F, 0x1E000, 0x1E006, 0x1E008, 0x1E018, 0x1E01B, 0x1E021,
            0x1E023, 0x1E024, 0x1E026, 0x1E02A,
            //  #133 (15723+1): sc=Gothic:Goth scx=Gothic:Goth
            0x10330, 0x1034A,
            //  #134 (15724+15): sc=Grantha:Gran
            0x11300, 0x11303, 0x11305, 0x1130C, 0x1130F, 0x11310, 0x11313, 0x11328,
            0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339, 0x1133C, 0x11344,
            0x11347, 0x11348, 0x1134B, 0x1134D, 0x11350, 0x11350, 0x11357, 0x11357,
            0x1135D, 0x11363, 0x11366, 0x1136C, 0x11370, 0x11374,
            //  #135 (15739+36): sc=Greek:Grek
            0x0370, 0x0373, 0x0375, 0x0377, 0x037A, 0x037D, 0x037F, 0x037F,
            0x0384, 0x0384, 0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C,
            0x038E, 0x03A1, 0x03A3, 0x03E1, 0x03F0, 0x03FF, 0x1D26, 0x1D2A,
            0x1D5D, 0x1D61, 0x1D66, 0x1D6A, 0x1DBF, 0x1DBF, 0x1F00, 0x1F15,
            0x1F18, 0x1F1D, 0x1F20, 0x1F45, 0x1F48, 0x1F4D, 0x1F50, 0x1F57,
            0x1F59, 0x1F59, 0x1F5B, 0x1F5B, 0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D,
            0x1F80, 0x1FB4, 0x1FB6, 0x1FC4, 0x1FC6, 0x1FD3, 0x1FD6, 0x1FDB,
            0x1FDD, 0x1FEF, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFE, 0x2126, 0x2126,
            0xAB65, 0xAB65, 0x10140, 0x1018E, 0x101A0, 0x101A0, 0x1D200, 0x1D245,
            //  #136 (15775+14): sc=Gujarati:Gujr
            0x0A81, 0x0A83, 0x0A85, 0x0A8D, 0x0A8F, 0x0A91, 0x0A93, 0x0AA8,
            0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3, 0x0AB5, 0x0AB9, 0x0ABC, 0x0AC5,
            0x0AC7, 0x0AC9, 0x0ACB, 0x0ACD, 0x0AD0, 0x0AD0, 0x0AE0, 0x0AE3,
            0x0AE6, 0x0AF1, 0x0AF9, 0x0AFF,
            //  #137 (15789+6): sc=Gunjala_Gondi:Gong
            0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D8E, 0x11D90, 0x11D91,
            0x11D93, 0x11D98, 0x11DA0, 0x11DA9,
            //  #138 (15795+16): sc=Gurmukhi:Guru
            0x0A01, 0x0A03, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10, 0x0A13, 0x0A28,
            0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36, 0x0A38, 0x0A39,
            0x0A3C, 0x0A3C, 0x0A3E, 0x0A42, 0x0A47, 0x0A48, 0x0A4B, 0x0A4D,
            0x0A51, 0x0A51, 0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A66, 0x0A76,
            //  #139 (15811+20): sc=Han:Hani
            0x2E80, 0x2E99, 0x2E9B, 0x2EF3, 0x2F00, 0x2FD5, 0x3005, 0x3005,
            0x3007, 0x3007, 0x3021, 0x3029, 0x3038, 0x303B, 0x3400, 0x4DBF,
            0x4E00, 0x9FFF, 0xF900, 0xFA6D, 0xFA70, 0xFAD9, 0x16FE2, 0x16FE3,
            0x16FF0, 0x16FF1, 0x20000, 0x2A6DF, 0x2A700, 0x2B738, 0x2B740, 0x2B81D,
            0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D, 0x30000, 0x3134A,
            //  #140 (15831+14): sc=Hangul:Hang
            0x1100, 0x11FF, 0x302E, 0x302F, 0x3131, 0x318E, 0x3200, 0x321E,
            0x3260, 0x327E, 0xA960, 0xA97C, 0xAC00, 0xD7A3, 0xD7B0, 0xD7C6,
            0xD7CB, 0xD7FB, 0xFFA0, 0xFFBE, 0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF,
            0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC,
            //  #141 (15845+2): sc=Hanifi_Rohingya:Rohg
            0x10D00, 0x10D27, 0x10D30, 0x10D39,
            //  #142 (15847+1): sc=Hanunoo:Hano
            0x1720, 0x1734,
            //  #143 (15848+3): sc=Hatran:Hatr scx=Hatran:Hatr
            0x108E0, 0x108F2, 0x108F4, 0x108F5, 0x108FB, 0x108FF,
            //  #144 (15851+9): sc=Hebrew:Hebr scx=Hebrew:Hebr
            0x0591, 0x05C7, 0x05D0, 0x05EA, 0x05EF, 0x05F4, 0xFB1D, 0xFB36,
            0xFB38, 0xFB3C, 0xFB3E, 0xFB3E, 0xFB40, 0xFB41, 0xFB43, 0xFB44,
            0xFB46, 0xFB4F,
            //  #145 (15860+5): sc=Hiragana:Hira
            0x3041, 0x3096, 0x309D, 0x309F, 0x1B001, 0x1B11F, 0x1B150, 0x1B152,
            0x1F200, 0x1F200,
            //  #146 (15865+2): sc=Imperial_Aramaic:Armi scx=Imperial_Aramaic:Armi
            0x10840, 0x10855, 0x10857, 0x1085F,
            //  #147 (15867+29): sc=Inherited:Zinh:Qaai
            0x0300, 0x036F, 0x0485, 0x0486, 0x064B, 0x0655, 0x0670, 0x0670,
            0x0951, 0x0954, 0x1AB0, 0x1ACE, 0x1CD0, 0x1CD2, 0x1CD4, 0x1CE0,
            0x1CE2, 0x1CE8, 0x1CED, 0x1CED, 0x1CF4, 0x1CF4, 0x1CF8, 0x1CF9,
            0x1DC0, 0x1DFF, 0x200C, 0x200D, 0x20D0, 0x20F0, 0x302A, 0x302D,
            0x3099, 0x309A, 0xFE00, 0xFE0F, 0xFE20, 0xFE2D, 0x101FD, 0x101FD,
            0x102E0, 0x102E0, 0x1133B, 0x1133B, 0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46,
            0x1D167, 0x1D169, 0x1D17B, 0x1D182, 0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD,
            0xE0100, 0xE01EF,
            //  #148 (15896+2): sc=Inscriptional_Pahlavi:Phli scx=Inscriptional_Pahlavi:Phli
            0x10B60, 0x10B72, 0x10B78, 0x10B7F,
            //  #149 (15898+2): sc=Inscriptional_Parthian:Prti scx=Inscriptional_Parthian:Prti
            0x10B40, 0x10B55, 0x10B58, 0x10B5F,
            //  #150 (15900+3): sc=Javanese:Java
            0xA980, 0xA9CD, 0xA9D0, 0xA9D9, 0xA9DE, 0xA9DF,
            //  #151 (15903+2): sc=Kaithi:Kthi
            0x11080, 0x110C2, 0x110CD, 0x110CD,
            //  #152 (15905+13): sc=Kannada:Knda
            0x0C80, 0x0C8C, 0x0C8E, 0x0C90, 0x0C92, 0x0CA8, 0x0CAA, 0x0CB3,
            0x0CB5, 0x0CB9, 0x0CBC, 0x0CC4, 0x0CC6, 0x0CC8, 0x0CCA, 0x0CCD,
            0x0CD5, 0x0CD6, 0x0CDD, 0x0CDE, 0x0CE0, 0x0CE3, 0x0CE6, 0x0CEF,
            0x0CF1, 0x0CF2,
            //  #153 (15918+13): sc=Katakana:Kana
            0x30A1, 0x30FA, 0x30FD, 0x30FF, 0x31F0, 0x31FF, 0x32D0, 0x32FE,
            0x3300, 0x3357, 0xFF66, 0xFF6F, 0xFF71, 0xFF9D, 0x1AFF0, 0x1AFF3,
            0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE, 0x1B000, 0x1B000, 0x1B120, 0x1B122,
            0x1B164, 0x1B167,
            //  #154 (15931+2): sc=Kayah_Li:Kali
            0xA900, 0xA92D, 0xA92F, 0xA92F,
            //  #155 (15933+8): sc=Kharoshthi:Khar scx=Kharoshthi:Khar
            0x10A00, 0x10A03, 0x10A05, 0x10A06, 0x10A0C, 0x10A13, 0x10A15, 0x10A17,
            0x10A19, 0x10A35, 0x10A38, 0x10A3A, 0x10A3F, 0x10A48, 0x10A50, 0x10A58,
            //  #156 (15941+2): sc=Khitan_Small_Script:Kits scx=Khitan_Small_Script:Kits
            0x16FE4, 0x16FE4, 0x18B00, 0x18CD5,
            //  #157 (15943+4): sc=Khmer:Khmr scx=Khmer:Khmr
            0x1780, 0x17DD, 0x17E0, 0x17E9, 0x17F0, 0x17F9, 0x19E0, 0x19FF,
            //  #158 (15947+2): sc=Khojki:Khoj
            0x11200, 0x11211, 0x11213, 0x1123E,
            //  #159 (15949+2): sc=Khudawadi:Sind
            0x112B0, 0x112EA, 0x112F0, 0x112F9,
            //  #160 (15951+11): sc=Lao:Laoo scx=Lao:Laoo
            0x0E81, 0x0E82, 0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3,
            0x0EA5, 0x0EA5, 0x0EA7, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6,
            0x0EC8, 0x0ECD, 0x0ED0, 0x0ED9, 0x0EDC, 0x0EDF,
            //  #161 (15962+38): sc=Latin:Latn
            0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00BA, 0x00BA,
            0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02B8, 0x02E0, 0x02E4,
            0x1D00, 0x1D25, 0x1D2C, 0x1D5C, 0x1D62, 0x1D65, 0x1D6B, 0x1D77,
            0x1D79, 0x1DBE, 0x1E00, 0x1EFF, 0x2071, 0x2071, 0x207F, 0x207F,
            0x2090, 0x209C, 0x212A, 0x212B, 0x2132, 0x2132, 0x214E, 0x214E,
            0x2160, 0x2188, 0x2C60, 0x2C7F, 0xA722, 0xA787, 0xA78B, 0xA7CA,
            0xA7D0, 0xA7D1, 0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9, 0xA7F2, 0xA7FF,
            0xAB30, 0xAB5A, 0xAB5C, 0xAB64, 0xAB66, 0xAB69, 0xFB00, 0xFB06,
            0xFF21, 0xFF3A, 0xFF41, 0xFF5A, 0x10780, 0x10785, 0x10787, 0x107B0,
            0x107B2, 0x107BA, 0x1DF00, 0x1DF1E,
            //  #162 (16000+3): sc=Lepcha:Lepc scx=Lepcha:Lepc
            0x1C00, 0x1C37, 0x1C3B, 0x1C49, 0x1C4D, 0x1C4F,
            //  #163 (16003+5): sc=Limbu:Limb
            0x1900, 0x191E, 0x1920, 0x192B, 0x1930, 0x193B, 0x1940, 0x1940,
            0x1944, 0x194F,
            //  #164 (16008+3): sc=Linear_A:Lina
            0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767,
            //  #165 (16011+7): sc=Linear_B:Linb
            0x10000, 0x1000B, 0x1000D, 0x10026, 0x10028, 0x1003A, 0x1003C, 0x1003D,
            0x1003F, 0x1004D, 0x10050, 0x1005D, 0x10080, 0x100FA,
            //  #166 (16018+2): sc=Lisu:Lisu scx=Lisu:Lisu
            0xA4D0, 0xA4FF, 0x11FB0, 0x11FB0,
            //  #167 (16020+1): sc=Lycian:Lyci scx=Lycian:Lyci
            0x10280, 0x1029C,
            //  #168 (16021+2): sc=Lydian:Lydi scx=Lydian:Lydi
            0x10920, 0x10939, 0x1093F, 0x1093F,
            //  #169 (16023+1): sc=Mahajani:Mahj
            0x11150, 0x11176,
            //  #170 (16024+1): sc=Makasar:Maka scx=Makasar:Maka
            0x11EE0, 0x11EF8,
            //  #171 (16025+7): sc=Malayalam:Mlym
            0x0D00, 0x0D0C, 0x0D0E, 0x0D10, 0x0D12, 0x0D44, 0x0D46, 0x0D48,
            0x0D4A, 0x0D4F, 0x0D54, 0x0D63, 0x0D66, 0x0D7F,
            //  #172 (16032+2): sc=Mandaic:Mand
            0x0840, 0x085B, 0x085E, 0x085E,
            //  #173 (16034+2): sc=Manichaean:Mani
            0x10AC0, 0x10AE6, 0x10AEB, 0x10AF6,
            //  #174 (16036+3): sc=Marchen:Marc scx=Marchen:Marc
            0x11C70, 0x11C8F, 0x11C92, 0x11CA7, 0x11CA9, 0x11CB6,
            //  #175 (16039+7): sc=Masaram_Gondi:Gonm
            0x11D00, 0x11D06, 0x11D08, 0x11D09, 0x11D0B, 0x11D36, 0x11D3A, 0x11D3A,
            0x11D3C, 0x11D3D, 0x11D3F, 0x11D47, 0x11D50, 0x11D59,
            //  #176 (16046+1): sc=Medefaidrin:Medf scx=Medefaidrin:Medf
            0x16E40, 0x16E9A,
            //  #177 (16047+3): sc=Meetei_Mayek:Mtei scx=Meetei_Mayek:Mtei
            0xAAE0, 0xAAF6, 0xABC0, 0xABED, 0xABF0, 0xABF9,
            //  #178 (16050+2): sc=Mende_Kikakui:Mend scx=Mende_Kikakui:Mend
            0x1E800, 0x1E8C4, 0x1E8C7, 0x1E8D6,
            //  #179 (16052+3): sc=Meroitic_Cursive:Merc scx=Meroitic_Cursive:Merc
            0x109A0, 0x109B7, 0x109BC, 0x109CF, 0x109D2, 0x109FF,
            //  #180 (16055+1): sc=Meroitic_Hieroglyphs:Mero scx=Meroitic_Hieroglyphs:Mero
            0x10980, 0x1099F,
            //  #181 (16056+3): sc=Miao:Plrd scx=Miao:Plrd
            0x16F00, 0x16F4A, 0x16F4F, 0x16F87, 0x16F8F, 0x16F9F,
            //  #182 (16059+2): sc=Modi:Modi
            0x11600, 0x11644, 0x11650, 0x11659,
            //  #183 (16061+6): sc=Mongolian:Mong
            0x1800, 0x1801, 0x1804, 0x1804, 0x1806, 0x1819, 0x1820, 0x1878,
            0x1880, 0x18AA, 0x11660, 0x1166C,
            //  #184 (16067+3): sc=Mro:Mroo scx=Mro:Mroo
            0x16A40, 0x16A5E, 0x16A60, 0x16A69, 0x16A6E, 0x16A6F,
            //  #185 (16070+5): sc=Multani:Mult
            0x11280, 0x11286, 0x11288, 0x11288, 0x1128A, 0x1128D, 0x1128F, 0x1129D,
            0x1129F, 0x112A9,
            //  #186 (16075+3): sc=Myanmar:Mymr
            0x1000, 0x109F, 0xA9E0, 0xA9FE, 0xAA60, 0xAA7F,
            //  #187 (16078+2): sc=Nabataean:Nbat scx=Nabataean:Nbat
            0x10880, 0x1089E, 0x108A7, 0x108AF,
            //  #188 (16080+3): sc=Nandinagari:Nand
            0x119A0, 0x119A7, 0x119AA, 0x119D7, 0x119DA, 0x119E4,
            //  #189 (16083+4): sc=New_Tai_Lue:Talu scx=New_Tai_Lue:Talu
            0x1980, 0x19AB, 0x19B0, 0x19C9, 0x19D0, 0x19DA, 0x19DE, 0x19DF,
            //  #190 (16087+2): sc=Newa:Newa scx=Newa:Newa
            0x11400, 0x1145B, 0x1145D, 0x11461,
            //  #191 (16089+2): sc=Nko:Nkoo
            0x07C0, 0x07FA, 0x07FD, 0x07FF,
            //  #192 (16091+2): sc=Nushu:Nshu scx=Nushu:Nshu
            0x16FE1, 0x16FE1, 0x1B170, 0x1B2FB,
            //  #193 (16093+4): sc=Nyiakeng_Puachue_Hmong:Hmnp scx=Nyiakeng_Puachue_Hmong:Hmnp
            0x1E100, 0x1E12C, 0x1E130, 0x1E13D, 0x1E140, 0x1E149, 0x1E14E, 0x1E14F,
            //  #194 (16097+1): sc=Ogham:Ogam scx=Ogham:Ogam
            0x1680, 0x169C,
            //  #195 (16098+1): sc=Ol_Chiki:Olck scx=Ol_Chiki:Olck
            0x1C50, 0x1C7F,
            //  #196 (16099+3): sc=Old_Hungarian:Hung scx=Old_Hungarian:Hung
            0x10C80, 0x10CB2, 0x10CC0, 0x10CF2, 0x10CFA, 0x10CFF,
            //  #197 (16102+2): sc=Old_Italic:Ital scx=Old_Italic:Ital
            0x10300, 0x10323, 0x1032D, 0x1032F,
            //  #198 (16104+1): sc=Old_North_Arabian:Narb scx=Old_North_Arabian:Narb
            0x10A80, 0x10A9F,
            //  #199 (16105+1): sc=Old_Permic:Perm
            0x10350, 0x1037A,
            //  #200 (16106+2): sc=Old_Persian:Xpeo scx=Old_Persian:Xpeo
            0x103A0, 0x103C3, 0x103C8, 0x103D5,
            //  #201 (16108+1): sc=Old_Sogdian:Sogo scx=Old_Sogdian:Sogo
            0x10F00, 0x10F27,
            //  #202 (16109+1): sc=Old_South_Arabian:Sarb scx=Old_South_Arabian:Sarb
            0x10A60, 0x10A7F,
            //  #203 (16110+1): sc=Old_Turkic:Orkh scx=Old_Turkic:Orkh
            0x10C00, 0x10C48,
            //  #204 (16111+1): sc=Old_Uyghur:Ougr
            0x10F70, 0x10F89,
            //  #205 (16112+14): sc=Oriya:Orya
            0x0B01, 0x0B03, 0x0B05, 0x0B0C, 0x0B0F, 0x0B10, 0x0B13, 0x0B28,
            0x0B2A, 0x0B30, 0x0B32, 0x0B33, 0x0B35, 0x0B39, 0x0B3C, 0x0B44,
            0x0B47, 0x0B48, 0x0B4B, 0x0B4D, 0x0B55, 0x0B57, 0x0B5C, 0x0B5D,
            0x0B5F, 0x0B63, 0x0B66, 0x0B77,
            //  #206 (16126+2): sc=Osage:Osge scx=Osage:Osge
            0x104B0, 0x104D3, 0x104D8, 0x104FB,
            //  #207 (16128+2): sc=Osmanya:Osma scx=Osmanya:Osma
            0x10480, 0x1049D, 0x104A0, 0x104A9,
            //  #208 (16130+5): sc=Pahawh_Hmong:Hmng scx=Pahawh_Hmong:Hmng
            0x16B00, 0x16B45, 0x16B50, 0x16B59, 0x16B5B, 0x16B61, 0x16B63, 0x16B77,
            0x16B7D, 0x16B8F,
            //  #209 (16135+1): sc=Palmyrene:Palm scx=Palmyrene:Palm
            0x10860, 0x1087F,
            //  #210 (16136+1): sc=Pau_Cin_Hau:Pauc scx=Pau_Cin_Hau:Pauc
            0x11AC0, 0x11AF8,
            //  #211 (16137+1): sc=Phags_Pa:Phag
            0xA840, 0xA877,
            //  #212 (16138+2): sc=Phoenician:Phnx scx=Phoenician:Phnx
            0x10900, 0x1091B, 0x1091F, 0x1091F,
            //  #213 (16140+3): sc=Psalter_Pahlavi:Phlp
            0x10B80, 0x10B91, 0x10B99, 0x10B9C, 0x10BA9, 0x10BAF,
            //  #214 (16143+2): sc=Rejang:Rjng scx=Rejang:Rjng
            0xA930, 0xA953, 0xA95F, 0xA95F,
            //  #215 (16145+2): sc=Runic:Runr scx=Runic:Runr
            0x16A0, 0x16EA, 0x16EE, 0x16F8,
            //  #216 (16147+2): sc=Samaritan:Samr scx=Samaritan:Samr
            0x0800, 0x082D, 0x0830, 0x083E,
            //  #217 (16149+2): sc=Saurashtra:Saur scx=Saurashtra:Saur
            0xA880, 0xA8C5, 0xA8CE, 0xA8D9,
            //  #218 (16151+1): sc=Sharada:Shrd
            0x11180, 0x111DF,
            //  #219 (16152+1): sc=Shavian:Shaw scx=Shavian:Shaw
            0x10450, 0x1047F,
            //  #220 (16153+2): sc=Siddham:Sidd scx=Siddham:Sidd
            0x11580, 0x115B5, 0x115B8, 0x115DD,
            //  #221 (16155+3): sc=SignWriting:Sgnw scx=SignWriting:Sgnw
            0x1D800, 0x1DA8B, 0x1DA9B, 0x1DA9F, 0x1DAA1, 0x1DAAF,
            //  #222 (16158+13): sc=Sinhala:Sinh
            0x0D81, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1, 0x0DB3, 0x0DBB,
            0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DCA, 0x0DCA, 0x0DCF, 0x0DD4,
            0x0DD6, 0x0DD6, 0x0DD8, 0x0DDF, 0x0DE6, 0x0DEF, 0x0DF2, 0x0DF4,
            0x111E1, 0x111F4,
            //  #223 (16171+1): sc=Sogdian:Sogd
            0x10F30, 0x10F59,
            //  #224 (16172+2): sc=Sora_Sompeng:Sora scx=Sora_Sompeng:Sora
            0x110D0, 0x110E8, 0x110F0, 0x110F9,
            //  #225 (16174+1): sc=Soyombo:Soyo scx=Soyombo:Soyo
            0x11A50, 0x11AA2,
            //  #226 (16175+2): sc=Sundanese:Sund scx=Sundanese:Sund
            0x1B80, 0x1BBF, 0x1CC0, 0x1CC7,
            //  #227 (16177+1): sc=Syloti_Nagri:Sylo
            0xA800, 0xA82C,
            //  #228 (16178+4): sc=Syriac:Syrc
            0x0700, 0x070D, 0x070F, 0x074A, 0x074D, 0x074F, 0x0860, 0x086A,
            //  #229 (16182+2): sc=Tagalog:Tglg
            0x1700, 0x1715, 0x171F, 0x171F,
            //  #230 (16184+3): sc=Tagbanwa:Tagb
            0x1760, 0x176C, 0x176E, 0x1770, 0x1772, 0x1773,
            //  #231 (16187+2): sc=Tai_Le:Tale
            0x1950, 0x196D, 0x1970, 0x1974,
            //  #232 (16189+5): sc=Tai_Tham:Lana scx=Tai_Tham:Lana
            0x1A20, 0x1A5E, 0x1A60, 0x1A7C, 0x1A7F, 0x1A89, 0x1A90, 0x1A99,
            0x1AA0, 0x1AAD,
            //  #233 (16194+2): sc=Tai_Viet:Tavt scx=Tai_Viet:Tavt
            0xAA80, 0xAAC2, 0xAADB, 0xAADF,
            //  #234 (16196+2): sc=Takri:Takr
            0x11680, 0x116B9, 0x116C0, 0x116C9,
            //  #235 (16198+18): sc=Tamil:Taml
            0x0B82, 0x0B83, 0x0B85, 0x0B8A, 0x0B8E, 0x0B90, 0x0B92, 0x0B95,
            0x0B99, 0x0B9A, 0x0B9C, 0x0B9C, 0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4,
            0x0BA8, 0x0BAA, 0x0BAE, 0x0BB9, 0x0BBE, 0x0BC2, 0x0BC6, 0x0BC8,
            0x0BCA, 0x0BCD, 0x0BD0, 0x0BD0, 0x0BD7, 0x0BD7, 0x0BE6, 0x0BFA,
            0x11FC0, 0x11FF1, 0x11FFF, 0x11FFF,
            //  #236 (16216+2): sc=Tangsa:Tnsa scx=Tangsa:Tnsa
            0x16A70, 0x16ABE, 0x16AC0, 0x16AC9,
            //  #237 (16218+4): sc=Tangut:Tang scx=Tangut:Tang
            0x16FE0, 0x16FE0, 0x17000, 0x187F7, 0x18800, 0x18AFF, 0x18D00, 0x18D08,
            //  #238 (16222+13): sc=Telugu:Telu
            0x0C00, 0x0C0C, 0x0C0E, 0x0C10, 0x0C12, 0x0C28, 0x0C2A, 0x0C39,
            0x0C3C, 0x0C44, 0x0C46, 0x0C48, 0x0C4A, 0x0C4D, 0x0C55, 0x0C56,
            0x0C58, 0x0C5A, 0x0C5D, 0x0C5D, 0x0C60, 0x0C63, 0x0C66, 0x0C6F,
            0x0C77, 0x0C7F,
            //  #239 (16235+1): sc=Thaana:Thaa
            0x0780, 0x07B1,
            //  #240 (16236+2): sc=Thai:Thai scx=Thai:Thai
            0x0E01, 0x0E3A, 0x0E40, 0x0E5B,
            //  #241 (16238+7): sc=Tibetan:Tibt scx=Tibetan:Tibt
            0x0F00, 0x0F47, 0x0F49, 0x0F6C, 0x0F71, 0x0F97, 0x0F99, 0x0FBC,
            0x0FBE, 0x0FCC, 0x0FCE, 0x0FD4, 0x0FD9, 0x0FDA,
            //  #242 (16245+3): sc=Tifinagh:Tfng scx=Tifinagh:Tfng
            0x2D30, 0x2D67, 0x2D6F, 0x2D70, 0x2D7F, 0x2D7F,
            //  #243 (16248+2): sc=Tirhuta:Tirh
            0x11480, 0x114C7, 0x114D0, 0x114D9,
            //  #244 (16250+1): sc=Toto scx=Toto
            0x1E290, 0x1E2AE,
            //  #245 (16251+2): sc=Ugaritic:Ugar scx=Ugaritic:Ugar
            0x10380, 0x1039D, 0x1039F, 0x1039F,
            //  #246 (16253+1): sc=Vai:Vaii scx=Vai:Vaii
            0xA500, 0xA62B,
            //  #247 (16254+8): sc=Vithkuqi:Vith scx=Vithkuqi:Vith
            0x10570, 0x1057A, 0x1057C, 0x1058A, 0x1058C, 0x10592, 0x10594, 0x10595,
            0x10597, 0x105A1, 0x105A3, 0x105B1, 0x105B3, 0x105B9, 0x105BB, 0x105BC,
            //  #248 (16262+2): sc=Wancho:Wcho scx=Wancho:Wcho
            0x1E2C0, 0x1E2F9, 0x1E2FF, 0x1E2FF,
            //  #249 (16264+2): sc=Warang_Citi:Wara scx=Warang_Citi:Wara
            0x118A0, 0x118F2, 0x118FF, 0x118FF,
            //  #250 (16266+3): sc=Yezidi:Yezi
            0x10E80, 0x10EA9, 0x10EAB, 0x10EAD, 0x10EB0, 0x10EB1,
            //  #251 (16269+2): sc=Yi:Yiii
            0xA000, 0xA48C, 0xA490, 0xA4C6,
            //  #252 (16271+1): sc=Zanabazar_Square:Zanb scx=Zanabazar_Square:Zanb
            0x11A00, 0x11A47,
            //  #253 (16272+5): scx=Adlam:Adlm
            0x061F, 0x061F, 0x0640, 0x0640, 0x1E900, 0x1E94B, 0x1E950, 0x1E959,
            0x1E95E, 0x1E95F,
            //  #254 (16277+51): scx=Arabic:Arab
            0x0600, 0x0604, 0x0606, 0x06DC, 0x06DE, 0x06FF, 0x0750, 0x077F,
            0x0870, 0x088E, 0x0890, 0x0891, 0x0898, 0x08E1, 0x08E3, 0x08FF,
            0xFB50, 0xFBC2, 0xFBD3, 0xFD8F, 0xFD92, 0xFDC7, 0xFDCF, 0xFDCF,
            0xFDF0, 0xFDFF, 0xFE70, 0xFE74, 0xFE76, 0xFEFC, 0x102E0, 0x102FB,
            0x10E60, 0x10E7E, 0x1EE00, 0x1EE03, 0x1EE05, 0x1EE1F, 0x1EE21, 0x1EE22,
            0x1EE24, 0x1EE24, 0x1EE27, 0x1EE27, 0x1EE29, 0x1EE32, 0x1EE34, 0x1EE37,
            0x1EE39, 0x1EE39, 0x1EE3B, 0x1EE3B, 0x1EE42, 0x1EE42, 0x1EE47, 0x1EE47,
            0x1EE49, 0x1EE49, 0x1EE4B, 0x1EE4B, 0x1EE4D, 0x1EE4F, 0x1EE51, 0x1EE52,
            0x1EE54, 0x1EE54, 0x1EE57, 0x1EE57, 0x1EE59, 0x1EE59, 0x1EE5B, 0x1EE5B,
            0x1EE5D, 0x1EE5D, 0x1EE5F, 0x1EE5F, 0x1EE61, 0x1EE62, 0x1EE64, 0x1EE64,
            0x1EE67, 0x1EE6A, 0x1EE6C, 0x1EE72, 0x1EE74, 0x1EE77, 0x1EE79, 0x1EE7C,
            0x1EE7E, 0x1EE7E, 0x1EE80, 0x1EE89, 0x1EE8B, 0x1EE9B, 0x1EEA1, 0x1EEA3,
            0x1EEA5, 0x1EEA9, 0x1EEAB, 0x1EEBB, 0x1EEF0, 0x1EEF1,
            //  #255 (16328+26): scx=Bengali:Beng
            0x0951, 0x0952, 0x0964, 0x0965, 0x0980, 0x0983, 0x0985, 0x098C,
            0x098F, 0x0990, 0x0993, 0x09A8, 0x09AA, 0x09B0, 0x09B2, 0x09B2,
            0x09B6, 0x09B9, 0x09BC, 0x09C4, 0x09C7, 0x09C8, 0x09CB, 0x09CE,
            0x09D7, 0x09D7, 0x09DC, 0x09DD, 0x09DF, 0x09E3, 0x09E6, 0x09FE,
            0x1CD0, 0x1CD0, 0x1CD2, 0x1CD2, 0x1CD5, 0x1CD6, 0x1CD8, 0x1CD8,
            0x1CE1, 0x1CE1, 0x1CEA, 0x1CEA, 0x1CED, 0x1CED, 0x1CF2, 0x1CF2,
            0x1CF5, 0x1CF7, 0xA8F1, 0xA8F1,
            //  #256 (16354+12): scx=Bopomofo:Bopo
            0x02EA, 0x02EB, 0x3001, 0x3003, 0x3008, 0x3011, 0x3013, 0x301F,
            0x302A, 0x302D, 0x3030, 0x3030, 0x3037, 0x3037, 0x30FB, 0x30FB,
            0x3105, 0x312F, 0x31A0, 0x31BF, 0xFE45, 0xFE46, 0xFF61, 0xFF65,
            //  #257 (16366+3): scx=Buginese:Bugi
            0x1A00, 0x1A1B, 0x1A1E, 0x1A1F, 0xA9CF, 0xA9CF,
            //  #258 (16369+2): scx=Buhid:Buhd
            0x1735, 0x1736, 0x1740, 0x1753,
            //  #259 (16371+4): scx=Chakma:Cakm
            0x09E6, 0x09EF, 0x1040, 0x1049, 0x11100, 0x11134, 0x11136, 0x11147,
            //  #260 (16375+148): scx=Common:Zyyy
            0x0000, 0x0040, 0x005B, 0x0060, 0x007B, 0x00A9, 0x00AB, 0x00B9,
            0x00BB, 0x00BF, 0x00D7, 0x00D7, 0x00F7, 0x00F7, 0x02B9, 0x02DF,
            0x02E5, 0x02E9, 0x02EC, 0x02FF, 0x0374, 0x0374, 0x037E, 0x037E,
            0x0385, 0x0385, 0x0387, 0x0387, 0x0605, 0x0605, 0x06DD, 0x06DD,
            0x08E2, 0x08E2, 0x0E3F, 0x0E3F, 0x0FD5, 0x0FD8, 0x16EB, 0x16ED,
            0x2000, 0x200B, 0x200E, 0x202E, 0x2030, 0x2064, 0x2066, 0x2070,
            0x2074, 0x207E, 0x2080, 0x208E, 0x20A0, 0x20C0, 0x2100, 0x2125,
            0x2127, 0x2129, 0x212C, 0x2131, 0x2133, 0x214D, 0x214F, 0x215F,
            0x2189, 0x218B, 0x2190, 0x2426, 0x2440, 0x244A, 0x2460, 0x27FF,
            0x2900, 0x2B73, 0x2B76, 0x2B95, 0x2B97, 0x2BFF, 0x2E00, 0x2E42,
            0x2E44, 0x2E5D, 0x2FF0, 0x2FFB, 0x3000, 0x3000, 0x3004, 0x3004,
            0x3012, 0x3012, 0x3020, 0x3020, 0x3036, 0x3036, 0x3248, 0x325F,
            0x327F, 0x327F, 0x32B1, 0x32BF, 0x32CC, 0x32CF, 0x3371, 0x337A,
            0x3380, 0x33DF, 0x33FF, 0x33FF, 0x4DC0, 0x4DFF, 0xA708, 0xA721,
            0xA788, 0xA78A, 0xAB5B, 0xAB5B, 0xAB6A, 0xAB6B, 0xFE10, 0xFE19,
            0xFE30, 0xFE44, 0xFE47, 0xFE52, 0xFE54, 0xFE66, 0xFE68, 0xFE6B,
            0xFEFF, 0xFEFF, 0xFF01, 0xFF20, 0xFF3B, 0xFF40, 0xFF5B, 0xFF60,
            0xFFE0, 0xFFE6, 0xFFE8, 0xFFEE, 0xFFF9, 0xFFFD, 0x10190, 0x1019C,
            0x101D0, 0x101FC, 0x1CF50, 0x1CFC3, 0x1D000, 0x1D0F5, 0x1D100, 0x1D126,
            0x1D129, 0x1D166, 0x1D16A, 0x1D17A, 0x1D183, 0x1D184, 0x1D18C, 0x1D1A9,
            0x1D1AE, 0x1D1EA, 0x1D2E0, 0x1D2F3, 0x1D300, 0x1D356, 0x1D372, 0x1D378,
            0x1D400, 0x1D454, 0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2,
            0x1D4A5, 0x1D4A6, 0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB,
            0x1D4BD, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514,
            0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544,
            0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A5, 0x1D6A8, 0x1D7CB,
            0x1D7CE, 0x1D7FF, 0x1EC71, 0x1ECB4, 0x1ED01, 0x1ED3D, 0x1F000, 0x1F02B,
            0x1F030, 0x1F093, 0x1F0A0, 0x1F0AE, 0x1F0B1, 0x1F0BF, 0x1F0C1, 0x1F0CF,
            0x1F0D1, 0x1F0F5, 0x1F100, 0x1F1AD, 0x1F1E6, 0x1F1FF, 0x1F201, 0x1F202,
            0x1F210, 0x1F23B, 0x1F240, 0x1F248, 0x1F260, 0x1F265, 0x1F300, 0x1F6D7,
            0x1F6DD, 0x1F6EC, 0x1F6F0, 0x1F6FC, 0x1F700, 0x1F773, 0x1F780, 0x1F7D8,
            0x1F7E0, 0x1F7EB, 0x1F7F0, 0x1F7F0, 0x1F800, 0x1F80B, 0x1F810, 0x1F847,
            0x1F850, 0x1F859, 0x1F860, 0x1F887, 0x1F890, 0x1F8AD, 0x1F8B0, 0x1F8B1,
            0x1F900, 0x1FA53, 0x1FA60, 0x1FA6D, 0x1FA70, 0x1FA74, 0x1FA78, 0x1FA7C,
            0x1FA80, 0x1FA86, 0x1FA90, 0x1FAAC, 0x1FAB0, 0x1FABA, 0x1FAC0, 0x1FAC5,
            0x1FAD0, 0x1FAD9, 0x1FAE0, 0x1FAE7, 0x1FAF0, 0x1FAF6, 0x1FB00, 0x1FB92,
            0x1FB94, 0x1FBCA, 0x1FBF0, 0x1FBF9, 0xE0001, 0xE0001, 0xE0020, 0xE007F,
            //  #261 (16523+4): scx=Coptic:Copt:Qaac
            0x03E2, 0x03EF, 0x2C80, 0x2CF3, 0x2CF9, 0x2CFF, 0x102E0, 0x102FB,
            //  #262 (16527+2): scx=Cypro_Minoan:Cpmn
            0x10100, 0x10101, 0x12F90, 0x12FF2,
            //  #263 (16529+9): scx=Cypriot:Cprt
            0x10100, 0x10102, 0x10107, 0x10133, 0x10137, 0x1013F, 0x10800, 0x10805,
            0x10808, 0x10808, 0x1080A, 0x10835, 0x10837, 0x10838, 0x1083C, 0x1083C,
            0x1083F, 0x1083F,
            //  #264 (16538+9): scx=Cyrillic:Cyrl
            0x0400, 0x052F, 0x1C80, 0x1C88, 0x1D2B, 0x1D2B, 0x1D78, 0x1D78,
            0x1DF8, 0x1DF8, 0x2DE0, 0x2DFF, 0x2E43, 0x2E43, 0xA640, 0xA69F,
            0xFE2E, 0xFE2F,
            //  #265 (16547+7): scx=Devanagari:Deva
            0x0900, 0x0952, 0x0955, 0x097F, 0x1CD0, 0x1CF6, 0x1CF8, 0x1CF9,
            0x20F0, 0x20F0, 0xA830, 0xA839, 0xA8E0, 0xA8FF,
            //  #266 (16554+3): scx=Dogra:Dogr
            0x0964, 0x096F, 0xA830, 0xA839, 0x11800, 0x1183B,
            //  #267 (16557+5): scx=Duployan:Dupl
            0x1BC00, 0x1BC6A, 0x1BC70, 0x1BC7C, 0x1BC80, 0x1BC88, 0x1BC90, 0x1BC99,
            0x1BC9C, 0x1BCA3,
            //  #268 (16562+9): scx=Georgian:Geor
            0x10A0, 0x10C5, 0x10C7, 0x10C7, 0x10CD, 0x10CD, 0x10D0, 0x10FF,
            0x1C90, 0x1CBA, 0x1CBD, 0x1CBF, 0x2D00, 0x2D25, 0x2D27, 0x2D27,
            0x2D2D, 0x2D2D,
            //  #269 (16571+10): scx=Glagolitic:Glag
            0x0484, 0x0484, 0x0487, 0x0487, 0x2C00, 0x2C5F, 0x2E43, 0x2E43,
            0xA66F, 0xA66F, 0x1E000, 0x1E006, 0x1E008, 0x1E018, 0x1E01B, 0x1E021,
            0x1E023, 0x1E024, 0x1E026, 0x1E02A,
            //  #270 (16581+25): scx=Grantha:Gran
            0x0951, 0x0952, 0x0964, 0x0965, 0x0BE6, 0x0BF3, 0x1CD0, 0x1CD0,
            0x1CD2, 0x1CD3, 0x1CF2, 0x1CF4, 0x1CF8, 0x1CF9, 0x20F0, 0x20F0,
            0x11300, 0x11303, 0x11305, 0x1130C, 0x1130F, 0x11310, 0x11313, 0x11328,
            0x1132A, 0x11330, 0x11332, 0x11333, 0x11335, 0x11339, 0x1133B, 0x11344,
            0x11347, 0x11348, 0x1134B, 0x1134D, 0x11350, 0x11350, 0x11357, 0x11357,
            0x1135D, 0x11363, 0x11366, 0x1136C, 0x11370, 0x11374, 0x11FD0, 0x11FD1,
            0x11FD3, 0x11FD3,
            //  #271 (16606+38): scx=Greek:Grek
            0x0342, 0x0342, 0x0345, 0x0345, 0x0370, 0x0373, 0x0375, 0x0377,
            0x037A, 0x037D, 0x037F, 0x037F, 0x0384, 0x0384, 0x0386, 0x0386,
            0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x03A1, 0x03A3, 0x03E1,
            0x03F0, 0x03FF, 0x1D26, 0x1D2A, 0x1D5D, 0x1D61, 0x1D66, 0x1D6A,
            0x1DBF, 0x1DC1, 0x1F00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45,
            0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B,
            0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FC4,
            0x1FC6, 0x1FD3, 0x1FD6, 0x1FDB, 0x1FDD, 0x1FEF, 0x1FF2, 0x1FF4,
            0x1FF6, 0x1FFE, 0x2126, 0x2126, 0xAB65, 0xAB65, 0x10140, 0x1018E,
            0x101A0, 0x101A0, 0x1D200, 0x1D245,
            //  #272 (16644+17): scx=Gujarati:Gujr
            0x0951, 0x0952, 0x0964, 0x0965, 0x0A81, 0x0A83, 0x0A85, 0x0A8D,
            0x0A8F, 0x0A91, 0x0A93, 0x0AA8, 0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3,
            0x0AB5, 0x0AB9, 0x0ABC, 0x0AC5, 0x0AC7, 0x0AC9, 0x0ACB, 0x0ACD,
            0x0AD0, 0x0AD0, 0x0AE0, 0x0AE3, 0x0AE6, 0x0AF1, 0x0AF9, 0x0AFF,
            0xA830, 0xA839,
            //  #273 (16661+7): scx=Gunjala_Gondi:Gong
            0x0964, 0x0965, 0x11D60, 0x11D65, 0x11D67, 0x11D68, 0x11D6A, 0x11D8E,
            0x11D90, 0x11D91, 0x11D93, 0x11D98, 0x11DA0, 0x11DA9,
            //  #274 (16668+19): scx=Gurmukhi:Guru
            0x0951, 0x0952, 0x0964, 0x0965, 0x0A01, 0x0A03, 0x0A05, 0x0A0A,
            0x0A0F, 0x0A10, 0x0A13, 0x0A28, 0x0A2A, 0x0A30, 0x0A32, 0x0A33,
            0x0A35, 0x0A36, 0x0A38, 0x0A39, 0x0A3C, 0x0A3C, 0x0A3E, 0x0A42,
            0x0A47, 0x0A48, 0x0A4B, 0x0A4D, 0x0A51, 0x0A51, 0x0A59, 0x0A5C,
            0x0A5E, 0x0A5E, 0x0A66, 0x0A76, 0xA830, 0xA839,
            //  #275 (16687+37): scx=Han:Hani
            0x2E80, 0x2E99, 0x2E9B, 0x2EF3, 0x2F00, 0x2FD5, 0x3001, 0x3003,
            0x3005, 0x3011, 0x3013, 0x301F, 0x3021, 0x302D, 0x3030, 0x3030,
            0x3037, 0x303F, 0x30FB, 0x30FB, 0x3190, 0x319F, 0x31C0, 0x31E3,
            0x3220, 0x3247, 0x3280, 0x32B0, 0x32C0, 0x32CB, 0x32FF, 0x32FF,
            0x3358, 0x3370, 0x337B, 0x337F, 0x33E0, 0x33FE, 0x3400, 0x4DBF,
            0x4E00, 0x9FFF, 0xA700, 0xA707, 0xF900, 0xFA6D, 0xFA70, 0xFAD9,
            0xFE45, 0xFE46, 0xFF61, 0xFF65, 0x16FE2, 0x16FE3, 0x16FF0, 0x16FF1,
            0x1D360, 0x1D371, 0x1F250, 0x1F251, 0x20000, 0x2A6DF, 0x2A700, 0x2B738,
            0x2B740, 0x2B81D, 0x2B820, 0x2CEA1, 0x2CEB0, 0x2EBE0, 0x2F800, 0x2FA1D,
            0x30000, 0x3134A,
            //  #276 (16724+21): scx=Hangul:Hang
            0x1100, 0x11FF, 0x3001, 0x3003, 0x3008, 0x3011, 0x3013, 0x301F,
            0x302E, 0x3030, 0x3037, 0x3037, 0x30FB, 0x30FB, 0x3131, 0x318E,
            0x3200, 0x321E, 0x3260, 0x327E, 0xA960, 0xA97C, 0xAC00, 0xD7A3,
            0xD7B0, 0xD7C6, 0xD7CB, 0xD7FB, 0xFE45, 0xFE46, 0xFF61, 0xFF65,
            0xFFA0, 0xFFBE, 0xFFC2, 0xFFC7, 0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7,
            0xFFDA, 0xFFDC,
            //  #277 (16745+7): scx=Hanifi_Rohingya:Rohg
            0x060C, 0x060C, 0x061B, 0x061B, 0x061F, 0x061F, 0x0640, 0x0640,
            0x06D4, 0x06D4, 0x10D00, 0x10D27, 0x10D30, 0x10D39,
            //  #278 (16752+1): scx=Hanunoo:Hano
            0x1720, 0x1736,
            //  #279 (16753+16): scx=Hiragana:Hira
            0x3001, 0x3003, 0x3008, 0x3011, 0x3013, 0x301F, 0x3030, 0x3035,
            0x3037, 0x3037, 0x303C, 0x303D, 0x3041, 0x3096, 0x3099, 0x30A0,
            0x30FB, 0x30FC, 0xFE45, 0xFE46, 0xFF61, 0xFF65, 0xFF70, 0xFF70,
            0xFF9E, 0xFF9F, 0x1B001, 0x1B11F, 0x1B150, 0x1B152, 0x1F200, 0x1F200,
            //  #280 (16769+20): scx=Inherited:Zinh:Qaai
            0x0300, 0x0341, 0x0343, 0x0344, 0x0346, 0x0362, 0x0953, 0x0954,
            0x1AB0, 0x1ACE, 0x1DC2, 0x1DF7, 0x1DF9, 0x1DF9, 0x1DFB, 0x1DFF,
            0x200C, 0x200D, 0x20D0, 0x20EF, 0xFE00, 0xFE0F, 0xFE20, 0xFE2D,
            0x101FD, 0x101FD, 0x1CF00, 0x1CF2D, 0x1CF30, 0x1CF46, 0x1D167, 0x1D169,
            0x1D17B, 0x1D182, 0x1D185, 0x1D18B, 0x1D1AA, 0x1D1AD, 0xE0100, 0xE01EF,
            //  #281 (16789+3): scx=Javanese:Java
            0xA980, 0xA9CD, 0xA9CF, 0xA9D9, 0xA9DE, 0xA9DF,
            //  #282 (16792+4): scx=Kaithi:Kthi
            0x0966, 0x096F, 0xA830, 0xA839, 0x11080, 0x110C2, 0x110CD, 0x110CD,
            //  #283 (16796+21): scx=Kannada:Knda
            0x0951, 0x0952, 0x0964, 0x0965, 0x0C80, 0x0C8C, 0x0C8E, 0x0C90,
            0x0C92, 0x0CA8, 0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9, 0x0CBC, 0x0CC4,
            0x0CC6, 0x0CC8, 0x0CCA, 0x0CCD, 0x0CD5, 0x0CD6, 0x0CDD, 0x0CDE,
            0x0CE0, 0x0CE3, 0x0CE6, 0x0CEF, 0x0CF1, 0x0CF2, 0x1CD0, 0x1CD0,
            0x1CD2, 0x1CD2, 0x1CDA, 0x1CDA, 0x1CF2, 0x1CF2, 0x1CF4, 0x1CF4,
            0xA830, 0xA835,
            //  #284 (16817+19): scx=Katakana:Kana
            0x3001, 0x3003, 0x3008, 0x3011, 0x3013, 0x301F, 0x3030, 0x3035,
            0x3037, 0x3037, 0x303C, 0x303D, 0x3099, 0x309C, 0x30A0, 0x30FF,
            0x31F0, 0x31FF, 0x32D0, 0x32FE, 0x3300, 0x3357, 0xFE45, 0xFE46,
            0xFF61, 0xFF9F, 0x1AFF0, 0x1AFF3, 0x1AFF5, 0x1AFFB, 0x1AFFD, 0x1AFFE,
            0x1B000, 0x1B000, 0x1B120, 0x1B122, 0x1B164, 0x1B167,
            //  #285 (16836+1): scx=Kayah_Li:Kali
            0xA900, 0xA92F,
            //  #286 (16837+4): scx=Khojki:Khoj
            0x0AE6, 0x0AEF, 0xA830, 0xA839, 0x11200, 0x11211, 0x11213, 0x1123E,
            //  #287 (16841+4): scx=Khudawadi:Sind
            0x0964, 0x0965, 0xA830, 0xA839, 0x112B0, 0x112EA, 0x112F0, 0x112F9,
            //  #288 (16845+46): scx=Latin:Latn
            0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00BA, 0x00BA,
            0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x02B8, 0x02E0, 0x02E4,
            0x0363, 0x036F, 0x0485, 0x0486, 0x0951, 0x0952, 0x10FB, 0x10FB,
            0x1D00, 0x1D25, 0x1D2C, 0x1D5C, 0x1D62, 0x1D65, 0x1D6B, 0x1D77,
            0x1D79, 0x1DBE, 0x1E00, 0x1EFF, 0x202F, 0x202F, 0x2071, 0x2071,
            0x207F, 0x207F, 0x2090, 0x209C, 0x20F0, 0x20F0, 0x212A, 0x212B,
            0x2132, 0x2132, 0x214E, 0x214E, 0x2160, 0x2188, 0x2C60, 0x2C7F,
            0xA700, 0xA707, 0xA722, 0xA787, 0xA78B, 0xA7CA, 0xA7D0, 0xA7D1,
            0xA7D3, 0xA7D3, 0xA7D5, 0xA7D9, 0xA7F2, 0xA7FF, 0xA92E, 0xA92E,
            0xAB30, 0xAB5A, 0xAB5C, 0xAB64, 0xAB66, 0xAB69, 0xFB00, 0xFB06,
            0xFF21, 0xFF3A, 0xFF41, 0xFF5A, 0x10780, 0x10785, 0x10787, 0x107B0,
            0x107B2, 0x107BA, 0x1DF00, 0x1DF1E,
            //  #289 (16891+6): scx=Limbu:Limb
            0x0965, 0x0965, 0x1900, 0x191E, 0x1920, 0x192B, 0x1930, 0x193B,
            0x1940, 0x1940, 0x1944, 0x194F,
            //  #290 (16897+4): scx=Linear_A:Lina
            0x10107, 0x10133, 0x10600, 0x10736, 0x10740, 0x10755, 0x10760, 0x10767,
            //  #291 (16901+10): scx=Linear_B:Linb
            0x10000, 0x1000B, 0x1000D, 0x10026, 0x10028, 0x1003A, 0x1003C, 0x1003D,
            0x1003F, 0x1004D, 0x10050, 0x1005D, 0x10080, 0x100FA, 0x10100, 0x10102,
            0x10107, 0x10133, 0x10137, 0x1013F,
            //  #292 (16911+3): scx=Mahajani:Mahj
            0x0964, 0x096F, 0xA830, 0xA839, 0x11150, 0x11176,
            //  #293 (16914+11): scx=Malayalam:Mlym
            0x0951, 0x0952, 0x0964, 0x0965, 0x0D00, 0x0D0C, 0x0D0E, 0x0D10,
            0x0D12, 0x0D44, 0x0D46, 0x0D48, 0x0D4A, 0x0D4F, 0x0D54, 0x0D63,
            0x0D66, 0x0D7F, 0x1CDA, 0x1CDA, 0xA830, 0xA832,
            //  #294 (16925+3): scx=Mandaic:Mand
            0x0640, 0x0640, 0x0840, 0x085B, 0x085E, 0x085E,
            //  #295 (16928+3): scx=Manichaean:Mani
            0x0640, 0x0640, 0x10AC0, 0x10AE6, 0x10AEB, 0x10AF6,
            //  #296 (16931+8): scx=Masaram_Gondi:Gonm
            0x0964, 0x0965, 0x11D00, 0x11D06, 0x11D08, 0x11D09, 0x11D0B, 0x11D36,
            0x11D3A, 0x11D3A, 0x11D3C, 0x11D3D, 0x11D3F, 0x11D47, 0x11D50, 0x11D59,
            //  #297 (16939+3): scx=Modi:Modi
            0xA830, 0xA839, 0x11600, 0x11644, 0x11650, 0x11659,
            //  #298 (16942+5): scx=Mongolian:Mong
            0x1800, 0x1819, 0x1820, 0x1878, 0x1880, 0x18AA, 0x202F, 0x202F,
            0x11660, 0x1166C,
            //  #299 (16947+6): scx=Multani:Mult
            0x0A66, 0x0A6F, 0x11280, 0x11286, 0x11288, 0x11288, 0x1128A, 0x1128D,
            0x1128F, 0x1129D, 0x1129F, 0x112A9,
            //  #300 (16953+4): scx=Myanmar:Mymr
            0x1000, 0x109F, 0xA92E, 0xA92E, 0xA9E0, 0xA9FE, 0xAA60, 0xAA7F,
            //  #301 (16957+9): scx=Nandinagari:Nand
            0x0964, 0x0965, 0x0CE6, 0x0CEF, 0x1CE9, 0x1CE9, 0x1CF2, 0x1CF2,
            0x1CFA, 0x1CFA, 0xA830, 0xA835, 0x119A0, 0x119A7, 0x119AA, 0x119D7,
            0x119DA, 0x119E4,
            //  #302 (16966+6): scx=Nko:Nkoo
            0x060C, 0x060C, 0x061B, 0x061B, 0x061F, 0x061F, 0x07C0, 0x07FA,
            0x07FD, 0x07FF, 0xFD3E, 0xFD3F,
            //  #303 (16972+2): scx=Old_Permic:Perm
            0x0483, 0x0483, 0x10350, 0x1037A,
            //  #304 (16974+3): scx=Old_Uyghur:Ougr
            0x0640, 0x0640, 0x10AF2, 0x10AF2, 0x10F70, 0x10F89,
            //  #305 (16977+18): scx=Oriya:Orya
            0x0951, 0x0952, 0x0964, 0x0965, 0x0B01, 0x0B03, 0x0B05, 0x0B0C,
            0x0B0F, 0x0B10, 0x0B13, 0x0B28, 0x0B2A, 0x0B30, 0x0B32, 0x0B33,
            0x0B35, 0x0B39, 0x0B3C, 0x0B44, 0x0B47, 0x0B48, 0x0B4B, 0x0B4D,
            0x0B55, 0x0B57, 0x0B5C, 0x0B5D, 0x0B5F, 0x0B63, 0x0B66, 0x0B77,
            0x1CDA, 0x1CDA, 0x1CF2, 0x1CF2,
            //  #306 (16995+3): scx=Phags_Pa:Phag
            0x1802, 0x1803, 0x1805, 0x1805, 0xA840, 0xA877,
            //  #307 (16998+4): scx=Psalter_Pahlavi:Phlp
            0x0640, 0x0640, 0x10B80, 0x10B91, 0x10B99, 0x10B9C, 0x10BA9, 0x10BAF,
            //  #308 (17002+6): scx=Sharada:Shrd
            0x0951, 0x0951, 0x1CD7, 0x1CD7, 0x1CD9, 0x1CD9, 0x1CDC, 0x1CDD,
            0x1CE0, 0x1CE0, 0x11180, 0x111DF,
            //  #309 (17008+14): scx=Sinhala:Sinh
            0x0964, 0x0965, 0x0D81, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1,
            0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DCA, 0x0DCA,
            0x0DCF, 0x0DD4, 0x0DD6, 0x0DD6, 0x0DD8, 0x0DDF, 0x0DE6, 0x0DEF,
            0x0DF2, 0x0DF4, 0x111E1, 0x111F4,
            //  #310 (17022+2): scx=Sogdian:Sogd
            0x0640, 0x0640, 0x10F30, 0x10F59,
            //  #311 (17024+3): scx=Syloti_Nagri:Sylo
            0x0964, 0x0965, 0x09E6, 0x09EF, 0xA800, 0xA82C,
            //  #312 (17027+12): scx=Syriac:Syrc
            0x060C, 0x060C, 0x061B, 0x061C, 0x061F, 0x061F, 0x0640, 0x0640,
            0x064B, 0x0655, 0x0670, 0x0670, 0x0700, 0x070D, 0x070F, 0x074A,
            0x074D, 0x074F, 0x0860, 0x086A, 0x1DF8, 0x1DF8, 0x1DFA, 0x1DFA,
            //  #313 (17039+3): scx=Tagalog:Tglg
            0x1700, 0x1715, 0x171F, 0x171F, 0x1735, 0x1736,
            //  #314 (17042+4): scx=Tagbanwa:Tagb
            0x1735, 0x1736, 0x1760, 0x176C, 0x176E, 0x1770, 0x1772, 0x1773,
            //  #315 (17046+3): scx=Tai_Le:Tale
            0x1040, 0x1049, 0x1950, 0x196D, 0x1970, 0x1974,
            //  #316 (17049+4): scx=Takri:Takr
            0x0964, 0x0965, 0xA830, 0xA839, 0x11680, 0x116B9, 0x116C0, 0x116C9,
            //  #317 (17053+25): scx=Tamil:Taml
            0x0951, 0x0952, 0x0964, 0x0965, 0x0B82, 0x0B83, 0x0B85, 0x0B8A,
            0x0B8E, 0x0B90, 0x0B92, 0x0B95, 0x0B99, 0x0B9A, 0x0B9C, 0x0B9C,
            0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA, 0x0BAE, 0x0BB9,
            0x0BBE, 0x0BC2, 0x0BC6, 0x0BC8, 0x0BCA, 0x0BCD, 0x0BD0, 0x0BD0,
            0x0BD7, 0x0BD7, 0x0BE6, 0x0BFA, 0x1CDA, 0x1CDA, 0xA8F3, 0xA8F3,
            0x11301, 0x11301, 0x11303, 0x11303, 0x1133B, 0x1133C, 0x11FC0, 0x11FF1,
            0x11FFF, 0x11FFF,
            //  #318 (17078+17): scx=Telugu:Telu
            0x0951, 0x0952, 0x0964, 0x0965, 0x0C00, 0x0C0C, 0x0C0E, 0x0C10,
            0x0C12, 0x0C28, 0x0C2A, 0x0C39, 0x0C3C, 0x0C44, 0x0C46, 0x0C48,
            0x0C4A, 0x0C4D, 0x0C55, 0x0C56, 0x0C58, 0x0C5A, 0x0C5D, 0x0C5D,
            0x0C60, 0x0C63, 0x0C66, 0x0C6F, 0x0C77, 0x0C7F, 0x1CDA, 0x1CDA,
            0x1CF2, 0x1CF2,
            //  #319 (17095+7): scx=Thaana:Thaa
            0x060C, 0x060C, 0x061B, 0x061C, 0x061F, 0x061F, 0x0660, 0x0669,
            0x0780, 0x07B1, 0xFDF2, 0xFDF2, 0xFDFD, 0xFDFD,
            //  #320 (17102+6): scx=Tirhuta:Tirh
            0x0951, 0x0952, 0x0964, 0x0965, 0x1CF2, 0x1CF2, 0xA830, 0xA839,
            0x11480, 0x114C7, 0x114D0, 0x114D9,
            //  #321 (17108+7): scx=Yezidi:Yezi
            0x060C, 0x060C, 0x061B, 0x061B, 0x061F, 0x061F, 0x0660, 0x0669,
            0x10E80, 0x10EA9, 0x10EAB, 0x10EAD, 0x10EB0, 0x10EB1,
            //  #322 (17115+7): scx=Yi:Yiii
            0x3001, 0x3002, 0x3008, 0x3011, 0x3014, 0x301B, 0x30FB, 0x30FB,
            0xA000, 0xA48C, 0xA490, 0xA4C6, 0xFF61, 0xFF65
        };

        template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
        const T5 unicode_property_data<T1, T2, T3, T4, T5, T6>::rangenumbertable[] =
        {
            { ptype::unknown, "*", 0 },	//  #0
            { ptype::general_category, "Other:C", 1 },	//  #1
            { ptype::general_category, "Control:Cc:cntrl", 2 },	//  #2
            { ptype::general_category, "Format:Cf", 3 },	//  #3
            { ptype::general_category, "Unassigned:Cn", 4 },	//  #4
            { ptype::general_category, "Private_Use:Co", 5 },	//  #5
            { ptype::general_category, "Surrogate:Cs", 6 },	//  #6
            { ptype::general_category, "Letter:L", 7 },	//  #7
            { ptype::general_category, "Cased_Letter:LC", 8 },	//  #8
            { ptype::general_category, "Lowercase_Letter:Ll", 9 },	//  #9
            { ptype::general_category, "Titlecase_Letter:Lt", 10 },	//  #10
            { ptype::general_category, "Uppercase_Letter:Lu", 11 },	//  #11
            { ptype::general_category, "Modifier_Letter:Lm", 12 },	//  #12
            { ptype::general_category, "Other_Letter:Lo", 13 },	//  #13
            { ptype::general_category, "Mark:M:Combining_Mark", 14 },	//  #14
            { ptype::general_category, "Spacing_Mark:Mc", 15 },	//  #15
            { ptype::general_category, "Enclosing_Mark:Me", 16 },	//  #16
            { ptype::general_category, "Nonspacing_Mark:Mn", 17 },	//  #17
            { ptype::general_category, "Number:N", 18 },	//  #18
            { ptype::general_category, "Decimal_Number:Nd:digit", 19 },	//  #19
            { ptype::general_category, "Letter_Number:Nl", 20 },	//  #20
            { ptype::general_category, "Other_Number:No", 21 },	//  #21
            { ptype::general_category, "Punctuation:P:punct", 22 },	//  #22
            { ptype::general_category, "Connector_Punctuation:Pc", 23 },	//  #23
            { ptype::general_category, "Dash_Punctuation:Pd", 24 },	//  #24
            { ptype::general_category, "Close_Punctuation:Pe", 25 },	//  #25
            { ptype::general_category, "Final_Punctuation:Pf", 26 },	//  #26
            { ptype::general_category, "Initial_Punctuation:Pi", 27 },	//  #27
            { ptype::general_category, "Other_Punctuation:Po", 28 },	//  #28
            { ptype::general_category, "Open_Punctuation:Ps", 29 },	//  #29
            { ptype::general_category, "Symbol:S", 30 },	//  #30
            { ptype::general_category, "Currency_Symbol:Sc", 31 },	//  #31
            { ptype::general_category, "Modifier_Symbol:Sk", 32 },	//  #32
            { ptype::general_category, "Math_Symbol:Sm", 33 },	//  #33
            { ptype::general_category, "Other_Symbol:So", 34 },	//  #34
            { ptype::general_category, "Separator:Z", 35 },	//  #35
            { ptype::general_category, "Line_Separator:Zl", 36 },	//  #36
            { ptype::general_category, "Paragraph_Separator:Zp", 37 },	//  #37
            { ptype::general_category, "Space_Separator:Zs", 38 },	//  #38
            { ptype::binary, "ASCII", 39 },	//  #39
            { ptype::binary, "ASCII_Hex_Digit:AHex", 40 },	//  #40
            { ptype::binary, "Alphabetic:Alpha", 41 },	//  #41
            { ptype::binary, "Any", 42 },	//  #42
            { ptype::binary, "Assigned", 43 },	//  #43
            { ptype::binary, "Bidi_Control:Bidi_C", 44 },	//  #44
            { ptype::binary, "Bidi_Mirrored:Bidi_M", 45 },	//  #45
            { ptype::binary, "Case_Ignorable:CI", 46 },	//  #46
            { ptype::binary, "Cased", 47 },	//  #47
            { ptype::binary, "Changes_When_Casefolded:CWCF", 48 },	//  #48
            { ptype::binary, "Changes_When_Casemapped:CWCM", 49 },	//  #49
            { ptype::binary, "Changes_When_Lowercased:CWL", 50 },	//  #50
            { ptype::binary, "Changes_When_NFKC_Casefolded:CWKCF", 51 },	//  #51
            { ptype::binary, "Changes_When_Titlecased:CWT", 52 },	//  #52
            { ptype::binary, "Changes_When_Uppercased:CWU", 53 },	//  #53
            { ptype::binary, "Dash", 54 },	//  #54
            { ptype::binary, "Default_Ignorable_Code_Point:DI", 55 },	//  #55
            { ptype::binary, "Deprecated:Dep", 56 },	//  #56
            { ptype::binary, "Diacritic:Dia", 57 },	//  #57
            { ptype::binary, "Emoji", 58 },	//  #58
            { ptype::binary, "Emoji_Component:EComp", 59 },	//  #59
            { ptype::binary, "Emoji_Modifier:EMod", 60 },	//  #60
            { ptype::binary, "Emoji_Modifier_Base:EBase", 61 },	//  #61
            { ptype::binary, "Emoji_Presentation:EPres", 62 },	//  #62
            { ptype::binary, "Extended_Pictographic:ExtPict", 63 },	//  #63
            { ptype::binary, "Extender:Ext", 64 },	//  #64
            { ptype::binary, "Grapheme_Base:Gr_Base", 65 },	//  #65
            { ptype::binary, "Grapheme_Extend:Gr_Ext", 66 },	//  #66
            { ptype::binary, "Hex_Digit:Hex", 67 },	//  #67
            { ptype::binary, "IDS_Binary_Operator:IDSB", 68 },	//  #68
            { ptype::binary, "IDS_Trinary_Operator:IDST", 69 },	//  #69
            { ptype::binary, "ID_Continue:IDC", 70 },	//  #70
            { ptype::binary, "ID_Start:IDS", 71 },	//  #71
            { ptype::binary, "Ideographic:Ideo", 72 },	//  #72
            { ptype::binary, "Join_Control:Join_C", 73 },	//  #73
            { ptype::binary, "Logical_Order_Exception:LOE", 74 },	//  #74
            { ptype::binary, "Lowercase:Lower", 75 },	//  #75
            { ptype::binary, "Math", 76 },	//  #76
            { ptype::binary, "Noncharacter_Code_Point:NChar", 77 },	//  #77
            { ptype::binary, "Pattern_Syntax:Pat_Syn", 78 },	//  #78
            { ptype::binary, "Pattern_White_Space:Pat_WS", 79 },	//  #79
            { ptype::binary, "Quotation_Mark:QMark", 80 },	//  #80
            { ptype::binary, "Radical", 81 },	//  #81
            { ptype::binary, "Regional_Indicator:RI", 82 },	//  #82
            { ptype::binary, "Sentence_Terminal:STerm", 83 },	//  #83
            { ptype::binary, "Soft_Dotted:SD", 84 },	//  #84
            { ptype::binary, "Terminal_Punctuation:Term", 85 },	//  #85
            { ptype::binary, "Unified_Ideograph:UIdeo", 86 },	//  #86
            { ptype::binary, "Uppercase:Upper", 87 },	//  #87
            { ptype::binary, "Variation_Selector:VS", 88 },	//  #88
            { ptype::binary, "White_Space:space", 89 },	//  #89
            { ptype::binary, "XID_Continue:XIDC", 90 },	//  #90
            { ptype::binary, "XID_Start:XIDS", 91 },	//  #91
            { ptype::script, "Adlam:Adlm", 92 },	//  #92
            { ptype::script, "Ahom:Ahom", 93 },	//  #93
            { ptype::script, "Anatolian_Hieroglyphs:Hluw", 94 },	//  #94
            { ptype::script, "Arabic:Arab", 95 },	//  #95
            { ptype::script, "Armenian:Armn", 96 },	//  #96
            { ptype::script, "Avestan:Avst", 97 },	//  #97
            { ptype::script, "Balinese:Bali", 98 },	//  #98
            { ptype::script, "Bamum:Bamu", 99 },	//  #99
            { ptype::script, "Bassa_Vah:Bass", 100 },	//  #100
            { ptype::script, "Batak:Batk", 101 },	//  #101
            { ptype::script, "Bengali:Beng", 102 },	//  #102
            { ptype::script, "Bhaiksuki:Bhks", 103 },	//  #103
            { ptype::script, "Bopomofo:Bopo", 104 },	//  #104
            { ptype::script, "Brahmi:Brah", 105 },	//  #105
            { ptype::script, "Braille:Brai", 106 },	//  #106
            { ptype::script, "Buginese:Bugi", 107 },	//  #107
            { ptype::script, "Buhid:Buhd", 108 },	//  #108
            { ptype::script, "Canadian_Aboriginal:Cans", 109 },	//  #109
            { ptype::script, "Carian:Cari", 110 },	//  #110
            { ptype::script, "Caucasian_Albanian:Aghb", 111 },	//  #111
            { ptype::script, "Chakma:Cakm", 112 },	//  #112
            { ptype::script, "Cham:Cham", 113 },	//  #113
            { ptype::script, "Cherokee:Cher", 114 },	//  #114
            { ptype::script, "Chorasmian:Chrs", 115 },	//  #115
            { ptype::script, "Common:Zyyy", 116 },	//  #116
            { ptype::script, "Coptic:Copt:Qaac", 117 },	//  #117
            { ptype::script, "Cypro_Minoan:Cpmn", 118 },	//  #118
            { ptype::script, "Cuneiform:Xsux", 119 },	//  #119
            { ptype::script, "Cypriot:Cprt", 120 },	//  #120
            { ptype::script, "Cyrillic:Cyrl", 121 },	//  #121
            { ptype::script, "Deseret:Dsrt", 122 },	//  #122
            { ptype::script, "Devanagari:Deva", 123 },	//  #123
            { ptype::script, "Dives_Akuru:Diak", 124 },	//  #124
            { ptype::script, "Dogra:Dogr", 125 },	//  #125
            { ptype::script, "Duployan:Dupl", 126 },	//  #126
            { ptype::script, "Egyptian_Hieroglyphs:Egyp", 127 },	//  #127
            { ptype::script, "Elbasan:Elba", 128 },	//  #128
            { ptype::script, "Elymaic:Elym", 129 },	//  #129
            { ptype::script, "Ethiopic:Ethi", 130 },	//  #130
            { ptype::script, "Georgian:Geor", 131 },	//  #131
            { ptype::script, "Glagolitic:Glag", 132 },	//  #132
            { ptype::script, "Gothic:Goth", 133 },	//  #133
            { ptype::script, "Grantha:Gran", 134 },	//  #134
            { ptype::script, "Greek:Grek", 135 },	//  #135
            { ptype::script, "Gujarati:Gujr", 136 },	//  #136
            { ptype::script, "Gunjala_Gondi:Gong", 137 },	//  #137
            { ptype::script, "Gurmukhi:Guru", 138 },	//  #138
            { ptype::script, "Han:Hani", 139 },	//  #139
            { ptype::script, "Hangul:Hang", 140 },	//  #140
            { ptype::script, "Hanifi_Rohingya:Rohg", 141 },	//  #141
            { ptype::script, "Hanunoo:Hano", 142 },	//  #142
            { ptype::script, "Hatran:Hatr", 143 },	//  #143
            { ptype::script, "Hebrew:Hebr", 144 },	//  #144
            { ptype::script, "Hiragana:Hira", 145 },	//  #145
            { ptype::script, "Imperial_Aramaic:Armi", 146 },	//  #146
            { ptype::script, "Inherited:Zinh:Qaai", 147 },	//  #147
            { ptype::script, "Inscriptional_Pahlavi:Phli", 148 },	//  #148
            { ptype::script, "Inscriptional_Parthian:Prti", 149 },	//  #149
            { ptype::script, "Javanese:Java", 150 },	//  #150
            { ptype::script, "Kaithi:Kthi", 151 },	//  #151
            { ptype::script, "Kannada:Knda", 152 },	//  #152
            { ptype::script, "Katakana:Kana", 153 },	//  #153
            { ptype::script, "Kayah_Li:Kali", 154 },	//  #154
            { ptype::script, "Kharoshthi:Khar", 155 },	//  #155
            { ptype::script, "Khitan_Small_Script:Kits", 156 },	//  #156
            { ptype::script, "Khmer:Khmr", 157 },	//  #157
            { ptype::script, "Khojki:Khoj", 158 },	//  #158
            { ptype::script, "Khudawadi:Sind", 159 },	//  #159
            { ptype::script, "Lao:Laoo", 160 },	//  #160
            { ptype::script, "Latin:Latn", 161 },	//  #161
            { ptype::script, "Lepcha:Lepc", 162 },	//  #162
            { ptype::script, "Limbu:Limb", 163 },	//  #163
            { ptype::script, "Linear_A:Lina", 164 },	//  #164
            { ptype::script, "Linear_B:Linb", 165 },	//  #165
            { ptype::script, "Lisu:Lisu", 166 },	//  #166
            { ptype::script, "Lycian:Lyci", 167 },	//  #167
            { ptype::script, "Lydian:Lydi", 168 },	//  #168
            { ptype::script, "Mahajani:Mahj", 169 },	//  #169
            { ptype::script, "Makasar:Maka", 170 },	//  #170
            { ptype::script, "Malayalam:Mlym", 171 },	//  #171
            { ptype::script, "Mandaic:Mand", 172 },	//  #172
            { ptype::script, "Manichaean:Mani", 173 },	//  #173
            { ptype::script, "Marchen:Marc", 174 },	//  #174
            { ptype::script, "Masaram_Gondi:Gonm", 175 },	//  #175
            { ptype::script, "Medefaidrin:Medf", 176 },	//  #176
            { ptype::script, "Meetei_Mayek:Mtei", 177 },	//  #177
            { ptype::script, "Mende_Kikakui:Mend", 178 },	//  #178
            { ptype::script, "Meroitic_Cursive:Merc", 179 },	//  #179
            { ptype::script, "Meroitic_Hieroglyphs:Mero", 180 },	//  #180
            { ptype::script, "Miao:Plrd", 181 },	//  #181
            { ptype::script, "Modi:Modi", 182 },	//  #182
            { ptype::script, "Mongolian:Mong", 183 },	//  #183
            { ptype::script, "Mro:Mroo", 184 },	//  #184
            { ptype::script, "Multani:Mult", 185 },	//  #185
            { ptype::script, "Myanmar:Mymr", 186 },	//  #186
            { ptype::script, "Nabataean:Nbat", 187 },	//  #187
            { ptype::script, "Nandinagari:Nand", 188 },	//  #188
            { ptype::script, "New_Tai_Lue:Talu", 189 },	//  #189
            { ptype::script, "Newa:Newa", 190 },	//  #190
            { ptype::script, "Nko:Nkoo", 191 },	//  #191
            { ptype::script, "Nushu:Nshu", 192 },	//  #192
            { ptype::script, "Nyiakeng_Puachue_Hmong:Hmnp", 193 },	//  #193
            { ptype::script, "Ogham:Ogam", 194 },	//  #194
            { ptype::script, "Ol_Chiki:Olck", 195 },	//  #195
            { ptype::script, "Old_Hungarian:Hung", 196 },	//  #196
            { ptype::script, "Old_Italic:Ital", 197 },	//  #197
            { ptype::script, "Old_North_Arabian:Narb", 198 },	//  #198
            { ptype::script, "Old_Permic:Perm", 199 },	//  #199
            { ptype::script, "Old_Persian:Xpeo", 200 },	//  #200
            { ptype::script, "Old_Sogdian:Sogo", 201 },	//  #201
            { ptype::script, "Old_South_Arabian:Sarb", 202 },	//  #202
            { ptype::script, "Old_Turkic:Orkh", 203 },	//  #203
            { ptype::script, "Old_Uyghur:Ougr", 204 },	//  #204
            { ptype::script, "Oriya:Orya", 205 },	//  #205
            { ptype::script, "Osage:Osge", 206 },	//  #206
            { ptype::script, "Osmanya:Osma", 207 },	//  #207
            { ptype::script, "Pahawh_Hmong:Hmng", 208 },	//  #208
            { ptype::script, "Palmyrene:Palm", 209 },	//  #209
            { ptype::script, "Pau_Cin_Hau:Pauc", 210 },	//  #210
            { ptype::script, "Phags_Pa:Phag", 211 },	//  #211
            { ptype::script, "Phoenician:Phnx", 212 },	//  #212
            { ptype::script, "Psalter_Pahlavi:Phlp", 213 },	//  #213
            { ptype::script, "Rejang:Rjng", 214 },	//  #214
            { ptype::script, "Runic:Runr", 215 },	//  #215
            { ptype::script, "Samaritan:Samr", 216 },	//  #216
            { ptype::script, "Saurashtra:Saur", 217 },	//  #217
            { ptype::script, "Sharada:Shrd", 218 },	//  #218
            { ptype::script, "Shavian:Shaw", 219 },	//  #219
            { ptype::script, "Siddham:Sidd", 220 },	//  #220
            { ptype::script, "SignWriting:Sgnw", 221 },	//  #221
            { ptype::script, "Sinhala:Sinh", 222 },	//  #222
            { ptype::script, "Sogdian:Sogd", 223 },	//  #223
            { ptype::script, "Sora_Sompeng:Sora", 224 },	//  #224
            { ptype::script, "Soyombo:Soyo", 225 },	//  #225
            { ptype::script, "Sundanese:Sund", 226 },	//  #226
            { ptype::script, "Syloti_Nagri:Sylo", 227 },	//  #227
            { ptype::script, "Syriac:Syrc", 228 },	//  #228
            { ptype::script, "Tagalog:Tglg", 229 },	//  #229
            { ptype::script, "Tagbanwa:Tagb", 230 },	//  #230
            { ptype::script, "Tai_Le:Tale", 231 },	//  #231
            { ptype::script, "Tai_Tham:Lana", 232 },	//  #232
            { ptype::script, "Tai_Viet:Tavt", 233 },	//  #233
            { ptype::script, "Takri:Takr", 234 },	//  #234
            { ptype::script, "Tamil:Taml", 235 },	//  #235
            { ptype::script, "Tangsa:Tnsa", 236 },	//  #236
            { ptype::script, "Tangut:Tang", 237 },	//  #237
            { ptype::script, "Telugu:Telu", 238 },	//  #238
            { ptype::script, "Thaana:Thaa", 239 },	//  #239
            { ptype::script, "Thai:Thai", 240 },	//  #240
            { ptype::script, "Tibetan:Tibt", 241 },	//  #241
            { ptype::script, "Tifinagh:Tfng", 242 },	//  #242
            { ptype::script, "Tirhuta:Tirh", 243 },	//  #243
            { ptype::script, "Toto", 244 },	//  #244
            { ptype::script, "Ugaritic:Ugar", 245 },	//  #245
            { ptype::script, "Vai:Vaii", 246 },	//  #246
            { ptype::script, "Vithkuqi:Vith", 247 },	//  #247
            { ptype::script, "Wancho:Wcho", 248 },	//  #248
            { ptype::script, "Warang_Citi:Wara", 249 },	//  #249
            { ptype::script, "Yezidi:Yezi", 250 },	//  #250
            { ptype::script, "Yi:Yiii", 251 },	//  #251
            { ptype::script, "Zanabazar_Square:Zanb", 252 },	//  #252
            { ptype::script_extensions, "Adlam:Adlm", 253 },	//  #253
            { ptype::script_extensions, "Ahom:Ahom", 93 },	//  #254
            { ptype::script_extensions, "Anatolian_Hieroglyphs:Hluw", 94 },	//  #255
            { ptype::script_extensions, "Arabic:Arab", 254 },	//  #256
            { ptype::script_extensions, "Armenian:Armn", 96 },	//  #257
            { ptype::script_extensions, "Avestan:Avst", 97 },	//  #258
            { ptype::script_extensions, "Balinese:Bali", 98 },	//  #259
            { ptype::script_extensions, "Bamum:Bamu", 99 },	//  #260
            { ptype::script_extensions, "Bassa_Vah:Bass", 100 },	//  #261
            { ptype::script_extensions, "Batak:Batk", 101 },	//  #262
            { ptype::script_extensions, "Bengali:Beng", 255 },	//  #263
            { ptype::script_extensions, "Bhaiksuki:Bhks", 103 },	//  #264
            { ptype::script_extensions, "Bopomofo:Bopo", 256 },	//  #265
            { ptype::script_extensions, "Brahmi:Brah", 105 },	//  #266
            { ptype::script_extensions, "Braille:Brai", 106 },	//  #267
            { ptype::script_extensions, "Buginese:Bugi", 257 },	//  #268
            { ptype::script_extensions, "Buhid:Buhd", 258 },	//  #269
            { ptype::script_extensions, "Canadian_Aboriginal:Cans", 109 },	//  #270
            { ptype::script_extensions, "Carian:Cari", 110 },	//  #271
            { ptype::script_extensions, "Caucasian_Albanian:Aghb", 111 },	//  #272
            { ptype::script_extensions, "Chakma:Cakm", 259 },	//  #273
            { ptype::script_extensions, "Cham:Cham", 113 },	//  #274
            { ptype::script_extensions, "Cherokee:Cher", 114 },	//  #275
            { ptype::script_extensions, "Chorasmian:Chrs", 115 },	//  #276
            { ptype::script_extensions, "Common:Zyyy", 260 },	//  #277
            { ptype::script_extensions, "Coptic:Copt:Qaac", 261 },	//  #278
            { ptype::script_extensions, "Cypro_Minoan:Cpmn", 262 },	//  #279
            { ptype::script_extensions, "Cuneiform:Xsux", 119 },	//  #280
            { ptype::script_extensions, "Cypriot:Cprt", 263 },	//  #281
            { ptype::script_extensions, "Cyrillic:Cyrl", 264 },	//  #282
            { ptype::script_extensions, "Deseret:Dsrt", 122 },	//  #283
            { ptype::script_extensions, "Devanagari:Deva", 265 },	//  #284
            { ptype::script_extensions, "Dives_Akuru:Diak", 124 },	//  #285
            { ptype::script_extensions, "Dogra:Dogr", 266 },	//  #286
            { ptype::script_extensions, "Duployan:Dupl", 267 },	//  #287
            { ptype::script_extensions, "Egyptian_Hieroglyphs:Egyp", 127 },	//  #288
            { ptype::script_extensions, "Elbasan:Elba", 128 },	//  #289
            { ptype::script_extensions, "Elymaic:Elym", 129 },	//  #290
            { ptype::script_extensions, "Ethiopic:Ethi", 130 },	//  #291
            { ptype::script_extensions, "Georgian:Geor", 268 },	//  #292
            { ptype::script_extensions, "Glagolitic:Glag", 269 },	//  #293
            { ptype::script_extensions, "Gothic:Goth", 133 },	//  #294
            { ptype::script_extensions, "Grantha:Gran", 270 },	//  #295
            { ptype::script_extensions, "Greek:Grek", 271 },	//  #296
            { ptype::script_extensions, "Gujarati:Gujr", 272 },	//  #297
            { ptype::script_extensions, "Gunjala_Gondi:Gong", 273 },	//  #298
            { ptype::script_extensions, "Gurmukhi:Guru", 274 },	//  #299
            { ptype::script_extensions, "Han:Hani", 275 },	//  #300
            { ptype::script_extensions, "Hangul:Hang", 276 },	//  #301
            { ptype::script_extensions, "Hanifi_Rohingya:Rohg", 277 },	//  #302
            { ptype::script_extensions, "Hanunoo:Hano", 278 },	//  #303
            { ptype::script_extensions, "Hatran:Hatr", 143 },	//  #304
            { ptype::script_extensions, "Hebrew:Hebr", 144 },	//  #305
            { ptype::script_extensions, "Hiragana:Hira", 279 },	//  #306
            { ptype::script_extensions, "Imperial_Aramaic:Armi", 146 },	//  #307
            { ptype::script_extensions, "Inherited:Zinh:Qaai", 280 },	//  #308
            { ptype::script_extensions, "Inscriptional_Pahlavi:Phli", 148 },	//  #309
            { ptype::script_extensions, "Inscriptional_Parthian:Prti", 149 },	//  #310
            { ptype::script_extensions, "Javanese:Java", 281 },	//  #311
            { ptype::script_extensions, "Kaithi:Kthi", 282 },	//  #312
            { ptype::script_extensions, "Kannada:Knda", 283 },	//  #313
            { ptype::script_extensions, "Katakana:Kana", 284 },	//  #314
            { ptype::script_extensions, "Kayah_Li:Kali", 285 },	//  #315
            { ptype::script_extensions, "Kharoshthi:Khar", 155 },	//  #316
            { ptype::script_extensions, "Khitan_Small_Script:Kits", 156 },	//  #317
            { ptype::script_extensions, "Khmer:Khmr", 157 },	//  #318
            { ptype::script_extensions, "Khojki:Khoj", 286 },	//  #319
            { ptype::script_extensions, "Khudawadi:Sind", 287 },	//  #320
            { ptype::script_extensions, "Lao:Laoo", 160 },	//  #321
            { ptype::script_extensions, "Latin:Latn", 288 },	//  #322
            { ptype::script_extensions, "Lepcha:Lepc", 162 },	//  #323
            { ptype::script_extensions, "Limbu:Limb", 289 },	//  #324
            { ptype::script_extensions, "Linear_A:Lina", 290 },	//  #325
            { ptype::script_extensions, "Linear_B:Linb", 291 },	//  #326
            { ptype::script_extensions, "Lisu:Lisu", 166 },	//  #327
            { ptype::script_extensions, "Lycian:Lyci", 167 },	//  #328
            { ptype::script_extensions, "Lydian:Lydi", 168 },	//  #329
            { ptype::script_extensions, "Mahajani:Mahj", 292 },	//  #330
            { ptype::script_extensions, "Makasar:Maka", 170 },	//  #331
            { ptype::script_extensions, "Malayalam:Mlym", 293 },	//  #332
            { ptype::script_extensions, "Mandaic:Mand", 294 },	//  #333
            { ptype::script_extensions, "Manichaean:Mani", 295 },	//  #334
            { ptype::script_extensions, "Marchen:Marc", 174 },	//  #335
            { ptype::script_extensions, "Masaram_Gondi:Gonm", 296 },	//  #336
            { ptype::script_extensions, "Medefaidrin:Medf", 176 },	//  #337
            { ptype::script_extensions, "Meetei_Mayek:Mtei", 177 },	//  #338
            { ptype::script_extensions, "Mende_Kikakui:Mend", 178 },	//  #339
            { ptype::script_extensions, "Meroitic_Cursive:Merc", 179 },	//  #340
            { ptype::script_extensions, "Meroitic_Hieroglyphs:Mero", 180 },	//  #341
            { ptype::script_extensions, "Miao:Plrd", 181 },	//  #342
            { ptype::script_extensions, "Modi:Modi", 297 },	//  #343
            { ptype::script_extensions, "Mongolian:Mong", 298 },	//  #344
            { ptype::script_extensions, "Mro:Mroo", 184 },	//  #345
            { ptype::script_extensions, "Multani:Mult", 299 },	//  #346
            { ptype::script_extensions, "Myanmar:Mymr", 300 },	//  #347
            { ptype::script_extensions, "Nabataean:Nbat", 187 },	//  #348
            { ptype::script_extensions, "Nandinagari:Nand", 301 },	//  #349
            { ptype::script_extensions, "New_Tai_Lue:Talu", 189 },	//  #350
            { ptype::script_extensions, "Newa:Newa", 190 },	//  #351
            { ptype::script_extensions, "Nko:Nkoo", 302 },	//  #352
            { ptype::script_extensions, "Nushu:Nshu", 192 },	//  #353
            { ptype::script_extensions, "Nyiakeng_Puachue_Hmong:Hmnp", 193 },	//  #354
            { ptype::script_extensions, "Ogham:Ogam", 194 },	//  #355
            { ptype::script_extensions, "Ol_Chiki:Olck", 195 },	//  #356
            { ptype::script_extensions, "Old_Hungarian:Hung", 196 },	//  #357
            { ptype::script_extensions, "Old_Italic:Ital", 197 },	//  #358
            { ptype::script_extensions, "Old_North_Arabian:Narb", 198 },	//  #359
            { ptype::script_extensions, "Old_Permic:Perm", 303 },	//  #360
            { ptype::script_extensions, "Old_Persian:Xpeo", 200 },	//  #361
            { ptype::script_extensions, "Old_Sogdian:Sogo", 201 },	//  #362
            { ptype::script_extensions, "Old_South_Arabian:Sarb", 202 },	//  #363
            { ptype::script_extensions, "Old_Turkic:Orkh", 203 },	//  #364
            { ptype::script_extensions, "Old_Uyghur:Ougr", 304 },	//  #365
            { ptype::script_extensions, "Oriya:Orya", 305 },	//  #366
            { ptype::script_extensions, "Osage:Osge", 206 },	//  #367
            { ptype::script_extensions, "Osmanya:Osma", 207 },	//  #368
            { ptype::script_extensions, "Pahawh_Hmong:Hmng", 208 },	//  #369
            { ptype::script_extensions, "Palmyrene:Palm", 209 },	//  #370
            { ptype::script_extensions, "Pau_Cin_Hau:Pauc", 210 },	//  #371
            { ptype::script_extensions, "Phags_Pa:Phag", 306 },	//  #372
            { ptype::script_extensions, "Phoenician:Phnx", 212 },	//  #373
            { ptype::script_extensions, "Psalter_Pahlavi:Phlp", 307 },	//  #374
            { ptype::script_extensions, "Rejang:Rjng", 214 },	//  #375
            { ptype::script_extensions, "Runic:Runr", 215 },	//  #376
            { ptype::script_extensions, "Samaritan:Samr", 216 },	//  #377
            { ptype::script_extensions, "Saurashtra:Saur", 217 },	//  #378
            { ptype::script_extensions, "Sharada:Shrd", 308 },	//  #379
            { ptype::script_extensions, "Shavian:Shaw", 219 },	//  #380
            { ptype::script_extensions, "Siddham:Sidd", 220 },	//  #381
            { ptype::script_extensions, "SignWriting:Sgnw", 221 },	//  #382
            { ptype::script_extensions, "Sinhala:Sinh", 309 },	//  #383
            { ptype::script_extensions, "Sogdian:Sogd", 310 },	//  #384
            { ptype::script_extensions, "Sora_Sompeng:Sora", 224 },	//  #385
            { ptype::script_extensions, "Soyombo:Soyo", 225 },	//  #386
            { ptype::script_extensions, "Sundanese:Sund", 226 },	//  #387
            { ptype::script_extensions, "Syloti_Nagri:Sylo", 311 },	//  #388
            { ptype::script_extensions, "Syriac:Syrc", 312 },	//  #389
            { ptype::script_extensions, "Tagalog:Tglg", 313 },	//  #390
            { ptype::script_extensions, "Tagbanwa:Tagb", 314 },	//  #391
            { ptype::script_extensions, "Tai_Le:Tale", 315 },	//  #392
            { ptype::script_extensions, "Tai_Tham:Lana", 232 },	//  #393
            { ptype::script_extensions, "Tai_Viet:Tavt", 233 },	//  #394
            { ptype::script_extensions, "Takri:Takr", 316 },	//  #395
            { ptype::script_extensions, "Tamil:Taml", 317 },	//  #396
            { ptype::script_extensions, "Tangsa:Tnsa", 236 },	//  #397
            { ptype::script_extensions, "Tangut:Tang", 237 },	//  #398
            { ptype::script_extensions, "Telugu:Telu", 318 },	//  #399
            { ptype::script_extensions, "Thaana:Thaa", 319 },	//  #400
            { ptype::script_extensions, "Thai:Thai", 240 },	//  #401
            { ptype::script_extensions, "Tibetan:Tibt", 241 },	//  #402
            { ptype::script_extensions, "Tifinagh:Tfng", 242 },	//  #403
            { ptype::script_extensions, "Tirhuta:Tirh", 320 },	//  #404
            { ptype::script_extensions, "Toto", 244 },	//  #405
            { ptype::script_extensions, "Ugaritic:Ugar", 245 },	//  #406
            { ptype::script_extensions, "Vai:Vaii", 246 },	//  #407
            { ptype::script_extensions, "Vithkuqi:Vith", 247 },	//  #408
            { ptype::script_extensions, "Wancho:Wcho", 248 },	//  #409
            { ptype::script_extensions, "Warang_Citi:Wara", 249 },	//  #410
            { ptype::script_extensions, "Yezidi:Yezi", 321 },	//  #411
            { ptype::script_extensions, "Yi:Yiii", 322 },	//  #412
            { ptype::script_extensions, "Zanabazar_Square:Zanb", 252 },	//  #413
            { ptype::unknown, "", 0 }
        };

        template <typename T1, typename T2, typename T3, typename T4, typename T5, typename T6>
        const T6 unicode_property_data<T1, T2, T3, T4, T5, T6>::positiontable[] =
        {
            { 0, 0 },	//  #0 unknown
            { 0, 725 },	//  #1 gc=Other:C
            { 0, 2 },	//  #2 gc=Control:Cc:cntrl
            { 2, 21 },	//  #3 gc=Format:Cf
            { 23, 698 },	//  #4 gc=Unassigned:Cn
            { 721, 3 },	//  #5 gc=Private_Use:Co
            { 724, 1 },	//  #6 gc=Surrogate:Cs
            { 725, 1883 },	//  #7 gc=Letter:L
            { 725, 1313 },	//  #8 gc=Cased_Letter:LC
            { 725, 657 },	//  #9 gc=Lowercase_Letter:Ll
            { 1382, 10 },	//  #10 gc=Titlecase_Letter:Lt
            { 1392, 646 },	//  #11 gc=Uppercase_Letter:Lu
            { 2038, 69 },	//  #12 gc=Modifier_Letter:Lm
            { 2107, 501 },	//  #13 gc=Other_Letter:Lo
            { 2608, 518 },	//  #14 gc=Mark:M:Combining_Mark
            { 2608, 177 },	//  #15 gc=Spacing_Mark:Mc
            { 2785, 5 },	//  #16 gc=Enclosing_Mark:Me
            { 2790, 336 },	//  #17 gc=Nonspacing_Mark:Mn
            { 3126, 145 },	//  #18 gc=Number:N
            { 3126, 62 },	//  #19 gc=Decimal_Number:Nd:digit
            { 3188, 12 },	//  #20 gc=Letter_Number:Nl
            { 3200, 71 },	//  #21 gc=Other_Number:No
            { 3271, 386 },	//  #22 gc=Punctuation:P:punct
            { 3271, 6 },	//  #23 gc=Connector_Punctuation:Pc
            { 3277, 19 },	//  #24 gc=Dash_Punctuation:Pd
            { 3296, 76 },	//  #25 gc=Close_Punctuation:Pe
            { 3372, 10 },	//  #26 gc=Final_Punctuation:Pf
            { 3382, 11 },	//  #27 gc=Initial_Punctuation:Pi
            { 3393, 185 },	//  #28 gc=Other_Punctuation:Po
            { 3578, 79 },	//  #29 gc=Open_Punctuation:Ps
            { 3657, 302 },	//  #30 gc=Symbol:S
            { 3657, 21 },	//  #31 gc=Currency_Symbol:Sc
            { 3678, 31 },	//  #32 gc=Modifier_Symbol:Sk
            { 3709, 64 },	//  #33 gc=Math_Symbol:Sm
            { 3773, 186 },	//  #34 gc=Other_Symbol:So
            { 3959, 9 },	//  #35 gc=Separator:Z
            { 3959, 1 },	//  #36 gc=Line_Separator:Zl
            { 3960, 1 },	//  #37 gc=Paragraph_Separator:Zp
            { 3961, 7 },	//  #38 gc=Space_Separator:Zs
            { 3968, 1 },	//  #39 bp=ASCII
            { 3969, 3 },	//  #40 bp=ASCII_Hex_Digit:AHex
            { 3972, 722 },	//  #41 bp=Alphabetic:Alpha
            { 4694, 1 },	//  #42 bp=Any
            { 4695, 0 },	//  #43 bp=Assigned
            { 4695, 4 },	//  #44 bp=Bidi_Control:Bidi_C
            { 4699, 114 },	//  #45 bp=Bidi_Mirrored:Bidi_M
            { 4813, 427 },	//  #46 bp=Case_Ignorable:CI
            { 5240, 155 },	//  #47 bp=Cased
            { 5395, 622 },	//  #48 bp=Changes_When_Casefolded:CWCF
            { 6017, 131 },	//  #49 bp=Changes_When_Casemapped:CWCM
            { 6148, 609 },	//  #50 bp=Changes_When_Lowercased:CWL
            { 6757, 838 },	//  #51 bp=Changes_When_NFKC_Casefolded:CWKCF
            { 7595, 626 },	//  #52 bp=Changes_When_Titlecased:CWT
            { 8221, 627 },	//  #53 bp=Changes_When_Uppercased:CWU
            { 8848, 23 },	//  #54 bp=Dash
            { 8871, 17 },	//  #55 bp=Default_Ignorable_Code_Point:DI
            { 8888, 8 },	//  #56 bp=Deprecated:Dep
            { 8896, 192 },	//  #57 bp=Diacritic:Dia
            { 9088, 153 },	//  #58 bp=Emoji
            { 9241, 10 },	//  #59 bp=Emoji_Component:EComp
            { 9251, 1 },	//  #60 bp=Emoji_Modifier:EMod
            { 9252, 40 },	//  #61 bp=Emoji_Modifier_Base:EBase
            { 9292, 83 },	//  #62 bp=Emoji_Presentation:EPres
            { 9375, 78 },	//  #63 bp=Extended_Pictographic:ExtPict
            { 9453, 33 },	//  #64 bp=Extender:Ext
            { 9486, 861 },	//  #65 bp=Grapheme_Base:Gr_Base
            { 10347, 353 },	//  #66 bp=Grapheme_Extend:Gr_Ext
            { 10700, 6 },	//  #67 bp=Hex_Digit:Hex
            { 10706, 2 },	//  #68 bp=IDS_Binary_Operator:IDSB
            { 10708, 1 },	//  #69 bp=IDS_Trinary_Operator:IDST
            { 10709, 756 },	//  #70 bp=ID_Continue:IDC
            { 11465, 648 },	//  #71 bp=ID_Start:IDS
            { 12113, 19 },	//  #72 bp=Ideographic:Ideo
            { 12132, 1 },	//  #73 bp=Join_Control:Join_C
            { 12133, 7 },	//  #74 bp=Logical_Order_Exception:LOE
            { 12140, 668 },	//  #75 bp=Lowercase:Lower
            { 12808, 138 },	//  #76 bp=Math
            { 12946, 18 },	//  #77 bp=Noncharacter_Code_Point:NChar
            { 12964, 28 },	//  #78 bp=Pattern_Syntax:Pat_Syn
            { 12992, 5 },	//  #79 bp=Pattern_White_Space:Pat_WS
            { 12997, 13 },	//  #80 bp=Quotation_Mark:QMark
            { 13010, 3 },	//  #81 bp=Radical
            { 13013, 1 },	//  #82 bp=Regional_Indicator:RI
            { 13014, 79 },	//  #83 bp=Sentence_Terminal:STerm
            { 13093, 32 },	//  #84 bp=Soft_Dotted:SD
            { 13125, 107 },	//  #85 bp=Terminal_Punctuation:Term
            { 13232, 15 },	//  #86 bp=Unified_Ideograph:UIdeo
            { 13247, 651 },	//  #87 bp=Uppercase:Upper
            { 13898, 4 },	//  #88 bp=Variation_Selector:VS
            { 13902, 10 },	//  #89 bp=White_Space:space
            { 13912, 763 },	//  #90 bp=XID_Continue:XIDC
            { 14675, 655 },	//  #91 bp=XID_Start:XIDS
            { 15330, 3 },	//  #92 sc=Adlam:Adlm
            { 15333, 3 },	//  #93 sc=Ahom:Ahom scx=Ahom:Ahom
            { 15336, 1 },	//  #94 sc=Anatolian_Hieroglyphs:Hluw scx=Anatolian_Hieroglyphs:Hluw
            { 15337, 57 },	//  #95 sc=Arabic:Arab
            { 15394, 4 },	//  #96 sc=Armenian:Armn scx=Armenian:Armn
            { 15398, 2 },	//  #97 sc=Avestan:Avst scx=Avestan:Avst
            { 15400, 2 },	//  #98 sc=Balinese:Bali scx=Balinese:Bali
            { 15402, 2 },	//  #99 sc=Bamum:Bamu scx=Bamum:Bamu
            { 15404, 2 },	//  #100 sc=Bassa_Vah:Bass scx=Bassa_Vah:Bass
            { 15406, 2 },	//  #101 sc=Batak:Batk scx=Batak:Batk
            { 15408, 14 },	//  #102 sc=Bengali:Beng
            { 15422, 4 },	//  #103 sc=Bhaiksuki:Bhks scx=Bhaiksuki:Bhks
            { 15426, 3 },	//  #104 sc=Bopomofo:Bopo
            { 15429, 3 },	//  #105 sc=Brahmi:Brah scx=Brahmi:Brah
            { 15432, 1 },	//  #106 sc=Braille:Brai scx=Braille:Brai
            { 15433, 2 },	//  #107 sc=Buginese:Bugi
            { 15435, 1 },	//  #108 sc=Buhid:Buhd
            { 15436, 3 },	//  #109 sc=Canadian_Aboriginal:Cans scx=Canadian_Aboriginal:Cans
            { 15439, 1 },	//  #110 sc=Carian:Cari scx=Carian:Cari
            { 15440, 2 },	//  #111 sc=Caucasian_Albanian:Aghb scx=Caucasian_Albanian:Aghb
            { 15442, 2 },	//  #112 sc=Chakma:Cakm
            { 15444, 4 },	//  #113 sc=Cham:Cham scx=Cham:Cham
            { 15448, 3 },	//  #114 sc=Cherokee:Cher scx=Cherokee:Cher
            { 15451, 1 },	//  #115 sc=Chorasmian:Chrs scx=Chorasmian:Chrs
            { 15452, 174 },	//  #116 sc=Common:Zyyy
            { 15626, 3 },	//  #117 sc=Coptic:Copt:Qaac
            { 15629, 1 },	//  #118 sc=Cypro_Minoan:Cpmn
            { 15630, 4 },	//  #119 sc=Cuneiform:Xsux scx=Cuneiform:Xsux
            { 15634, 6 },	//  #120 sc=Cypriot:Cprt
            { 15640, 8 },	//  #121 sc=Cyrillic:Cyrl
            { 15648, 1 },	//  #122 sc=Deseret:Dsrt scx=Deseret:Dsrt
            { 15649, 4 },	//  #123 sc=Devanagari:Deva
            { 15653, 8 },	//  #124 sc=Dives_Akuru:Diak scx=Dives_Akuru:Diak
            { 15661, 1 },	//  #125 sc=Dogra:Dogr
            { 15662, 5 },	//  #126 sc=Duployan:Dupl
            { 15667, 2 },	//  #127 sc=Egyptian_Hieroglyphs:Egyp scx=Egyptian_Hieroglyphs:Egyp
            { 15669, 1 },	//  #128 sc=Elbasan:Elba scx=Elbasan:Elba
            { 15670, 1 },	//  #129 sc=Elymaic:Elym scx=Elymaic:Elym
            { 15671, 36 },	//  #130 sc=Ethiopic:Ethi scx=Ethiopic:Ethi
            { 15707, 10 },	//  #131 sc=Georgian:Geor
            { 15717, 6 },	//  #132 sc=Glagolitic:Glag
            { 15723, 1 },	//  #133 sc=Gothic:Goth scx=Gothic:Goth
            { 15724, 15 },	//  #134 sc=Grantha:Gran
            { 15739, 36 },	//  #135 sc=Greek:Grek
            { 15775, 14 },	//  #136 sc=Gujarati:Gujr
            { 15789, 6 },	//  #137 sc=Gunjala_Gondi:Gong
            { 15795, 16 },	//  #138 sc=Gurmukhi:Guru
            { 15811, 20 },	//  #139 sc=Han:Hani
            { 15831, 14 },	//  #140 sc=Hangul:Hang
            { 15845, 2 },	//  #141 sc=Hanifi_Rohingya:Rohg
            { 15847, 1 },	//  #142 sc=Hanunoo:Hano
            { 15848, 3 },	//  #143 sc=Hatran:Hatr scx=Hatran:Hatr
            { 15851, 9 },	//  #144 sc=Hebrew:Hebr scx=Hebrew:Hebr
            { 15860, 5 },	//  #145 sc=Hiragana:Hira
            { 15865, 2 },	//  #146 sc=Imperial_Aramaic:Armi scx=Imperial_Aramaic:Armi
            { 15867, 29 },	//  #147 sc=Inherited:Zinh:Qaai
            { 15896, 2 },	//  #148 sc=Inscriptional_Pahlavi:Phli scx=Inscriptional_Pahlavi:Phli
            { 15898, 2 },	//  #149 sc=Inscriptional_Parthian:Prti scx=Inscriptional_Parthian:Prti
            { 15900, 3 },	//  #150 sc=Javanese:Java
            { 15903, 2 },	//  #151 sc=Kaithi:Kthi
            { 15905, 13 },	//  #152 sc=Kannada:Knda
            { 15918, 13 },	//  #153 sc=Katakana:Kana
            { 15931, 2 },	//  #154 sc=Kayah_Li:Kali
            { 15933, 8 },	//  #155 sc=Kharoshthi:Khar scx=Kharoshthi:Khar
            { 15941, 2 },	//  #156 sc=Khitan_Small_Script:Kits scx=Khitan_Small_Script:Kits
            { 15943, 4 },	//  #157 sc=Khmer:Khmr scx=Khmer:Khmr
            { 15947, 2 },	//  #158 sc=Khojki:Khoj
            { 15949, 2 },	//  #159 sc=Khudawadi:Sind
            { 15951, 11 },	//  #160 sc=Lao:Laoo scx=Lao:Laoo
            { 15962, 38 },	//  #161 sc=Latin:Latn
            { 16000, 3 },	//  #162 sc=Lepcha:Lepc scx=Lepcha:Lepc
            { 16003, 5 },	//  #163 sc=Limbu:Limb
            { 16008, 3 },	//  #164 sc=Linear_A:Lina
            { 16011, 7 },	//  #165 sc=Linear_B:Linb
            { 16018, 2 },	//  #166 sc=Lisu:Lisu scx=Lisu:Lisu
            { 16020, 1 },	//  #167 sc=Lycian:Lyci scx=Lycian:Lyci
            { 16021, 2 },	//  #168 sc=Lydian:Lydi scx=Lydian:Lydi
            { 16023, 1 },	//  #169 sc=Mahajani:Mahj
            { 16024, 1 },	//  #170 sc=Makasar:Maka scx=Makasar:Maka
            { 16025, 7 },	//  #171 sc=Malayalam:Mlym
            { 16032, 2 },	//  #172 sc=Mandaic:Mand
            { 16034, 2 },	//  #173 sc=Manichaean:Mani
            { 16036, 3 },	//  #174 sc=Marchen:Marc scx=Marchen:Marc
            { 16039, 7 },	//  #175 sc=Masaram_Gondi:Gonm
            { 16046, 1 },	//  #176 sc=Medefaidrin:Medf scx=Medefaidrin:Medf
            { 16047, 3 },	//  #177 sc=Meetei_Mayek:Mtei scx=Meetei_Mayek:Mtei
            { 16050, 2 },	//  #178 sc=Mende_Kikakui:Mend scx=Mende_Kikakui:Mend
            { 16052, 3 },	//  #179 sc=Meroitic_Cursive:Merc scx=Meroitic_Cursive:Merc
            { 16055, 1 },	//  #180 sc=Meroitic_Hieroglyphs:Mero scx=Meroitic_Hieroglyphs:Mero
            { 16056, 3 },	//  #181 sc=Miao:Plrd scx=Miao:Plrd
            { 16059, 2 },	//  #182 sc=Modi:Modi
            { 16061, 6 },	//  #183 sc=Mongolian:Mong
            { 16067, 3 },	//  #184 sc=Mro:Mroo scx=Mro:Mroo
            { 16070, 5 },	//  #185 sc=Multani:Mult
            { 16075, 3 },	//  #186 sc=Myanmar:Mymr
            { 16078, 2 },	//  #187 sc=Nabataean:Nbat scx=Nabataean:Nbat
            { 16080, 3 },	//  #188 sc=Nandinagari:Nand
            { 16083, 4 },	//  #189 sc=New_Tai_Lue:Talu scx=New_Tai_Lue:Talu
            { 16087, 2 },	//  #190 sc=Newa:Newa scx=Newa:Newa
            { 16089, 2 },	//  #191 sc=Nko:Nkoo
            { 16091, 2 },	//  #192 sc=Nushu:Nshu scx=Nushu:Nshu
            { 16093, 4 },	//  #193 sc=Nyiakeng_Puachue_Hmong:Hmnp scx=Nyiakeng_Puachue_Hmong:Hmnp
            { 16097, 1 },	//  #194 sc=Ogham:Ogam scx=Ogham:Ogam
            { 16098, 1 },	//  #195 sc=Ol_Chiki:Olck scx=Ol_Chiki:Olck
            { 16099, 3 },	//  #196 sc=Old_Hungarian:Hung scx=Old_Hungarian:Hung
            { 16102, 2 },	//  #197 sc=Old_Italic:Ital scx=Old_Italic:Ital
            { 16104, 1 },	//  #198 sc=Old_North_Arabian:Narb scx=Old_North_Arabian:Narb
            { 16105, 1 },	//  #199 sc=Old_Permic:Perm
            { 16106, 2 },	//  #200 sc=Old_Persian:Xpeo scx=Old_Persian:Xpeo
            { 16108, 1 },	//  #201 sc=Old_Sogdian:Sogo scx=Old_Sogdian:Sogo
            { 16109, 1 },	//  #202 sc=Old_South_Arabian:Sarb scx=Old_South_Arabian:Sarb
            { 16110, 1 },	//  #203 sc=Old_Turkic:Orkh scx=Old_Turkic:Orkh
            { 16111, 1 },	//  #204 sc=Old_Uyghur:Ougr
            { 16112, 14 },	//  #205 sc=Oriya:Orya
            { 16126, 2 },	//  #206 sc=Osage:Osge scx=Osage:Osge
            { 16128, 2 },	//  #207 sc=Osmanya:Osma scx=Osmanya:Osma
            { 16130, 5 },	//  #208 sc=Pahawh_Hmong:Hmng scx=Pahawh_Hmong:Hmng
            { 16135, 1 },	//  #209 sc=Palmyrene:Palm scx=Palmyrene:Palm
            { 16136, 1 },	//  #210 sc=Pau_Cin_Hau:Pauc scx=Pau_Cin_Hau:Pauc
            { 16137, 1 },	//  #211 sc=Phags_Pa:Phag
            { 16138, 2 },	//  #212 sc=Phoenician:Phnx scx=Phoenician:Phnx
            { 16140, 3 },	//  #213 sc=Psalter_Pahlavi:Phlp
            { 16143, 2 },	//  #214 sc=Rejang:Rjng scx=Rejang:Rjng
            { 16145, 2 },	//  #215 sc=Runic:Runr scx=Runic:Runr
            { 16147, 2 },	//  #216 sc=Samaritan:Samr scx=Samaritan:Samr
            { 16149, 2 },	//  #217 sc=Saurashtra:Saur scx=Saurashtra:Saur
            { 16151, 1 },	//  #218 sc=Sharada:Shrd
            { 16152, 1 },	//  #219 sc=Shavian:Shaw scx=Shavian:Shaw
            { 16153, 2 },	//  #220 sc=Siddham:Sidd scx=Siddham:Sidd
            { 16155, 3 },	//  #221 sc=SignWriting:Sgnw scx=SignWriting:Sgnw
            { 16158, 13 },	//  #222 sc=Sinhala:Sinh
            { 16171, 1 },	//  #223 sc=Sogdian:Sogd
            { 16172, 2 },	//  #224 sc=Sora_Sompeng:Sora scx=Sora_Sompeng:Sora
            { 16174, 1 },	//  #225 sc=Soyombo:Soyo scx=Soyombo:Soyo
            { 16175, 2 },	//  #226 sc=Sundanese:Sund scx=Sundanese:Sund
            { 16177, 1 },	//  #227 sc=Syloti_Nagri:Sylo
            { 16178, 4 },	//  #228 sc=Syriac:Syrc
            { 16182, 2 },	//  #229 sc=Tagalog:Tglg
            { 16184, 3 },	//  #230 sc=Tagbanwa:Tagb
            { 16187, 2 },	//  #231 sc=Tai_Le:Tale
            { 16189, 5 },	//  #232 sc=Tai_Tham:Lana scx=Tai_Tham:Lana
            { 16194, 2 },	//  #233 sc=Tai_Viet:Tavt scx=Tai_Viet:Tavt
            { 16196, 2 },	//  #234 sc=Takri:Takr
            { 16198, 18 },	//  #235 sc=Tamil:Taml
            { 16216, 2 },	//  #236 sc=Tangsa:Tnsa scx=Tangsa:Tnsa
            { 16218, 4 },	//  #237 sc=Tangut:Tang scx=Tangut:Tang
            { 16222, 13 },	//  #238 sc=Telugu:Telu
            { 16235, 1 },	//  #239 sc=Thaana:Thaa
            { 16236, 2 },	//  #240 sc=Thai:Thai scx=Thai:Thai
            { 16238, 7 },	//  #241 sc=Tibetan:Tibt scx=Tibetan:Tibt
            { 16245, 3 },	//  #242 sc=Tifinagh:Tfng scx=Tifinagh:Tfng
            { 16248, 2 },	//  #243 sc=Tirhuta:Tirh
            { 16250, 1 },	//  #244 sc=Toto scx=Toto
            { 16251, 2 },	//  #245 sc=Ugaritic:Ugar scx=Ugaritic:Ugar
            { 16253, 1 },	//  #246 sc=Vai:Vaii scx=Vai:Vaii
            { 16254, 8 },	//  #247 sc=Vithkuqi:Vith scx=Vithkuqi:Vith
            { 16262, 2 },	//  #248 sc=Wancho:Wcho scx=Wancho:Wcho
            { 16264, 2 },	//  #249 sc=Warang_Citi:Wara scx=Warang_Citi:Wara
            { 16266, 3 },	//  #250 sc=Yezidi:Yezi
            { 16269, 2 },	//  #251 sc=Yi:Yiii
            { 16271, 1 },	//  #252 sc=Zanabazar_Square:Zanb scx=Zanabazar_Square:Zanb
            { 16272, 5 },	//  #253 scx=Adlam:Adlm
            { 16277, 51 },	//  #254 scx=Arabic:Arab
            { 16328, 26 },	//  #255 scx=Bengali:Beng
            { 16354, 12 },	//  #256 scx=Bopomofo:Bopo
            { 16366, 3 },	//  #257 scx=Buginese:Bugi
            { 16369, 2 },	//  #258 scx=Buhid:Buhd
            { 16371, 4 },	//  #259 scx=Chakma:Cakm
            { 16375, 148 },	//  #260 scx=Common:Zyyy
            { 16523, 4 },	//  #261 scx=Coptic:Copt:Qaac
            { 16527, 2 },	//  #262 scx=Cypro_Minoan:Cpmn
            { 16529, 9 },	//  #263 scx=Cypriot:Cprt
            { 16538, 9 },	//  #264 scx=Cyrillic:Cyrl
            { 16547, 7 },	//  #265 scx=Devanagari:Deva
            { 16554, 3 },	//  #266 scx=Dogra:Dogr
            { 16557, 5 },	//  #267 scx=Duployan:Dupl
            { 16562, 9 },	//  #268 scx=Georgian:Geor
            { 16571, 10 },	//  #269 scx=Glagolitic:Glag
            { 16581, 25 },	//  #270 scx=Grantha:Gran
            { 16606, 38 },	//  #271 scx=Greek:Grek
            { 16644, 17 },	//  #272 scx=Gujarati:Gujr
            { 16661, 7 },	//  #273 scx=Gunjala_Gondi:Gong
            { 16668, 19 },	//  #274 scx=Gurmukhi:Guru
            { 16687, 37 },	//  #275 scx=Han:Hani
            { 16724, 21 },	//  #276 scx=Hangul:Hang
            { 16745, 7 },	//  #277 scx=Hanifi_Rohingya:Rohg
            { 16752, 1 },	//  #278 scx=Hanunoo:Hano
            { 16753, 16 },	//  #279 scx=Hiragana:Hira
            { 16769, 20 },	//  #280 scx=Inherited:Zinh:Qaai
            { 16789, 3 },	//  #281 scx=Javanese:Java
            { 16792, 4 },	//  #282 scx=Kaithi:Kthi
            { 16796, 21 },	//  #283 scx=Kannada:Knda
            { 16817, 19 },	//  #284 scx=Katakana:Kana
            { 16836, 1 },	//  #285 scx=Kayah_Li:Kali
            { 16837, 4 },	//  #286 scx=Khojki:Khoj
            { 16841, 4 },	//  #287 scx=Khudawadi:Sind
            { 16845, 46 },	//  #288 scx=Latin:Latn
            { 16891, 6 },	//  #289 scx=Limbu:Limb
            { 16897, 4 },	//  #290 scx=Linear_A:Lina
            { 16901, 10 },	//  #291 scx=Linear_B:Linb
            { 16911, 3 },	//  #292 scx=Mahajani:Mahj
            { 16914, 11 },	//  #293 scx=Malayalam:Mlym
            { 16925, 3 },	//  #294 scx=Mandaic:Mand
            { 16928, 3 },	//  #295 scx=Manichaean:Mani
            { 16931, 8 },	//  #296 scx=Masaram_Gondi:Gonm
            { 16939, 3 },	//  #297 scx=Modi:Modi
            { 16942, 5 },	//  #298 scx=Mongolian:Mong
            { 16947, 6 },	//  #299 scx=Multani:Mult
            { 16953, 4 },	//  #300 scx=Myanmar:Mymr
            { 16957, 9 },	//  #301 scx=Nandinagari:Nand
            { 16966, 6 },	//  #302 scx=Nko:Nkoo
            { 16972, 2 },	//  #303 scx=Old_Permic:Perm
            { 16974, 3 },	//  #304 scx=Old_Uyghur:Ougr
            { 16977, 18 },	//  #305 scx=Oriya:Orya
            { 16995, 3 },	//  #306 scx=Phags_Pa:Phag
            { 16998, 4 },	//  #307 scx=Psalter_Pahlavi:Phlp
            { 17002, 6 },	//  #308 scx=Sharada:Shrd
            { 17008, 14 },	//  #309 scx=Sinhala:Sinh
            { 17022, 2 },	//  #310 scx=Sogdian:Sogd
            { 17024, 3 },	//  #311 scx=Syloti_Nagri:Sylo
            { 17027, 12 },	//  #312 scx=Syriac:Syrc
            { 17039, 3 },	//  #313 scx=Tagalog:Tglg
            { 17042, 4 },	//  #314 scx=Tagbanwa:Tagb
            { 17046, 3 },	//  #315 scx=Tai_Le:Tale
            { 17049, 4 },	//  #316 scx=Takri:Takr
            { 17053, 25 },	//  #317 scx=Tamil:Taml
            { 17078, 17 },	//  #318 scx=Telugu:Telu
            { 17095, 7 },	//  #319 scx=Thaana:Thaa
            { 17102, 6 },	//  #320 scx=Tirhuta:Tirh
            { 17108, 7 },	//  #321 scx=Yezidi:Yezi
            { 17115, 7 }	//  #322 scx=Yi:Yiii
        };
#define SRELL_UPDATA_VERSION 110
        //  ... "srell_updata.hpp"]

        //template <typename PairType>
        class unicode_property {
        public:

            typedef uint_l32 property_type;
            typedef simple_array<char> pstring;

            static const property_type error_property = static_cast<property_type>(-1);

            unicode_property() {
            }

            unicode_property& operator=(const unicode_property&) {
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            unicode_property& operator=(unicode_property&&) SRELL_NOEXCEPT {
                return *this;
            }
#endif

            static property_type lookup_property(const pstring& name, const pstring& value) {
                pname_type ptype = name.size() ? lookup_property_name(name) : updata::ptype::general_category;
                property_type property_number = lookup_property_value(ptype, value);

                if (property_number == updata::unknown && name.size() == 0) {
                    ptype = updata::ptype::binary;
                    property_number = lookup_property_value(ptype, value);
                }

                return property_number != updata::unknown ? property_number : error_property;
            }

            static std::size_t ranges_offset(const property_type property_number) {
#if defined(SRELL_UPDATA_VERSION)
                return updata::positiontable[property_number].offset;
#else
                const offset_and_number* const postable = updata::position_table();
                return postable[property_number].offset;
#endif
            }

            static std::size_t number_of_ranges(const property_type property_number) {
#if defined(SRELL_UPDATA_VERSION)
                return updata::positiontable[property_number].number_of_pairs;
#else
                const offset_and_number* const postable = updata::position_table();
                return postable[property_number].number_of_pairs;
#endif
            }

            static const uchar32* ranges_address(const property_type property_number) {
#if defined(SRELL_UPDATA_VERSION)
                return &updata::rangetable[ranges_offset(property_number) << 1];
#else
                const uchar32* const ranges = updata::ranges();
                return &ranges[ranges_offset(property_number) << 1];
#endif
            }

        private:

            typedef uint_l32 pname_type;
            typedef const char* pname_string_type;

#if defined(SRELL_UPDATA_VERSION) && (SRELL_UPDATA_VERSION >= 200)
            struct pvalue_type {
                pname_type pname;
                property_type pnumber;
                pname_string_type csstrings;
            };
#else
            struct pvalue_type {
                pname_type pname;
                pname_string_type csstrings;
                property_type pnumber;
            };
#endif

            struct offset_and_number {
                std::size_t offset;
                std::size_t number_of_pairs;
            };

            typedef unicode_property_data<property_type,
                pname_type,
                pname_string_type,
                uchar32,
                pvalue_type,
                offset_and_number
            >
                updata;

            static pname_type lookup_property_name(const pstring& name) {
#if defined(SRELL_UPDATA_VERSION)
                for (std::size_t pno = 0; *updata::propertynametable[pno]; ++pno) {
                    if (check_if_included(name, updata::propertynametable[pno]))
                        return static_cast<pname_type>(pno);
                }
#else
                const pname_string_type* const pname_table = updata::propertyname_table();

                for (std::size_t pno = 0; *pname_table[pno]; ++pno) {
                    if (check_if_included(name, pname_table[pno]))
                        return static_cast<pname_type>(pno);
                }
#endif
                return updata::ptype::unknown;
            }

            //  Checks if value is included in colon-separated strings.
            static bool check_if_included(const pstring& value, pname_string_type csstrings) {
                if (static_cast<uchar32>(*csstrings) != meta_char::mc_astrsk)	//  '*'
                {
                    while (*csstrings) {
                        const pname_string_type begin = csstrings;

                        for (; static_cast<uchar32>(*csstrings) != meta_char::mc_colon && static_cast<uchar32>(*csstrings) != char_ctrl::cc_nul; ++csstrings);

                        const std::size_t length = csstrings - begin;

                        if (static_cast<std::size_t>(value.size()) == length)
                            if (value.compare(0, value.size(), begin, length) == 0)
                                return true;

                        if (static_cast<uchar32>(*csstrings) == meta_char::mc_colon)
                            ++csstrings;
                    }
                }
                return false;
            }

            static property_type lookup_property_value(const pname_type ptype, const pstring& value) {
#if defined(SRELL_UPDATA_VERSION)
                for (std::size_t pno = 0; *updata::rangenumbertable[pno].csstrings; ++pno) {
                    const pvalue_type& pvalue = updata::rangenumbertable[pno];
                    if (pvalue.pname == ptype && check_if_included(value, pvalue.csstrings))
                        return pvalue.pnumber;
                }
#else
                const pvalue_type* const pvalue_table = updata::rangenumber_table();

                for (std::size_t pno = 0; *pvalue_table[pno].csstrings; ++pno) {
                    const pvalue_type& pvalue = pvalue_table[pno];
                    if (pvalue.pname == ptype && check_if_included(value, pvalue.csstrings))
                        return pvalue.pnumber;
                }
#endif
                return updata::unknown;
            }

        public:

            static const std::size_t number_of_properties = updata::last_property_number + 1;
            static const std::size_t last_property_number = updata::last_property_number;
#if defined(SRELL_UPDATA_VERSION) && (SRELL_UPDATA_VERSION >= 200)
            static const std::size_t last_pos_number = updata::last_pos_number;
#endif
            static const property_type gc_Zs = updata::gc_Space_Separator;
            static const property_type gc_Cn = updata::gc_Unassigned;
            static const property_type bp_Assigned = updata::bp_Assigned;

            //  UnicodeIDStart::
            //  UnicodeIDContinue::
            static const property_type bp_ID_Start = updata::bp_ID_Start;
            static const property_type bp_ID_Continue = updata::bp_ID_Continue;
        };
        //  unicode_property

#endif	//  !defined(SRELL_NO_UNICODE_PROPERTY)
    }	//  namespace regex_internal

//  ... "rei_up.hpp"]
//  ["rei_char_class.hpp" ...

    namespace regex_internal {

        struct range_pair	//  , public std::pair<charT, charT>
        {
            uchar32 second;
            uchar32 first;

            void set(const uchar32 min, const uchar32 max) {
                this->first = min;
                this->second = max;
            }

            bool is_range_valid() const {
                return first <= second;
            }

            bool operator==(const range_pair& right) const {
                return this->first == right.first && this->second == right.second;
            }

            bool operator<(const range_pair& right) const {
                return this->second < right.first;	//  This assumes that optimise() has been called.
            }

            void swap(range_pair& right) {
                const range_pair tmp = *this;
                *this = right;
                right = tmp;
            }

            bool unify_range(const range_pair& right) {
                range_pair& left = *this;

                if (right.first <= left.second || left.second + 1 == right.first)	//  r1 <= l2 || l2+1 == r1
                {
                    //  l1 l2+1 < r1 r2 excluded.

                    if (left.first <= right.second || right.second + 1 == left.first)	//  l1 <= r2 || r2+1 == l1
                    {
                        //  r1 r2+1 < l1 l2 excluded.

                        if (left.first > right.first)
                            left.first = right.first;

                        if (left.second < right.second)
                            left.second = right.second;

                        return true;
                    }
                }
                return false;
            }
        };
        //  range_pair

        struct range_pair_helper : public range_pair {
            range_pair_helper(const uchar32 min, const uchar32 max) {
                this->first = min;
                this->second = max;
            }

            range_pair_helper(const uchar32 minmax) {
                this->first = minmax;
                this->second = minmax;
            }
        };
        //  range_pair_helper

        struct range_pairs	//  : public simple_array<range_pair>
        {
        public:

            typedef simple_array<range_pair> array_type;
            typedef array_type::size_type size_type;

            range_pairs() {
            }

            range_pairs(const range_pairs& rp) : rparray_(rp.rparray_) {
            }

            range_pairs& operator=(const range_pairs& rp) {
                rparray_.operator=(rp.rparray_);
                return *this;
            }

            range_pairs(const size_type initsize) : rparray_(initsize) {
            }

            range_pairs(const range_pairs& right, size_type pos, size_type size)
                : rparray_(right.rparray_, pos, size) {
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            range_pairs(range_pairs&& rp) SRELL_NOEXCEPT
                : rparray_(std::move(rp.rparray_)) {
            }

            range_pairs& operator=(range_pairs&& rp) SRELL_NOEXCEPT {
                rparray_.operator=(std::move(rp.rparray_));
                return *this;
            }
#endif

            void clear() {
                rparray_.clear();
            }

            size_type size() const {
                return rparray_.size();
            }

            const range_pair& operator[](const size_type pos) const {
                return rparray_[pos];
            }
            range_pair& operator[](const size_type pos) {
                return rparray_[pos];
            }

            void resize(const size_type size) {
                rparray_.resize(size);
            }

            void swap(range_pairs& right) {
                rparray_.swap(right.rparray_);
            }

            void set_solerange(const range_pair& right) {
                rparray_.clear();
                rparray_.push_back(right);
            }

            void append_newclass(const range_pairs& right) {
                rparray_.append(right.rparray_);
            }

            void append_newpair(const range_pair& right) {
                rparray_.push_back(right);
            }

            void join(const range_pair& right) {
                size_type pos = 0;

                for (; pos < rparray_.size(); ++pos) {
                    range_pair& curpair = rparray_[pos];

                    if (curpair.unify_range(right)) {
                        for (++pos; pos < rparray_.size();) {
                            if (curpair.unify_range(rparray_[pos]))
                                rparray_.erase(pos);
                            else
                                break;
                        }
                        return;
                    }
                    if (right.second < curpair.first)
                        break;
                }
                rparray_.insert(pos, right);
            }

            void merge(const range_pairs& right) {
                for (size_type i = 0; i < right.size(); ++i)
                    join(right[i]);
            }

            bool same(uchar32 pos, const uchar32 count, const range_pairs& right) const {
                if (count == right.size()) {
                    for (uchar32 i = 0; i < count; ++i, ++pos)
                        if (!(rparray_[pos] == right[i]))
                            return false;

                    return true;
                }
                return false;
            }

            int relationship(const range_pairs& right) const {
                if (rparray_.size() == right.rparray_.size()) {
                    for (size_type i = 0; i < rparray_.size(); ++i) {
                        if (!(this->rparray_[i] == right.rparray_[i])) {
                            if (i == 0)
                                goto check_overlap;

                            return 1;	//  Overlapped.
                        }
                    }
                    return 0;	//  Same.
                }
            check_overlap:
                return is_overlap(right) ? 1 : 2;	//  Overlapped or exclusive.
            }

            void negation() {
                uchar32 begin = 0;
                range_pairs newpairs;

                for (size_type i = 0; i < rparray_.size(); ++i) {
                    const range_pair& range = rparray_[i];

                    if (begin < range.first)
                        newpairs.join(range_pair_helper(begin, range.first - 1));

                    begin = range.second + 1;
                }

                if (begin <= constants::unicode_max_codepoint)
                    newpairs.join(range_pair_helper(begin, constants::unicode_max_codepoint));

                *this = newpairs;
            }

            bool is_overlap(const range_pairs& right) const {
                for (size_type i = 0; i < rparray_.size(); ++i) {
                    const range_pair& leftrange = rparray_[i];

                    for (size_type j = 0; j < right.size(); ++j) {
                        const range_pair& rightrange = right[j];

                        if (rightrange.first <= leftrange.second)	//  Excludes l1 l2 < r1 r2.
                            if (leftrange.first <= rightrange.second)	//  Excludes r1 r2 < l1 l2.
                                return true;
                    }
                }
                return false;
            }

            void load_from_memory(const uchar32* array, size_type number_of_pairs) {
                for (; number_of_pairs; --number_of_pairs, array += 2)
                    join(range_pair_helper(array[0], array[1]));
            }

            void make_caseunfoldedcharset() {
                uchar32 table[unicode_case_folding::rev_maxset] = {};
                bitset<constants::unicode_max_codepoint + 1> bs;

                for (size_type i = 0; i < rparray_.size(); ++i) {
                    const range_pair& range = rparray_[i];

                    for (uchar32 ucp = range.first; ucp <= range.second; ++ucp) {
                        const uchar32 setnum = unicode_case_folding::casefoldedcharset(table, ucp);

                        for (uchar32 j = 0; j < setnum; ++j)
                            bs.set(table[j]);
                    }
                }
                load_from_bitset(bs);
            }

            //  For updataout.hpp.
            void remove_range(const range_pair& right) {
                for (size_type pos = 0; pos < rparray_.size();) {
                    range_pair& left = rparray_[pos];

                    if (right.first <= left.first && left.first <= right.second)	//  r1 <= l1 <= r2.
                    {
                        if (left.second > right.second)	//  r1 <= l1 <= r2 < l2.
                        {
                            left.first = right.second + 1;	//  carry doesn't happen.
                            ++pos;
                        } else	//  r1 <= l1 <= l2 <= r2.
                            rparray_.erase(pos);
                    } else if (right.first <= left.second && left.second <= right.second)	//  r1 <= l2 <= r2.
                    {
                        if (left.first < right.first)	//  l1 < r1 <= l2 <= r2.
                        {
                            left.second = right.first - 1;
                            ++pos;
                        } else	//  r1 <= l1 <= l2 <= r2.
                            rparray_.erase(pos);
                    } else if (left.first < right.first && right.second < left.second)	//  l1 < r1 && r2 < l2.
                    {
                        range_pair newrange(left);

                        left.second = right.first - 1;
                        newrange.first = right.second + 1;
                        rparray_.insert(++pos, newrange);
                        ++pos;
                    } else
                        ++pos;
                }
            }

            //	template <typename ucf>
            uchar32 consists_of_one_character(const bool icase) const {
                if (rparray_.size() >= 1) {
                    uchar32(* const casefolding_func)(const uchar32) = !icase ? do_nothing : unicode_case_folding::do_casefolding;
                    const uchar32 ucp1st = casefolding_func(rparray_[0].first);

                    for (size_type no = 0; no < rparray_.size(); ++no) {
                        const range_pair& cr = rparray_[no];

                        for (uchar32 ucp = cr.first;; ++ucp) {
                            if (ucp1st != casefolding_func(ucp))
                                return constants::invalid_u32value;

                            if (ucp == cr.second)
                                break;
                        }
                    }
                    return ucp1st;
                }
                return constants::invalid_u32value;
            }

            void split_ranges(range_pairs& kept, range_pairs& removed, const range_pairs& rightranges) const {
                range_pair newpair;

                kept.rparray_ = this->rparray_;	//  Subtraction set.
                removed.clear();	//  Intersection set.

                for (size_type i = 0;; ++i) {
                RETRY_SAMEINDEXNO:
                    if (i >= kept.rparray_.size())
                        break;

                    range_pair& left = kept.rparray_[i];

                    for (size_type j = 0; j < rightranges.rparray_.size(); ++j) {
                        const range_pair& right = rightranges.rparray_[j];

                        if (right.first <= left.second)	//  Excludes l1 l2 < r1 r2.
                        {
                            if (left.first <= right.second)	//  Excludes r1 r2 < l1 l2.
                            {
                                if (left.first < right.first) {
                                    if (right.second < left.second) {
                                        removed.join(range_pair_helper(right.first, right.second));

                                        newpair.set(right.second + 1, left.second);
                                        left.second = right.first - 1;
                                        kept.rparray_.insert(i + 1, newpair);
                                    } else {
                                        removed.join(range_pair_helper(right.first, left.second));
                                        left.second = right.first - 1;
                                    }
                                } else if (right.second < left.second) {
                                    removed.join(range_pair_helper(left.first, right.second));
                                    left.first = right.second + 1;
                                } else {
                                    removed.join(range_pair_helper(left.first, left.second));
                                    kept.rparray_.erase(i);
                                    goto RETRY_SAMEINDEXNO;
                                }
                            }
                        } else
                            break;
                    }
                }
            }

#if defined(SRELLDBG_NO_BITSET)
            bool is_included(const uchar32 ch) const {
#if 01
                const range_pair* const end = rparray_.data() + rparray_.size();

                for (const range_pair* cur = rparray_.data(); cur != end; ++cur) {
                    if (ch <= cur->second)
                        return ch >= cur->first;
#else
                for (size_type i = 0; i < rparray_.size(); ++i) {
                    if (rparray_[i].is_included(ch))
                        return true;
#endif
                }
                return false;
                }
#endif	//  defined(SRELLDBG_NO_BITSET)

            //  For multiple_range_pairs functions.

            bool is_included_ls(const uchar32 pos, uchar32 count, const uchar32 c) const {
                const range_pair* cur = &rparray_[pos];

                for (; count; ++cur, --count) {
                    if (c <= cur->second)
                        return c >= cur->first;
                }
                return false;
            }

            bool is_included(const uchar32 pos, uchar32 count, const uchar32 c) const {
                const range_pair* base = &rparray_[pos];

                while (count) {
                    uchar32 mid = count >> 1;
                    const range_pair& rp = base[mid];

                    if (c <= rp.second) {
                        if (c >= rp.first)
                            return true;

                        count = mid;
                    } else {
                        ++mid;
                        count -= mid;
                        base += mid;
                    }
                }
                return false;
            }

            void replace(const size_type pos, const size_type count, const range_pairs & right) {
                rparray_.replace(pos, count, right.rparray_);
            }

#if !defined(SRELLDBG_NO_CCPOS)

            //  For Eytzinger layout functions.

            bool is_included_el(uchar32 pos, const uchar32 len, const uchar32 c) const {
                const range_pair* const base = &rparray_[pos];

#if defined(__GNUC__)
                __builtin_prefetch(base);
#endif
                for (pos = 0; pos < len;) {
                    const range_pair& rp = base[pos];

                    if (c <= rp.second) {
                        if (c >= rp.first)
                            return true;

                        pos = (pos << 1) + 1;
                    } else {
                        pos = (pos << 1) + 2;
                    }
                }
                return false;
            }

            uchar32 create_el(const range_pair * srcbase, const uchar32 srcsize) {
                const uchar32 basepos = static_cast<uchar32>(rparray_.size());

                rparray_.resize(basepos + srcsize);
                set_eytzinger_layout(0, srcbase, srcsize, &rparray_[basepos], 0);

                return srcsize;
            }

#endif	//  !defined(SRELLDBG_NO_CCPOS)

            uint_l32 total_codepoints() const {
                uint_l32 num = 0;

                for (size_type no = 0; no < rparray_.size(); ++no) {
                    const range_pair& cr = rparray_[no];

                    num += cr.second - cr.first + 1;
                }
                return num;
            }

        private:

#if !defined(SRELLDBG_NO_CCPOS)

            uchar32 set_eytzinger_layout(uchar32 srcpos, const range_pair* const srcbase, const uchar32 srclen,
                range_pair* const destbase, const uchar32 destpos) {
                if (destpos < srclen) {
                    const uchar32 nextpos = (destpos << 1) + 1;

                    srcpos = set_eytzinger_layout(srcpos, srcbase, srclen, destbase, nextpos);
                    destbase[destpos] = srcbase[srcpos++];
                    srcpos = set_eytzinger_layout(srcpos, srcbase, srclen, destbase, nextpos + 1);
                }
                return srcpos;
            }

#endif	//  !defined(SRELLDBG_NO_CCPOS)

            static uchar32 do_nothing(const uchar32 cp) {
                return cp;
            }

            template <typename BitSetT>
            void load_from_bitset(const BitSetT & bs) {
                uchar32 begin = constants::invalid_u32value;
                range_pairs newranges;

                for (uchar32 ucp = 0;; ++ucp) {
                    if (ucp > constants::unicode_max_codepoint || !bs.test(ucp)) {
                        if (begin != constants::invalid_u32value) {
                            newranges.join(range_pair_helper(begin, ucp - 1));
                            begin = constants::invalid_u32value;
                        }
                        if (ucp > constants::unicode_max_codepoint)
                            break;
                    } else if (begin == constants::invalid_u32value && bs.test(ucp))
                        begin = ucp;
                }
                rparray_.swap(newranges.rparray_);
            }

            array_type rparray_;

        public:	//  For debug.

            void print_pairs(const int, const char* const = NULL, const char* const = NULL) const;
            };
        //  range_pairs

#if !defined(SRELL_NO_UNICODE_PROPERTY)

//  For RegExpIdentifierStart and RegExpIdentifierPart
        struct identifier_charclass {
        public:

            void clear() {
                char_class_.clear();
                char_class_pos_.clear();
            }

            void setup() {
                if (char_class_pos_.size() == 0) {
                    static const uchar32 additions[] = {
                        //  reg_exp_identifier_start, reg_exp_identifier_part.
                        0x24, 0x24, 0x5f, 0x5f, 0x200c, 0x200d	//  '$' '_' <ZWNJ>-<ZWJ>
                    };
                    range_pairs ranges;

                    //  For reg_exp_identifier_start.
                    {
                        const uchar32* const IDs_address = unicode_property::ranges_address(unicode_property::bp_ID_Start);
                        const std::size_t IDs_number = unicode_property::number_of_ranges(unicode_property::bp_ID_Start);
                        ranges.load_from_memory(IDs_address, IDs_number);
                    }
                    ranges.load_from_memory(&additions[0], 2);
                    append_charclass(ranges);

                    //  For reg_exp_identifier_part.
                    ranges.clear();
                    {
                        const uchar32* const IDc_address = unicode_property::ranges_address(unicode_property::bp_ID_Continue);
                        const std::size_t IDc_number = unicode_property::number_of_ranges(unicode_property::bp_ID_Continue);
                        ranges.load_from_memory(IDc_address, IDc_number);
                    }
                    ranges.load_from_memory(&additions[0], 3);
                    append_charclass(ranges);
                }
            }

            bool is_identifier(const uchar32 ch, const bool part) const {
                const range_pair& rp = char_class_pos_[part ? 1 : 0];

                return char_class_.is_included(rp.first, rp.second, ch);
            }

        private:

            void append_charclass(const range_pairs& rps) {
                char_class_pos_.push_back(range_pair_helper(static_cast<uchar32>(char_class_.size()), static_cast<uchar32>(rps.size())));
                char_class_.append_newclass(rps);
            }

            range_pairs char_class_;
            range_pairs::array_type char_class_pos_;
        };
        //  identifier_charclass
#endif	//  !defined(SRELL_NO_UNICODE_PROPERTY)

        class re_character_class {
        public:

            enum {	//    0       1      2      3     4           5
                newline, dotall, space, digit, word, icase_word,
                //                6
                number_of_predefcls
            };
            static const uint_l32 error_property = static_cast<uint_l32>(-1);

#if !defined(SRELL_NO_UNICODE_PROPERTY)
            typedef unicode_property::pstring pstring;
#endif

            re_character_class() {
                setup_predefinedclass();
            }

            re_character_class& operator=(const re_character_class& that) {
                if (this != &that) {
                    this->char_class_ = that.char_class_;
                    this->char_class_pos_ = that.char_class_pos_;
#if !defined(SRELLDBG_NO_CCPOS)
                    this->char_class_el_ = that.char_class_el_;
                    this->char_class_pos_el_ = that.char_class_pos_el_;
#endif
                }
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            re_character_class& operator=(re_character_class&& that) SRELL_NOEXCEPT {
                if (this != &that) {
                    this->char_class_ = std::move(that.char_class_);
                    this->char_class_pos_ = std::move(that.char_class_pos_);
#if !defined(SRELLDBG_NO_CCPOS)
                    this->char_class_el_ = std::move(that.char_class_el_);
                    this->char_class_pos_el_ = std::move(that.char_class_pos_el_);
#endif
                }
                return *this;
            }
#endif

            bool is_included(const uint_l32 class_number, const uchar32 c) const {
                //		return char_class_.is_included(char_class_pos_[class_number], c);
                const range_pair& rp = char_class_pos_[class_number];

                return char_class_.is_included(rp.first, rp.second, c);
            }

#if !defined(SRELLDBG_NO_CCPOS)
            //	bool is_included(const uint_l32 pos, const uint_l32 len, const uchar32 &c) const
            bool is_included(const uchar32 pos, const uchar32 len, const uchar32 c) const {
                return char_class_el_.is_included_el(pos, len, c);
            }
#endif

            void setup_icase_word() {
                range_pair& icase_pos = char_class_pos_[icase_word];

                if (icase_pos.second == char_class_pos_[word].second) {
                    range_pairs icasewordclass(char_class_, icase_pos.first, icase_pos.second);

                    icasewordclass.make_caseunfoldedcharset();
                    //  Includes 017f and 212a so that they and their case-folded
                    //  characters 's' and 'k' will be excluded from the character
                    //  set that /[\W]/i matches.

                    char_class_.replace(icase_pos.first, icase_pos.second, icasewordclass);

                    if (icase_pos.second < static_cast<uchar32>(icasewordclass.size())) {
                        const uchar32 delta = static_cast<uchar32>(icasewordclass.size() - icase_pos.second);

                        for (int i = number_of_predefcls; i < static_cast<int>(char_class_pos_.size()); ++i)
                            char_class_pos_[i].first += delta;
                    }
                    icase_pos.second = static_cast<uchar32>(icasewordclass.size());
                }
            }

            void clear() {
                char_class_pos_.resize(number_of_predefcls);

                uchar32 basesize = 0;
                for (int i = 0; i < number_of_predefcls; ++i)
                    basesize += char_class_pos_[i].second;

                char_class_.resize(basesize);

#if !defined(SRELLDBG_NO_CCPOS)
                char_class_el_.clear();
                char_class_pos_el_.clear();
#endif
            }

            uint_l32 register_newclass(const range_pairs& rps) {
                for (range_pairs::size_type no = 0; no < char_class_pos_.size(); ++no) {
                    const range_pair& rp = char_class_pos_[no];

                    if (char_class_.same(rp.first, rp.second, rps))
                        return static_cast<uint_l32>(no);
                }

                append_charclass(rps);
                return static_cast<uint_l32>(char_class_pos_.size() - 1);
            }

            range_pairs operator[](const uint_l32 no) const {
                const range_pair& ccpos = char_class_pos_[no];
                range_pairs rp(ccpos.second);

                for (uchar32 i = 0; i < ccpos.second; ++i)
                    rp[i] = char_class_[ccpos.first + i];

                return rp;
            }

#if !defined(SRELLDBG_NO_CCPOS)
            const range_pair& charclasspos(const uint_l32 no)	//  const
            {
                const range_pair& pos = char_class_pos_el_[no];

                if (pos.second == 0)
                    finalise(no);
                return pos;
            }

            void finalise() {
                char_class_el_.clear();
                char_class_pos_el_.resize(char_class_pos_.size());
                std::memset(&char_class_pos_el_[0], 0, char_class_pos_el_.size() * sizeof(range_pairs::array_type::value_type));
            }

            void finalise(const uint_l32 no) {
                const range_pair& posinfo = char_class_pos_[no];
                range_pair& outpair = char_class_pos_el_[no];

                outpair.first = static_cast<uchar32>(char_class_el_.size());
                outpair.second = char_class_el_.create_el(&char_class_[posinfo.first], posinfo.second);	//arraysize;

            }

#endif	//  #if !defined(SRELLDBG_NO_CCPOS)

            void optimise() {
            }

#if !defined(SRELL_NO_UNICODE_PROPERTY)

            uint_l32 lookup_property(const pstring& pname, const pstring& pvalue, const bool icase) {
                const uint_l32 property_number = static_cast<uint_l32>(unicode_property::lookup_property(pname, pvalue));

                if (property_number != unicode_property::error_property && property_number < unicode_property::number_of_properties) {
                    const uint_l32 charclass_number = register_property_as_charclass(property_number, icase);
                    return charclass_number;
                }
                return error_property;
            }

#endif	//  !defined(SRELL_NO_UNICODE_PROPERTY)

            void swap(re_character_class& right) {
                if (this != &right) {
                    this->char_class_.swap(right.char_class_);
                    this->char_class_pos_.swap(right.char_class_pos_);
#if !defined(SRELLDBG_NO_CCPOS)
                    this->char_class_el_.swap(right.char_class_el_);
                    this->char_class_pos_el_.swap(right.char_class_pos_el_);
#endif
                }
            }

        private:

#if !defined(SRELL_NO_UNICODE_PROPERTY)

            uint_l32 register_property_as_charclass(const uint_l32 property_number, const bool icase) {
                if (property_number == unicode_property::bp_Assigned) {
                    //  \p{Assigned} == \P{Cn}
                    return load_updata_and_register_as_charclass(unicode_property::gc_Cn, false, true);
                }
                return load_updata_and_register_as_charclass(property_number, icase, false);
            }

            uint_l32 load_updata_and_register_as_charclass(const uint_l32 property_number, const bool /* icase */, const bool negation) {
                const uchar32* const address = unicode_property::ranges_address(property_number);
                //		const std::size_t offset = unicode_property::ranges_offset(property_number);
                const std::size_t number = unicode_property::number_of_ranges(property_number);
                range_pairs newranges;

                newranges.load_from_memory(address, number);

                if (negation)
                    newranges.negation();

                return register_newclass(newranges);
            }

#endif	//  !defined(SRELL_NO_UNICODE_PROPERTY)

            void append_charclass(const range_pairs& rps) {
                char_class_pos_.push_back(range_pair_helper(static_cast<uchar32>(char_class_.size()), static_cast<uchar32>(rps.size())));
                char_class_.append_newclass(rps);
            }

            //  The production CharacterClassEscape::s  evaluates as follows:
            //    Return the set of characters containing the characters that are on the right-hand side of the WhiteSpace or LineTerminator productions.
            //  WhiteSpace::<TAB> <VT> <FF> <SP> <NBSP> <ZWNBSP> <USP>
            //               0009 000B 000C 0020   00A0     FEFF    Zs
            //  LineTerminator::<LF> <CR> <LS> <PS>
            //                  000A 000D 2028 2029

            void setup_predefinedclass() {
#if !defined(SRELL_NO_UNICODE_PROPERTY)
                const uchar32* const Zs_address = unicode_property::ranges_address(unicode_property::gc_Zs);
                //		const std::size_t Zs_offset = unicode_property::ranges_offset(unicode_property::gc_Zs);
                const std::size_t Zs_number = unicode_property::number_of_ranges(unicode_property::gc_Zs);
#else
                static const uchar32 Zs[] = {
                    0x1680, 0x1680, 0x2000, 0x200a,	// 0x2028, 0x2029,
                    0x202f, 0x202f, 0x205f, 0x205f, 0x3000, 0x3000
                };
#endif	//  defined(SRELL_NO_UNICODE_PROPERTY)
                static const uchar32 allranges[] = {
                    //  dotall.
                    0x0000, 0x10ffff,
                    //  newline.
                    0x0a, 0x0a, 0x0d, 0x0d,	//  \n \r
                    //  newline, space.
                    0x2028, 0x2029,
                    //  space.
                    0x09, 0x0d,	//  \t \n \v \f \r
                    0x20, 0x20,	//  ' '
                    0xa0, 0xa0,	//  <NBSP>
                    0xfeff, 0xfeff,	//  <BOM>
                    //  digit, word.
                    0x30, 0x39,	//  '0'-'9'
                    0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a	//  'A'-'Z' '_' 'a'-'z'
                };
                range_pairs ranges;

                //  newline.
                ranges.load_from_memory(&allranges[2], 3);
                append_charclass(ranges);

                //  dotall.
                ranges.clear();
                ranges.load_from_memory(&allranges[0], 1);
                append_charclass(ranges);

                //  space.
                ranges.clear();
                ranges.load_from_memory(&allranges[6], 5);
#if !defined(SRELL_NO_UNICODE_PROPERTY)
                ranges.load_from_memory(Zs_address, Zs_number);
#else
                ranges.load_from_memory(Zs, 5);
#endif
                append_charclass(ranges);

                //  digit.
                ranges.clear();
                ranges.load_from_memory(&allranges[16], 1);
                append_charclass(ranges);

                //  word.
                ranges.clear();
                ranges.load_from_memory(&allranges[16], 4);
                append_charclass(ranges);

                //  Reservation for icase_word.
                append_charclass(ranges);
            }

        private:

            range_pairs char_class_;
            range_pairs::array_type char_class_pos_;

#if !defined(SRELLDBG_NO_CCPOS)
            range_pairs char_class_el_;
            range_pairs::array_type char_class_pos_el_;

#endif

        public:	//  For debug.

            void print_classes(const int) const;
        };
        //  re_character_class

        }	//  namespace regex_internal

    //  ... "rei_char_class.hpp"]
    //  ["rei_groupname_mapper.hpp" ...

    namespace regex_internal {

#if !defined(SRELL_NO_NAMEDCAPTURE)

        template <typename charT, typename numberT>
        class groupname_and_backrefnumber_mapper {
        public:

            typedef simple_array<charT> gname_string;
            typedef typename gname_string::size_type size_type;
            static const numberT notfound = static_cast<numberT>(-1);

            groupname_and_backrefnumber_mapper() {
            }

            groupname_and_backrefnumber_mapper(const groupname_and_backrefnumber_mapper& right)
                : names_(right.names_), keysize_classno_(right.keysize_classno_) {
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            groupname_and_backrefnumber_mapper(groupname_and_backrefnumber_mapper&& right) SRELL_NOEXCEPT
                : names_(std::move(right.names_)), keysize_classno_(std::move(right.keysize_classno_)) {
            }
#endif

            groupname_and_backrefnumber_mapper& operator=(const groupname_and_backrefnumber_mapper& right) {
                if (this != &right) {
                    names_ = right.names_;
                    keysize_classno_ = right.keysize_classno_;
                }
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            groupname_and_backrefnumber_mapper& operator=(groupname_and_backrefnumber_mapper&& right) SRELL_NOEXCEPT {
                if (this != &right) {
                    names_ = std::move(right.names_);
                    keysize_classno_ = std::move(right.keysize_classno_);
                }
                return *this;
            }
#endif

            void clear() {
                names_.clear();
                keysize_classno_.clear();
            }

            numberT operator[](const gname_string& gname) const {
                numberT pos = 0;
                for (std::size_t i = 0; i < static_cast<std::size_t>(keysize_classno_.size()); i += 2) {
                    const numberT keysize = keysize_classno_[i];

                    if (keysize == static_cast<numberT>(gname.size()) && sameseq(pos, gname))
                        return keysize_classno_[++i];

                    pos += keysize;
                }
                return notfound;
            }

            gname_string operator[](const numberT indexno) const {
                numberT pos = 0;
                for (std::size_t i = 0; i < static_cast<std::size_t>(keysize_classno_.size()); ++i) {
                    const numberT keysize = keysize_classno_[i];
                    const numberT classno = keysize_classno_[++i];

                    if (classno == indexno)
                        return gname_string(names_, pos, keysize);

                    pos += keysize;
                }
                return gname_string();
            }

            size_type size() const {
                return static_cast<size_type>(keysize_classno_.size() >> 1);
            }

            bool push_back(const gname_string& gname, const numberT class_number) {
                const numberT num = operator[](gname);

                if (num == notfound) {
                    names_.append(gname);
                    keysize_classno_.append(1, static_cast<numberT>(gname.size()));
                    keysize_classno_.append(1, class_number);
                    return true;
                }
                return false;	//  Already exists.
            }

            void swap(groupname_and_backrefnumber_mapper& right) {
                this->names_.swap(right.names_);
                keysize_classno_.swap(right.keysize_classno_);
            }

        private:

            bool sameseq(size_type pos, const gname_string& gname) const {
                for (size_type i = 0; i < gname.size(); ++i, ++pos)
                    if (pos >= names_.size() || names_[pos] != gname[i])
                        return false;

                return true;
            }

            gname_string names_;
            simple_array<numberT> keysize_classno_;

        public:	//  For debug.

            void print_mappings(const int) const;
        };
        //  groupname_and_backrefnumber_mapper

        template <typename charT>
        class groupname_mapper : public groupname_and_backrefnumber_mapper<charT, uint_l32> {
        };

#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)

    }	//  namespace regex_internal

//  ... "rei_groupname_mapper.hpp"]
//  ["rei_state.hpp" ...

    namespace regex_internal {

        struct re_quantifier {
            static const uint_l32 infinity = static_cast<uint_l32>(~0);

            //  atleast and atmost: for check_counter.
            //  offset and length: for charcter_class.
            //  (Special case 1) in roundbracket_open and roundbracket_pop atleast and atmost represent
            //    the minimum and maximum bracket numbers respectively inside the brackets itself.
            //  (Special case 2) in repeat_in_push and repeat_in_pop atleast and atmost represent the
            //    minimum and maximum bracket numbers respectively inside the repetition.
            union {
                uint_l32 atleast;
                //  (Special case 3: v1) in lookaround_open represents the number of characters to be rewound.
                //  (Special case 3: v2) in lookaround_open represents: 0=lookaheads, 1=lookbehinds,
                //    2=matchpointrewinder.
                //  (Special case 4) in NFA_states[0] represents the class number of the first character class.
                uchar32 offset;
            };
            union {
                uint_l32 atmost;
                uchar32 length;
            };

            union {
                bool is_greedy;
                uint_l32 padding_;
            };

            void reset(const uint_l32 len = 1) {
                atleast = atmost = len;
                is_greedy = true;
            }

            void set(const uint_l32 min, const uint_l32 max) {
                atleast = min;
                atmost = max;
            }

            void set(const uint_l32 min, const uint_l32 max, const bool greedy) {
                atleast = min;
                atmost = max;
                is_greedy = greedy;
            }

            void setccpos(const uchar32 o, const uchar32 l) {
                offset = o;
                length = l;
            }

            bool is_valid() const {
                return atleast <= atmost && atmost > 0;
            }

            void set_infinity() {
                atmost = infinity;
            }

            bool is_infinity() const {
                return atmost == infinity;
            }

            bool is_same() const {
                return atleast == atmost;
            }

            bool is_default() const {
                return atleast == 1 && atmost == 1;
            }

            bool is_asterisk() const {
                return atleast == 0 && atmost == infinity;
            }
            bool is_plus() const {
                return atleast == 1 && atmost == infinity;
            }
            bool is_asterisk_or_plus() const {
                return atleast <= 1 && atmost == infinity;
            }
            bool is_question_or_asterisk() const {
                return atleast == 0 && (atmost == 1 || atmost == infinity);
            }

            bool has_simple_equivalence() const {
                return (atleast <= 1 && atmost <= 3) || (atleast == 2 && atmost <= 4) || (atleast == atmost && atmost <= 6);
            }

            void multiply(const re_quantifier& q) {
                if (atleast != infinity) {
                    if (q.atleast != infinity)
                        atleast *= q.atleast;
                    else
                        atleast = infinity;
                }

                if (atmost != infinity) {
                    if (q.atmost != infinity)
                        atmost *= q.atmost;
                    else
                        atmost = infinity;
                }
            }

            void add(const re_quantifier& q) {
                if (atleast != infinity) {
                    if (q.atleast != infinity && (atleast + q.atleast) >= atleast)
                        atleast += q.atleast;
                    else
                        atleast = infinity;
                }

                if (atmost != infinity) {
                    if (q.atmost != infinity && (atmost + q.atmost) >= atmost)
                        atmost += q.atmost;
                    else
                        atmost = infinity;
                }
            }
        };
        //  re_quantifier

        struct re_state {
            union {
                uchar32 character;	//  For character.
                uint_l32 number;	//  For character_class, brackets, counter, repeat, backreference.
            };

            re_state_type type;

            union {
                std::ptrdiff_t next1;
                re_state* next_state1;
                //  Points to the next state.
                //  (Special case 1) in lookaround_open points to the next of lookaround_close.
            };
            union {
                std::ptrdiff_t next2;
                re_state* next_state2;
                //  character and character_class: points to another possibility, non-backtracking.
                //  epsilon: points to another possibility, backtracking.
                //  save_and_reset_counter, roundbracket_open, and repeat_in_push: points to a
                //    restore state, backtracking.
                //  check_counter: complementary to next1 based on quantifier.is_greedy.
                //  (Special case 1) roundbracket_close, check_0_width_repeat, and backreference:
                //    points to the next state as an exit after 0 width match.
                //  (Special case 2) in NFA_states[0] holds the entry point for match_continuous/regex_match.
                //  (Special case 3) in lookaround_open points to the contents of brackets.
            };

            re_quantifier quantifier;	//  For check_counter, roundbrackets, repeasts, (?<=...) and (?<!...),
                //  and character_class.

            union {
                bool is_not;	//  For \B, (?!...) and (?<!...).
                bool dont_push;	//  For check_counter.
                bool backrefnumber_unresolved;	//  For backreference (used only in compiler).
                bool icase;	//  For [0] only.
                bool multiline;	//  For bol, eol.
                uint_l32 padding_;
            };

            //  st_character,               //  0x00
                //  char/number:        character
                //  next1:              gen.
                //  next2:              +1 (exit. used only when '*' or '?')
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_character_class,         //  0x01
                //  char/number:        character class number
                //  next1:              gen.
                //  next2:              +1 (exit. used only when '*' or '?')
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_epsilon,                 //  0x02
                //  char/number:        -
                //  next1:              gen.
                //  next2:              alt.
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_check_counter,           //  0x03
                //  char/number:        counter number
                //  next1:              greedy: epsilon that may push backtracking data to decrement_counter.
                //                      not-greedy: out-of-loop
                //  next2:              complementary to next1
                //  q.atleast:          gen.
                //  q.atmost:           gen.
                //  q.greedy:           gen.
                //  is_not/dont_push:   - (was dont_push)

            //  st_decrement_counter,       //  0x04
                //  char/number:        counter number
                //  next1:              0 (always treated as "not matched")
                //  next2:              0
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_save_and_reset_counter,  //  0x05
                //  char/number:        counter number
                //  next1:              +2 (check_counter)
                //  next2:              +1 (restore_counter)
                //  quantifiers:        -
                //  is_not/dont_push:   - (was dont_push)

            //  st_restore_counter,         //  0x06
                //  char/number:        counter number
                //  next1:              0 (always treated as "not matched")
                //  next2:              0
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_roundbracket_open,       //  0x07
                //  char/number:        bracket number
                //  next1:              +2 (next of roundbracket_pop, atom)
                //  next2:              +1 (roundbracket_pop)
                //  q.atleast:          min bracket number inside this bracket (except myself's number)
                //  q.atmost:           max bracket number inside this bracket
                //  q.greedy:           -
                //  is_not/dont_push:   - (was dont_push)

            //  st_roundbracket_pop,        //  0x08
                //  char/number:        bracket number
                //  next1:              0 (always treated as "not matched")
                //  next2:              0
                //  q.atleast:          min bracket number inside this bracket (i.except myself's number)
                //  q.atmost:           max bracket number inside this bracket
                //  q.greedy:           -
                //  is_not/dont_push:   - (was dont_push)

            //  st_roundbracket_close,      //  0x09
                //  char/number:        bracket number
                //  next1:              gen.
                //  next2:              +1 (exit for 0 width loop)
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_repeat_in_push,          //  0x0a
                //  char/number:        repeat counter
                //  next1:              +2 (next of repeat_in_pop, atom)
                //  next2:              +1 (repeat_in_pop)
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_repeat_in_pop,           //  0x0b
                //  char/number:        repeat counter
                //  next1:              0 (always treated as "not matched")
                //  next2:              0
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_check_0_width_repeat,    //  0x0c
                //  char/number:        repeat counter
                //  next1:              gen. (epsilon or check_counter)
                //  next2:              +1 (exit for 0 width loop)
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_backreference,           //  0x0d
                //  char/number:        bracket number
                //  next1:              gen.
                //  next2:              +1 (exit for 0 width match)
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_lookaround_open,         //  0x0e
                //  char/number:        -
                //  next1:              next of lookaround_close (to where jumps after lookaround assertion)
                //  next2:              +2 (the contents of brackets)
                //  q.atleast:          <fixed-width> number of chars to be rewound (for (?<=...) (?<!...))
                //                      <variable-width> 0: lookahead, 1: lookbehind, 2: mprewinder.
                //  q.atmost:           -
                //  q.greedy:           -
                //  is_not/dont_push:   not

            //  st_bol,                     //  0x0f
                //  char/number:        -
                //  next1/next2:        -
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_eol,                     //  0x10
                //  char/number:        -
                //  next1/next2:        -
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_boundary,                //  0x11
                //  char/number:        -
                //  next1/next2:        -
                //  quantifiers:        -
                //  is_not/dont_push:   not

            //  st_success,                 //  0x12
                //  char/number:        -
                //  next1/next2:        -
                //  quantifiers:        -
                //  is_not/dont_push:   -

            //  st_move_nextpos,            //  0x13
                //  char/number:        -
                //  next1/next2:        -
                //  quantifiers:        -
                //  is_not/dont_push:   -

            void reset() {
                number = 0;
                type = st_character;
                next1 = 1;
                next2 = 0;
                is_not = false;
                quantifier.reset();
            }

            bool is_character_or_class() const {
                return type == st_character || type == st_character_class;
            }

            bool has_quantifier() const {
                //  1. character:  size == 1 && type == character,
                //  2. [...]:      size == 1 && type == character_class,
                //  3. (...):      size == ? && type == roundbracket_open,
                //  4. (?:...):    size == ? && type == epsilon && character == ':',
                //  5. backref:    size == ? && type == backreference,
                //  -- assertions boundary --
                //  6. lookaround: size == ? && type == lookaround,
                //  7. assertion:  size == 0 && type == one of assertions (^, $, \b and \B).
#if !defined(SRELL_ENABLE_GT)
                return type < st_zero_width_boundary;
#else
        //  5.5. independent: size == ? && type == lookaround && character == '>',
                return type < st_zero_width_boundary || (type == st_lookaround_open && character == meta_char::mc_gt);
#endif
            }

            bool is_noncapturinggroup() const {
                return type == st_epsilon && character == meta_char::mc_colon;
            }

            bool has_0widthchecker() const {
                return type == st_roundbracket_open || type == st_backreference;
            }

            bool is_negcharclass() const {
                return type == st_character_class && is_not;
            }

            bool is_branch() const {
                return type == st_epsilon && next2 != 0 && character == meta_char::mc_bar;	//  '|'
            }
        };
        //  re_state

        template <typename charT>
        //struct re_flags
        struct re_compiler_state {
            //	bool i;
            //	bool m;
            //	bool s;

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
            bool back;
#endif

            bool backref_used;

            simple_array<uint_l32> atleast_widths_of_brackets;
#if !defined(SRELL_NO_NAMEDCAPTURE)
            groupname_mapper<charT> unresolved_gnames;
#endif

#if !defined(SRELL_NO_UNICODE_PROPERTY)
            identifier_charclass idchecker;
#endif

            void reset(const regex_constants::syntax_option_type& /* flags */) {
                //		i = (flags & regex_constants::icase) != 0;	//  Case-insensitive.
                //		m = (flags & regex_constants::multiline) != 0;
                //		s = (flags & regex_constants::dotall) != 0;

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                back = false;
#endif

                backref_used = false;
                atleast_widths_of_brackets.clear();

#if !defined(SRELL_NO_NAMEDCAPTURE)
                unresolved_gnames.clear();
#endif

#if !defined(SRELL_NO_UNICODE_PROPERTY)
                //		idchecker.clear();	//  Keeps data once created.
#endif
            }

            void restore_from(const re_compiler_state& backup) {
#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                back = backup.back;
#endif
            }
        };
        //  re_compiler_state

    }	//  namespace regex_internal

//  ... "rei_state.hpp"]
//  ["rei_search_state.hpp" ...

    template <typename BidirectionalIterator>
    class sub_match /* : std::pair<BidirectionalIterator, BidirectionalIterator> */;

    namespace regex_internal {

        //template <typename charT>
        struct re_state;

        template </* typename charT, */typename BidirectionalIterator>
        struct re_search_state_core {
            const re_state/* <charT> */* in_NFA_states;
            BidirectionalIterator in_string;
        };

        template <typename BidirectionalIterator>
        struct re_submatch_core {
            BidirectionalIterator open_at;
            BidirectionalIterator close_at;
        };

        template <typename BidirectionalIterator>
        struct re_submatch_type {
            re_submatch_core<BidirectionalIterator> core;
            uint_l32 counter;
        };

        template </*typename charT, */typename BidirectionalIterator>
        struct re_search_state_types {
            typedef re_submatch_core<BidirectionalIterator> submatch_core;
            typedef re_submatch_type<BidirectionalIterator> submatch_type;
            typedef uint_l32 counter_type;
            typedef BidirectionalIterator position_type;

            typedef std::vector<submatch_type> submatch_array;

            typedef re_search_state_core</*charT, */BidirectionalIterator> search_core_state;

            typedef std::vector<search_core_state> backtracking_array;
            typedef std::vector<submatch_core> capture_array;
            typedef simple_array<counter_type> counter_array;
            typedef std::vector<position_type> repeat_array;
        };

        template </*typename charT1, */typename charT2>
        struct re_search_state_types</*charT1, */const charT2*> {
            typedef re_submatch_core<const charT2*> submatch_core;
            typedef re_submatch_type<const charT2*> submatch_type;
            typedef uint_l32 counter_type;
            typedef const charT2* position_type;

            typedef simple_array<submatch_type> submatch_array;

            typedef re_search_state_core</*charT1, */const charT2*> search_core_state;

            typedef simple_array<search_core_state> backtracking_array;
            typedef simple_array<submatch_core> capture_array;
            typedef simple_array<position_type> repeat_array;
            typedef simple_array<counter_type> counter_array;
        };
        //  re_search_state_types

        template </*typename charT, */typename BidirectionalIterator>
        class re_search_state : public re_search_state_types</*charT, */BidirectionalIterator> {
        private:

            typedef re_search_state_types</*charT, */BidirectionalIterator> base_type;

        public:

            typedef typename base_type::submatch_core submatchcore_type;
            typedef typename base_type::submatch_type submatch_type;
            typedef typename base_type::counter_type counter_type;
            typedef typename base_type::position_type position_type;

            typedef typename base_type::submatch_array submatch_array;

            typedef typename base_type::search_core_state search_core_state;

            typedef typename base_type::backtracking_array backtracking_array;
            typedef typename base_type::capture_array capture_array;
            typedef typename base_type::counter_array counter_array;
            typedef typename base_type::repeat_array repeat_array;

            typedef typename backtracking_array::size_type btstack_size_type;

        public:

            struct bottom_state {
                btstack_size_type btstack_size;
                typename capture_array::size_type capturestack_size;
                typename counter_array::size_type counterstack_size;
                typename repeat_array::size_type repeatstack_size;

                bottom_state(
                    const btstack_size_type bt,
                    const typename capture_array::size_type h,
                    const typename counter_array::size_type c,
                    const typename repeat_array::size_type r)
                    : btstack_size(bt)
                    , capturestack_size(h)
                    , counterstack_size(c)
                    , repeatstack_size(r) {
                }
            };

        public:

            search_core_state nth;

#if !defined(SRELL_NO_LIMIT_COUNTER)
            std::size_t failure_counter;
#endif

            BidirectionalIterator srchend;
            BidirectionalIterator lblim;

            BidirectionalIterator nextpos;

            backtracking_array bt_stack;

            capture_array capture_stack;
            counter_array counter_stack;
            repeat_array repeat_stack;

            submatch_array bracket;
            counter_array counter;
            repeat_array repeat;

            btstack_size_type btstack_size;

            BidirectionalIterator srchbegin;

        public:

            void init
            (
                const BidirectionalIterator begin,
                const BidirectionalIterator end,
                const BidirectionalIterator lookbehindlimit,
                const regex_constants::match_flag_type flags
            ) {
                lblim = lookbehindlimit;
                nextpos = srchbegin = begin;
                srchend = end;
                flags_ = flags;
            }

            void set_entrypoint(const re_state* const entry) {
                entry_state_ = entry;
            }

            void init_for_automaton
            (
                uint_l32 num_of_submatches,
                const uint_l32 num_of_counters,
                const uint_l32 num_of_repeats
            ) {

                bracket.resize(num_of_submatches);
                counter.resize(num_of_counters);
                repeat.resize(num_of_repeats);

                nth.in_string = (flags_ & regex_constants::match_continuous) ? srchbegin : srchend;

                while (num_of_submatches > 1) {
                    submatch_type& br = bracket[--num_of_submatches];

                    br.core.open_at = br.core.close_at = this->srchend;
                    br.counter = 0;
                    //  15.10.2.9; AtomEscape:
                    //  If the regular expression has n or more capturing parentheses
                    //  but the nth one is undefined because it hasn't captured anything,
                    //  then the backreference always succeeds.

                    //  C.f., table 27 and 28 on TR1, table 142 and 143 on C++11.
                }

                clear_stacks();
            }

#if defined(SRELL_NO_LIMIT_COUNTER)
            void reset(/* const BidirectionalIterator start */)
#else
            void reset(/* const BidirectionalIterator start, */ const std::size_t limit)
#endif
            {
                nth.in_NFA_states = this->entry_state_;

                bracket[0].core.open_at = nth.in_string;

#if !defined(SRELL_NO_LIMIT_COUNTER)
                failure_counter = limit;
#endif
            }

            bool is_at_lookbehindlimit() const {
                return nth.in_string == this->lblim;
            }

            bool is_at_srchend() const {
                return nth.in_string == this->srchend;
            }

            bool is_null() const {
                return nth.in_string == bracket[0].core.open_at;
            }

            //	regex_constants::match_flag_type flags() const
            //	{
            //		return this->flags_;
            //	}

            bool match_not_bol_flag() const {
                if (this->flags_ & regex_constants::match_not_bol)
                    return true;
                return false;
            }

            bool match_not_eol_flag() const {
                if (this->flags_ & regex_constants::match_not_eol)
                    return true;
                return false;
            }

            bool match_not_bow_flag() const {
                if (this->flags_ & regex_constants::match_not_bow)
                    return true;
                return false;
            }

            bool match_not_eow_flag() const {
                if (this->flags_ & regex_constants::match_not_eow)
                    return true;
                return false;
            }

            bool match_prev_avail_flag() const {
                if (this->flags_ & regex_constants::match_prev_avail)
                    return true;
                return false;
            }

            bool match_not_null_flag() const {
                if (this->flags_ & regex_constants::match_not_null)
                    return true;
                return false;
            }

            bool match_continuous_flag() const {
                if (this->flags_ & regex_constants::match_continuous)
                    return true;
                return false;
            }

            bool match_match_flag() const {
                if (this->flags_ & regex_constants::match_match_)
                    return true;
                return false;
            }

            bool set_bracket0(const BidirectionalIterator begin, const BidirectionalIterator end) {
                nth.in_string = begin;
                nextpos = end;
                return true;
            }

            void clear_stacks() {
                btstack_size = 0;
                bt_stack.clear();
                capture_stack.clear();
                repeat_stack.clear();
                counter_stack.clear();
            }

            btstack_size_type size() const	//  For debug.
            {
                return bt_stack.size();
            }

            bool is_empty() const	//  For debug.
            {
                if (btstack_size == 0
                    && bt_stack.size() == 0
                    && capture_stack.size() == 0
                    && repeat_stack.size() == 0
                    && counter_stack.size() == 0)
                    return true;

                return false;
            }

        private:

            /* const */regex_constants::match_flag_type flags_;
            const re_state/* <charT> */* /* const */entry_state_;
        };
        //  re_search_state

    }	//  namespace regex_internal

//  ... "rei_search_state.hpp"]
//  ["rei_bmh.hpp" ...

    namespace regex_internal {

#if !defined(SRELLDBG_NO_BMH)

        template <typename charT, typename utf_traits>
        class re_bmh {
        public:

            re_bmh() {
            }

            re_bmh(const re_bmh& right) {
                operator=(right);
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            re_bmh(re_bmh&& right) SRELL_NOEXCEPT {
                operator=(std::move(right));
            }
#endif

            re_bmh& operator=(const re_bmh& that) {
                if (this != &that) {
                    this->u32string_ = that.u32string_;

                    this->bmtable_ = that.bmtable_;
                    this->repseq_ = that.repseq_;
                }
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            re_bmh& operator=(re_bmh&& that) SRELL_NOEXCEPT {
                if (this != &that) {
                    this->u32string_ = std::move(that.u32string_);

                    this->bmtable_ = std::move(that.bmtable_);
                    this->repseq_ = std::move(that.repseq_);
                }
                return *this;
            }
#endif

            void clear() {
                u32string_.clear();

                bmtable_.clear();
                repseq_.clear();
            }

            void setup(const simple_array<uchar32>& u32s, const bool icase) {
                u32string_ = u32s;
                setup_();

                if (!icase)
                    setup_for_casesensitive();
                else
                    setup_for_icase();
            }

            template <typename RandomAccessIterator>
            bool do_casesensitivesearch(re_search_state<RandomAccessIterator>& sstate, const std::random_access_iterator_tag) const {
                RandomAccessIterator begin = sstate.srchbegin;
                const RandomAccessIterator end = sstate.srchend;
                std::size_t offset = static_cast<std::size_t>(repseq_.size() - 1);
                const charT* const relastchar = &repseq_[offset];

                for (; static_cast<std::size_t>(end - begin) > offset;) {
                    begin += offset;

                    if (*begin == *relastchar) {
                        const charT* re = relastchar;
                        RandomAccessIterator tail = begin;

                        for (; *--re == *--tail;) {
                            if (re == repseq_.data())
                                return sstate.set_bracket0(tail, ++begin);
                        }
                    }
                    offset = bmtable_[*begin & 0xff];
                }
                return false;
            }

            template <typename BidirectionalIterator>
            bool do_casesensitivesearch(re_search_state<BidirectionalIterator>& sstate, const std::bidirectional_iterator_tag) const {
                BidirectionalIterator begin = sstate.srchbegin;
                const BidirectionalIterator end = sstate.srchend;
                std::size_t offset = static_cast<std::size_t>(repseq_.size() - 1);
                const charT* const relastchar = &repseq_[offset];

                for (;;) {
                    for (; offset; --offset, ++begin)
                        if (begin == end)
                            return false;

                    if (*begin == *relastchar) {
                        const charT* re = relastchar;
                        BidirectionalIterator tail = begin;

                        for (; *--re == *--tail;) {
                            if (re == repseq_.data())
                                return sstate.set_bracket0(tail, ++begin);
                        }
                    }
                    offset = bmtable_[*begin & 0xff];
                }
            }

            template <typename RandomAccessIterator>
            bool do_icasesearch(re_search_state<RandomAccessIterator>& sstate, const std::random_access_iterator_tag) const {
                const RandomAccessIterator begin = sstate.srchbegin;
                const RandomAccessIterator end = sstate.srchend;
                std::size_t offset = bmtable_[256];
                const uchar32 entrychar = u32string_[u32string_.size() - 1];
                const uchar32* const re2ndlastchar = &u32string_[u32string_.size() - 2];
                RandomAccessIterator curpos = begin;

                for (; static_cast<std::size_t>(end - curpos) > offset;) {
                    curpos += offset;

                    for (; utf_traits::is_trailing(*curpos);)
                        if (++curpos == end)
                            return false;

                    const uchar32 txtlastchar = utf_traits::codepoint(curpos, end);

                    if (txtlastchar == entrychar || unicode_case_folding::do_casefolding(txtlastchar) == entrychar) {
                        const uchar32* re = re2ndlastchar;
                        RandomAccessIterator tail = curpos;

                        //				for (; *--re == unicode_case_folding::do_casefolding(utf_traits::dec_codepoint(tail, begin));)
                        for (; *re == unicode_case_folding::do_casefolding(utf_traits::dec_codepoint(tail, begin)); --re) {
                            if (re == u32string_.data()) {
                                utf_traits::codepoint_inc(curpos, end);
                                return sstate.set_bracket0(tail, curpos);
                            }
                            if (tail == begin)
                                break;
                        }
                    }
                    offset = bmtable_[txtlastchar & 0xff];
                }
                return false;
            }

            template <typename BidirectionalIterator>
            bool do_icasesearch(re_search_state<BidirectionalIterator>& sstate, const std::bidirectional_iterator_tag) const {
                const BidirectionalIterator begin = sstate.srchbegin;
                const BidirectionalIterator end = sstate.srchend;

                if (begin != end) {
                    std::size_t offset = bmtable_[256];	//static_cast<std::size_t>(u32string_.size() - 1);
                    const uchar32 entrychar = u32string_[offset];
                    const uchar32* const re2ndlastchar = &u32string_[offset - 1];
                    BidirectionalIterator curpos = begin;

                    for (;;) {
                        for (;;) {
                            if (++curpos == end)
                                return false;
                            if (!utf_traits::is_trailing(*curpos))
                                if (--offset == 0)
                                    break;
                        }
                        //				const uchar32 txtlastchar = unicode_case_folding::do_casefolding(utf_traits::codepoint(curpos, end));
                        const uchar32 txtlastchar = utf_traits::codepoint(curpos, end);

                        //				if (txtlastchar == *re2ndlastchar)
                        //				if (txtlastchar == *re2ndlastchar || unicode_case_folding::do_casefolding(txtlastchar) == *re2ndlastchar)
                        if (txtlastchar == entrychar || unicode_case_folding::do_casefolding(txtlastchar) == entrychar) {
                            const uchar32* re = re2ndlastchar;
                            BidirectionalIterator tail = curpos;

                            for (; *re == unicode_case_folding::do_casefolding(utf_traits::dec_codepoint(tail, begin)); --re) {
                                if (re == u32string_.data()) {
                                    utf_traits::codepoint_inc(curpos, end);
                                    return sstate.set_bracket0(tail, curpos);
                                }
                                if (tail == begin)
                                    break;
                            }
                        }
                        offset = bmtable_[txtlastchar & 0xff];
                    }
                }
                return false;
            }

        private:

            void setup_() {
                bmtable_.resize(257);
            }

            void setup_for_casesensitive() {
                charT mbstr[utf_traits::maxseqlen];
                const std::size_t u32str_lastcharpos_ = static_cast<std::size_t>(u32string_.size() - 1);

                repseq_.clear();

                for (std::size_t i = 0; i <= u32str_lastcharpos_; ++i) {
                    const uchar32 seqlen = utf_traits::to_codeunits(mbstr, u32string_[i]);

                    for (uchar32 j = 0; j < seqlen; ++j)
                        repseq_.push_back(mbstr[j]);
                }

                for (std::size_t i = 0; i < 256; ++i)
                    bmtable_[i] = static_cast<std::size_t>(repseq_.size());

                const std::size_t repseq_lastcharpos_ = static_cast<std::size_t>(repseq_.size() - 1);

                for (std::size_t i = 0; i < repseq_lastcharpos_; ++i)
                    bmtable_[repseq_[i] & 0xff] = repseq_lastcharpos_ - i;
            }

            void setup_for_icase() {
                charT mbstr[utf_traits::maxseqlen];
                uchar32 u32table[unicode_case_folding::rev_maxset];
                const std::size_t u32str_lastcharpos = static_cast<std::size_t>(u32string_.size() - 1);
                simple_array<std::size_t> minlen(u32string_.size());
                std::size_t cu_repseq_lastcharpos = 0;

                for (std::size_t i = 0; i <= u32str_lastcharpos; ++i) {
                    const uchar32 setnum = unicode_case_folding::casefoldedcharset(u32table, u32string_[i]);
                    uchar32 u32c = u32table[0];

                    for (uchar32 j = 1; j < setnum; ++j)
                        if (u32c > u32table[j])
                            u32c = u32table[j];

                    if (i < u32str_lastcharpos)
                        cu_repseq_lastcharpos += minlen[i] = utf_traits::to_codeunits(mbstr, u32c);
                }

                ++cu_repseq_lastcharpos;

                for (std::size_t i = 0; i < 256; ++i)
                    bmtable_[i] = cu_repseq_lastcharpos;

                bmtable_[256] = --cu_repseq_lastcharpos;

                for (std::size_t i = 0; i < u32str_lastcharpos; ++i) {
                    const uchar32 setnum = unicode_case_folding::casefoldedcharset(u32table, u32string_[i]);

                    for (uchar32 j = 0; j < setnum; ++j)
                        bmtable_[u32table[j] & 0xff] = cu_repseq_lastcharpos;

                    cu_repseq_lastcharpos -= minlen[i];
                }
            }

        public:	//  For debug.

            void print_table() const;
            void print_seq() const;

        private:

            simple_array<uchar32> u32string_;
            //	std::size_t bmtable_[256];
            simple_array<std::size_t> bmtable_;
            simple_array<charT> repseq_;
        };
        //  re_bmh

#endif	//  !defined(SRELLDBG_NO_BMH)
    }	//  namespace regex_internal

//  ... "rei_bmh.hpp"]
//  ["rei_compiler.hpp" ...

    namespace regex_internal {

        template <typename charT, typename traits>
        struct re_object_core {
        protected:

            typedef re_state/*<charT>*/ state_type;
            typedef simple_array<state_type> state_array;

            state_array NFA_states;
            re_character_class character_class;

#if !defined(SRELLDBG_NO_1STCHRCLS)
#if !defined(SRELLDBG_NO_BITSET)
            bitset<traits::utf_traits::bitsetsize> firstchar_class_bs;
#else
            range_pairs firstchar_class;
#endif
#endif

#if !defined(SRELL_NO_LIMIT_COUNTER)
        public:

            std::size_t limit_counter;

        protected:
#endif

            typedef typename traits::utf_traits utf_traits;

            uint_l32 number_of_brackets;
            uint_l32 number_of_counters;
            uint_l32 number_of_repeats;
            regex_constants::syntax_option_type soflags;

#if !defined(SRELL_NO_NAMEDCAPTURE)
            groupname_mapper<charT> namedcaptures;
            typedef typename groupname_mapper<charT>::gname_string gname_string;
#endif

#if !defined(SRELLDBG_NO_BMH)
            re_bmh<charT, utf_traits>* bmdata;
#endif

#if !defined(SRELL_NO_LIMIT_COUNTER)
        private:

            static const std::size_t lcounter_defnum_ = 16777216;

#endif

        protected:

            re_object_core()
#if !defined(SRELL_NO_LIMIT_COUNTER)
                : limit_counter(lcounter_defnum_)
#if !defined(SRELLDBG_NO_BMH)
                , bmdata(NULL)
#endif
#elif !defined(SRELLDBG_NO_BMH)
                : bmdata(NULL)
#endif
            {
            }

            re_object_core(const re_object_core& right)
#if !defined(SRELLDBG_NO_BMH)
                : bmdata(NULL)
#endif
            {
                operator=(right);
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            re_object_core(re_object_core&& right) SRELL_NOEXCEPT
#if !defined(SRELLDBG_NO_BMH)
                : bmdata(NULL)
#endif
            {
                operator=(std::move(right));
            }
#endif

#if !defined(SRELLDBG_NO_BMH)
            ~re_object_core() {
                if (bmdata)
                    delete bmdata;
            }
#endif

            void reset(const regex_constants::syntax_option_type flags) {
                NFA_states.clear();
                character_class.clear();

#if !defined(SRELLDBG_NO_1STCHRCLS)
#if !defined(SRELLDBG_NO_BITSET)
                firstchar_class_bs.reset();
#else
                firstchar_class.clear();
#endif
#endif

#if !defined(SRELL_NO_LIMIT_COUNTER)
                limit_counter = lcounter_defnum_;
#endif

                number_of_brackets = 1;
                number_of_counters = 0;
                number_of_repeats = 0;
                soflags = flags;	//  regex_constants::ECMAScript;

#if !defined(SRELL_NO_NAMEDCAPTURE)
                namedcaptures.clear();
#endif

#if !defined(SRELLDBG_NO_BMH)
                //		bmdata->clear();
                if (bmdata)
                    delete bmdata;
                bmdata = NULL;
#endif
            }

            re_object_core& operator=(const re_object_core& that) {
                if (this != &that) {
                    this->NFA_states = that.NFA_states;
                    this->character_class = that.character_class;

#if !defined(SRELLDBG_NO_1STCHRCLS)
#if !defined(SRELLDBG_NO_BITSET)
                    this->firstchar_class_bs = that.firstchar_class_bs;
#else
                    this->firstchar_class = that.firstchar_class;
#endif
#endif

#if !defined(SRELL_NO_LIMIT_COUNTER)
                    this->limit_counter = that.limit_counter;
#endif

                    //			this->utf_traits_inst = that.utf_traits_inst;

                    this->number_of_brackets = that.number_of_brackets;
                    this->number_of_counters = that.number_of_counters;
                    this->number_of_repeats = that.number_of_repeats;
                    this->soflags = that.soflags;

#if !defined(SRELL_NO_NAMEDCAPTURE)
                    this->namedcaptures = that.namedcaptures;
#endif

#if !defined(SRELLDBG_NO_BMH)
                    if (that.bmdata) {
                        if (this->bmdata)
                            *this->bmdata = *that.bmdata;
                        else
                            this->bmdata = new re_bmh<charT, utf_traits>(*that.bmdata);
                    } else if (this->bmdata) {
                        delete this->bmdata;
                        this->bmdata = NULL;
                    }
#endif

                    if (that.NFA_states.size())
                        repair_nextstates(&that.NFA_states[0]);
                }
                return *this;
            }

#if defined(SRELL_CPP11_MOVE_ENABLED)
            re_object_core& operator=(re_object_core&& that) SRELL_NOEXCEPT {
                if (this != &that) {
                    this->NFA_states = std::move(that.NFA_states);
                    this->character_class = std::move(that.character_class);

#if !defined(SRELLDBG_NO_1STCHRCLS)
#if !defined(SRELLDBG_NO_BITSET)
                    this->firstchar_class_bs = std::move(that.firstchar_class_bs);
#else
                    this->firstchar_class = std::move(that.firstchar_class);
#endif
#endif

#if !defined(SRELL_NO_LIMIT_COUNTER)
                    this->limit_counter = that.limit_counter;
#endif

                    this->number_of_brackets = that.number_of_brackets;
                    this->number_of_counters = that.number_of_counters;
                    this->number_of_repeats = that.number_of_repeats;
                    this->soflags = that.soflags;

#if !defined(SRELL_NO_NAMEDCAPTURE)
                    this->namedcaptures = std::move(that.namedcaptures);
#endif

#if !defined(SRELLDBG_NO_BMH)
                    if (this->bmdata)
                        delete this->bmdata;
                    this->bmdata = that.bmdata;
                    that.bmdata = NULL;
#endif
                }
                return *this;
            }
#endif	//  defined(SRELL_CPP11_MOVE_ENABLED)

            void swap(re_object_core& right) {
                if (this != &right) {
                    this->NFA_states.swap(right.NFA_states);
                    this->character_class.swap(right.character_class);

#if !defined(SRELLDBG_NO_1STCHRCLS)
#if !defined(SRELLDBG_NO_BITSET)
                    this->firstchar_class_bs.swap(right.firstchar_class_bs);
#else
                    this->firstchar_class.swap(right.firstchar_class);
#endif
#endif

#if !defined(SRELL_NO_LIMIT_COUNTER)
                    {
                        const std::size_t tmp_limit_counter = this->limit_counter;
                        this->limit_counter = right.limit_counter;
                        right.limit_counter = tmp_limit_counter;
                    }
#endif
                    //			this->utf_traits_inst.swap(right.utf_traits_inst);

                    {
                        const uint_l32 tmp_numof_brackets = this->number_of_brackets;
                        this->number_of_brackets = right.number_of_brackets;
                        right.number_of_brackets = tmp_numof_brackets;
                    }
                    {
                        const uint_l32 tmp_numof_counters = this->number_of_counters;
                        this->number_of_counters = right.number_of_counters;
                        right.number_of_counters = tmp_numof_counters;
                    }
                    {
                        const uint_l32 tmp_numof_repeats = this->number_of_repeats;
                        this->number_of_repeats = right.number_of_repeats;
                        right.number_of_repeats = tmp_numof_repeats;
                    }
                    {
                        const regex_constants::syntax_option_type tmp_soflags = this->soflags;
                        this->soflags = right.soflags;
                        right.soflags = tmp_soflags;
                    }

#if !defined(SRELL_NO_NAMEDCAPTURE)
                    this->namedcaptures.swap(right.namedcaptures);
#endif

#if !defined(SRELLDBG_NO_BMH)
                    {
                        re_bmh<charT, utf_traits>* const tmp_bmdata = this->bmdata;
                        this->bmdata = right.bmdata;
                        right.bmdata = tmp_bmdata;
                    }
#endif
                }
            }

            void throw_error(const regex_constants::error_type& e) {
                //		reset();
                NFA_states.clear();
#if !defined(SRELLDBG_NO_BMH)
                if (bmdata)
                    delete bmdata;
                bmdata = NULL;
#endif
                throw regex_error(e);
            }

        private:

            void repair_nextstates(const state_type* const oldbase) {
                state_type* const newbase = &this->NFA_states[0];

                for (typename state_array::size_type i = 0; i < this->NFA_states.size(); ++i) {
                    state_type& state = this->NFA_states[i];

                    if (state.next_state1)
                        state.next_state1 = state.next_state1 - oldbase + newbase;

                    if (state.next_state2)
                        state.next_state2 = state.next_state2 - oldbase + newbase;
                }
            }
        };
        //  re_object_core

        template <typename charT, typename traits>
        class re_compiler : public re_object_core<charT, traits> {
        protected:

            template <typename ForwardIterator>
            bool compile(ForwardIterator begin, const ForwardIterator end, const regex_constants::syntax_option_type flags /* = regex_constants::ECMAScript */) {
                simple_array<uchar32> u32;

                while (begin != end) {
                    const uchar32 u32c = utf_traits::codepoint_inc(begin, end);
                    if (u32c > constants::unicode_max_codepoint)
                        this->throw_error(regex_constants::error_utf8);
                    u32.push_back(u32c);
                }

                return compile_core(u32.data(), u32.data() + u32.size(), flags);
            }

            bool is_icase() const {
#if !defined(SRELL_NO_ICASE)
                if (this->soflags & regex_constants::icase)
                    return true;
#endif
                return false;
            }
            bool is_ricase() const {
#if !defined(SRELL_NO_ICASE)
                return /* this->NFA_states.size() && */ this->NFA_states[0].icase == true;
#else
                return false;
#endif
            }

            bool is_multiline() const {
                if (this->soflags & regex_constants::multiline)
                    return true;
                return false;
            }

            bool is_dotall() const {
                return (this->soflags & regex_constants::dotall) ? true : false;
            }

            bool is_optimize() const {
                return (this->soflags & regex_constants::optimize) ? true : false;
            }

        private:

            typedef re_object_core<charT, traits> base_type;
            typedef typename base_type::utf_traits utf_traits;
            typedef typename base_type::state_type state_type;
            typedef typename base_type::state_array state_array;
#if !defined(SRELL_NO_NAMEDCAPTURE)
            typedef typename base_type::gname_string gname_string;
#endif
#if !defined(SRELL_NO_UNICODE_PROPERTY)
            typedef typename re_character_class::pstring pstring;
#endif
            typedef typename state_array::size_type state_size_type;

            bool compile_core(const uchar32* begin, const uchar32* const end, const regex_constants::syntax_option_type flags) {
                re_quantifier piececharlen;
                re_compiler_state<charT> cstate;
                state_type atom;

                this->reset(flags);
                //		this->soflags = flags;
                cstate.reset(flags);

                atom.reset();
                atom.type = st_epsilon;
                atom.next2 = 1;
                this->NFA_states.push_back(atom);

                if (!make_nfa_states(this->NFA_states, piececharlen, begin, end, cstate)) {
                    return false;
                }

                if (begin != end)
                    this->throw_error(regex_constants::error_paren);	//  ')'s are too many.

                if (!check_backreferences(cstate))
                    this->throw_error(regex_constants::error_backref);

#if !defined(SRELL_NO_ICASE)
                if (this->is_icase())
                    this->NFA_states[0].icase = check_if_really_needs_icase_search();
#endif

#if !defined(SRELLDBG_NO_BMH)
                setup_bmhdata();
#endif

                atom.type = st_success;
                atom.next1 = 0;
                atom.next2 = 0;
                this->NFA_states.push_back(atom);

                optimise();
                relativejump_to_absolutejump();

                return true;
            }

            bool make_nfa_states(state_array& piece, re_quantifier& piececharlen, const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
                typename state_array::size_type prevbranch_end = 0;
                state_type atom;
                state_array branch;
                re_quantifier branchsize;

                piececharlen.reset(0);

                for (;;) {
                    branch.clear();

                    if (!make_branch(branch, branchsize, curpos, end, cstate))
                        return false;

                    //  For piececharlen.atleast, 0 as the initial value and 0 as an
                    //  actual value must be distinguished.
                    if (piececharlen.atmost == 0 || piececharlen.atleast > branchsize.atleast)
                        piececharlen.atleast = branchsize.atleast;

                    if (piececharlen.atmost < branchsize.atmost)
                        piececharlen.atmost = branchsize.atmost;

                    if (curpos != end && *curpos == meta_char::mc_bar) {
                        atom.reset();
                        atom.character = meta_char::mc_bar;
                        atom.type = st_epsilon;
                        atom.next2 = static_cast<std::ptrdiff_t>(branch.size()) + 2;
                        branch.insert(0, atom);
                    }

                    if (prevbranch_end)
                        piece[prevbranch_end].next1 = static_cast<std::ptrdiff_t>(branch.size()) + 1;

                    piece += branch;

                    //  end or ')'
                    if (curpos == end || *curpos == meta_char::mc_rbracl)
                        break;

                    //  *curpos == '|'

                    prevbranch_end = piece.size();
                    atom.reset();
                    atom.type = st_epsilon;
                    piece.push_back(atom);

                    ++curpos;
                }
                return true;
            }

            bool make_branch(state_array& branch, re_quantifier& branchsize, const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
                state_array piece;
                state_array piece_with_quantifier;
                re_quantifier quantifier;

                branchsize.reset(0);

                for (;;) {
                    re_quantifier piececharlen;

                    if (curpos == end)
                        return true;

                    piece.clear();
                    piece_with_quantifier.clear();

                    switch (*curpos) {
                        //			case char_ctrl::cc_nul:	//  '\0':
                        case meta_char::mc_bar:	//  '|':
                        case meta_char::mc_rbracl:	//  ')':
                            return true;

                        default:
                            if (!get_atom(piece, piececharlen, curpos, end, cstate))
                                return false;
                    }

                    if (piece.size()) {
                        const state_type& firstatom = piece[0];

                        quantifier.reset();	//  quantifier.atleast = quantifier.atmost = 1;

                        if (firstatom.has_quantifier()) {
                            if (curpos != end && !get_quantifier(quantifier, curpos, end))
                                return false;
                        }

                        if (piece.size() == 2 && firstatom.is_noncapturinggroup() && piece[1].is_noncapturinggroup()) {
                            //  (?:) alone or followed by a quantifier.
        //					piece_with_quantifier += piece;
                            ;	//  Do nothing.
                        } else
                            combine_piece_with_quantifier(piece_with_quantifier, piece, quantifier, piececharlen);

#if 01
                        piececharlen.multiply(quantifier);
                        branchsize.add(piececharlen);
#else
                        branchsize.atleast += piececharlen.atleast * quantifier.atleast;
                        if (!branchsize.is_infinity()) {
                            if (piececharlen.is_infinity() || quantifier.is_infinity())
                                branchsize.set_infinity();
                            else
                                branchsize.atmost += piececharlen.atmost * quantifier.atmost;
                        }
#endif

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)

                        if (!cstate.back)
                            branch += piece_with_quantifier;
                        else
                            branch.insert(0, piece_with_quantifier);
#else
                        branch += piece_with_quantifier;
#endif
                    }
                }
            }

            bool get_atom(state_array& piece, re_quantifier& atomsize, const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
                state_type atom;

                atom.reset();
                atom.character = *curpos++;

                switch (atom.character) {
                    case meta_char::mc_rbraop:	//  '(':
                        return get_piece_in_roundbrackets(piece, atomsize, curpos, end, cstate);

                    case meta_char::mc_sbraop:	//  '[':
                        if (!register_character_class(atom, curpos, end, cstate))
                            return false;

                        break;

                    case meta_char::mc_escape:	//  '\\':
                        if (!translate_atom_escape(atom, curpos, end, cstate))
                            return false;

                        break;

                    case meta_char::mc_period:	//  '.':
                        atom.type = st_character_class;
#if !defined(SRELL_NO_SINGLELINE)
                        if (this->is_dotall()) {
                            atom.number = static_cast<uint_l32>(re_character_class::dotall);
                        } else
#endif
                        {
                            //				atom.number = static_cast<uint_l32>(re_character_class::newline);
                            range_pairs nlclass = this->character_class[static_cast<uint_l32>(re_character_class::newline)];

                            nlclass.negation();
                            atom.number = this->character_class.register_newclass(nlclass);
                        }
                        break;

                    case meta_char::mc_caret:	//  '^':
                        atom.type = st_bol;
                        atom.quantifier.reset(0);
                        //			if (current_flags.m)
                        if (is_multiline())
                            atom.multiline = true;
                        break;

                    case meta_char::mc_dollar:	//  '$':
                        atom.type = st_eol;
                        atom.quantifier.reset(0);
                        //			if (current_flags.m)
                        if (is_multiline())
                            atom.multiline = true;
                        break;

                    case meta_char::mc_astrsk:	//  '*':
                    case meta_char::mc_plus:	//  '+':
                    case meta_char::mc_query:	//  '?':
                    case meta_char::mc_cbraop:	//  '{'
                        this->throw_error(regex_constants::error_badrepeat);

                    default:;
                }

                if (atom.type == st_character) {
                    if (this->is_icase())
                        atom.character = unicode_case_folding::do_casefolding(atom.character);
                }

                piece.push_back(atom);
                atomsize = atom.quantifier;

                return true;
            }

            //  '('.

            bool get_piece_in_roundbrackets(state_array& piece, re_quantifier& piececharlen, const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
                const re_compiler_state<charT> original_cstate(cstate);
                state_type atom;

                if (curpos == end)
                    this->throw_error(regex_constants::error_paren);

                atom.reset();
                atom.type = st_roundbracket_open;

                if (*curpos == meta_char::mc_query)	//  '?'
                {
                    if (!extended_roundbrackets(piece, atom, ++curpos, end, cstate))
                        return false;
                }

                if (atom.type == st_roundbracket_open) {
                    push_bracket_open(piece, atom);
                }

                //		if (curpos == end)
                //			this->throw_error(regex_constants::error_paren);

                if (!make_nfa_states(piece, piececharlen, curpos, end, cstate))
                    return false;

                //  end or ')'?
                if (curpos == end)
                    this->throw_error(regex_constants::error_paren);

                ++curpos;

                cstate.restore_from(original_cstate);

                switch (atom.type) {
                    case st_epsilon:

                        //			if (piece.size() <= 2)	//  ':' or ':' + one.
                        if (piece.size() == 2)	//  ':' + something.
                        {
                            piece.erase(0);
                            return true;
                        }

                        piece[0].quantifier.atmost = this->number_of_brackets - 1;
                        break;

                        //		case st_lookaround_pop:
                    case st_lookaround_open:
                    {
                        state_type& firstatom = piece[0];

#if defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                        //				if (firstatom.reverse)
                        if (firstatom.quantifier.atleast)	//  > 0 means lookbehind.
                        {
                            if (!piececharlen.is_same() || piececharlen.is_infinity())
                                this->throw_error(regex_constants::error_lookbehind);

                            firstatom.quantifier = piececharlen;
                        }
#endif

#if defined(SRELL_ENABLE_GT)
                        if (firstatom.character != meta_char::mc_gt)
#endif
                            piececharlen.reset(0);

                        firstatom.next1 = static_cast<std::ptrdiff_t>(piece.size()) + 1;

                        atom.type = st_lookaround_close;
                        atom.next1 = 0;
                        atom.next2 = 0;
                    }
                    break;

                    default:
                        set_bracket_close(piece, atom, piececharlen, cstate);
                }

                piece.push_back(atom);
                return true;
            }

            bool extended_roundbrackets(state_array& piece, state_type& atom, const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                bool lookbehind = false;
#endif

                if (curpos == end)
                    this->throw_error(regex_constants::error_paren);

                atom.character = *curpos;

                if (atom.character == meta_char::mc_lt)	//  '<'
                {
#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                    lookbehind = true;
#endif
                    if (++curpos == end)
                        this->throw_error(regex_constants::error_paren);

                    atom.character = *curpos;

                    if (atom.character != meta_char::mc_eq && atom.character != meta_char::mc_exclam) {
#if !defined(SRELL_NO_NAMEDCAPTURE)
                        return parse_groupname(curpos, end, cstate);
#else
                        this->throw_error(regex_constants::error_paren);
#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)
                    }
                } else
                    atom.quantifier.atleast = 0;
                //  Sets atleast to 0 for other assertions than lookbehinds. The automaton
                //  checks atleast to know whether lookbehinds or other assertions.

                switch (atom.character) {
                    case meta_char::mc_colon:
                        atom.type = st_epsilon;
                        atom.quantifier.atleast = this->number_of_brackets;
                        break;

                    case meta_char::mc_exclam:	//  '!':
                        atom.is_not = true;
                        //@fallthrough@

                    case meta_char::mc_eq:	//  '=':
#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                        cstate.back = lookbehind;
#else
//			atom.reverse = lookbehind;
#endif

#if defined(SRELL_ENABLE_GT)
                    case meta_char::mc_gt:
#endif
                        atom.type = st_lookaround_open;
                        atom.next2 = 1;
                        break;

                    default:
                        this->throw_error(regex_constants::error_paren);
                }

                ++curpos;
                piece.push_back(atom);
                return true;
            }

            void push_bracket_open(state_array& piece, state_type& atom) {
                atom.number = this->number_of_brackets;
                atom.next1 = 2;
                atom.next2 = 1;
                piece.push_back(atom);
                ++this->number_of_brackets;

                atom.type = st_roundbracket_pop;
                atom.next1 = 0;
                atom.next2 = 0;
                piece.push_back(atom);
            }

            void set_bracket_close(state_array& piece, state_type& atom, const re_quantifier& piececharlen, re_compiler_state<charT>& cstate) {
                //		uint_l32 max_bracketno = atom.number;

                atom.type = st_roundbracket_close;
                atom.next1 = 1;
                atom.next2 = 1;
#if 0
                for (typename state_array::size_type i = 0; i < piece.size(); ++i) {
                    const state_type& state = piece[i];

                    if (state.type == st_roundbracket_open && max_bracketno < state.number)
                        max_bracketno = state.number;
                }
#endif

                re_quantifier& rb_open = piece[0].quantifier;
                re_quantifier& rb_pop = piece[1].quantifier;

                rb_open.atleast = rb_pop.atleast = atom.number + 1;
                rb_open.atmost = rb_pop.atmost = this->number_of_brackets - 1;	//  max_bracketno;

                if (cstate.atleast_widths_of_brackets.size() < atom.number)
                    cstate.atleast_widths_of_brackets.resize(atom.number, 0);

                cstate.atleast_widths_of_brackets[atom.number - 1] = piececharlen.atleast;
            }

            void combine_piece_with_quantifier(state_array& piece_with_quantifier, state_array& piece, const re_quantifier& quantifier, const re_quantifier& piececharlen) {
                state_type& firstatom = piece[0];
                //		const bool firstpiece_is_roundbracket_open = (firstatom.type == st_roundbracket_open);
                const bool piece_has_0widthchecker = firstatom.has_0widthchecker();
                const bool piece_is_noncapturinggroup_contaning_capturinggroup = firstatom.is_noncapturinggroup() && firstatom.quantifier.is_valid();
                state_type atom;

                if (quantifier.atmost == 0)
                    return;

                atom.reset();
                atom.quantifier = quantifier;
                if (firstatom.is_character_or_class())
                    atom.character = meta_char::mc_astrsk;	//  For nextpos_optimisation1_3().

                if (quantifier.atmost == 1) {
                    if (quantifier.atleast == 0) {
                        atom.type = st_epsilon;
                        atom.next2 = static_cast<std::ptrdiff_t>(piece.size()) + 1;

                        if (!quantifier.is_greedy) {
                            atom.next1 = atom.next2;
                            atom.next2 = 1;
                        }

                        if (atom.character == meta_char::mc_astrsk)
                            firstatom.quantifier = quantifier;

                        piece_with_quantifier.push_back(atom);
                        //      (push)
                    }

                    if (piece.size() >= 2 && firstatom.type == st_roundbracket_open && piece[1].type == st_roundbracket_pop) {
                        firstatom.quantifier.atmost = 0u;
                        piece[1].quantifier.atmost = 0u;
                    }

                    piece_with_quantifier += piece;
                    return;
                }

                //  atmost >= 2

#if !defined(SRELLDBG_NO_SIMPLEEQUIV)
        //  The counter requires at least 6 states: save, restore, check, inc, dec, atom(s).
        //  A character or charclass quantified by one of these has a simple equivalent representation:
        //  a{0,2}  1.epsilon(2|5), 2.CHorCL(3), 3.epsilon(4|5), 4.CHorCL(5), [5].
        //  a{0,3}  1.epsilon(2|7), 2.CHorCL(3), 3.epsilon(4|7), 4.CHorCL(5), 5.epsilon(6|7), 6.CHorCL(7), [7].
        //  a{1,2}  1.CHorCL(2), 2.epsilon(3|4), 3.CHorCL(4), [4].
        //  a{1,3}  1.CHorCL(2), 2.epsilon(3|6), 3.CHorCL(4), 4.epsilon(5|6), 5.CHorCL(6), [6].
        //  a{2,3}  1.CHorCL(2), 2.CHorCL(3), 3.epsilon(4|5), 4.CHorCL(5), [5].
        //  a{2,4}  1.CHorCL(2), 2.CHorCL(3), 3.epsilon(4|7), 4.CHorCL(5), 5.epsilon(6|7), 6.CHorCL(7), [7].
                if (piece.size() == 1 && firstatom.is_character_or_class() && quantifier.has_simple_equivalence()) {
                    const typename state_array::size_type branchsize = piece.size() + 1;

                    for (uint_l32 i = 0; i < quantifier.atleast; ++i)
                        piece_with_quantifier += piece;

                    if (atom.character == meta_char::mc_astrsk)
                        firstatom.quantifier.set(0, 1, quantifier.is_greedy);

                    atom.type = st_epsilon;
                    atom.next2 = (quantifier.atmost - quantifier.atleast) * branchsize;
                    if (!quantifier.is_greedy) {
                        atom.next1 = atom.next2;
                        atom.next2 = 1;
                    }
                    for (uint_l32 i = quantifier.atleast; i < quantifier.atmost; ++i) {
                        piece_with_quantifier.push_back(atom);
                        piece_with_quantifier += piece;
                        quantifier.is_greedy ? (atom.next2 -= branchsize) : (atom.next1 -= branchsize);
                    }
                    return;
                }
#endif	//  !defined(SRELLDBG_NO_SIMPLEEQUIV)

                atom.type = st_epsilon;
                if (quantifier.is_asterisk())	//  {0,}
                {
                    //  greedy:  1.epsilon(2|4), 2.piece, 3.LAorC0WR(1|0), 4.OutOfLoop.
                    //  !greedy: 1.epsilon(4|2), 2.piece, 3.LAorC0WR(1|0), 4.OutOfLoop.
                    //  LAorC0WR: LastAtomOfPiece or Check0WidthRepeat.
                    //  atom.type points to 1.
                } else if (quantifier.is_plus())	//  {1,}
                {
#if !defined(SRELLDBG_NO_ASTERISK_OPT)

                    if (piece.size() == 1 && firstatom.is_character_or_class()) {
                        piece_with_quantifier += piece;
                        --atom.quantifier.atleast;	//  /.+/ -> /..*/.
                    } else
#endif
                    {
                        atom.next1 = 2;
                        atom.next2 = 0;
                        piece_with_quantifier.push_back(atom);
                        //  greedy:  1.epsilon(3), 2.epsilon(3|5), 3.piece, 4.LAorC0WR(2|0), 5.OutOfLoop.
                        //  !greedy: 1.epsilon(3), 2.epsilon(5|3), 3.piece, 4.LAorC0WR(2|0), 5.OutOfLoop.
                        //  atom.type points to 2.
                    }
                } else {
                    atom.number = this->number_of_counters;
                    ++this->number_of_counters;

                    atom.type = st_save_and_reset_counter;
                    atom.next1 = 2;
                    atom.next2 = 1;
                    piece_with_quantifier.push_back(atom);

                    atom.type = st_restore_counter;
                    atom.next1 = 0;
                    atom.next2 = 0;
                    piece_with_quantifier.push_back(atom);
                    //  1.save_and_reset_counter(3|2), 2.restore_counter(0|0),

                    atom.next1 = 0;
                    atom.next2 = 0;
                    atom.type = st_decrement_counter;
                    piece.insert(0, atom);

                    atom.next1 = 2;
                    //			atom.next2 = piece[1].is_character_or_class() ? 0 : 1;
                    //			atom.next2 = 0;
                    for (state_size_type i = 1; i < piece.size(); ++i) {
                        const state_type& state = piece[i];

                        if (state.is_character_or_class() || (state.type == st_epsilon && state.next2 == 0))
                            ;
                        else {
                            atom.next2 = 1;
                            break;
                        }
                    }
                    atom.type = st_epsilon;	//  st_increment_counter;
                    piece.insert(0, atom);
                    piece[0].number = 0;

                    atom.type = st_check_counter;
                    //  greedy:  3.check_counter(4|6), 4.piece, 5.LAorC0WR(3|0), 6.OutOfLoop.
                    //  !greedy: 3.check_counter(6|4), 4.piece, 5.LAorC0WR(3|0), 6.OutOfLoop.
                    //  4.piece = { 4a.increment_counter(4c|4b), 4b.decrement_counter(0|0), 4c.OriginalPiece }.
                }

                //  atom.type is epsilon or check_counter.
                //  Its "next"s point to piece and OutOfLoop.

                if (!piece_is_noncapturinggroup_contaning_capturinggroup && (piececharlen.atleast || piece_has_0widthchecker)) {
                    const typename state_array::size_type piece_size = piece.size();
                    state_type& lastatom = piece[piece_size - 1];

                    lastatom.next1 = 0 - static_cast<std::ptrdiff_t>(piece_size);
                    //  Points to the one immediately before piece, which will be pushed last in this block.

                //  atom.type has already been set. epsilon or check_counter.
                    atom.next1 = 1;
                    atom.next2 = static_cast<std::ptrdiff_t>(piece_size) + 1;
                    if (!quantifier.is_greedy) {
                        atom.next1 = atom.next2;
                        atom.next2 = 1;
                    }
                    piece_with_quantifier.push_back(atom);
                } else {
                    //  atom.type has already been set. epsilon or check_counter.
                    atom.next1 = 1;
                    atom.next2 = static_cast<std::ptrdiff_t>(piece.size()) + 4;	//  To OutOfLoop.
                        //  The reason for +3 than above is that push, pop, and check_0_width are added below.
                    if (!quantifier.is_greedy) {
                        atom.next1 = atom.next2;
                        atom.next2 = 1;
                    }
                    piece_with_quantifier.push_back(atom);	//  *1

                    atom.number = this->number_of_repeats;
                    ++this->number_of_repeats;

                    const state_size_type org1stpos = (atom.type == st_check_counter) ? 2 : 0;

                    if (piece_is_noncapturinggroup_contaning_capturinggroup)
                        atom.quantifier = piece[org1stpos].quantifier;
                    else
                        atom.quantifier.set(1, 0);

                    atom.type = st_repeat_in_pop;
                    atom.next1 = 0;
                    atom.next2 = 0;
                    piece.insert(org1stpos, atom);

                    atom.type = st_repeat_in_push;
                    atom.next1 = 2;
                    atom.next2 = 1;
                    piece.insert(org1stpos, atom);

                    atom.type = st_check_0_width_repeat;
                    atom.next1 = 0 - static_cast<std::ptrdiff_t>(piece.size()) - 1;	//  Points to *1.
                    atom.next2 = 1;
                    piece.push_back(atom);
                    //  greedy:  1.epsilon(2|6),
                    //  !greedy: 1.epsilon(6|2),
                    //    2.repeat_in_push(4|3), 3.repeat_in_pop(0|0), 4.piece,
                    //    5.check_0_width_repeat(1|6), 6.OutOfLoop.
                    //  or
                    //  greedy:  1.check_counter(2|8),
                    //  !greedy: 1.check_counter(8|2),
                    //    2.increment_counter(4|3), 3.decrement_counter(0|0)
                    //    4.repeat_in_push(6|5), 5.repeat_in_pop(0|0), 6.piece,
                    //    7.check_0_width_repeat(1|8), 8.OutOfLoop.
                }
                piece_with_quantifier += piece;
            }

#if !defined(SRELL_NO_NAMEDCAPTURE)
            bool parse_groupname(const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
                const gname_string groupname = get_groupname(curpos, end, cstate);

                if (!this->namedcaptures.push_back(groupname, this->number_of_brackets))
                    this->throw_error(regex_constants::error_backref);

                return true;
            }
#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)

            //  '['.

            bool register_character_class(state_type& atom, const uchar32*& curpos, const uchar32* const end, const re_compiler_state<charT>& /* cstate */) {
                range_pair code_range;
                range_pairs ranges;
                state_type classatom;

                if (curpos == end)
                    this->throw_error(regex_constants::error_brack);

                atom.type = st_character_class;

                if (*curpos == meta_char::mc_caret)	//  '^'
                {
                    atom.is_not = true;
                    ++curpos;
                }

                for (;;) {
                    if (curpos == end)
                        this->throw_error(regex_constants::error_brack);

                    if (*curpos == meta_char::mc_sbracl)	//   ']'
                        break;

                    classatom.reset();

                    if (!get_character_in_class(classatom, curpos, end))
                        return false;

                    if (classatom.type == st_character_class) {
                        add_predefclass_to_charclass(ranges, classatom);
                        continue;
                    }

                    code_range.first = code_range.second = classatom.character;

                    if (curpos == end)
                        this->throw_error(regex_constants::error_brack);

                    if (*curpos == meta_char::mc_minus)	//  '-'
                    {
                        ++curpos;

                        if (curpos == end)
                            this->throw_error(regex_constants::error_brack);

                        if (*curpos == meta_char::mc_sbracl) {
                        PUSH_SEPARATELY:
                            ranges.join(code_range);
                            code_range.first = code_range.second = meta_char::mc_minus;
                        } else {
                            if (!get_character_in_class(classatom, curpos, end))
                                return false;

                            if (classatom.type == st_character_class) {
                                add_predefclass_to_charclass(ranges, classatom);
                                goto PUSH_SEPARATELY;
                            }

                            code_range.second = classatom.character;

                            if (!code_range.is_range_valid())
                                this->throw_error(regex_constants::error_range);
                        }
                    }
                    ranges.join(code_range);
                }

                //  *curpos == ']'
                ++curpos;
                if (this->is_icase())
                    ranges.make_caseunfoldedcharset();

                if (atom.is_not) {
                    ranges.negation();
                    atom.is_not = false;
                }

                //		atom.character = this->is_icase() ? ranges.template consists_of_one_character<unicode_case_folding>() : ranges.template consists_of_one_character<nocase_faketraits>();
                atom.character = ranges.consists_of_one_character(this->is_icase());

                if (atom.character != constants::invalid_u32value) {
                    atom.type = st_character;
                    return true;
                }

                atom.number = this->character_class.register_newclass(ranges);

                return true;
            }

            bool get_character_in_class(state_type& atom, const uchar32*& curpos, const uchar32* const end /* , const re_compiler_state &cstate */) {
                atom.character = *curpos++;

                return atom.character != meta_char::mc_escape	//  '\\'
                    || translate_escseq(atom, curpos, end);
            }

            void add_predefclass_to_charclass(range_pairs& cls, const state_type& classatom) {
                range_pairs predefclass = this->character_class[classatom.number];

                if (classatom.is_not)
                    predefclass.negation();

                cls.merge(predefclass);
            }

            //  Escape characters which appear both in and out of [] pairs.
            bool translate_escseq(state_type& atom, const uchar32*& curpos, const uchar32* const end) {
                if (curpos == end)
                    this->throw_error(regex_constants::error_escape);

                atom.character = *curpos++;

                switch (atom.character) {
                    //  Predefined classes.

                    case char_alnum::ch_D:	//  'D':
                        atom.is_not = true;
                        //@fallthrough@

                    case char_alnum::ch_d:	//  'd':
                        atom.number = static_cast<uint_l32>(re_character_class::digit);	//  \d, \D.
                        atom.type = st_character_class;
                        break;

                    case char_alnum::ch_S:	//  'S':
                        atom.is_not = true;
                        //@fallthrough@

                    case char_alnum::ch_s:	//  's':
                        atom.number = static_cast<uint_l32>(re_character_class::space);	//  \s, \S.
                        atom.type = st_character_class;
                        break;

                    case char_alnum::ch_W:	//  'W':
                        atom.is_not = true;
                        //@fallthrough@

                    case char_alnum::ch_w:	//  'w':
                        if (this->is_icase()) {
                            this->character_class.setup_icase_word();
                            atom.number = static_cast<uint_l32>(re_character_class::icase_word);
                        } else
                            atom.number = static_cast<uint_l32>(re_character_class::word);	//  \w, \W.
                        atom.type = st_character_class;
                        break;

#if !defined(SRELL_NO_UNICODE_PROPERTY)
                        //  Prepared for Unicode properties and script names.
                    case char_alnum::ch_P:	//  \P{...}
                        atom.is_not = true;
                        //@fallthrough@

                    case char_alnum::ch_p:	//  \p{...}
                        atom.number = get_property_number(curpos, end);
                        atom.type = st_character_class;
                        break;
#endif	//  !defined(SRELL_NO_UNICODE_PROPERTY)

                    case char_alnum::ch_b:
                        atom.character = char_ctrl::cc_bs;	//  '\b' 0x08:BS
                        break;

                    case char_alnum::ch_t:
                        atom.character = char_ctrl::cc_htab;	//  '\t' 0x09:HT
                        break;

                    case char_alnum::ch_n:
                        atom.character = char_ctrl::cc_nl;	//  '\n' 0x0a:LF
                        break;

                    case char_alnum::ch_v:
                        atom.character = char_ctrl::cc_vtab;	//  '\v' 0x0b:VT
                        break;

                    case char_alnum::ch_f:
                        atom.character = char_ctrl::cc_ff;	//  '\f' 0x0c:FF
                        break;

                    case char_alnum::ch_r:
                        atom.character = char_ctrl::cc_cr;	//  '\r' 0x0d:CR
                        break;

                    case char_alnum::ch_c:	//  \cX
                        if (curpos != end) {
                            //				atom.character = static_cast<uchar32>(utf_traits().codepoint_inc(curpos, end) & 0x1f);	//  *curpos++
                            atom.character = static_cast<uchar32>(*curpos | constants::asc_icase);

                            if (atom.character >= char_alnum::ch_a && atom.character <= char_alnum::ch_z)
                                atom.character = static_cast<uchar32>(*curpos++ & 0x1f);
                            else {
                                this->throw_error(regex_constants::error_escape);	//  Strict.
            //					atom.character = char_alnum::ch_c;	//  Loose.
                            }
                        }
                        break;

                    case char_alnum::ch_0:
                        atom.character = char_ctrl::cc_nul;	//  '\0' 0x00:NUL
                        break;

                    case char_alnum::ch_u:	//  \uhhhh, \u{h~hhhhhh}
                        atom.character = parse_escape_u(curpos, end);
                        break;

                    case char_alnum::ch_x:	//  \xhh
                        atom.character = translate_numbers(curpos, end, 16, 2, 2, 0xff);
                        break;

                        //  SyntaxCharacter, '/', and '-'.
                    case meta_char::mc_caret:	//  '^'
                    case meta_char::mc_dollar:	//  '$'
                    case meta_char::mc_escape:	//  '\\'
                    case meta_char::mc_period:	//  '.'
                    case meta_char::mc_astrsk:	//  '*'
                    case meta_char::mc_plus:	//  '+'
                    case meta_char::mc_query:	//  '?'
                    case meta_char::mc_rbraop:	//  '('
                    case meta_char::mc_rbracl:	//  ')'
                    case meta_char::mc_sbraop:	//  '['
                    case meta_char::mc_sbracl:	//  ']'
                    case meta_char::mc_cbraop:	//  '{'
                    case meta_char::mc_cbracl:	//  '}'
                    case meta_char::mc_bar:		//  '|'
                    case char_other::co_slash:	//  '/'
                    case meta_char::mc_minus:	//  '-' allowed only in charclass.
                        break;

                    default:
                        atom.character = constants::invalid_u32value;
                }

                if (atom.character == constants::invalid_u32value)
                    this->throw_error(regex_constants::error_escape);

                return true;
            }

            uchar32 parse_escape_u(const uchar32*& curpos, const uchar32* const end) const {
                uchar32 ucp;

                if (curpos == end)
                    return constants::invalid_u32value;

                if (*curpos == meta_char::mc_cbraop) {
                    //			ucp = translate_numbers(++curpos, end, 16, 1, 6, constants::unicode_max_codepoint, true);
                    ucp = translate_numbers(++curpos, end, 16, 1, 0, constants::unicode_max_codepoint);

                    if (curpos == end || *curpos != meta_char::mc_cbracl)
                        return constants::invalid_u32value;

                    ++curpos;
                } else {
                    ucp = translate_numbers(curpos, end, 16, 4, 4, 0xffff);

                    if (ucp >= 0xd800 && ucp <= 0xdbff) {
                        const uchar32* prefetch = curpos;

                        if (prefetch != end && *prefetch == meta_char::mc_escape && ++prefetch != end && *prefetch == char_alnum::ch_u) {
                            const uchar32 nextucp = translate_numbers(++prefetch, end, 16, 4, 4, 0xffff);

                            if (nextucp >= 0xdc00 && nextucp <= 0xdfff) {
                                curpos = prefetch;
                                ucp = (((ucp << 10) & 0xffc00) | (nextucp & 0x3ff)) + 0x10000;
                            }
                        }
                    }
                }
                return ucp;
            }

#if !defined(SRELL_NO_UNICODE_PROPERTY)
            uint_l32 get_property_number(const uchar32*& curpos, const uchar32* const end) {
                if (curpos == end || *curpos != meta_char::mc_cbraop)	//  '{'
                    this->throw_error(regex_constants::error_escape);

                pstring pname;
                pstring pvalue(get_property_name_or_value(++curpos, end));

                if (!pvalue.size())
                    this->throw_error(regex_constants::error_escape);

                if (static_cast<uchar32>(pvalue[pvalue.size() - 1]) != char_other::co_sp)	//  ' ', not a value.
                {
                    if (curpos == end)
                        this->throw_error(regex_constants::error_escape);

                    if (*curpos == meta_char::mc_eq)	//  '='
                    {
                        pname = pvalue;
                        pvalue = get_property_name_or_value(++curpos, end);
                        if (!pvalue.size())
                            this->throw_error(regex_constants::error_escape);
                    }
                }

                if (curpos == end || *curpos != meta_char::mc_cbracl)	//  '}'
                    this->throw_error(regex_constants::error_escape);

                if (static_cast<uchar32>(pvalue[pvalue.size() - 1]) == char_other::co_sp)	//  ' ', value.
                    pvalue.resize(pvalue.size() - 1);

                ++curpos;

                const uint_l32 class_number = this->character_class.lookup_property(pname, pvalue, this->is_icase());

                if (class_number == re_character_class::error_property)
                    this->throw_error(regex_constants::error_escape);

                return class_number;
            }

            pstring get_property_name_or_value(const uchar32*& curpos, const uchar32* const end) const {
                pstring name_or_value;
                bool number_found = false;

                for (;; ++curpos) {
                    if (curpos == end)
                        break;

                    const uchar32 curchar = *curpos;

                    if (curchar >= char_alnum::ch_A && curchar <= char_alnum::ch_Z)
                        ;
                    else if (curchar >= char_alnum::ch_a && curchar <= char_alnum::ch_z)
                        ;
                    else if (curchar == char_other::co_ll)	//  '_'
                        ;
                    else if (curchar >= char_alnum::ch_0 && curchar <= char_alnum::ch_9)
                        number_found = true;
                    else
                        break;

                    name_or_value.append(1, static_cast<typename pstring::value_type>(curchar));
                }
                if (number_found)
                    name_or_value.append(1, char_other::co_sp);	//  ' '

                return name_or_value;
            }
#endif	//  !defined(SRELL_NO_UNICODE_PROPERTY)

            //  Escape characters which do not appear in [] pairs.

            bool translate_atom_escape(state_type& atom, const uchar32*& curpos, const uchar32* const end, /* const */ re_compiler_state<charT>& cstate) {
                if (curpos == end)
                    this->throw_error(regex_constants::error_escape);

                atom.character = *curpos;

                switch (atom.character) {
                    case meta_char::mc_minus:	//  '-'
                        this->throw_error(regex_constants::error_escape);
                        //@fallthrough@

                    case char_alnum::ch_B:	//  'B':
                        atom.is_not = true;
                        //@fallthrough@

                    case char_alnum::ch_b:	//  'b':
                        atom.type = st_boundary;	//  \b, \B.
                        atom.quantifier.reset(0);
                        //			atom.number = 0;
                        if (this->is_icase()) {
                            this->character_class.setup_icase_word();
                            atom.number = static_cast<uint_l32>(re_character_class::icase_word);
                        } else
                            atom.number = static_cast<uint_l32>(re_character_class::word);	//  \w, \W.
                        break;

                        //		case char_alnum::ch_A:	//  'A':
                        //			atom.type   = st_bol;	//  '\A'
                        //		case char_alnum::ch_Z:	//  'Z':
                        //			atom.type   = st_eol;	//  '\Z'
                        //		case char_alnum::ch_z:	//  'z':
                        //			atom.type   = st_eol;	//  '\z'

                                //  Backreferences.

#if !defined(SRELL_NO_NAMEDCAPTURE)
        //  Prepared for named captures.
                    case char_alnum::ch_k:	//  'k':
                        return parse_backreference_name(atom, curpos, end, cstate);	//  \k.
#endif

                    default:

                        if (atom.character >= char_alnum::ch_1 && atom.character <= char_alnum::ch_9)	//  \1, \9.
                            return parse_backreference_number(atom, curpos, end, cstate);

                        translate_escseq(atom, curpos, end);

                        if (atom.type == st_character_class) {
                            range_pairs newclass = this->character_class[atom.number];

                            if (atom.is_not) {
                                newclass.negation();
                                atom.is_not = false;
                            }

                            if (this->is_icase() && atom.number >= static_cast<uint_l32>(re_character_class::number_of_predefcls))
                                newclass.make_caseunfoldedcharset();

                            atom.number = this->character_class.register_newclass(newclass);
                        }
                        return true;
                }

                ++curpos;
                return true;
            }

            bool parse_backreference_number(state_type& atom, const uchar32*& curpos, const uchar32* const end, const re_compiler_state<charT>& cstate) {
                const uchar32 backrefno = translate_numbers(curpos, end, 10, 0, 0, 0xfffffffe);
                //  22.2.1.1 Static Semantics: Early Errors:
                //  It is a Syntax Error if NcapturingParens >= 23^2 - 1.

                if (backrefno == constants::invalid_u32value)
                    this->throw_error(regex_constants::error_escape);

                atom.number = static_cast<uint_l32>(backrefno);
                atom.backrefnumber_unresolved = false;

                return backreference_postprocess(atom, cstate);
            }

            bool backreference_postprocess(state_type& atom, const re_compiler_state<charT>& /* cstate */) const {
                atom.next2 = 1;
                atom.type = st_backreference;

                //		atom.quantifier.atleast = cstate.atleast_widths_of_brackets[atom.number - 1];
                            //  Moved to check_backreferences().

                return true;
            }

#if !defined(SRELL_NO_NAMEDCAPTURE)
            bool parse_backreference_name(state_type& atom, const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate) {
                if (++curpos == end || *curpos != meta_char::mc_lt)
                    this->throw_error(regex_constants::error_escape);

                const gname_string groupname = get_groupname(++curpos, end, cstate);

                atom.number = this->namedcaptures[groupname];

                if (atom.number != groupname_mapper<charT>::notfound)
                    atom.backrefnumber_unresolved = false;
                else {
                    atom.backrefnumber_unresolved = true;
                    atom.number = static_cast<uint_l32>(cstate.unresolved_gnames.size());
                    cstate.unresolved_gnames.push_back(groupname, atom.number);
                }

                return backreference_postprocess(atom, cstate);
            }

#if !defined(SRELL_NO_UNICODE_PROPERTY)
            gname_string get_groupname(const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>& cstate)
#else
            gname_string get_groupname(const uchar32*& curpos, const uchar32* const end, re_compiler_state<charT>&)
#endif
            {
                charT mbstr[utf_traits::maxseqlen];
                gname_string groupname;

#if !defined(SRELL_NO_UNICODE_PROPERTY)
                cstate.idchecker.setup();
#endif
                for (;;) {
                    if (curpos == end)
                        this->throw_error(regex_constants::error_escape);

                    uchar32 curchar = *curpos++;

                    if (curchar == meta_char::mc_gt)	//  '>'
                        break;

                    if (curchar == meta_char::mc_escape && curpos != end && *curpos == char_alnum::ch_u)	//  '\\', 'u'.
                        curchar = parse_escape_u(++curpos, end);

#if defined(SRELL_NO_UNICODE_PROPERTY)
                    if (curchar != meta_char::mc_escape)
#else
                    if (cstate.idchecker.is_identifier(curchar, groupname.size() != 0))
#endif
                        ;	//  OK.
                    else
                        curchar = constants::invalid_u32value;

                    if (curchar == constants::invalid_u32value)
                        this->throw_error(regex_constants::error_escape);

                    const uchar32 seqlen = utf_traits::to_codeunits(mbstr, curchar);
                    for (uchar32 i = 0; i < seqlen; ++i)
                        groupname.append(1, mbstr[i]);
                }
                if (!groupname.size())
                    this->throw_error(regex_constants::error_escape);

                return groupname;
            }
#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)

            bool get_quantifier(re_quantifier& quantifier, const uchar32*& curpos, const uchar32* const end) {
                switch (*curpos) {
                    case meta_char::mc_astrsk:	//  '*':
                        --quantifier.atleast;
                        //@fallthrough@

                    case meta_char::mc_plus:	//  '+':
                        quantifier.set_infinity();
                        break;

                    case meta_char::mc_query:	//  '?':
                        --quantifier.atleast;
                        break;

                    case meta_char::mc_cbraop:	//  '{':
                        get_brace_with_quantifier(quantifier, curpos, end);
                        break;

                    default:
                        return true;
                }

                if (++curpos != end && *curpos == meta_char::mc_query)	//  '?'
                {
                    quantifier.is_greedy = false;
                    ++curpos;
                }
                return true;
            }

            void get_brace_with_quantifier(re_quantifier& quantifier, const uchar32*& curpos, const uchar32* const end) {
                ++curpos;

                quantifier.atleast = static_cast<uint_l32>(translate_numbers(curpos, end, 10, 1, 0, constants::max_u32value));

                if (quantifier.atleast == static_cast<uint_l32>(constants::invalid_u32value))
                    goto THROW_ERROR_BRACE;

                if (curpos == end)
                    goto THROW_ERROR_BRACE;

                if (*curpos == meta_char::mc_comma)	//  ','
                {
                    ++curpos;

                    quantifier.atmost = static_cast<uint_l32>(translate_numbers(curpos, end, 10, 1, 0, constants::max_u32value));

                    if (quantifier.atmost == static_cast<uint_l32>(constants::invalid_u32value))
                        quantifier.set_infinity();

                    if (!quantifier.is_valid())
                        this->throw_error(regex_constants::error_badbrace);
                } else
                    quantifier.atmost = quantifier.atleast;

                if (curpos == end || *curpos != meta_char::mc_cbracl)	//  '}'
                {
                THROW_ERROR_BRACE:
                    this->throw_error(regex_constants::error_brace);
                }
                //  *curpos == '}'
            }

            uchar32 translate_numbers(const uchar32*& curpos, const uchar32* const end, const int radix, const std::size_t minsize, const std::size_t maxsize, const uchar32 maxvalue) const {
                std::size_t count = 0;
                uchar32 u32value = 0;
                int num;

                for (; maxsize == 0 || count < maxsize; ++curpos, ++count) {

                    if (curpos == end || (num = tonumber(*curpos, radix)) == -1)
                        break;

                    const uchar32 nextvalue = u32value * radix + num;

                    if ((/* maxvalue != 0 && */ nextvalue > maxvalue) || nextvalue < u32value)
                        break;

                    u32value = nextvalue;
                }

                if (count >= minsize)
                    return u32value;

                return constants::invalid_u32value;
            }

            int tonumber(const uchar32 ch, const int radix) const {
                if ((ch >= char_alnum::ch_0 && ch <= char_alnum::ch_7) || (radix >= 10 && (ch == char_alnum::ch_8 || ch == char_alnum::ch_9)))
                    return static_cast<int>(ch - char_alnum::ch_0);

                if (radix == 16) {
                    if (ch >= char_alnum::ch_a && ch <= char_alnum::ch_f)
                        return static_cast<int>(ch - char_alnum::ch_a + 10);

                    if (ch >= char_alnum::ch_A && ch <= char_alnum::ch_F)
                        return static_cast<int>(ch - char_alnum::ch_A + 10);
                }
                return -1;
            }

            bool check_backreferences(re_compiler_state<charT>& cstate) {
                for (typename state_array::size_type backrefpos = 0; backrefpos < this->NFA_states.size(); ++backrefpos) {
                    state_type& brs = this->NFA_states[backrefpos];

                    if (brs.type == st_backreference) {
                        const uint_l32& backrefno = brs.number;

#if !defined(SRELL_NO_NAMEDCAPTURE)
                        if (brs.backrefnumber_unresolved) {
                            if (backrefno >= cstate.unresolved_gnames.size())
                                return false;	//  Internal error.

                            brs.number = this->namedcaptures[cstate.unresolved_gnames[backrefno]];

                            if (backrefno == groupname_mapper<charT>::notfound)
                                return false;

                            brs.backrefnumber_unresolved = false;
                        }
#endif

                        for (typename state_array::size_type roundbracket_closepos = 0;; ++roundbracket_closepos) {
                            if (roundbracket_closepos < this->NFA_states.size()) {
                                const state_type& rbcs = this->NFA_states[roundbracket_closepos];

                                if (rbcs.type == st_roundbracket_close && rbcs.number == backrefno) {
                                    if (roundbracket_closepos < backrefpos) {
                                        //								brs.quantifier.atleast = cstate.atleast_widths_of_brackets[backrefno - 1];
                                                                        //  20210429: It was reported that clang-tidy was dissatisfied with this code.
                                                                        //  20211006: Replaced with the following code:

                                        const uint_l32 backrefnoindex = backrefno - 1;

                                        //  This can never be true. Added only for satisfying clang-tidy.
                                        if (backrefnoindex >= cstate.atleast_widths_of_brackets.size())
                                            return false;

                                        brs.quantifier.atleast = cstate.atleast_widths_of_brackets[backrefnoindex];

                                        cstate.backref_used = true;
                                    } else {
                                        brs.type = st_epsilon;
                                        brs.next2 = 0;
                                    }
                                    break;
                                }
                            } else
                                return false;
                        }
                    }
                }
                return true;
            }

#if !defined(SRELLDBG_NO_1STCHRCLS)

            void create_firstchar_class() {
#if !defined(SRELLDBG_NO_BITSET)
                range_pairs fcc;
#else
                range_pairs& fcc = this->firstchar_class;
#endif

                const bool canbe0length = gather_nextchars(fcc, static_cast<typename state_array::size_type>(this->NFA_states[0].next1), 0u, false);

                if (canbe0length) {
                    fcc.set_solerange(range_pair_helper(0, constants::unicode_max_codepoint));
                    //  Expressions would consist of assertions only, such as /^$/.
                    //  We cannot but accept every codepoint.
                }

#if !defined(SRELLDBG_NO_BITSET)
                this->NFA_states[0].quantifier.atleast = this->character_class.register_newclass(fcc);

                set_bitset_table(fcc);
#endif
            }

#if !defined(SRELLDBG_NO_BITSET)
            void set_bitset_table(const range_pairs& fcc) {
                for (typename range_pairs::size_type i = 0; i < fcc.size(); ++i) {
                    const range_pair& range = fcc[i];

#if 0
                    uchar32 second = range.second <= constants::unicode_max_codepoint ? range.second : constants::unicode_max_codepoint;

#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(push)
#pragma warning(disable:4127)
#endif
                    if (utf_traits::utftype == 16)
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(pop)
#endif
                    {
                        if (second >= 0x10000 && range.first < 0x10000) {
                            this->firstchar_class_bs.set_range(utf_traits::firstcodeunit(0x10000) & utf_traits::bitsetmask, utf_traits::firstcodeunit(second) & utf_traits::bitsetmask);
                            second = 0xffff;
                        }
                    }
                    this->firstchar_class_bs.set_range(utf_traits::firstcodeunit(range.first) & utf_traits::bitsetmask, utf_traits::firstcodeunit(second) & utf_traits::bitsetmask);

#else
                    for (uchar32 ucp = range.first; ucp <= constants::unicode_max_codepoint; ++ucp) {
                        this->firstchar_class_bs.set(utf_traits::firstcodeunit(ucp) & utf_traits::bitsetmask);

                        if (ucp == range.second)
                            break;
                    }
#endif
                }
            }
#endif	//  !defined(SRELLDBG_NO_BITSET)
#endif	//  !defined(SRELLDBG_NO_1STCHRCLS)

            bool gather_nextchars(range_pairs& nextcharclass, typename state_array::size_type pos, simple_array<bool>& checked, const uint_l32 bracket_number, const bool subsequent) const {
                bool canbe0length = false;

                for (;;) {
                    const state_type& state = this->NFA_states[pos];

                    if (checked[pos])
                        break;

                    checked[pos] = true;

                    if (state.next2
                        && (state.type != st_check_counter || !state.quantifier.is_greedy || state.quantifier.atleast == 0)
                        && (state.type != st_save_and_reset_counter)
                        && (state.type != st_roundbracket_open)
                        && (state.type != st_roundbracket_close || state.number != bracket_number)
                        && (state.type != st_repeat_in_push)
                        && (state.type != st_backreference || (state.quantifier.atleast == 0 && state.next1 != state.next2))
                        && (state.type != st_lookaround_open))
                        if (gather_nextchars(nextcharclass, pos + state.next2, checked, bracket_number, subsequent))
                            canbe0length = true;

                    switch (state.type) {
                        case st_character:
                            nextcharclass.join(range_pair_helper(state.character));

                            if (this->is_ricase())
                                nextcharclass.make_caseunfoldedcharset();

                            return canbe0length;

                        case st_character_class:
                            nextcharclass.merge(this->character_class[state.number]);
                            return canbe0length;

                        case st_backreference:
                        {
                            const typename state_array::size_type nextpos = find_next1_of_bracketopen(state.number);

                            const bool length0 = gather_nextchars(nextcharclass, nextpos, state.number, subsequent);

                            if (!length0)
                                return canbe0length;
                        }
                        break;

                        case st_eol:
                        case st_bol:
                            if (!subsequent)
                                break;

                            //@fallthrough@

                        case st_boundary:
                            if (subsequent)
                                nextcharclass.set_solerange(range_pair_helper(0, constants::unicode_max_codepoint));

                            break;

                        case st_lookaround_open:
                            //				if (!state.is_not && !state.reverse)
                            if (!state.is_not && state.quantifier.atleast == 0) {
                                gather_nextchars(nextcharclass, pos + 1, checked, 0u, subsequent);
                            } else if (subsequent)
                                nextcharclass.set_solerange(range_pair_helper(0, constants::unicode_max_codepoint));

                            break;

                        case st_roundbracket_close:
                            if (/* bracket_number == 0 || */ state.number != bracket_number)
                                break;
                            //@fallthrough@

                        case st_success:	//  == st_lookaround_close.
                            return true;

                        case st_check_counter:
                            if (!state.quantifier.is_greedy && state.quantifier.atleast >= 1)
                                return canbe0length;
                            //@fallthrough@

                        default:;
                    }

                    if (state.next1)
                        pos += state.next1;
                    else
                        break;
                }
                return canbe0length;
            }

            bool gather_nextchars(range_pairs& nextcharclass, const typename state_array::size_type pos, const uint_l32 bracket_number, const bool subsequent) const {
                simple_array<bool> checked;

                checked.resize(this->NFA_states.size(), false);
                return gather_nextchars(nextcharclass, pos, checked, bracket_number, subsequent);
            }

            typename state_array::size_type find_next1_of_bracketopen(const uint_l32 bracketno) const {
                for (typename state_array::size_type no = 0; no < this->NFA_states.size(); ++no) {
                    const state_type& state = this->NFA_states[no];

                    if (state.type == st_roundbracket_open && state.number == bracketno)
                        return no + state.next1;
                }
                return 0;
            }

            void relativejump_to_absolutejump() {
                for (typename state_array::size_type pos = 0; pos < this->NFA_states.size(); ++pos) {
                    state_type& state = this->NFA_states[pos];

#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                    if (state.next1 || state.type == st_character || state.type == st_character_class)
#else
                    if (state.next1)
#endif
                        state.next_state1 = &this->NFA_states[pos + state.next1];
                    else
                        state.next_state1 = NULL;

                    if (state.next2)
                        state.next_state2 = &this->NFA_states[pos + state.next2];
                    else
                        state.next_state2 = NULL;
                }
            }

            void optimise() {
#if !defined(SRELLDBG_NO_BRANCH_OPT2) && !defined(SRELLDBG_NO_ASTERISK_OPT)
                branch_optimisation2();
#endif

#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                asterisk_optimisation();
#endif

#if !defined(SRELLDBG_NO_BRANCH_OPT) && !defined(SRELLDBG_NO_ASTERISK_OPT)
                branch_optimisation();
#endif

#if !defined(SRELLDBG_NO_1STCHRCLS)
                create_firstchar_class();
#endif

#if !defined(SRELLDBG_NO_SKIP_EPSILON)
                skip_epsilon();
#endif

#if !defined(SRELLDBG_NO_CCPOS)
                set_charclass_posinfo();
#endif
            }

#if !defined(SRELLDBG_NO_SKIP_EPSILON)

            void skip_epsilon() {
                for (typename state_array::size_type pos = 0; pos < this->NFA_states.size(); ++pos) {
                    state_type& state = this->NFA_states[pos];

                    if (state.next1)
                        state.next1 = static_cast<std::ptrdiff_t>(skip_nonbranch_epsilon(pos + state.next1) - pos);

                    if (state.next2)
                        state.next2 = static_cast<std::ptrdiff_t>(skip_nonbranch_epsilon(pos + state.next2) - pos);
                }
            }

            typename state_array::size_type skip_nonbranch_epsilon(typename state_array::size_type pos) const {
                for (;;) {
                    const state_type& state = this->NFA_states[pos];

                    if (state.type == st_epsilon && state.next2 == 0) {
                        pos += state.next1;
                        continue;
                    }
                    break;
                }
                return pos;
            }

#endif

#if !defined(SRELLDBG_NO_ASTERISK_OPT)

            void asterisk_optimisation() {
                state_type* prevstate_is_astrskepsilon = NULL;
                const state_type* prevcharstate = NULL;
                state_size_type mnp_inspos = 0;
                bool inspos_updatable = true;
#if !defined(SRELLDBG_NO_SPLITCC)
                bool inserted = false;
#endif

                for (typename state_array::size_type cur = 1; cur < this->NFA_states.size(); ++cur) {
                    state_type& curstate = this->NFA_states[cur];

                    switch (curstate.type) {
                        case st_epsilon:
                            if (curstate.character == meta_char::mc_astrsk) {
                                prevstate_is_astrskepsilon = &curstate;
                            } else {
                                prevstate_is_astrskepsilon = NULL;
                                inspos_updatable = false;
                            }
                            break;

                        case st_character:
                        case st_character_class:
                            if (inspos_updatable) {
                                if (prevcharstate) {
                                    if (prevcharstate->type != curstate.type || prevcharstate->number != curstate.number)
                                        inspos_updatable = false;
                                }
                                if (inspos_updatable) {
                                    if (prevstate_is_astrskepsilon) {
                                        inspos_updatable = false;
                                        if (prevstate_is_astrskepsilon->quantifier.is_asterisk_or_plus()) {
                                            mnp_inspos = cur + 1;
                                        }
                                    }
                                }
                                prevcharstate = &curstate;
                            }
                            if (prevstate_is_astrskepsilon) {
                                const re_quantifier& eq = prevstate_is_astrskepsilon->quantifier;
                                const state_size_type epsilonno = cur - 1;
                                const state_size_type faroffset = eq.is_greedy ? prevstate_is_astrskepsilon->next2 : prevstate_is_astrskepsilon->next1;
                                const state_size_type nextno = epsilonno + faroffset;
#if !defined(SRELLDBG_NO_SPLITCC)
                                const state_size_type origlen = this->NFA_states.size();
#endif

                                if (is_exclusive_sequence(eq, cur, nextno)) {
                                    state_type& epsilonstate = this->NFA_states[epsilonno];
                                    state_type& curstate2 = this->NFA_states[cur];

                                    epsilonstate.next1 = 1;
                                    epsilonstate.next2 = 0;
                                    epsilonstate.number = 0;
                                    //						curstate2.quantifier.is_greedy = true;
                                    if (epsilonstate.quantifier.is_infinity()) {
                                        curstate2.next1 = 0;
                                        curstate2.next2 = faroffset - 1;
                                    } else	//  ? or {0,1}
                                    {
                                        curstate2.next2 = faroffset - 1;
                                    }

#if !defined(SRELLDBG_NO_SPLITCC)
                                    if (mnp_inspos == nextno && origlen != this->NFA_states.size())
                                        inserted = true;
#endif
                                }
                                prevstate_is_astrskepsilon = NULL;
                            }
                            break;

                        default:
                            prevstate_is_astrskepsilon = NULL;
                            inspos_updatable = false;
                    }
                }

#if !defined(SRELLDBG_NO_NEXTPOS_OPT)

                if (mnp_inspos != 0) {
                    state_size_type cur = mnp_inspos;

                    if (this->NFA_states[cur].type != st_success) {
                        const state_type& prevstate = this->NFA_states[cur - 1];

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND) && !defined(SRELLDBG_NO_MPREWINDER) && !defined(SRELLDBG_NO_1STCHRCLS) && !defined(SRELLDBG_NO_BITSET)

#if !defined(SRELLDBG_NO_SPLITCC)
                        if (!inserted && prevstate.next1 == 0)
#else
                        if (prevstate.next1 == 0)
#endif
                        {
                            range_pairs prevcc;
                            range_pairs nextcc;

                            //					gather_if_char_or_cc_strict(prevcc, prevstate);
                            if (prevstate.type == st_character) {
                                prevcc.set_solerange(range_pair_helper(prevstate.character));
                            } else if (prevstate.type == st_character_class) {
                                prevcc = this->character_class[prevstate.number];
                            }

                            gather_nextchars(nextcc, cur, 0u, true);

                            const uint_l32 cpnum_prevcc = prevcc.total_codepoints();
                            const uint_l32 cpnum_nextcc = nextcc.total_codepoints();

                            if (cpnum_nextcc != 0 && cpnum_nextcc < cpnum_prevcc) {
                                state_array newNFAs;
                                state_type atom;

                                atom.reset();
                                atom.character = meta_char::mc_eq;	//  '='
                                atom.type = st_lookaround_open;
                                atom.next1 = static_cast<std::ptrdiff_t>(cur - 1) * 2 + 2;
                                atom.next2 = 1;
                                atom.quantifier.atleast = 2; //  Match point rewinder.
                                newNFAs.append(1, atom);

                                newNFAs.append(this->NFA_states, 1, cur - 1);

                                atom.type = st_lookaround_close;
                                atom.next1 = 0;
                                atom.next2 = 0;
                                newNFAs.append(1, atom);

                                insert_at(1, newNFAs.size());
                                this->NFA_states.replace(1, newNFAs.size(), newNFAs);
                                this->NFA_states[0].next2 = this->NFA_states[0].next1;
                                this->NFA_states[0].next1 = 1;

                                return;
                            }
                        }
#endif	//  !defined(SRELL_FIXEDWIDTHLOOKBEHIND) && !defined(SRELLDBG_NO_MPREWINDER) && !defined(SRELLDBG_NO_1STCHRCLS) && !defined(SRELLDBG_NO_BITSET)

                        insert_at(cur, 1);
                        state_type& mnpstate = this->NFA_states[cur];
                        state_type& charstate = this->NFA_states[cur - 1];

                        mnpstate.type = st_move_nextpos;

#if !defined(SRELLDBG_NO_SPLITCC)

                        if (inserted) {
                            charstate.next2 = 1;
                        } else
#endif
                            if (charstate.next1 == 0) {
                                mnpstate.next1 = charstate.next2 - 1;
                                charstate.next2 = 1;
                            } else {
                                mnpstate.next1 = -2;
                                charstate.next1 = 1;
                            }
                    }
                }
#endif	//  !defined(SRELLDBG_NO_NEXTPOS_OPT)
            }

            bool is_exclusive_sequence(const re_quantifier& eq, const state_size_type curno, const state_size_type nextno)	//  const
            {
                const state_type& curstate = this->NFA_states[curno];
                range_pairs curchar_class;
                range_pairs nextchar_class;

                if (curstate.type == st_character) {
                    curchar_class.join(range_pair_helper(curstate.character));
                } else if (curstate.type == st_character_class) {
                    curchar_class = this->character_class[curstate.number];
                    if (curchar_class.size() == 0)	//  Means [], which always makes matching fail.
                        return true;	//  For preventing the automaton from pushing bt data.
                } else {
                    return false;
                }

                const bool canbe0length = gather_nextchars(nextchar_class, nextno, 0u, true);

                if (nextchar_class.size()) {
                    if (!canbe0length || eq.is_greedy) {
#if !defined(SRELLDBG_NO_SPLITCC)

                        range_pairs kept;
                        range_pairs removed;

                        curchar_class.split_ranges(kept, removed, nextchar_class);

                        if (removed.size() == 0)	//  !curchar_class.is_overlap(nextchar_class)
                            return true;

                        if (curstate.type == st_character_class && kept.size() && eq.is_infinity()) {
                            {
                                state_type& curstate2 = this->NFA_states[curno];

                                curstate2.character = kept.consists_of_one_character(this->is_icase());
                                if (curstate2.character != constants::invalid_u32value)
                                    curstate2.type = st_character;
                                else
                                    curstate2.number = this->character_class.register_newclass(kept);
                            }
                            const re_quantifier backupeq(eq);

                            insert_at(nextno, 2);
                            state_type& n0 = this->NFA_states[nextno];
                            state_type& n1 = this->NFA_states[nextno + 1];

                            n0.reset();
                            n0.type = st_epsilon;
                            n0.character = meta_char::mc_astrsk;
                            n0.quantifier = backupeq;
                            //					n0.next2 = 1;
                            n0.next2 = 2;
                            if (!n0.quantifier.is_greedy) {
                                n0.next1 = n0.next2;
                                n0.next2 = 1;
                            }

                            n1.reset();
                            n1.type = st_character_class;

                            n1.character = removed.consists_of_one_character(this->is_icase());
                            if (n1.character != constants::invalid_u32value)
                                n1.type = st_character;
                            else
                                n1.number = this->character_class.register_newclass(removed);

                            n1.next1 = -2;
                            //					n1.next2 = 0;
                            return true;
                        }

#else	//  defined(SRELLDBG_NO_SPLITCC)

                        if (!curchar_class.is_overlap(nextchar_class)) {
                            return true;
                        }

#endif	//  !defined(SRELLDBG_NO_SPLITCC)
                    }
                } else if (/* nextchar_class.size() == 0 && */ (!canbe0length || only_success_left(nextno))) {
                    //  (size() == 0 && !canbe0length) means [].
                    return eq.is_greedy;
                }

                return false;
            }

            bool only_success_left(typename state_array::size_type pos) const {
                for (;;) {
                    const state_type& state = this->NFA_states[pos];

                    switch (state.type) {
                        case st_success:
                            return true;

                        case st_roundbracket_close:
                        case st_backreference:
                            if (state.next2 != 0 && state.next1 != state.next2)
                                return false;
                            break;

                        case st_epsilon:
                            if (state.next2 != 0 && !only_success_left(pos + state.next2))
                                return false;
                            break;

                        case st_roundbracket_open:
                            break;	//  /a*()/

                        default:
                            return false;
                    }
                    if (state.next1)
                        pos += state.next1;
                    else
                        return false;
                }
            }
#endif	//  !defined(SRELLDBG_NO_ASTERISK_OPT)

            void insert_at(const typename state_array::size_type pos, const std::ptrdiff_t len) {
                state_type newstate;

                for (typename state_array::size_type cur = 0; cur < pos; ++cur) {
                    state_type& state = this->NFA_states[cur];

                    if (state.next1 && (cur + state.next1) >= pos)
                        state.next1 += len;

                    if (state.next2 && (cur + state.next2) >= pos)
                        state.next2 += len;
                }

                for (typename state_array::size_type cur = pos; cur < this->NFA_states.size(); ++cur) {
                    state_type& state = this->NFA_states[cur];

                    if ((cur + state.next1) < pos)
                        state.next1 -= len;

                    if ((cur + state.next2) < pos)
                        state.next2 -= len;
                }

                newstate.reset();
                newstate.type = st_epsilon;
                for (std::ptrdiff_t count = 0; count < len; ++count)
                    this->NFA_states.insert(pos, newstate);
            }

#if !defined(SRELLDBG_NO_NEXTPOS_OPT)
#endif	//  !defined(SRELLDBG_NO_NEXTPOS_OPT)

#if !defined(SRELLDBG_NO_BRANCH_OPT) || defined(SRELLTEST_NEXTPOS_OPT2)
            typename state_array::size_type gather_if_char_or_charclass(range_pairs& charclass, typename state_array::size_type pos) const {
                for (; pos < this->NFA_states.size();) {
                    const state_type& curstate = this->NFA_states[pos];

                    if (curstate.type == st_character && curstate.next2 == 0) {
                        charclass.set_solerange(range_pair_helper(curstate.character));
                        return pos;
                    } else if (curstate.type == st_character_class && curstate.next2 == 0) {
                        charclass = this->character_class[curstate.number];
                        return pos;
                    } else if (curstate.type == st_epsilon && curstate.next2 == 0) {
                    } else
                        break;

                    pos += curstate.next1;
                }
                return 0;
            }
#endif	//  !defined(SRELLDBG_NO_BRANCH_OPT) || defined(SRELLTEST_NEXTPOS_OPT2)

#if !defined(SRELLDBG_NO_BRANCH_OPT)
            void branch_optimisation() {
                range_pairs nextcharclass1;

                for (typename state_array::size_type pos = 0; pos < this->NFA_states.size(); ++pos) {
                    const state_type& state = this->NFA_states[pos];

                    if (state.is_branch()) {
                        const typename state_array::size_type nextcharpos = gather_if_char_or_charclass(nextcharclass1, pos + state.next1);

                        if (nextcharpos) {
                            range_pairs nextcharclass2;

                            const bool canbe0length = gather_nextchars(nextcharclass2, pos + state.next2, 0u /* bracket_number */, true);

                            if (!canbe0length && !nextcharclass1.is_overlap(nextcharclass2)) {
                                state_type& branch = this->NFA_states[pos];
                                state_type& next1 = this->NFA_states[nextcharpos];

                                next1.next2 = pos + branch.next2 - nextcharpos;
                                branch.next2 = 0;
                            }
                        }
                    }
                }
            }
#endif	//  !defined(SRELLDBG_NO_BRANCH_OPT)

#if !defined(SRELL_NO_ICASE)
            bool check_if_really_needs_icase_search() {
                uchar32 u32chars[unicode_case_folding::rev_maxset];

                for (typename state_array::size_type i = 0; i < this->NFA_states.size(); ++i) {
                    const state_type& state = this->NFA_states[i];

                    if (state.type == st_character) {
                        if (unicode_case_folding::casefoldedcharset(u32chars, state.character) > 1)
                            return true;
                    } else if (state.type == st_backreference)
                        return true;
                }
                //		this->soflags &= ~regex_constants::icase;
                return false;
            }
#endif	//  !defined(SRELL_NO_ICASE)

#if !defined(SRELLDBG_NO_BMH)
            void setup_bmhdata() {
                simple_array<uchar32> u32s;

                for (typename state_array::size_type i = 1; i < this->NFA_states.size(); ++i) {
                    const state_type& state = this->NFA_states[i];

                    if (state.type == st_character)
                        u32s.push_back(state.character);
                    else {
                        u32s.clear();
                        break;
                    }
                }

                if (u32s.size() > 1)
                    //		if ((u32s.size() > 1 && !this->is_ricase()) || (u32s.size() > 2 && this->is_ricase()))
                {
                    if (this->bmdata)
                        this->bmdata->clear();
                    else
                        this->bmdata = new re_bmh<charT, utf_traits>;

                    this->bmdata->setup(u32s, this->is_ricase());
                    return /* false */;
                }

                if (this->bmdata)
                    delete this->bmdata;
                this->bmdata = NULL;
                //		return true;
            }
#endif	//  !defined(SRELLDBG_NO_BMH)

#if !defined(SRELLDBG_NO_CCPOS)
            void set_charclass_posinfo() {
                this->character_class.finalise();
                for (typename state_array::size_type i = 1; i < this->NFA_states.size(); ++i) {
                    state_type& state = this->NFA_states[i];

                    if (state.type == st_character_class) {
                        const range_pair& posinfo = this->character_class.charclasspos(state.number);
                        state.quantifier.setccpos(posinfo.first, posinfo.second);
                    }
                }
            }
#endif	//  !defined(SRELLDBG_NO_CCPOS)

#if !defined(SRELLDBG_NO_BRANCH_OPT2)

            bool gather_if_char_or_charclass_strict(range_pairs& out, const state_type& state) const {
                if (state.type == st_character /* && state.next2 == 0 */) {
                    out.set_solerange(range_pair_helper(state.character));
                } else if (state.type == st_character_class /* && state.next2 == 0 */) {
                    out = this->character_class[state.number];
                } else
                    return false;

                return true;
            }

            void branch_optimisation2() {
                range_pairs basealt1stch;
                range_pairs nextalt1stch;

                for (state_size_type pos = 0; pos < this->NFA_states.size(); ++pos) {
                    const state_type& curstate = this->NFA_states[pos];

                    if (curstate.is_branch()) {
                        const state_size_type next1pos = pos + curstate.next1;
                        state_size_type precharchainpos = pos;

                        if (gather_if_char_or_charclass_strict(basealt1stch, this->NFA_states[next1pos])) {
                            state_size_type next2pos = precharchainpos + curstate.next2;
                            state_size_type postcharchainpos = 0;

                            for (;;) {
                                state_size_type next2next1pos = next2pos;
                                state_type& nstate2 = this->NFA_states[next2pos];
                                state_size_type next2next2pos = 0;

                                if (nstate2.is_branch()) {
                                    next2next2pos = next2pos + nstate2.next2;
                                    next2next1pos += nstate2.next1;
                                }

                                if (gather_if_char_or_charclass_strict(nextalt1stch, this->NFA_states[next2next1pos])) {
                                    const int relation = basealt1stch.relationship(nextalt1stch);

                                    if (relation == 0) {
                                        if (next2next2pos)	//  if (nstate2.is_branch())
                                        {
                                            nstate2.reset();
                                            nstate2.type = st_epsilon;
                                        }

                                        if (postcharchainpos == 0) {
                                            postcharchainpos = next1pos + 1;
                                            insert_at(postcharchainpos, 1);
                                            this->NFA_states[next1pos].next1 = 1;
                                        } else {
                                            const state_size_type prevbranchpos = postcharchainpos;

                                            postcharchainpos = prevbranchpos + this->NFA_states[prevbranchpos].next2;
                                            insert_at(postcharchainpos, 1);
                                            this->NFA_states[prevbranchpos].next2 = postcharchainpos - prevbranchpos;
                                            //  Fix for bug210423. This line cannot be omitted, because
                                            //  NFA_states[prevbranchpos].next2 has been incremented in insert_at().
                                        }

                                        //								if (next2next1pos >= postcharchainpos)
                                        ++next2next1pos;

                                        if (precharchainpos >= postcharchainpos)
                                            ++precharchainpos;

                                        state_type& prechainbranchpoint = this->NFA_states[precharchainpos];
                                        if (next2next2pos) {
                                            //									if (next2next2pos >= postcharchainpos)
                                            ++next2next2pos;
                                            prechainbranchpoint.next2 = next2next2pos - precharchainpos;
                                        } else {
                                            prechainbranchpoint.next2 = 0;
                                        }

                                        state_type& newbranchpoint = this->NFA_states[postcharchainpos];
                                        newbranchpoint.character = meta_char::mc_bar;
                                        //								newbranchpoint.next1 = 1;
                                        newbranchpoint.next2 = next2next1pos + this->NFA_states[next2next1pos].next1 - postcharchainpos;
                                    } else if (relation == 1) {
                                        break;
                                    } else
                                        precharchainpos = next2pos;
                                } else {
                                    //  Fix for bug210428.
                                    //  Original: /mm2|m|mm/
                                    //  1st step: /m(?:m2||m)/ <- No more optimisation can be performed. Must quit.
                                    //  2nd step: /mm(?:2||)/ <- BUG.
                                    break;
                                }

                                if (next2next2pos == 0)
                                    break;

                                next2pos = next2next2pos;
                            }
                        }
                    }
                }
            }
#endif	//   !defined(SRELLDBG_NO_BRANCH_OPT2)

        public:	//  For debug.

            void print_NFA_states(const int) const;
        };
        //  re_compiler

    }	//  namespace regex_internal

//  ... "rei_compiler.hpp"]
//  ["regex_sub_match.hpp" ...

//  28.9, class template sub_match:
    template <class BidirectionalIterator>
    class sub_match : public std::pair<BidirectionalIterator, BidirectionalIterator> {
    public:

        typedef typename std::iterator_traits<BidirectionalIterator>::value_type value_type;
        typedef typename std::iterator_traits<BidirectionalIterator>::difference_type difference_type;
        typedef BidirectionalIterator iterator;
        typedef std::basic_string<value_type> string_type;

        bool matched;

        //	constexpr sub_match();	//  C++11.

        sub_match() : matched(false) {
        }

        difference_type length() const {
            return matched ? std::distance(this->first, this->second) : 0;
        }

        operator string_type() const {
            return matched ? string_type(this->first, this->second) : string_type();
        }

        string_type str() const {
            return matched ? string_type(this->first, this->second) : string_type();
        }

        int compare(const sub_match& s) const {
            return str().compare(s.str());
        }

        int compare(const string_type& s) const {
            return str().compare(s);
        }

        int compare(const value_type* const s) const {
            return str().compare(s);
        }
    };

    //  28.9.2, sub_match non-member operators:
    //  [7.9.2] sub_match non-member operators

    //  Compares sub_match & with sub_match &.
    template <class BiIter>
    bool operator==(const sub_match<BiIter>& lhs, const sub_match<BiIter>& rhs) {
        return lhs.compare(rhs) == 0;	//  1
    }

    template <class BiIter>
    bool operator!=(const sub_match<BiIter>& lhs, const sub_match<BiIter>& rhs) {
        return lhs.compare(rhs) != 0;	//  2
    }

    template <class BiIter>
    bool operator<(const sub_match<BiIter>& lhs, const sub_match<BiIter>& rhs) {
        return lhs.compare(rhs) < 0;	//  3
    }

    template <class BiIter>
    bool operator<=(const sub_match<BiIter>& lhs, const sub_match<BiIter>& rhs) {
        return lhs.compare(rhs) <= 0;	//  4
    }

    template <class BiIter>
    bool operator>=(const sub_match<BiIter>& lhs, const sub_match<BiIter>& rhs) {
        return lhs.compare(rhs) >= 0;	//  5
    }

    template <class BiIter>
    bool operator>(const sub_match<BiIter>& lhs, const sub_match<BiIter>& rhs) {
        return lhs.compare(rhs) > 0;	//  6
    }

    //  Compares basic_string & with sub_match &.
    template <class BiIter, class ST, class SA>
    bool operator==(
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs.compare(lhs.c_str()) == 0;	//  7
    }

    template <class BiIter, class ST, class SA>
    bool operator!=(
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(lhs == rhs);	//  8
    }

    template <class BiIter, class ST, class SA>
    bool operator<(
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs.compare(lhs.c_str()) > 0;	//  9
    }

    template <class BiIter, class ST, class SA>
    bool operator>(
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs < lhs;	//  10
    }

    template <class BiIter, class ST, class SA>
    bool operator>=(
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(lhs < rhs);	//  11
    }

    template <class BiIter, class ST, class SA>
    bool operator<=(
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(rhs < lhs);	//  12
    }

    //  Compares sub_match & with basic_string &.
    template <class BiIter, class ST, class SA>
    bool operator==(
        const sub_match<BiIter>& lhs,
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& rhs
        ) {
        return lhs.compare(rhs.c_str()) == 0;	//  13
    }

    template <class BiIter, class ST, class SA>
    bool operator!=(
        const sub_match<BiIter>& lhs,
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& rhs
        ) {
        return !(lhs == rhs);	//  14
    }

    template <class BiIter, class ST, class SA>
    bool operator<(
        const sub_match<BiIter>& lhs,
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& rhs
        ) {
        return lhs.compare(rhs.c_str()) < 0;	//  15
    }

    template <class BiIter, class ST, class SA>
    bool operator>(
        const sub_match<BiIter>& lhs,
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& rhs
        ) {
        return rhs < lhs;	//  16
    }

    template <class BiIter, class ST, class SA>
    bool operator>=(
        const sub_match<BiIter>& lhs,
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& rhs
        ) {
        return !(lhs < rhs);	//  17
    }

    template <class BiIter, class ST, class SA>
    bool operator<=(
        const sub_match<BiIter>& lhs,
        const std::basic_string<typename std::iterator_traits<BiIter>::value_type, ST, SA>& rhs
        ) {
        return !(rhs < lhs);	//  18
    }

    //  Compares iterator_traits::value_type * with sub_match &.
    template <class BiIter>
    bool operator==(
        typename std::iterator_traits<BiIter>::value_type const* lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs.compare(lhs) == 0;	//  19
    }

    template <class BiIter>
    bool operator!=(
        typename std::iterator_traits<BiIter>::value_type const* lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(lhs == rhs);	//  20
    }

    template <class BiIter>
    bool operator<(
        typename std::iterator_traits<BiIter>::value_type const* lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs.compare(lhs) > 0;	//  21
    }

    template <class BiIter>
    bool operator>(
        typename std::iterator_traits<BiIter>::value_type const* lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs < lhs;	//  22
    }

    template <class BiIter>
    bool operator>=(
        typename std::iterator_traits<BiIter>::value_type const* lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(lhs < rhs);	//  23
    }

    template <class BiIter>
    bool operator<=(
        typename std::iterator_traits<BiIter>::value_type const* lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(rhs < lhs);	//  24
    }

    //  Compares sub_match & with iterator_traits::value_type *.
    template <class BiIter>
    bool operator==(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const* rhs
        ) {
        return lhs.compare(rhs) == 0;	//  25
    }

    template <class BiIter>
    bool operator!=(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const* rhs
        ) {
        return !(lhs == rhs);	//  26
    }

    template <class BiIter>
    bool operator<(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const* rhs
        ) {
        return lhs.compare(rhs) < 0;	//  27
    }

    template <class BiIter>
    bool operator>(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const* rhs
        ) {
        return rhs < lhs;	//  28
    }

    template <class BiIter>
    bool operator>=(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const* rhs
        ) {
        return !(lhs < rhs);	//  29
    }

    template <class BiIter>
    bool operator<=(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const* rhs
        ) {
        return !(rhs < lhs);	//  30
    }

    //  Compares iterator_traits::value_type & with sub_match &.
    template <class BiIter>
    bool operator==(
        typename std::iterator_traits<BiIter>::value_type const& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs.compare(typename sub_match<BiIter>::string_type(1, lhs)) == 0;	//  31
    }

    template <class BiIter>
    bool operator!=(
        typename std::iterator_traits<BiIter>::value_type const& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(lhs == rhs);	//  32
    }

    template <class BiIter>
    bool operator<(
        typename std::iterator_traits<BiIter>::value_type const& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs.compare(typename sub_match<BiIter>::string_type(1, lhs)) > 0;	//  33
    }

    template <class BiIter>
    bool operator>(
        typename std::iterator_traits<BiIter>::value_type const& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return rhs < lhs;	//  34
    }

    template <class BiIter>
    bool operator>=(
        typename std::iterator_traits<BiIter>::value_type const& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(lhs < rhs);	//  35
    }

    template <class BiIter>
    bool operator<=(
        typename std::iterator_traits<BiIter>::value_type const& lhs,
        const sub_match<BiIter>& rhs
        ) {
        return !(rhs < lhs);	//  36
    }

    //  Compares sub_match & with iterator_traits::value_type &.
    template <class BiIter>
    bool operator==(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const& rhs
        ) {
        return lhs.compare(typename sub_match<BiIter>::string_type(1, rhs)) == 0;	//  37
    }

    template <class BiIter>
    bool operator!=(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const& rhs
        ) {
        return !(lhs == rhs);	//  38
    }

    template <class BiIter>
    bool operator<(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const& rhs
        ) {
        return lhs.compare(typename sub_match<BiIter>::string_type(1, rhs)) < 0;	//  39
    }

    template <class BiIter>
    bool operator>(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const& rhs
        ) {
        return rhs < lhs;	//  40
    }

    template <class BiIter>
    bool operator>=(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const& rhs
        ) {
        return !(lhs < rhs);	//  41
    }

    template <class BiIter>
    bool operator<=(
        const sub_match<BiIter>& lhs,
        typename std::iterator_traits<BiIter>::value_type const& rhs
        ) {
        return !(rhs < lhs);	//  42
    }

    template <class charT, class ST, class BiIter>
    std::basic_ostream<charT, ST>& operator<<(std::basic_ostream<charT, ST>& os, const sub_match<BiIter>& m) {
        return (os << m.str());
    }

    typedef sub_match<const char*> csub_match;
    typedef sub_match<const wchar_t*> wcsub_match;
    typedef sub_match<std::string::const_iterator> ssub_match;
    typedef sub_match<std::wstring::const_iterator> wssub_match;

#if defined(SRELL_CPP11_CHAR1632_ENABLED)
    typedef sub_match<const char16_t*> u16csub_match;
    typedef sub_match<const char32_t*> u32csub_match;
    typedef sub_match<std::u16string::const_iterator> u16ssub_match;
    typedef sub_match<std::u32string::const_iterator> u32ssub_match;
#endif

#if defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef sub_match<const char8_t*> u8csub_match;
#endif
#if defined(SRELL_CPP20_CHAR8_ENABLED) && SRELL_CPP20_CHAR8_ENABLED >= 2
    typedef sub_match<std::u8string::const_iterator> u8ssub_match;
#endif

    typedef csub_match u8ccsub_match;
    typedef ssub_match u8cssub_match;
#if !defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef u8ccsub_match u8csub_match;
#endif
#if !defined(SRELL_CPP20_CHAR8_ENABLED) || SRELL_CPP20_CHAR8_ENABLED < 2
    typedef u8cssub_match u8ssub_match;
#endif

#if defined(WCHAR_MAX)
#if WCHAR_MAX >= 0x10ffff
    typedef wcsub_match u32wcsub_match;
    typedef wssub_match u32wssub_match;
    typedef u32wcsub_match u1632wcsub_match;
    typedef u32wssub_match u1632wssub_match;
#elif WCHAR_MAX >= 0xffff
    typedef wcsub_match u16wcsub_match;
    typedef wssub_match u16wssub_match;
    typedef u16wcsub_match u1632wcsub_match;
    typedef u16wssub_match u1632wssub_match;
#endif
#endif

    //  ... "regex_sub_match.hpp"]
    //  ["regex_match_results.hpp" ...

    //  28.10, class template match_results:
    template <class BidirectionalIterator, class Allocator = std::allocator<sub_match<BidirectionalIterator> > >
    class match_results {
    public:

        typedef sub_match<BidirectionalIterator> value_type;
        typedef const value_type& const_reference;
        typedef const_reference reference;
        //	typedef implementation defined const_iterator;
        typedef typename std::vector<value_type, Allocator>::const_iterator const_iterator;
        typedef const_iterator iterator;
        typedef typename std::iterator_traits<BidirectionalIterator>::difference_type difference_type;

#if defined(__cplusplus) && __cplusplus >= 201103L
        typedef typename std::allocator_traits<Allocator>::size_type size_type;
#else
        typedef typename Allocator::size_type size_type;	//  TR1.
#endif

        typedef Allocator allocator_type;
        typedef typename std::iterator_traits<BidirectionalIterator>::value_type char_type;
        typedef std::basic_string<char_type> string_type;

    public:

        //  28.10.1, construct/copy/destroy:
        //  [7.10.1] construct/copy/destroy
        explicit match_results(const Allocator& a = Allocator()) : ready_(false), sub_matches_(a) {
        }

        match_results(const match_results& m) {
            operator=(m);
        }

#if defined(SRELL_CPP11_MOVE_ENABLED)
        match_results(match_results&& m) SRELL_NOEXCEPT {
            operator=(std::move(m));
        }
#endif

        match_results& operator=(const match_results& m) {
            if (this != &m) {
                //			this->sstate_ = m.sstate_;
                this->ready_ = m.ready_;
                this->sub_matches_ = m.sub_matches_;
                this->prefix_ = m.prefix_;
                this->suffix_ = m.suffix_;
                this->base_ = m.base_;
#if !defined(SRELL_NO_NAMEDCAPTURE)
                this->gnames_ = m.gnames_;
#endif
            }
            return *this;
        }

#if defined(SRELL_CPP11_MOVE_ENABLED)
        match_results& operator=(match_results&& m) SRELL_NOEXCEPT {
            if (this != &m) {
                //			this->sstate_ = std::move(m.sstate_);
                this->ready_ = m.ready_;
                this->sub_matches_ = std::move(m.sub_matches_);
                this->prefix_ = std::move(m.prefix_);
                this->suffix_ = std::move(m.suffix_);
                this->base_ = m.base_;
#if !defined(SRELL_NO_NAMEDCAPTURE)
                this->gnames_ = std::move(m.gnames_);
#endif
            }
            return *this;
        }
#endif

        //	~match_results();

            //  28.10.2, state:
        bool ready() const {
            return ready_;
        }

        //  28.10.3, size:
        //  [7.10.2] size
        size_type size() const {
            return sub_matches_.size();
        }

        size_type max_size() const {
            return sub_matches_.max_size();
            //		return static_cast<size_type>(~0) / sizeof (value_type);
        }

        bool empty() const {
            return size() == 0;
        }

        //  28.10.4, element access:
        //  [7.10.3] element access
        difference_type length(const size_type sub = 0) const {
            return (*this)[sub].length();
        }

        difference_type position(const size_type sub = 0) const {
            const_reference ref = (*this)[sub];

            return std::distance(base_, ref.first);
        }

        string_type str(const size_type sub = 0) const {
            return string_type((*this)[sub]);
        }

        const_reference operator[](const size_type n) const {
#if defined(SRELL_STRICT_IMPL)
            return n < sub_matches_.size() ? sub_matches_[n] : unmatched_;
#else
            return sub_matches_[n];
#endif
        }

#if !defined(SRELL_NO_NAMEDCAPTURE)

        //  Helpers for overload resolution of the integer literal 0 of signed types.
        template <typename IntegerType>
        difference_type length(const IntegerType zero) const {
            return length(static_cast<size_type>(zero));
        }
        template <typename IntegerType>
        difference_type position(const IntegerType zero) const {
            return position(static_cast<size_type>(zero));
        }
        template <typename IntegerType>
        string_type str(const IntegerType zero) const {
            return str(static_cast<size_type>(zero));
        }
        template <typename IntegerType>
        const_reference operator[](const IntegerType zero) const {
            return operator[](static_cast<size_type>(zero));
        }

        difference_type length(const string_type& sub) const {
            return (*this)[sub].length();
        }

        difference_type position(const string_type& sub) const {
            const_reference ref = (*this)[sub];

            return std::distance(base_, ref.first);
        }

        string_type str(const string_type& sub) const {
            return string_type((*this)[sub]);
        }

        const_reference operator[](const string_type& sub) const {
            return sub_matches_[lookup_and_check_backref_number(sub.c_str(), sub.c_str() + sub.size())];
        }

        difference_type length(const char_type* sub) const {
            return (*this)[sub].length();
        }

        difference_type position(const char_type* sub) const {
            const_reference ref = (*this)[sub];

            return std::distance(base_, ref.first);
        }

        string_type str(const char_type* sub) const {
            return string_type((*this)[sub]);
        }

        const_reference operator[](const char_type* sub) const {
            return sub_matches_[lookup_and_check_backref_number(sub, sub + std::char_traits<char_type>::length(sub))];
        }

#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)

        const_reference prefix() const {
            return prefix_;
        }

        const_reference suffix() const {
            return suffix_;
        }

        const_iterator begin() const {
            return sub_matches_.begin();
        }

        const_iterator end() const {
            return sub_matches_.end();
        }

        const_iterator cbegin() const {
            return sub_matches_.begin();
        }

        const_iterator cend() const {
            return sub_matches_.end();
        }

        //  28.10.5, format:
        //  [7.10.4] format
        template <class OutputIter>
        OutputIter format(
            OutputIter out,
            const char_type* fmt_first,
            const char_type* const fmt_last,
            regex_constants::match_flag_type /* flags */ = regex_constants::format_default
        ) const {
            if (this->ready() && !this->empty()) {
#if !defined(SRELL_NO_NAMEDCAPTURE)
                const bool no_groupnames = gnames_.size() == 0;
#endif
                const value_type& m0 = (*this)[0];

                while (fmt_first != fmt_last) {
                    if (*fmt_first != static_cast<char_type>(regex_internal::meta_char::mc_dollar))	//  '$'
                    {
                        *out++ = *fmt_first++;
                    } else {
                        ++fmt_first;
                        if (fmt_first == fmt_last) {
                            *out++ = regex_internal::meta_char::mc_dollar;	//  '$';
                        } else if (*fmt_first == static_cast<char_type>(regex_internal::char_other::co_amp))	//  '&', $&
                        {
                            out = std::copy(m0.first, m0.second, out);
                            ++fmt_first;
                        } else if (*fmt_first == static_cast<char_type>(regex_internal::char_other::co_grav))	//  '`', $`, prefix.
                        {
                            out = std::copy(this->prefix().first, this->prefix().second, out);
                            ++fmt_first;
                        } else if (*fmt_first == static_cast<char_type>(regex_internal::char_other::co_apos))	//  '\'', $', suffix.
                        {
                            out = std::copy(this->suffix().first, this->suffix().second, out);
                            ++fmt_first;
                        }
#if !defined(SRELL_NO_NAMEDCAPTURE)
                        else if (*fmt_first == static_cast<char_type>(regex_internal::meta_char::mc_lt) && !no_groupnames)	//  '<', $<
                        {
                            const char_type* const current_backup = fmt_first;
                            bool replaced = false;

                            if (++fmt_first == fmt_last)
                                ;	//  Do nothing.
                            else {
                                const char_type* const name_begin = fmt_first;

                                for (;; ++fmt_first) {
                                    if (*fmt_first == static_cast<char_type>(regex_internal::meta_char::mc_gt)) {
                                        const regex_internal::uint_l32 backref_number = lookup_backref_number(name_begin, fmt_first);

                                        if (backref_number != regex_internal::groupname_mapper<char_type>::notfound) {
                                            const value_type& mn = (*this)[backref_number];

                                            if (mn.matched)
                                                out = std::copy(mn.first, mn.second, out);
                                            //										replaced = true;
                                        }
                                        replaced = true;
                                        ++fmt_first;
                                        break;
                                    }
                                    if (fmt_first == fmt_last)
                                        break;
                                }
                            }
                            if (!replaced) {
                                fmt_first = current_backup;
                                *out++ = regex_internal::meta_char::mc_dollar;	//  '$';
                            }
                        }
#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)
                        else {
                            const char_type* const backup_pos = fmt_first;
                            size_type backref_number = 0;

                            if (fmt_first != fmt_last && *fmt_first >= static_cast<char_type>(regex_internal::char_alnum::ch_0) && *fmt_first <= static_cast<char_type>(regex_internal::char_alnum::ch_9))	//  '0'-'9'
                            {
                                backref_number += *fmt_first - regex_internal::char_alnum::ch_0;	//  '0';

                                if (++fmt_first != fmt_last && *fmt_first >= static_cast<char_type>(regex_internal::char_alnum::ch_0) && *fmt_first <= static_cast<char_type>(regex_internal::char_alnum::ch_9))	//  '0'-'9'
                                {
                                    backref_number *= 10;
                                    backref_number += *fmt_first - regex_internal::char_alnum::ch_0;	//  '0';
                                    ++fmt_first;
                                }
                            }

                            if (backref_number && backref_number < this->size()) {
                                const value_type& mn = (*this)[backref_number];

                                if (mn.matched)
                                    out = std::copy(mn.first, mn.second, out);
                            } else {
                                *out++ = regex_internal::meta_char::mc_dollar;	//  '$';

                                fmt_first = backup_pos;
                                if (*fmt_first == static_cast<char_type>(regex_internal::meta_char::mc_dollar))
                                    ++fmt_first;
                            }
                        }
                    }
                }
            }
            return out;
        }

        template <class OutputIter, class ST, class SA>
        OutputIter format(
            OutputIter out,
            const std::basic_string<char_type, ST, SA>& fmt,
            regex_constants::match_flag_type flags = regex_constants::format_default
        ) const {
            return format(out, fmt.data(), fmt.data() + fmt.size(), flags);
        }

        template <class ST, class SA>
        std::basic_string<char_type, ST, SA> format(
            const string_type& fmt,
            regex_constants::match_flag_type flags = regex_constants::format_default
        ) const {
            std::basic_string<char_type, ST, SA> result;

            //		format(std::back_insert_iterator<string_type>(result), fmt, flags);
            format(std::back_inserter(result), fmt, flags);
            return result;
        }

        string_type format(const char_type* fmt, regex_constants::match_flag_type flags = regex_constants::format_default) const {
            string_type result;

            format(std::back_inserter(result), fmt, fmt + std::char_traits<char_type>::length(fmt), flags);
            return result;
        }

        //  28.10.6, allocator:
        //  [7.10.5] allocator
        allocator_type get_allocator() const {
            return allocator_type();
        }

        //  28.10.7, swap:
        //  [7.10.6] swap
        void swap(match_results& that) {
            const match_results tmp(that);
            that = *this;
            *this = tmp;
        }

    public:	//  For internal.

        typedef match_results<BidirectionalIterator> match_results_type;
        typedef typename match_results_type::size_type match_results_size_type;
        typedef typename regex_internal::re_search_state</*charT, */BidirectionalIterator> search_state_type;

        search_state_type sstate_;

        void clear_() {
            ready_ = false;
            sub_matches_.clear();
            //		prefix_.matched = false;
            //		suffix_.matched = false;
#if !defined(SRELL_NO_NAMEDCAPTURE)
            gnames_.clear();
#endif
        }

        //	template <typename charT>
#if !defined(SRELL_NO_NAMEDCAPTURE)
        bool set_match_results_(const regex_internal::groupname_mapper<char_type>& gnames)
#else
        bool set_match_results_()
#endif
        {
            sub_matches_.resize(sstate_.bracket.size());
            //		value_type &m0 = sub_matches_[0];

            sub_matches_[0].matched = true;

            for (regex_internal::uint_l32 i = 1; i < static_cast<regex_internal::uint_l32>(sstate_.bracket.size()); ++i) {
                const typename search_state_type::submatch_type& br = sstate_.bracket[i];
                value_type& sm = sub_matches_[i];

                sm.first = br.core.open_at;
                sm.second = br.core.close_at;
                sm.matched = br.counter != 0;
            }

            base_ = sstate_.lblim;
            prefix_.first = sstate_.srchbegin;
            prefix_.second = sub_matches_[0].first = sstate_.bracket[0].core.open_at;
            suffix_.first = sub_matches_[0].second = sstate_.nth.in_string;
            suffix_.second = sstate_.srchend;

            prefix_.matched = prefix_.first != prefix_.second;	//  The spec says prefix().first != prefix().second
            suffix_.matched = suffix_.first != suffix_.second;	//  The spec says suffix().first != suffix().second

#if !defined(SRELL_NO_NAMEDCAPTURE)
            gnames_ = gnames;
#endif
            ready_ = true;
            return true;
        }

        bool set_match_results_bmh_() {
            sub_matches_.resize(1);
            //		value_type &m0 = sub_matches_[0];

            sub_matches_[0].matched = true;

            base_ = sstate_.lblim;
            prefix_.first = sstate_.srchbegin;
            prefix_.second = sub_matches_[0].first = sstate_.nth.in_string;
            suffix_.first = sub_matches_[0].second = sstate_.nextpos;
            suffix_.second = sstate_.srchend;

            prefix_.matched = prefix_.first != prefix_.second;
            suffix_.matched = suffix_.first != suffix_.second;

            ready_ = true;
            return true;
        }

        void set_prefix_first_(const BidirectionalIterator pf) {
            prefix_.first = pf;
        }

        bool mark_as_failed_() {
            ready_ = true;	//  30.11.2 and 3: Postconditions: m.ready() == true in all cases.
            return false;
        }

    private:

#if !defined(SRELL_NO_NAMEDCAPTURE)

        regex_internal::uint_l32 lookup_backref_number(const char_type* begin, const char_type* const end) const {
            typename regex_internal::groupname_mapper<char_type>::gname_string key(end - begin);

            for (std::size_t i = 0; begin != end; ++begin, ++i)
                key[i] = *begin;

            return gnames_[key];
        }

        regex_internal::uint_l32 lookup_and_check_backref_number(const char_type* begin, const char_type* const end) const {
            const regex_internal::uint_l32 backrefno = lookup_backref_number(begin, end);

            if (backrefno == regex_internal::groupname_mapper<char_type>::notfound)
                throw regex_error(regex_constants::error_backref);

            return backrefno;
        }

#endif	//  !defined(SRELL_NO_NAMEDCAPTURE)

    public:	//  For debug.

        template <typename BasicRegexT>
        void print_sub_matches(const BasicRegexT&, const int) const;
        void print_addresses(const value_type&, const char* const) const;

    private:

        typedef std::vector<value_type, Allocator> sub_match_array;

        bool ready_;
        sub_match_array sub_matches_;
        value_type prefix_;
        value_type suffix_;
        BidirectionalIterator base_;

#if !defined(SRELL_NO_NAMEDCAPTURE)
        regex_internal::groupname_mapper<char_type> gnames_;
#endif
#if defined(SRELL_STRICT_IMPL)
        value_type unmatched_;
#endif
    };

    //  28.10.7, match_results swap:
    //  [7.10.6] match_results swap
    template <class BidirectionalIterator, class Allocator>
    void swap(
        match_results<BidirectionalIterator, Allocator>& m1,
        match_results<BidirectionalIterator, Allocator>& m2
    ) {
        m1.swap(m2);
    }

    //  28.10.8, match_results comparisons
    template <class BidirectionalIterator, class Allocator>
    bool operator==(
        const match_results<BidirectionalIterator, Allocator>& m1,
        const match_results<BidirectionalIterator, Allocator>& m2
        ) {
        if (!m1.ready() && !m2.ready())
            return true;

        if (m1.ready() && m2.ready()) {
            if (m1.empty() && m2.empty())
                return true;

            if (!m1.empty() && !m2.empty()) {
                return m1.prefix() == m2.prefix() && m1.size() == m2.size() && std::equal(m1.begin(), m1.end(), m2.begin()) && m1.suffix() == m2.suffix();
            }
        }
        return false;
    }

    template <class BidirectionalIterator, class Allocator>
    bool operator!=(
        const match_results<BidirectionalIterator, Allocator>& m1,
        const match_results<BidirectionalIterator, Allocator>& m2
        ) {
        return !(m1 == m2);
    }

    typedef match_results<const char*> cmatch;
    typedef match_results<const wchar_t*> wcmatch;
    typedef match_results<std::string::const_iterator> smatch;
    typedef match_results<std::wstring::const_iterator> wsmatch;

#if defined(SRELL_CPP11_CHAR1632_ENABLED)
    typedef match_results<const char16_t*> u16cmatch;
    typedef match_results<const char32_t*> u32cmatch;
    typedef match_results<std::u16string::const_iterator> u16smatch;
    typedef match_results<std::u32string::const_iterator> u32smatch;
#endif

#if defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef match_results<const char8_t*> u8cmatch;
#endif
#if defined(SRELL_CPP20_CHAR8_ENABLED) && SRELL_CPP20_CHAR8_ENABLED >= 2
    typedef match_results<std::u8string::const_iterator> u8smatch;
#endif

    typedef cmatch u8ccmatch;
    typedef smatch u8csmatch;
#if !defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef u8ccmatch u8cmatch;
#endif
#if !defined(SRELL_CPP20_CHAR8_ENABLED) || SRELL_CPP20_CHAR8_ENABLED < 2
    typedef u8csmatch u8smatch;
#endif

#if defined(WCHAR_MAX)
#if WCHAR_MAX >= 0x10ffff
    typedef wcmatch u32wcmatch;
    typedef wsmatch u32wsmatch;
    typedef u32wcmatch u1632wcmatch;
    typedef u32wsmatch u1632wsmatch;
#elif WCHAR_MAX >= 0xffff
    typedef wcmatch u16wcmatch;
    typedef wsmatch u16wsmatch;
    typedef u16wcmatch u1632wcmatch;
    typedef u16wsmatch u1632wsmatch;
#endif
#endif

    //  ... "regex_match_results.hpp"]
    //  ["rei_algorithm.hpp" ...

    namespace regex_internal {

        template <typename charT, typename traits>
        class regex_object : public re_compiler<charT, traits> {
        public:

            template <typename BidirectionalIterator>
            bool search
            (
                const BidirectionalIterator begin,
                const BidirectionalIterator end,
                const BidirectionalIterator lookbehind_limit,
                match_results<BidirectionalIterator>& results,
                const regex_constants::match_flag_type flags /* = regex_constants::match_default */
            ) const {
                results.clear_();

                //		results.sstate_.template init<utf_traits>(begin, end, lookbehind_limit, flags);
                results.sstate_.init(begin, end, lookbehind_limit, flags);

                if (results.sstate_.match_continuous_flag()) {
                    if (this->NFA_states.size()) {
                        results.sstate_.set_entrypoint(this->NFA_states[0].next_state2);
                        goto DO_SEARCH;
                    }
                } else
#if !defined(SRELLDBG_NO_BMH)
                    if (this->bmdata) {
#if !defined(SRELL_NO_ICASE)
                        if (!this->is_ricase() ? this->bmdata->do_casesensitivesearch(results.sstate_, typename std::iterator_traits<BidirectionalIterator>::iterator_category()) : this->bmdata->do_icasesearch(results.sstate_, typename std::iterator_traits<BidirectionalIterator>::iterator_category()))
#else
                        if (this->bmdata->do_casesensitivesearch(results.sstate_, typename std::iterator_traits<BidirectionalIterator>::iterator_category()))
#endif
                            return results.set_match_results_bmh_();
                    } else
#endif
                        if (this->NFA_states.size()) {
                            results.sstate_.set_entrypoint(this->NFA_states[0].next_state1);

                        DO_SEARCH:
                            results.sstate_.init_for_automaton(this->number_of_brackets, this->number_of_counters, this->number_of_repeats);

#if !defined(SRELL_NO_ICASE)
                            if (!this->is_ricase() ? do_search<false>(results) : do_search<true>(results))
#else
                            if (do_search<false>(results))
#endif
                            {
#if !defined(SRELL_NO_NAMEDCAPTURE)
                                return results.set_match_results_(this->namedcaptures);
#else
                                return results.set_match_results_();
#endif
                            }
                        }
                    return results.mark_as_failed_();
            }

        private:

            typedef typename traits::utf_traits utf_traits;

            template <const bool icase, typename BidirectionalIterator>
            bool do_search
            (
                match_results<BidirectionalIterator>& results
            ) const {
                re_search_state</*charT, */BidirectionalIterator>& sstate = results.sstate_;
                const BidirectionalIterator searchend = sstate.nth.in_string;

                for (;;) {
                    const bool final = sstate.nextpos == searchend;

                    sstate.nth.in_string = sstate.nextpos;

                    if (!final) {

#ifdef SRELLDBG_NO_1STCHRCLS
                        utf_traits::codepoint_inc(sstate.nextpos, sstate.srchend);
#else
                        {
#if !defined(SRELLDBG_NO_BITSET)
                            if (!this->firstchar_class_bs.test((*sstate.nextpos++) & utf_traits::bitsetmask))
#else
                            const uchar32 firstchar = utf_traits::codepoint_inc(sstate.nextpos, sstate.srchend);

                            if (!this->firstchar_class.is_included(firstchar))
#endif
                                continue;
                        }
#endif
                    }
                    //  Even when final == true, we have to try for such expressions
                    //  as "" =~ /^$/ or "..." =~ /$/.

#if defined(SRELL_NO_LIMIT_COUNTER)
                    sstate.reset(/* first */);
#else
                    sstate.reset(/* first, */ this->limit_counter);
#endif
                    if (run_automaton<icase, false>(sstate /* , false */))
                        return true;

                    if (final)
                        break;
                }
                return false;
            }

            template <typename T, const bool>
            struct casehelper {
                static T canonicalise(const T t) {
                    return t;
                }
            };

            template <typename T>
            struct casehelper<T, true> {
                static T canonicalise(const T t) {
                    return unicode_case_folding::do_casefolding(t);
                }
            };

            template <const bool icase, const bool reverse, typename BidirectionalIterator>
            bool run_automaton
            (
                //		match_results<BidirectionalIterator> &results,
                re_search_state</*charT, */BidirectionalIterator>& sstate
                //		, const bool is_recursive /* = false */
            ) const {
                typedef casehelper<uchar32, icase> casehelper_type;
                typedef typename re_object_core<charT, traits>::state_type state_type;
                typedef re_search_state</*charT, */BidirectionalIterator> ss_type;
                //		typedef typename ss_type::search_core_state scstate_type;
                typedef typename ss_type::submatch_type submatch_type;
                typedef typename ss_type::submatchcore_type submatchcore_type;
                typedef typename ss_type::counter_type counter_type;
                typedef typename ss_type::position_type position_type;
                bool is_matched;

                goto START;

            JUDGE:
                if (is_matched) {
                MATCHED:
                    sstate.nth.in_NFA_states = sstate.nth.in_NFA_states->next_state1;
                } else {
                NOT_MATCHED:

#if !defined(SRELL_NO_LIMIT_COUNTER)
                    if (--sstate.failure_counter) {
#endif
                        if (sstate.bt_stack.size() > sstate.btstack_size) {
                            sstate.nth = sstate.bt_stack.back();
                            sstate.bt_stack.pop_back();

                            sstate.nth.in_NFA_states = sstate.nth.in_NFA_states->next_state2;
                            //					continue;
                        } else {
                            return false;
                        }
#if !defined(SRELL_NO_LIMIT_COUNTER)
                    } else
                        throw regex_error(regex_constants::error_complexity);
#endif
                }

                //		START:
                for (;;) {
                START:
                    const state_type& current_NFA = *sstate.nth.in_NFA_states;

                    switch (current_NFA.type) {
                        case st_character:

#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(push)
#pragma warning(disable:4127)
#endif
                            if (!reverse)
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(pop)
#endif
                            {
                                if (!sstate.is_at_srchend()) {
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    const BidirectionalIterator prevpos = sstate.nth.in_string;
#endif
                                    const uchar32 uchar = casehelper_type::canonicalise(utf_traits::codepoint_inc(sstate.nth.in_string, sstate.srchend));
                                RETRY_CF:
                                    const state_type& current_NFA2 = *sstate.nth.in_NFA_states;

                                    if (current_NFA2.character == uchar)
                                        goto MATCHED;

#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    if (current_NFA2.next_state2) {
                                        sstate.nth.in_NFA_states = current_NFA2.next_state2;

                                        if (sstate.nth.in_NFA_states->type == st_character)
                                            goto RETRY_CF;

                                        sstate.nth.in_string = prevpos;
                                        continue;
                                    }
#endif
                                }
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                else if (current_NFA.next_state2) {
                                    sstate.nth.in_NFA_states = current_NFA.next_state2;
                                    continue;
                                }
#endif
                            } else	//  reverse == true.
                            {
                                if (!sstate.is_at_lookbehindlimit()) {
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    const BidirectionalIterator prevpos = sstate.nth.in_string;
#endif
                                    const uchar32 uchar = casehelper_type::canonicalise(utf_traits::dec_codepoint(sstate.nth.in_string, sstate.lblim));
                                RETRY_CB:
                                    const state_type& current_NFA2 = *sstate.nth.in_NFA_states;

                                    if (current_NFA2.character == uchar)
                                        goto MATCHED;

#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    if (current_NFA2.next_state2) {
                                        sstate.nth.in_NFA_states = current_NFA2.next_state2;

                                        if (sstate.nth.in_NFA_states->type == st_character)
                                            goto RETRY_CB;

                                        sstate.nth.in_string = prevpos;
                                        continue;
                                    }
#endif
                                }
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                else if (current_NFA.next_state2) {
                                    sstate.nth.in_NFA_states = current_NFA.next_state2;
                                    continue;
                                }
#endif
                            }
                            goto NOT_MATCHED;

                        case st_character_class:

#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(push)
#pragma warning(disable:4127)
#endif
                            if (!reverse)
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(pop)
#endif
                            {
                                if (!sstate.is_at_srchend()) {
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    const BidirectionalIterator prevpos = sstate.nth.in_string;
#endif
                                    const uchar32 uchar = utf_traits::codepoint_inc(sstate.nth.in_string, sstate.srchend);
                                    //						RETRY_CCF:
                                    const state_type& current_NFA2 = *sstate.nth.in_NFA_states;

#if !defined(SRELLDBG_NO_CCPOS)
                                    if (this->character_class.is_included(current_NFA2.quantifier.offset, current_NFA2.quantifier.length, uchar))
#else
                                    if (this->character_class.is_included(current_NFA2.number, uchar))
#endif
                                        goto MATCHED;

#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    if (current_NFA2.next_state2) {
                                        sstate.nth.in_NFA_states = current_NFA2.next_state2;

                                        //							if (sstate.nth.in_NFA_states->type == st_character_class)
                                        //								goto RETRY_CCF;

                                        sstate.nth.in_string = prevpos;
                                        continue;
                                    }
#endif
                                }
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                else if (current_NFA.next_state2) {
                                    sstate.nth.in_NFA_states = current_NFA.next_state2;
                                    continue;
                                }
#endif
                            } else	//  reverse == true.
                            {
                                if (!sstate.is_at_lookbehindlimit()) {
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    const BidirectionalIterator prevpos = sstate.nth.in_string;
#endif
                                    const uchar32 uchar = utf_traits::dec_codepoint(sstate.nth.in_string, sstate.lblim);
                                    //						RETRY_CCB:
                                    const state_type& current_NFA2 = *sstate.nth.in_NFA_states;

#if !defined(SRELLDBG_NO_CCPOS)
                                    if (this->character_class.is_included(current_NFA2.quantifier.offset, current_NFA2.quantifier.length, uchar))
#else
                                    if (this->character_class.is_included(current_NFA2.number, uchar))
#endif
                                        goto MATCHED;

#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                    if (current_NFA2.next_state2) {
                                        sstate.nth.in_NFA_states = current_NFA2.next_state2;

                                        //							if (sstate.nth.in_NFA_states->type == st_character_class)
                                        //								goto RETRY_CCB;

                                        sstate.nth.in_string = prevpos;
                                        continue;
                                    }
#endif
                                }
#if !defined(SRELLDBG_NO_ASTERISK_OPT)
                                else if (current_NFA.next_state2) {
                                    sstate.nth.in_NFA_states = current_NFA.next_state2;
                                    continue;
                                }
#endif
                            }
                            goto NOT_MATCHED;

                        case st_epsilon:

#if defined(SRELLDBG_NO_SKIP_EPSILON)
                            if (current_NFA.next_state2)
#endif
                            {
                                sstate.bt_stack.push_back(sstate.nth);	//	sstate.push();
                            }

                            sstate.nth.in_NFA_states = current_NFA.next_state1;
                            continue;

                        default:
                            switch (current_NFA.type) {

                                case st_check_counter:
                                {
                                    const uint_l32 counter = sstate.counter[current_NFA.number];

                                    if (counter < current_NFA.quantifier.atmost) {
                                        ++sstate.counter[current_NFA.number];

                                    LOOP_WITHOUT_INCREMENT:

                                        if (counter >= current_NFA.quantifier.atleast) {
                                            sstate.bt_stack.push_back(sstate.nth);
                                            sstate.nth.in_NFA_states = current_NFA.next_state1;
                                        } else {
                                            sstate.nth.in_NFA_states
                                                = current_NFA.quantifier.is_greedy
                                                ? current_NFA.next_state1
                                                : current_NFA.next_state2;
                                        }
                                    } else {
                                        if (current_NFA.quantifier.is_infinity())
                                            goto LOOP_WITHOUT_INCREMENT;

                                        sstate.nth.in_NFA_states
                                            = current_NFA.quantifier.is_greedy
                                            ? current_NFA.next_state2
                                            : current_NFA.next_state1;
                                    }
                                }
                                continue;

                                case st_decrement_counter:
                                    --sstate.counter[current_NFA.number];
                                    goto NOT_MATCHED;

                                case st_save_and_reset_counter:
                                {
                                    counter_type& c = sstate.counter[current_NFA.number];

                                    sstate.counter_stack.push_back(c);
                                    sstate.bt_stack.push_back(sstate.nth);
                                    c = 0;
                                }
                                goto MATCHED;

                                case st_restore_counter:
                                    sstate.counter[current_NFA.number] = sstate.counter_stack.back();
                                    sstate.counter_stack.pop_back();
                                    goto NOT_MATCHED;

                                case st_roundbracket_open:	//  '(':
                                {
                                    submatch_type& bracket = sstate.bracket[current_NFA.number];

                                    sstate.capture_stack.push_back(bracket.core);

#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(push)
#pragma warning(disable:4127)
#endif
                                    if (!reverse)
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(pop)
#endif
                                    {
                                        bracket.core.open_at = sstate.nth.in_string;
                                    } else
                                        bracket.core.close_at = sstate.nth.in_string;

                                    ++bracket.counter;

                                    for (uint_l32 brno = current_NFA.quantifier.atleast; brno <= current_NFA.quantifier.atmost; ++brno) {
                                        submatch_type& inner_bracket = sstate.bracket[brno];

                                        sstate.capture_stack.push_back(inner_bracket.core);
                                        sstate.counter_stack.push_back(inner_bracket.counter);
                                        inner_bracket.core.open_at = inner_bracket.core.close_at = sstate.srchend;
                                        inner_bracket.counter = 0;
                                        //  ECMAScript spec (3-5.1) 15.10.2.5, NOTE 3.
                                        //  ECMAScript 2018 (ES9) 21.2.2.5.1, Note 3.
                                    }

                                    sstate.bt_stack.push_back(sstate.nth);
                                }
                                goto MATCHED;

                                case st_roundbracket_pop:	//  '/':
                                {
                                    for (uint_l32 brno = current_NFA.quantifier.atmost; brno >= current_NFA.quantifier.atleast; --brno) {
                                        submatch_type& inner_bracket = sstate.bracket[brno];

                                        inner_bracket.counter = sstate.counter_stack.back();
                                        inner_bracket.core = sstate.capture_stack.back();
                                        sstate.counter_stack.pop_back();
                                        sstate.capture_stack.pop_back();
                                    }

                                    submatch_type& bracket = sstate.bracket[current_NFA.number];

                                    bracket.core = sstate.capture_stack.back();
                                    sstate.capture_stack.pop_back();
                                    --bracket.counter;
                                }
                                goto NOT_MATCHED;

                                case st_roundbracket_close:	//  ')':
                                {
                                    submatch_type& bracket = sstate.bracket[current_NFA.number];
                                    submatchcore_type& brc = bracket.core;

                                    if ((!reverse ? brc.open_at : brc.close_at) != sstate.nth.in_string) {
                                        sstate.nth.in_NFA_states = current_NFA.next_state1;
                                    } else	//  0 width match, breaks from the loop.
                                    {
                                        if (current_NFA.next_state1->type != st_check_counter) {
                                            if (bracket.counter > 1)
                                                goto NOT_MATCHED;	//  ECMAScript spec 15.10.2.5, note 4.

                                            sstate.nth.in_NFA_states = current_NFA.next_state2;
                                            //  Accepts 0 width match and exits.
                                        } else {
                                            //  A pair with check_counter.
                                            const counter_type counter = sstate.counter[current_NFA.next_state1->number];

                                            if (counter > current_NFA.next_state1->quantifier.atleast)
                                                goto NOT_MATCHED;	//  Takes a captured string in the previous loop.

                                            sstate.nth.in_NFA_states = current_NFA.next_state1;
                                            //  Accepts 0 width match and continues.
                                        }
                                    }
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(push)
#pragma warning(disable:4127)
#endif
                                    if (!reverse)
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(pop)
#endif
                                    {
                                        brc.close_at = sstate.nth.in_string;
                                    } else	//  reverse == true.
                                    {
                                        brc.open_at = sstate.nth.in_string;
                                    }
                                }
                                continue;

                                case st_repeat_in_push:
                                {
                                    position_type& r = sstate.repeat[current_NFA.number];

                                    sstate.repeat_stack.push_back(r);
                                    r = sstate.nth.in_string;

                                    for (uint_l32 brno = current_NFA.quantifier.atleast; brno <= current_NFA.quantifier.atmost; ++brno) {
                                        submatch_type& inner_bracket = sstate.bracket[brno];

                                        sstate.capture_stack.push_back(inner_bracket.core);
                                        sstate.counter_stack.push_back(inner_bracket.counter);
                                        inner_bracket.core.open_at = inner_bracket.core.close_at = sstate.srchend;
                                        inner_bracket.counter = 0;
                                        //  ECMAScript 2019 (ES10) 21.2.2.5.1, Note 3.
                                    }
                                    sstate.bt_stack.push_back(sstate.nth);
                                }
                                goto MATCHED;

                                case st_repeat_in_pop:
                                    for (uint_l32 brno = current_NFA.quantifier.atmost; brno >= current_NFA.quantifier.atleast; --brno) {
                                        submatch_type& inner_bracket = sstate.bracket[brno];

                                        inner_bracket.counter = sstate.counter_stack.back();
                                        inner_bracket.core = sstate.capture_stack.back();
                                        sstate.counter_stack.pop_back();
                                        sstate.capture_stack.pop_back();
                                    }

                                    sstate.repeat[current_NFA.number] = sstate.repeat_stack.back();
                                    sstate.repeat_stack.pop_back();
                                    goto NOT_MATCHED;

                                case st_check_0_width_repeat:
                                    if (sstate.nth.in_string != sstate.repeat[current_NFA.number])
                                        goto MATCHED;

                                    sstate.nth.in_NFA_states = current_NFA.next_state2;
                                    continue;

                                case st_backreference:	//  '\\':
                                {
                                    const submatch_type& bracket = sstate.bracket[current_NFA.number];

                                    if (!bracket.counter)	//  Undefined.
                                    {
                                    ESCAPE_FROM_ZERO_WIDTH_MATCH:
                                        sstate.nth.in_NFA_states = current_NFA.next_state2;
                                        continue;
                                    } else {
                                        const submatchcore_type& brc = bracket.core;

                                        if (brc.open_at == brc.close_at) {
                                            goto ESCAPE_FROM_ZERO_WIDTH_MATCH;
                                        } else {
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(push)
#pragma warning(disable:4127)
#endif
                                            if (!reverse)
#if defined(_MSC_VER) && _MSC_VER >= 1400
#pragma warning(pop)
#endif
                                            {
                                                for (BidirectionalIterator backrefpos = brc.open_at; backrefpos != brc.close_at;) {
                                                    if (!sstate.is_at_srchend()) {
                                                        const uchar32 uchartxt = utf_traits::codepoint_inc(sstate.nth.in_string, sstate.srchend);
                                                        const uchar32 ucharref = utf_traits::codepoint_inc(backrefpos, brc.close_at);

                                                        if (casehelper_type::canonicalise(uchartxt) == casehelper_type::canonicalise(ucharref))
                                                            continue;
                                                    }
                                                    goto NOT_MATCHED;
                                                }
                                            } else	//  reverse == true.
                                            {
                                                for (BidirectionalIterator backrefpos = brc.close_at; backrefpos != brc.open_at;) {
                                                    if (!sstate.is_at_lookbehindlimit()) {
                                                        const uchar32 uchartxt = utf_traits::dec_codepoint(sstate.nth.in_string, sstate.lblim);
                                                        const uchar32 ucharref = utf_traits::dec_codepoint(backrefpos, brc.open_at);

                                                        if (casehelper_type::canonicalise(uchartxt) == casehelper_type::canonicalise(ucharref))
                                                            continue;
                                                    }
                                                    goto NOT_MATCHED;
                                                }
                                            }
                                        }
                                    }
                                }
                                goto MATCHED;

                                case st_lookaround_open:
                                {
                                    for (uint_l32 i = 1; i < this->number_of_brackets; ++i) {
                                        const submatch_type& sm = sstate.bracket[i];
                                        sstate.capture_stack.push_back(sm.core);
                                        sstate.counter_stack.push_back(sm.counter);
                                    }

                                    for (uint_l32 i = 0; i < this->number_of_counters; ++i)
                                        sstate.counter_stack.push_back(sstate.counter[i]);

                                    for (uint_l32 i = 0; i < this->number_of_repeats; ++i)
                                        sstate.repeat_stack.push_back(sstate.repeat[i]);

                                    const typename ss_type::bottom_state backup_bottom(sstate.btstack_size, sstate.capture_stack.size(), sstate.counter_stack.size(), sstate.repeat_stack.size());
                                    const BidirectionalIterator orgpos = sstate.nth.in_string;

                                    sstate.btstack_size = sstate.bt_stack.size();

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND) && !defined(SRELLDBG_NO_MPREWINDER)
                                    if (current_NFA.quantifier.atleast == 2) {
                                        sstate.repeat_stack.push_back(sstate.lblim);
                                        sstate.lblim = sstate.srchbegin;
                                    }
#endif

#if defined(SRELL_FIXEDWIDTHLOOKBEHIND)

                                    //					if (current_NFA.reverse)
                                    {
                                        for (uint_l32 i = 0; i < current_NFA.quantifier.atleast; ++i) {
                                            if (!sstate.is_at_lookbehindlimit()) {
                                                utf_traits::dec_codepoint(sstate.nth.in_string, sstate.lblim);
                                                continue;
                                            }
                                            is_matched = false;
                                            goto AFTER_LOOKAROUND;
                                        }
                                    }
#endif
                                    sstate.nth.in_NFA_states = current_NFA.next_state2;

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                                    is_matched = current_NFA.quantifier.atleast == 0 ? run_automaton<icase, false>(sstate /* , true */) : run_automaton<icase, true>(sstate /* , true */);
#else
                                    is_matched = run_automaton<icase, false>(sstate /* , true */);
#endif

#if defined(SRELL_FIXEDWIDTHLOOKBEHIND)
                                    AFTER_LOOKAROUND:
#endif
                                    {

#if !defined(SRELL_FIXEDWIDTHLOOKBEHIND) && !defined(SRELLDBG_NO_MPREWINDER)
                                        if (current_NFA.quantifier.atleast == 2) {
                                            sstate.lblim = sstate.repeat_stack[backup_bottom.repeatstack_size];
                                            if (is_matched)
                                                sstate.bracket[0].core.open_at = sstate.nth.in_string;
                                        }
#endif

#if defined(SRELL_ENABLE_GT)
                                        if (current_NFA.character != meta_char::mc_gt)	//  '>'
#endif
                                        {
                                            sstate.nth.in_string = orgpos;
                                        }
                                        sstate.bt_stack.resize(sstate.btstack_size);

                                        sstate.btstack_size = backup_bottom.btstack_size;
                                        sstate.capture_stack.resize(backup_bottom.capturestack_size);
                                        sstate.counter_stack.resize(backup_bottom.counterstack_size);
                                        sstate.repeat_stack.resize(backup_bottom.repeatstack_size);

                                        is_matched ^= current_NFA.is_not;
                                    }
                                }
                                if (is_matched) {
                                    sstate.nth.in_NFA_states = current_NFA.next_state1;
                                    continue;
                                }

                                //			case st_lookaround_pop:
                                for (uint_l32 i = this->number_of_repeats; i;) {
                                    sstate.repeat[--i] = sstate.repeat_stack.back();
                                    sstate.repeat_stack.pop_back();
                                }

                                for (uint_l32 i = this->number_of_counters; i;) {
                                    sstate.counter[--i] = sstate.counter_stack.back();
                                    sstate.counter_stack.pop_back();
                                }

                                for (uint_l32 i = this->number_of_brackets; i > 1;) {
                                    submatch_type& sm = sstate.bracket[--i];

                                    sm.counter = sstate.counter_stack.back();
                                    sm.core = sstate.capture_stack.back();
                                    sstate.counter_stack.pop_back();
                                    sstate.capture_stack.pop_back();
                                }
                                goto NOT_MATCHED;

                                case st_bol:	//  '^':
                                    if (sstate.is_at_lookbehindlimit() && !sstate.match_prev_avail_flag()) {
                                        if (!sstate.match_not_bol_flag())
                                            goto MATCHED;
                                    }
                                    //  !sstate.is_at_lookbehindlimit() || sstate.match_prev_avail_flag()
                                    else if (current_NFA.multiline) {
                                        const uchar32 prevchar = utf_traits::prevcodepoint(sstate.nth.in_string, sstate.lblim);

                                        if (this->character_class.is_included(re_character_class::newline, prevchar))
                                            goto MATCHED;
                                    }
                                    goto NOT_MATCHED;

                                case st_eol:	//  '$':
                                    if (sstate.is_at_srchend()) {
                                        if (!sstate.match_not_eol_flag())
                                            goto MATCHED;
                                    } else if (current_NFA.multiline) {
                                        const uchar32 nextchar = utf_traits::codepoint(sstate.nth.in_string, sstate.srchend);

                                        if (this->character_class.is_included(re_character_class::newline, nextchar))
                                            goto MATCHED;
                                    }
                                    goto NOT_MATCHED;

                                case st_boundary:	//  '\b' '\B'
                                    is_matched = current_NFA.is_not;
                                    //				is_matched = current_NFA.character == char_alnum::ch_B;

                                                    //  First, suppose the previous character is not \w but \W.

                                    if (sstate.is_at_srchend()) {
                                        if (sstate.match_not_eow_flag())
                                            is_matched = !is_matched;
                                    } else if (this->character_class.is_included(current_NFA.number, utf_traits::codepoint(sstate.nth.in_string, sstate.srchend))) {
                                        is_matched = !is_matched;
                                    }
                                    //      \W/last     \w
                                    //  \b  false       true
                                    //  \B  true        false

                                    //  Second, if the actual previous character is \w, flip is_matched.

                                    if (sstate.is_at_lookbehindlimit() && !sstate.match_prev_avail_flag()) {
                                        if (sstate.match_not_bow_flag())
                                            is_matched = !is_matched;
                                    }
                                    //  !sstate.is_at_lookbehindlimit() || sstate.match_prev_avail_flag()
                                    else if (this->character_class.is_included(current_NFA.number, utf_traits::prevcodepoint(sstate.nth.in_string, sstate.lblim))) {
                                        is_matched = !is_matched;
                                    }
                                    //  \b                          \B
                                    //  pre cur \W/last \w          pre cur \W/last \w
                                    //  \W/base false   true        \W/base true    false
                                    //  \w      true    false       \w      false   true

                                    goto JUDGE;

                                case st_success:	//  == lookaround_close.
                    //				if (is_recursive)
                                    if (sstate.btstack_size)
                                        return true;

                                    if
                                        (
                                            (!sstate.match_not_null_flag() || !sstate.is_null())
                                            &&
                                            (!sstate.match_match_flag() || sstate.is_at_srchend())
                                            )
                                        return true;

                                    goto NOT_MATCHED;

#if !defined(SRELLDBG_NO_NEXTPOS_OPT)
                                case st_move_nextpos:
#if !defined(SRELLDBG_NO_1STCHRCLS) && !defined(SRELLDBG_NO_BITSET)
                                    sstate.nextpos = sstate.nth.in_string;
                                    if (!sstate.is_at_srchend())
                                        ++sstate.nextpos;
#else	//  defined(SRELLDBG_NO_1STCHRCLS) || defined(SRELLDBG_NO_BITSET)
                                    if (sstate.nth.in_string != sstate.bracket[0].core.open_at) {
                                        sstate.nextpos = sstate.nth.in_string;
                                        if (!sstate.is_at_srchend())
                                            utf_traits::codepoint_inc(sstate.nextpos, sstate.srchend);
                                    }
#endif
                                    goto MATCHED;
#endif

                                default:
                                    //  Reaching here means that this->NFA_states is corrupted.
                                    throw regex_error(regex_constants::error_internal);

                            }
                    }
                }
            }
        };
        //  regex_object

    }	//  namespace regex_internal

//  ... "rei_algorithm.hpp"]
//  ["basic_regex.hpp" ...

//  28.8, class template basic_regex:
    template <class charT, class traits = regex_traits<charT> >
    class basic_regex : public regex_internal::regex_object<charT, traits> {
    public:

        //  Types:
        typedef charT value_type;
        typedef traits traits_type;
        typedef typename traits::string_type string_type;
        typedef regex_constants::syntax_option_type flag_type;
        typedef typename traits::locale_type locale_type;

        //  28.8.1, constants:
        //  [7.8.1] constants
        static const regex_constants::syntax_option_type icase = regex_constants::icase;
        static const regex_constants::syntax_option_type nosubs = regex_constants::nosubs;
        static const regex_constants::syntax_option_type optimize = regex_constants::optimize;
        static const regex_constants::syntax_option_type collate = regex_constants::collate;
        static const regex_constants::syntax_option_type ECMAScript = regex_constants::ECMAScript;
        static const regex_constants::syntax_option_type basic = regex_constants::basic;
        static const regex_constants::syntax_option_type extended = regex_constants::extended;
        static const regex_constants::syntax_option_type awk = regex_constants::awk;
        static const regex_constants::syntax_option_type grep = regex_constants::grep;
        static const regex_constants::syntax_option_type egrep = regex_constants::egrep;
        static const regex_constants::syntax_option_type multiline = regex_constants::multiline;

        static const regex_constants::syntax_option_type dotall = regex_constants::dotall;

        //  28.8.2, construct/copy/destroy:
        //  [7.8.2] construct/copy/destroy
        basic_regex() {
        }

        explicit basic_regex(const charT* const p, const flag_type f = regex_constants::ECMAScript) {
            assign(p, p + std::char_traits<charT>::length(p), f);
        }

        basic_regex(const charT* const p, const std::size_t len, const flag_type f = regex_constants::ECMAScript) {
            assign(p, p + len, f);
        }

        basic_regex(const basic_regex& e) {
            assign(e);
        }

#if defined(SRELL_CPP11_MOVE_ENABLED)
        basic_regex(basic_regex&& e) SRELL_NOEXCEPT {
            assign(std::move(e));
        }
#endif

        template <class ST, class SA>
        explicit basic_regex(const std::basic_string<charT, ST, SA>& p, const flag_type f = regex_constants::ECMAScript) {
            assign(p, f);
        }

        template <class ForwardIterator>
        basic_regex(ForwardIterator first, ForwardIterator last, const flag_type f = regex_constants::ECMAScript) {
            assign(first, last, f);
        }

#if defined(SRELL_CPP11_INITIALIZER_LIST_ENABLED)
        basic_regex(std::initializer_list<charT> il, const flag_type f = regex_constants::ECMAScript) {
            assign(il, f);
        }
#endif

        //	~basic_regex();

        basic_regex& operator=(const basic_regex& right) {
            return assign(right);
        }

#if defined(SRELL_CPP11_MOVE_ENABLED)
        basic_regex& operator=(basic_regex&& e) SRELL_NOEXCEPT {
            return assign(std::move(e));
        }
#endif

        basic_regex& operator=(const charT* const ptr) {
            return assign(ptr);
        }

#if defined(SRELL_CPP11_INITIALIZER_LIST_ENABLED)
        basic_regex& operator=(std::initializer_list<charT> il) {
            return assign(il.begin(), il.end());
        }
#endif

        template <class ST, class SA>
        basic_regex& operator=(const std::basic_string<charT, ST, SA>& p) {
            return assign(p);
        }

        //  28.8.3, assign:
        //  [7.8.3] assign
        basic_regex& assign(const basic_regex& right) {
            regex_internal::re_object_core<charT, traits>::operator=(right);
            return *this;
        }

#if defined(SRELL_CPP11_MOVE_ENABLED)
        basic_regex& assign(basic_regex&& right) SRELL_NOEXCEPT {
            regex_internal::re_object_core<charT, traits>::operator=(std::move(right));
            return *this;
        }
#endif

        basic_regex& assign(const charT* const ptr, const flag_type f = regex_constants::ECMAScript) {
            return assign(ptr, ptr + std::char_traits<charT>::length(ptr), f);
        }

        basic_regex& assign(const charT* const p, std::size_t len, const flag_type f = regex_constants::ECMAScript) {
            return assign(p, p + len, f);
        }

        template <class string_traits, class A>
        basic_regex& assign(const std::basic_string<charT, string_traits, A>& s, const flag_type f = regex_constants::ECMAScript) {
            return assign(s.c_str(), s.c_str() + s.size(), f);
        }

        template <class InputIterator>
        basic_regex& assign(InputIterator first, InputIterator last, const flag_type f = regex_constants::ECMAScript) {
#if defined(SRELL_STRICT_IMPL)
            basic_regex tmp;
            tmp.compile(first, last, f);
            tmp.swap(*this);
#else
            this->compile(first, last, f);
#endif
            return *this;
        }

#if defined(SRELL_CPP11_INITIALIZER_LIST_ENABLED)
        basic_regex& assign(std::initializer_list<charT> il, const flag_type f = regex_constants::ECMAScript) {
            return assign(il.begin(), il.end(), f);
        }
#endif

        //  28.8.4, const operations:
        //  [7.8.4] const operations
        unsigned mark_count() const {
            return this->number_of_brackets - 1;
        }

        flag_type flags() const {
            return this->soflags;
        }

        //  28.8.5, locale:
        //  [7.8.5] locale
        locale_type imbue(locale_type loc) {
            return this->traits_inst.imbue(loc);
        }

        locale_type getloc() const {
            return this->traits_inst.getloc();
        }

        //  28.8.6, swap:
        //  [7.8.6] swap
        void swap(basic_regex& e) {
            regex_internal::re_object_core<charT, traits>::swap(e);
        }
    };

    //  28.8.6, basic_regex swap:
    template <class charT, class traits>
    void swap(basic_regex<charT, traits>& lhs, basic_regex<charT, traits>& rhs) {
        lhs.swap(rhs);
    }

    typedef basic_regex<char> regex;
    typedef basic_regex<wchar_t> wregex;

#if defined(WCHAR_MAX)
#if WCHAR_MAX >= 0x10ffff
    typedef wregex u32wregex;
    typedef u32wregex u1632wregex;
#elif WCHAR_MAX >= 0xffff
    typedef basic_regex<wchar_t, u16regex_traits<wchar_t> > u16wregex;
    typedef u16wregex u1632wregex;
#endif
#endif

#if defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef basic_regex<char8_t> u8regex;
#endif

    typedef basic_regex<char, u8regex_traits<char> > u8cregex;
#if !defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef u8cregex u8regex;
#endif

#if defined(SRELL_CPP11_CHAR1632_ENABLED)
    typedef basic_regex<char16_t> u16regex;
    typedef basic_regex<char32_t> u32regex;
#endif

    //  ... "basic_regex.hpp"]
    //  ["regex_iterator.hpp" ...

    //  28.12.1, class template regex_iterator:
    template <class BidirectionalIterator, class charT = typename std::iterator_traits<BidirectionalIterator>::value_type, class traits = regex_traits<charT> >
    class regex_iterator {
    public:

        typedef basic_regex<charT, traits> regex_type;
        typedef match_results<BidirectionalIterator> value_type;
        typedef std::ptrdiff_t difference_type;
        typedef const value_type* pointer;
        typedef const value_type& reference;
        typedef std::forward_iterator_tag iterator_category;

        regex_iterator() {
            //  28.12.1.1: Constructs an end-of-sequence iterator.
        }

        regex_iterator(
            const BidirectionalIterator a,
            const BidirectionalIterator b,
            const regex_type& re,
            const regex_constants::match_flag_type m = regex_constants::match_default)
            : begin(a), end(b), pregex(&re), flags(m) {
            regex_search(begin, end, begin, match, *pregex, flags);
            //  28.12.1.1: If this call returns false the constructor
            //    sets *this to the end-of-sequence iterator.
        }

        regex_iterator(const regex_iterator& that) {
            operator=(that);
        }

        regex_iterator& operator=(const regex_iterator& that) {
            if (this != &that) {
                this->begin = that.begin;
                this->end = that.end;
                this->pregex = that.pregex;
                this->flags = that.flags;
                this->match = that.match;
            }
            return *this;
        }

        bool operator==(const regex_iterator& right) const {
            //  It is probably safe to assume that match.size() == 0 means
            //  end-of-sequence, because it happens only when 1) never tried
            //  regex_search, or 2) regex_search returned false.

            if (this->match.size() == 0 || right.match.size() == 0)
                return this->match.size() == right.match.size();

            return
                this->begin == right.begin
                &&
                this->end == right.end
                &&
                this->pregex == right.pregex
                &&
                this->flags == right.flags
                &&
                this->match[0] == right.match[0];
        }

        bool operator!=(const regex_iterator& right) const {
            return !(*this == right);
        }

        const value_type& operator*() const {
            return match;
        }

        const value_type* operator->() const {
            return &match;
        }

        regex_iterator& operator++() {
            if (this->match.size()) {
                BidirectionalIterator start = match[0].second;

                if (match[0].first == start)	//  The iterator holds a 0-length match.
                {
                    if (start == end) {
                        match.clear_();
                        //    28.12.1.4.2: If the iterator holds a zero-length match and
                        //  start == end the operator sets *this to the end-ofsequence
                        //  iterator and returns *this.
                    } else {
                        //    28.12.1.4.3: Otherwise, if the iterator holds a zero-length match
                        //  the operator calls regex_search(start, end, match, *pregex, flags
                        //  | regex_constants::match_not_null | regex_constants::match_continuous).
                        //  If the call returns true the operator returns *this. [Cont...]

                        if (!regex_search(start, end, begin, match, *pregex, flags | regex_constants::match_not_null | regex_constants::match_continuous)) {
                            const BidirectionalIterator prevend = start;

                            //  [...Cont] Otherwise the operator increments start and continues
                            //  as if the most recent match was not a zero-length match.
    //						++start;
                            utf_traits::codepoint_inc(start, end);

                            flags |= regex_constants::match_prev_avail;

                            if (regex_search(start, end, begin, match, *pregex, flags)) {
                                //    28.12.1.4.5-6: In all cases in which the call to regex_search
                                //  returns true, match.prefix().first shall be equal to the previous
                                //  value of match[0].second, ... match[i].position() shall return
                                //  distance(begin, match[i].first).
                                //    This means that match[i].position() gives the offset from the
                                //  beginning of the target sequence, which is often not the same as
                                //  the offset from the sequence passed in the call to regex_search.
                                //
                                //  To satisfy this:
                                match.set_prefix_first_(prevend);
                            }
                        }
                    }
                } else {
                    //    28.12.1.4.4: If the most recent match was not a zero-length match,
                    //  the operator sets flags to flags | regex_constants::match_prev_avail
                    //  and calls regex_search(start, end, match, *pregex, flags). [Cont...]
                    flags |= regex_constants::match_prev_avail;

                    regex_search(start, end, begin, match, *pregex, flags);
                    //  [...Cont] If the call returns false the iterator sets *this to
                    //  the end-of-sequence iterator. The iterator then returns *this.
                    //
                    //    28.12.1.4.5-6: In all cases in which the call to regex_search
                    //  returns true, match.prefix().first shall be equal to the previous
                    //  value of match[0].second, ... match[i].position() shall return
                    //  distance(begin, match[i].first).
                    //    This means that match[i].position() gives the offset from the
                    //  beginning of the target sequence, which is often not the same as
                    //  the offset from the sequence passed in the call to regex_search.
                    //
                    //  These should already be done in regex_search.
                }
            }
            return *this;
        }

        regex_iterator operator++(int) {
            const regex_iterator tmp = *this;
            ++(*this);
            return tmp;
        }

    private:

        BidirectionalIterator                begin;
        BidirectionalIterator                end;
        const regex_type* pregex;
        regex_constants::match_flag_type     flags;
        match_results<BidirectionalIterator> match;

        typedef typename traits::utf_traits utf_traits;
    };

    typedef regex_iterator<const char*> cregex_iterator;
    typedef regex_iterator<const wchar_t*> wcregex_iterator;
    typedef regex_iterator<std::string::const_iterator> sregex_iterator;
    typedef regex_iterator<std::wstring::const_iterator> wsregex_iterator;

#if defined(SRELL_CPP11_CHAR1632_ENABLED)
    typedef regex_iterator<const char16_t*> u16cregex_iterator;
    typedef regex_iterator<const char32_t*> u32cregex_iterator;
    typedef regex_iterator<std::u16string::const_iterator> u16sregex_iterator;
    typedef regex_iterator<std::u32string::const_iterator> u32sregex_iterator;
#endif

#if defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef regex_iterator<const char8_t*> u8cregex_iterator;
#endif
#if defined(SRELL_CPP20_CHAR8_ENABLED) && SRELL_CPP20_CHAR8_ENABLED >= 2
    typedef regex_iterator<std::u8string::const_iterator> u8sregex_iterator;
#endif

    typedef regex_iterator<const char*, std::iterator_traits<const char*>::value_type, u8regex_traits<std::iterator_traits<const char*>::value_type> > u8ccregex_iterator;
    typedef regex_iterator<std::string::const_iterator, std::iterator_traits<std::string::const_iterator>::value_type, u8regex_traits<std::iterator_traits<std::string::const_iterator>::value_type> > u8csregex_iterator;
#if !defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef u8ccregex_iterator u8cregex_iterator;
#endif
#if !defined(SRELL_CPP20_CHAR8_ENABLED) || SRELL_CPP20_CHAR8_ENABLED < 2
    typedef u8csregex_iterator u8sregex_iterator;
#endif

#if defined(WCHAR_MAX)
#if WCHAR_MAX >= 0x10ffff
    typedef wcregex_iterator u32wcregex_iterator;
    typedef wsregex_iterator u32wsregex_iterator;
    typedef u32wcregex_iterator u1632wcregex_iterator;
    typedef u32wsregex_iterator u1632wsregex_iterator;
#elif WCHAR_MAX >= 0xffff
    typedef regex_iterator<const wchar_t*, std::iterator_traits<const wchar_t*>::value_type, u16regex_traits<std::iterator_traits<const wchar_t*>::value_type> > u16wcregex_iterator;
    typedef regex_iterator<std::wstring::const_iterator, std::iterator_traits<std::wstring::const_iterator>::value_type, u16regex_traits<std::iterator_traits<std::wstring::const_iterator>::value_type> > u16wsregex_iterator;
    typedef u16wcregex_iterator u1632wcregex_iterator;
    typedef u16wsregex_iterator u1632wsregex_iterator;
#endif
#endif

    //  ... "regex_iterator.hpp"]
    //  ["regex_algorithm.hpp" ...

    //  28.11.2, function template regex_match:
    //  [7.11.2] Function template regex_match
    template <class BidirectionalIterator, class Allocator, class charT, class traits>
    bool regex_match(
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        match_results<BidirectionalIterator, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return e.search(first, last, first, m, flags | regex_constants::match_continuous | regex_constants::match_match_);
    }

    template <class BidirectionalIterator, class charT, class traits>
    bool regex_match(
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        //  4 Effects: Behaves "as if" by constructing an instance of
        //  match_results<BidirectionalIterator> what, and then returning the
        //  result of regex_match(first, last, what, e, flags).

        match_results<BidirectionalIterator> what;

        return regex_match(first, last, what, e, flags);
    }

    template <class charT, class Allocator, class traits>
    bool regex_match(
        const charT* const str,
        match_results<const charT*, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_match(str, str + std::char_traits<charT>::length(str), m, e, flags);
    }

    template <class ST, class SA, class Allocator, class charT, class traits>
    bool regex_match(
        const std::basic_string<charT, ST, SA>& s,
        match_results<typename std::basic_string<charT, ST, SA>::const_iterator, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_match(s.begin(), s.end(), m, e, flags);
    }

    template <class charT, class traits>
    bool regex_match(
        const charT* const str,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_match(str, str + std::char_traits<charT>::length(str), e, flags);
    }

    template <class ST, class SA, class charT, class traits>
    bool regex_match(
        const std::basic_string<charT, ST, SA>& s,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_match(s.begin(), s.end(), e, flags);
    }

    template <class BidirectionalIterator, class Allocator, class charT, class traits>
    bool regex_search(
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        const BidirectionalIterator lookbehind_limit,
        match_results<BidirectionalIterator, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return e.search(first, last, lookbehind_limit, m, flags);
    }

    template <class BidirectionalIterator, class charT, class traits>
    bool regex_search(
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        const BidirectionalIterator lookbehind_limit,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        //  6 Effects: Behaves "as if" by constructing an object what of type
        //  match_results<iterator> and then returning the result of
        //  regex_search(first, last, what, e, flags).

        match_results<BidirectionalIterator> what;
        return regex_search(first, last, lookbehind_limit, what, e, flags);
    }

    //  28.11.3, function template regex_search:
    //  7.11.3 regex_search [tr.re.alg.search]
    template <class BidirectionalIterator, class Allocator, class charT, class traits>
    bool regex_search(
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        match_results<BidirectionalIterator, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return e.search(first, last, first, m, flags);
    }

    template <class BidirectionalIterator, class charT, class traits>
    bool regex_search(
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        //  6 Effects: Behaves "as if" by constructing an object what of type
        //  match_results<iterator> and then returning the result of
        //  regex_search(first, last, what, e, flags).

        match_results<BidirectionalIterator> what;
        return regex_search(first, last, what, e, flags);
    }

    template <class charT, class Allocator, class traits>
    bool regex_search(
        const charT* const str,
        match_results<const charT*, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_search(str, str + std::char_traits<charT>::length(str), m, e, flags);
    }

    template <class charT, class traits>
    bool regex_search(
        const charT* const str,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_search(str, str + std::char_traits<charT>::length(str), e, flags);
    }

    template <class ST, class SA, class charT, class traits>
    bool regex_search(
        const std::basic_string<charT, ST, SA>& s,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_search(s.begin(), s.end(), e, flags);
    }

    template <class ST, class SA, class Allocator, class charT, class traits>
    bool regex_search(
        const std::basic_string<charT, ST, SA>& s,
        match_results<typename std::basic_string<charT, ST, SA>::const_iterator, Allocator>& m,
        const basic_regex<charT, traits>& e,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        return regex_search(s.begin(), s.end(), m, e, flags);
    }

    //  28.11.4, function template regex_replace:
    //  [7.11.4] Function template regex_replace
    template <class OutputIterator, class BidirectionalIterator, class traits, class charT, class ST, class SA>
    OutputIterator regex_replace(
        OutputIterator out,
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        const basic_regex<charT, traits>& e,
        const std::basic_string<charT, ST, SA>& fmt,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        typedef regex_iterator<BidirectionalIterator, charT, traits> iterator_type;

        const bool do_copy = !(flags & regex_constants::format_no_copy);
        const iterator_type eos;
        iterator_type i(first, last, e, flags);
        typename iterator_type::value_type::value_type last_m_suffix;

        last_m_suffix.first = first;
        last_m_suffix.second = last;

        for (; i != eos; ++i) {
            if (do_copy)
                out = std::copy(i->prefix().first, i->prefix().second, out);

            out = i->format(out, fmt, flags);
            last_m_suffix = i->suffix();

            if (flags & regex_constants::format_first_only)
                break;
        }

        if (do_copy)
            out = std::copy(last_m_suffix.first, last_m_suffix.second, out);

        return out;
    }

    template <class OutputIterator, class BidirectionalIterator, class traits, class charT>
    OutputIterator regex_replace(
        OutputIterator out,
        const BidirectionalIterator first,
        const BidirectionalIterator last,
        const basic_regex<charT, traits>& e,
        const charT* const fmt,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        //  Strictly speaking, this should be implemented as a version different
        //  from the above with changing the line i->format(out, fmt, flags) to
        //  i->format(out, fmt, fmt + char_traits<charT>::length(fmt), flags).

        const std::basic_string<charT> fs(fmt, fmt + std::char_traits<charT>::length(fmt));

        return regex_replace(out, first, last, e, fs, flags);
    }

    template <class traits, class charT, class ST, class SA, class FST, class FSA>
    std::basic_string<charT, ST, SA> regex_replace(
        const std::basic_string<charT, ST, SA>& s,
        const basic_regex<charT, traits>& e,
        const std::basic_string<charT, FST, FSA>& fmt,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        std::basic_string<charT, ST, SA> result;

        regex_replace(std::back_inserter(result), s.begin(), s.end(), e, fmt, flags);
        return result;
    }

    template <class traits, class charT, class ST, class SA>
    std::basic_string<charT, ST, SA> regex_replace(
        const std::basic_string<charT, ST, SA>& s,
        const basic_regex<charT, traits>& e,
        const charT* const fmt,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        std::basic_string<charT, ST, SA> result;

        regex_replace(std::back_inserter(result), s.begin(), s.end(), e, fmt, flags);
        return result;
    }

    template <class traits, class charT, class ST, class SA>
    std::basic_string<charT> regex_replace(
        const charT* const s,
        const basic_regex<charT, traits>& e,
        const std::basic_string<charT, ST, SA>& fmt,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        std::basic_string<charT> result;

        regex_replace(std::back_inserter(result), s, s + std::char_traits<charT>::length(s), e, fmt, flags);
        return result;
    }

    template <class traits, class charT>
    std::basic_string<charT> regex_replace(
        const charT* const s,
        const basic_regex<charT, traits>& e,
        const charT* const fmt,
        const regex_constants::match_flag_type flags = regex_constants::match_default
    ) {
        std::basic_string<charT> result;

        regex_replace(std::back_inserter(result), s, s + std::char_traits<charT>::length(s), e, fmt, flags);
        return result;
    }

    //  ... "regex_algorithm.hpp"]
    //  ["regex_token_iterator.hpp" ...

    //  28.12.2, class template regex_token_iterator:
    template <class BidirectionalIterator, class charT = typename std::iterator_traits<BidirectionalIterator>::value_type, class traits = regex_traits<charT> >
    class regex_token_iterator {
    public:

        typedef basic_regex<charT, traits> regex_type;
        typedef sub_match<BidirectionalIterator> value_type;
        typedef std::ptrdiff_t difference_type;
        typedef const value_type* pointer;
        typedef const value_type& reference;
        typedef std::forward_iterator_tag iterator_category;

        regex_token_iterator() : result(NULL) {
            //  Constructs the end-of-sequence iterator.
        }

        regex_token_iterator(
            const BidirectionalIterator a,
            const BidirectionalIterator b,
            const regex_type& re,
            int submatch = 0,
            regex_constants::match_flag_type m = regex_constants::match_default
        ) : position(a, b, re, m), result(NULL), subs(1, submatch) {
            post_constructor(a, b);
        }

        regex_token_iterator(
            const BidirectionalIterator a,
            const BidirectionalIterator b,
            const regex_type& re,
            const std::vector<int>& submatches,
            regex_constants::match_flag_type m = regex_constants::match_default
        ) : position(a, b, re, m), result(NULL), subs(submatches) {
            post_constructor(a, b);
        }

#if defined(SRELL_CPP11_INITIALIZER_LIST_ENABLED)
        regex_token_iterator(
            const BidirectionalIterator a,
            const BidirectionalIterator b,
            const regex_type& re,
            std::initializer_list<int> submatches,
            regex_constants::match_flag_type m = regex_constants::match_default
        ) : position(a, b, re, m), result(NULL), subs(submatches) {
            post_constructor(a, b);
        }
#endif

        template <std::size_t N>	//  Was R in TR1.
        regex_token_iterator(
            const BidirectionalIterator a,
            const BidirectionalIterator b,
            const regex_type& re,
            const int(&submatches)[N],
            regex_constants::match_flag_type m = regex_constants::match_default
        ) : position(a, b, re, m), result(NULL), subs(submatches, submatches + N) {
            post_constructor(a, b);
        }

        regex_token_iterator(const regex_token_iterator& that) {
            operator=(that);
        }

        regex_token_iterator& operator=(const regex_token_iterator& that) {
            if (this != &that) {
                this->position = that.position;
                this->result = that.result;
                this->suffix = that.suffix;
                this->N = that.N;
                this->subs = that.subs;
            }
            return *this;
        }

        bool operator==(const regex_token_iterator& right) {
            if (this->result == NULL || right.result == NULL)
                return this->result == right.result;

            if (this->result == &this->suffix || right.result == &right.suffix)
                return this->suffix == right.suffix;

            return
                this->position == right.position
                &&
                this->N == right.N
                &&
                this->subs == right.subs;
        }

        bool operator!=(const regex_token_iterator& right) {
            return !(*this == right);
        }

        const value_type& operator*() {
            return *result;
        }

        const value_type* operator->() {
            return result;
        }

        regex_token_iterator& operator++() {
            position_iterator prev(position);
            position_iterator eos_iterator;

            if (result != NULL)
                //  To avoid inifinite loop. The specification does not require, though.
            {
                if (result == &suffix) {
                    result = NULL;	//  end-of-sequence.
                } else {
                    ++this->N;
                    for (;;) {
                        if (this->N < subs.size()) {
                            result = subs[this->N] != -1 ? &((*position)[subs[this->N]]) : &((*position).prefix());
                            break;
                        }

                        this->N = 0;
                        ++position;

                        if (position == eos_iterator) {
                            if (this->N < subs.size() && prev->suffix().length() && minus1_in_subs()) {
                                suffix = prev->suffix();
                                result = &suffix;
                            } else {
                                result = NULL;
                            }
                            break;
                        }
                    }
                }
            }
            return *this;
        }

        regex_token_iterator operator++(int) {
            const regex_token_iterator tmp(*this);
            ++(*this);
            return tmp;
        }

    private:

        void post_constructor(const BidirectionalIterator a, const BidirectionalIterator b) {
            position_iterator eos_iterator;

            this->N = 0;

            if (position != eos_iterator && subs.size()) {
                result = subs[this->N] != -1 ? &((*position)[subs[this->N]]) : &((*position).prefix());
            } else if (minus1_in_subs())	//  end-of-sequence.
            {
                suffix.first = a;
                suffix.second = b;
                suffix.matched = a != b;
                //  28.1.2.7: In a suffix iterator the member result holds a pointer
                //  to the data member suffix, the value of the member suffix.match is true,

                if (suffix.matched)
                    result = &suffix;
                else
                    result = NULL;	//  Means end-of-sequence.
            }
        }

        bool minus1_in_subs() const {
            for (std::size_t i = 0; i < subs.size(); ++i)
                if (subs[i] == -1)
                    return true;

            return false;
        }

    private:

        typedef regex_iterator<BidirectionalIterator, charT, traits> position_iterator;
        position_iterator position;
        const value_type* result;
        value_type suffix;
        std::size_t N;
        std::vector<int> subs;
    };

    typedef regex_token_iterator<const char*> cregex_token_iterator;
    typedef regex_token_iterator<const wchar_t*> wcregex_token_iterator;
    typedef regex_token_iterator<std::string::const_iterator> sregex_token_iterator;
    typedef regex_token_iterator<std::wstring::const_iterator> wsregex_token_iterator;

#if defined(SRELL_CPP11_CHAR1632_ENABLED)
    typedef regex_token_iterator<const char16_t*> u16cregex_token_iterator;
    typedef regex_token_iterator<const char32_t*> u32cregex_token_iterator;
    typedef regex_token_iterator<std::u16string::const_iterator> u16sregex_token_iterator;
    typedef regex_token_iterator<std::u32string::const_iterator> u32sregex_token_iterator;
#endif

#if defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef regex_token_iterator<const char8_t*> u8cregex_token_iterator;
#endif
#if defined(SRELL_CPP20_CHAR8_ENABLED) && SRELL_CPP20_CHAR8_ENABLED >= 2
    typedef regex_token_iterator<std::u8string::const_iterator> u8sregex_token_iterator;
#endif

    typedef regex_token_iterator<const char*, std::iterator_traits<const char*>::value_type, u8regex_traits<std::iterator_traits<const char*>::value_type> > u8ccregex_token_iterator;
    typedef regex_token_iterator<std::string::const_iterator, std::iterator_traits<std::string::const_iterator>::value_type, u8regex_traits<std::iterator_traits<std::string::const_iterator>::value_type> > u8csregex_token_iterator;
#if !defined(SRELL_CPP20_CHAR8_ENABLED)
    typedef u8ccregex_token_iterator u8cregex_token_iterator;
#endif
#if !defined(SRELL_CPP20_CHAR8_ENABLED) || SRELL_CPP20_CHAR8_ENABLED < 2
    typedef u8csregex_token_iterator u8sregex_token_iterator;
#endif

#if defined(WCHAR_MAX)
#if WCHAR_MAX >= 0x10ffff
    typedef wcregex_token_iterator u32wcregex_token_iterator;
    typedef wsregex_token_iterator u32wsregex_token_iterator;
    typedef u32wcregex_token_iterator u1632wcregex_token_iterator;
    typedef u32wsregex_token_iterator u1632wsregex_token_iterator;
#elif WCHAR_MAX >= 0xffff
    typedef regex_token_iterator<const wchar_t*, std::iterator_traits<const wchar_t*>::value_type, u16regex_traits<std::iterator_traits<const wchar_t*>::value_type> > u16wcregex_token_iterator;
    typedef regex_token_iterator<std::wstring::const_iterator, std::iterator_traits<std::wstring::const_iterator>::value_type, u16regex_traits<std::iterator_traits<std::wstring::const_iterator>::value_type> > u16wsregex_token_iterator;
    typedef u16wcregex_token_iterator u1632wcregex_token_iterator;
    typedef u16wsregex_token_iterator u1632wsregex_token_iterator;
#endif
#endif

    //  ... "regex_token_iterator.hpp"]

    }		//  namespace srell

#ifdef SRELL_NOEXCEPT
#undef SRELL_NOEXCEPT
#endif

#ifdef SRELL_CPP20_CHAR8_ENABLED
#undef SRELL_CPP20_CHAR8_ENABLED
#endif

#ifdef SRELL_CPP11_CHAR1632_ENABLED
#undef SRELL_CPP11_CHAR1632_ENABLED
#endif

#ifdef SRELL_CPP11_INITIALIZER_LIST_ENABLED
#undef SRELL_CPP11_INITIALIZER_LIST_ENABLED
#endif

#ifdef SRELL_CPP11_MOVE_ENABLED
#undef SRELL_CPP11_MOVE_ENABLED
#endif

#endif	//  SRELL_REGEX_TEMPLATE_LIBRARY
