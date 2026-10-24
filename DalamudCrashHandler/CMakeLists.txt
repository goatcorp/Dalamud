set(SHARED_DIR "${PROJECT_SOURCE_DIR}/shared")

add_executable(DalamudCrashHandler
    DalamudCrashHandler.cpp
    miniz.c
    "${SHARED_DIR}/hardware_info.cpp"
    "${SHARED_DIR}/logging.cpp"
    "${SHARED_DIR}/unicode.cpp"
    DalamudCrashHandler.rc)

if(MSVC)
target_sources(DalamudCrashHandler PRIVATE
    "createprocess.manifest")

set_source_files_properties(
    "createprocess.manifest"
    PROPERTIES VS_TOOL_OVERRIDE "Manifest")
endif()

target_compile_features(DalamudCrashHandler PRIVATE c_std_17)

if(MSVC)
    set_target_properties(DalamudCrashHandler PROPERTIES
        VS_GLOBAL_GenerateManifest "false"
        VS_GLOBAL_EmbedManifest "false")

    # TODO: use same standard for all projects (here cxx_std_23)
    target_compile_options(DalamudCrashHandler PRIVATE $<$<COMPILE_LANGUAGE:CXX>:/std:c++latest>)
else()
    target_compile_features(DalamudCrashHandler PRIVATE cxx_std_23)
endif()

target_compile_definitions(DalamudCrashHandler PRIVATE _CRT_SECURE_NO_WARNINGS _CONSOLE)
target_include_directories(DalamudCrashHandler PRIVATE "${SHARED_DIR}")
target_link_libraries(DalamudCrashHandler PRIVATE version winhttp dbghelp comctl32 dxgi)
