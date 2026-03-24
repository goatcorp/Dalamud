if(NOT WIN32 AND NOT CMAKE_SYSTEM_NAME STREQUAL "Windows")
    set(CMAKE_SYSTEM_NAME Windows)
    set(TOOLCHAIN_PREFIX x86_64-w64-mingw32)
    message(STATUS "Cross-compiling using default settings with ${TOOLCHAIN_PREFIX}")

    set(CMAKE_C_COMPILER ${TOOLCHAIN_PREFIX}-gcc)
    set(CMAKE_CXX_COMPILER ${TOOLCHAIN_PREFIX}-g++)
    set(CMAKE_Fortran_COMPILER ${TOOLCHAIN_PREFIX}-gfortran)
    set(CMAKE_RC_COMPILER ${TOOLCHAIN_PREFIX}-windres)
    set(CMAKE_ASM_MASM_COMPILER ${CMAKE_CURRENT_LIST_DIR}/../lib/JWasm/build/jwasm)
    set(CMAKE_AR ${TOOLCHAIN_PREFIX}-ar)
    set(CMAKE_RANLIB ${TOOLCHAIN_PREFIX}-ranlib)
    set(CMAKE_OBJDUMP ${TOOLCHAIN_PREFIX}-objdump)

    set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
    set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
    set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
    set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)

    set(CMAKE_GET_RUNTIME_DEPENDENCIES_PLATFORM windows+pe)
    set(CMAKE_GET_RUNTIME_DEPENDENCIES_TOOL objdump)
    set(CMAKE_GET_RUNTIME_DEPENDENCIES_COMMAND ${CMAKE_OBJDUMP})

    set(CMAKE_ASM_MASM_FLAGS "-win64 -Zg -c")
    set(CMAKE_ASM_MASM_FLAGS_DEBUG "-Zi")

    if(NOT CMAKE_SCRIPT_MODE_FILE)
        enable_language(RC)
    endif()
endif()


message(STATUS "Using CMAKE_C_COMPILER: ${CMAKE_C_COMPILER}")
message(STATUS "Using CMAKE_CXX_COMPILER: ${CMAKE_CXX_COMPILER}")
message(STATUS "Using CMAKE_RC_COMPILER: ${CMAKE_RC_COMPILER}")
message(STATUS "Using CMAKE_ASM_MASM_COMPILER: ${CMAKE_ASM_MASM_COMPILER}")
message(STATUS "Using CMAKE_RANLIB: ${CMAKE_RANLIB}")


# https://gitlab.kitware.com/cmake/cmake/-/issues/20753
# + every distro handles mingw differently, and some distros ship with broken toolchains...
execute_process(COMMAND ${CMAKE_CXX_COMPILER} -print-search-dirs
    OUTPUT_VARIABLE MINGW_SEARCH_DIRS
)

# Some distros use sysroot, some distros use install path...
execute_process(COMMAND ${CMAKE_CXX_COMPILER} -print-sysroot
    OUTPUT_VARIABLE CMAKE_FIND_ROOT_PATH
)
if(CMAKE_FIND_ROOT_PATH)
    string(REGEX MATCH "([^\r\n]*)" _ ${CMAKE_FIND_ROOT_PATH})
    set(CMAKE_FIND_ROOT_PATH ${CMAKE_MATCH_1})
endif()

if(NOT CMAKE_FIND_ROOT_PATH)
    string(REGEX MATCH "install: ([^\r\n]*)" _ ${MINGW_SEARCH_DIRS})
    set(CMAKE_FIND_ROOT_PATH ${CMAKE_MATCH_1})
endif()
message(STATUS "Using sysroot: ${CMAKE_FIND_ROOT_PATH}")


# Some distros rely on proper search path setups, some distros break print-file-name for fun...
if(NOT MINGW_DLL_DIRS)
    string(REGEX MATCH "libraries: =([^\r\n]*)" _ ${MINGW_SEARCH_DIRS})
    set(MINGW_SEARCH_DIRS ${CMAKE_MATCH_1})
    cmake_path(CONVERT "${MINGW_SEARCH_DIRS}" TO_CMAKE_PATH_LIST MINGW_DLL_DIRS)

    cmake_path(CONVERT "${CMAKE_FIND_ROOT_PATH}/mingw/bin" TO_CMAKE_PATH_LIST MINGW_SYSROOT_BIN)
    list(APPEND MINGW_DLL_DIRS ${MINGW_SYSROOT_BIN})

    set(CMAKE_C_IMPLICIT_LINK_DIRECTORIES ${MINGW_DLL_DIRS})
endif()
message(STATUS "Using MinGW DLL dirs: ${MINGW_DLL_DIRS}")
