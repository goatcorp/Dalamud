add_library(DalamudCrashHandler SHARED
    DalamudCrashHandler.cpp
    ../shared/logging.cpp
    ../shared/unicode.cpp
    miniz.c
    DalamudCrashHandler.rc
    dalamud.ico)

target_compile_definitions(DalamudCrashHandler PRIVATE
    UNICODE
    _UNICODE)

target_link_libraries(DalamudCrashHandler PRIVATE
    Version
    Winhttp
    Dbghelp)
