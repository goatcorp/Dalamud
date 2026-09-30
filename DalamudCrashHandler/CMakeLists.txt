set(SHARED_DIR "${PROJECT_SOURCE_DIR}/shared")

add_executable(DalamudCrashHandler
    DalamudCrashHandler.cpp
    miniz.c
    "${SHARED_DIR}/hardware_info.cpp"
    "${SHARED_DIR}/logging.cpp"
    "${SHARED_DIR}/unicode.cpp"
    DalamudCrashHandler.rc)

target_compile_features(DalamudCrashHandler PRIVATE c_std_17)

# TODO v143: CMake doesn't know cxx_std_26 for old msvc
target_compile_options(DalamudCrashHandler PRIVATE $<$<COMPILE_LANGUAGE:CXX>:/std:c++latest>)
target_compile_definitions(DalamudCrashHandler PRIVATE _CRT_SECURE_NO_WARNINGS _CONSOLE)
target_link_libraries(DalamudCrashHandler PRIVATE Version Winhttp Dbghelp)
