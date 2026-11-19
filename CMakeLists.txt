# Native components of Dalamud
#
#   external/             cimgui, cimplot, cimguizmo  (DLLs P/Invoked by the ImGui bindings)
#   Dalamud.Boot/         Dalamud.Boot                (DLL injected into the game)
#   DalamudCrashHandler/  DalamudCrashHandler         (EXE spawned by Dalamud.Boot to handle crashes)

cmake_minimum_required(VERSION 4.2)

if(NOT WIN32)
    include("${CMAKE_CURRENT_LIST_DIR}/tools/linux/CMakeMinGW.cmake")
endif()

project(DalamudNative LANGUAGES C CXX)

if(NOT DEFINED CMAKE_RUNTIME_OUTPUT_DIRECTORY)
    set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${PROJECT_SOURCE_DIR}/bin/$<CONFIG>")
endif()

# PDBs for every configuration
set(CMAKE_MSVC_DEBUG_INFORMATION_FORMAT ProgramDatabase)
set(CMAKE_VS_JUST_MY_CODE_DEBUGGING $<CONFIG:Debug>)

# No C++20 modules
set(CMAKE_CXX_SCAN_FOR_MODULES OFF)

if(MSVC)
    # Hybrid CRT
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    add_link_options(
        "$<$<CONFIG:Debug>:/NODEFAULTLIB:libucrtd.lib;/DEFAULTLIB:ucrtd.lib>"
        "$<$<NOT:$<CONFIG:Debug>>:/NODEFAULTLIB:libucrt.lib;/DEFAULTLIB:ucrt.lib>")
endif()

if(MINGW)
    # Target Windows 10
    add_compile_definitions(_WIN32_WINNT=0x0A00 WINVER=0x0A00)

    # No lib*.dll, just *.dll
    set(CMAKE_STATIC_LIBRARY_PREFIX "")
    set(CMAKE_SHARED_LIBRARY_PREFIX "")
    set(CMAKE_IMPORT_LIBRARY_PREFIX "")
endif()

add_compile_definitions(UNICODE _UNICODE _WINDOWS)

# Shared flags
set(DALAMUD_C_CXX "$<COMPILE_LANGUAGE:C,CXX>")

if(MSVC)
    add_compile_options(
        "$<${DALAMUD_C_CXX}:/W3;/sdl;/permissive-;/Zc:inline;/FC;/Gy;/MP>"
        "$<$<AND:${DALAMUD_C_CXX},$<CONFIG:Release>>:/Oi>")

    add_link_options(
        /DEBUG
        /INCREMENTAL:NO
        "$<$<CONFIG:Release>:/OPT:REF;/OPT:ICF>"
        "$<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/MANIFESTUAC:NO>")

else()
    # FIXME JADE: MinGW link options.
endif()

# Write empty props file to prevent the managed one from leaking into the native projects.
# Kind of a hack, maybe the managed projects should be in another directory
file(WRITE "${PROJECT_BINARY_DIR}/Directory.Build.props" "<Project />\n")

add_subdirectory(external)
add_subdirectory(Dalamud.Boot)
add_subdirectory(DalamudCrashHandler)

if(MINGW)
    # Copy MinGW runtime required by targets and subdir targets into bin/<Config>

    function(dalamud_collect_targets dir collected)
        get_property(_targets DIRECTORY "${dir}" PROPERTY BUILDSYSTEM_TARGETS)
        get_property(_subdirs DIRECTORY "${dir}" PROPERTY SUBDIRECTORIES)

        foreach(_subdir IN LISTS _subdirs)
            dalamud_collect_targets("${_subdir}" _collected_sub)
            list(APPEND _targets ${_collected_sub})
        endforeach()

        set(${collected} ${_targets} PARENT_SCOPE)
    endfunction()

    dalamud_collect_targets("${PROJECT_SOURCE_DIR}" _collected_targets)

    set(_collected_files)
    foreach(_target IN LISTS _collected_targets)
        get_target_property(_target_type ${_target} TYPE)
        if(_target_type MATCHES "^(SHARED_LIBRARY|MODULE_LIBRARY|EXECUTABLE)$")
            list(APPEND _collected_files "$<TARGET_FILE:${_target}>")
        endif()
    endforeach()

    set(_stamp "${PROJECT_BINARY_DIR}/$<CONFIG>/mingw-runtime-deps.stamp")
    add_custom_command(OUTPUT "${_stamp}"
        COMMAND ${CMAKE_COMMAND}
            "-DFILES=${_collected_files}"
            "-DDEST=${CMAKE_RUNTIME_OUTPUT_DIRECTORY}"
            -P "${PROJECT_SOURCE_DIR}/tools/linux/CMakeMinGWDeps.cmake"
        COMMAND ${CMAKE_COMMAND} -E touch "${_stamp}"
        DEPENDS ${_collected_files}
        VERBATIM)
    add_custom_target(mingw_runtime_deps ALL DEPENDS "${_stamp}")
endif()
