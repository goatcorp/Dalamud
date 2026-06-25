cmake_minimum_required(VERSION 3.23)

project(Dalamud C CXX ASM_MASM)

set(CMAKE_CXX_STANDARD 23)
set(CMAKE_CXX_STANDARD_REQUIRED True)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin")
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/build/lib_intermediates")

# CoreCLR
add_library(CoreCLR STATIC
    ../lib/CoreCLR/boot.cpp
    ../lib/CoreCLR/CoreCLR.cpp)

target_compile_definitions(CoreCLR PUBLIC
    UNICODE _UNICODE)

target_include_directories(CoreCLR PUBLIC
    ../lib/CoreCLR)

# minhook
add_library(minhook STATIC
    ../lib/TsudaKageyu-minhook/src/buffer.c
    ../lib/TsudaKageyu-minhook/src/HDE/hde32.c
    ../lib/TsudaKageyu-minhook/src/HDE/hde64.c
    ../lib/TsudaKageyu-minhook/src/hook.c
    ../lib/TsudaKageyu-minhook/src/trampoline.c)

target_include_directories(minhook PUBLIC
    ../lib/TsudaKageyu-minhook/include)

# imgui
add_library(imgui OBJECT
    ../lib/cimgui/imgui/imgui.cpp
    ../lib/cimgui/imgui/imgui_demo.cpp
    ../lib/cimgui/imgui/imgui_draw.cpp
    ../lib/cimgui/imgui/imgui_tables.cpp
    ../lib/cimgui/imgui/imgui_widgets.cpp)

target_compile_definitions(imgui PRIVATE
    UNICODE
    _UNICODE
    IMGUI_DISABLE_OBSOLETE_FUNCTIONS=1)

# Projects
add_subdirectory(Dalamud.Boot)
add_subdirectory(DalamudCrashHandler)
add_subdirectory(external/cimgui)
add_subdirectory(external/cimguizmo)
add_subdirectory(external/cimplot)
