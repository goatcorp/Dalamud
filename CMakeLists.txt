# Native components of Dalamud
#
#   external/             cimgui, cimplot, cimguizmo  (DLLs P/Invoked by the ImGui bindings)
#   Dalamud.Boot/         Dalamud.Boot                (DLL injected into the game)
#   DalamudCrashHandler/  DalamudCrashHandler         (EXE spawned by Dalamud.Boot to handle crashes)

cmake_minimum_required(VERSION 4.0)
project(DalamudNative LANGUAGES C CXX)

if(NOT MSVC)
    message(FATAL_ERROR "Only MSVC is supported for now.")
endif()

set(DALAMUD_BIN_DIR "${PROJECT_SOURCE_DIR}/bin/$<CONFIG>")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${DALAMUD_BIN_DIR}")
set(CMAKE_PDB_OUTPUT_DIRECTORY "${DALAMUD_BIN_DIR}")

# PDBs for every configuration
set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT ProgramDatabase)
set(CMAKE_VS_JUST_MY_CODE_DEBUGGING $<CONFIG:Debug>)

# No C++20 modules
set(CMAKE_CXX_SCAN_FOR_MODULES OFF)

add_compile_definitions(UNICODE _UNICODE _WINDOWS)

# Shared flags
set(DALAMUD_C_CXX "$<COMPILE_LANGUAGE:C,CXX>")
add_compile_options(
    "$<${DALAMUD_C_CXX}:/W3;/sdl;/permissive-;/Zc:inline;/FC;/Gy;/MP>"
    "$<$<AND:${DALAMUD_C_CXX},$<CONFIG:Release>>:/Oi>")

add_link_options(
    /DEBUG
    /INCREMENTAL:NO
    "$<$<CONFIG:Release>:/OPT:REF;/OPT:ICF>"
    "$<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/MANIFESTUAC:NO>")

# Write empty props file to prevent the managed one from leaking into the native projects.
# Kind of a hack, maybe the managed projects should be in another directory
file(WRITE "${PROJECT_BINARY_DIR}/Directory.Build.props" "<Project />\n")

add_subdirectory(external)
add_subdirectory(Dalamud.Boot)
add_subdirectory(DalamudCrashHandler)
