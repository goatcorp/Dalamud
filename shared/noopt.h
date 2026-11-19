#pragma once

/* O0 isn't standardized, O0h well..
 * NOOPT_BEGIN NOINLINE void foobar() { } NOOPT_END
 * and yet again some other projects pushing through despite its fragility.
 */

#if defined(_MSC_VER)
    #define NOINLINE __declspec(noinline)
    #define NOOPT_BEGIN __pragma(optimize("", off))
    #define NOOPT_END   __pragma(optimize("", on))

#elif defined(__clang__)
    #define NOINLINE __attribute__((noinline, optnone))
    #define NOOPT_BEGIN
    #define NOOPT_END

#elif defined(__GNUC__)
    #define NOINLINE __attribute__((noinline, optimize("O0")))
    #define NOOPT_BEGIN
    #define NOOPT_END

#else
    #define NOINLINE
    #define NOOPT_BEGIN
    #define NOOPT_END
#endif
