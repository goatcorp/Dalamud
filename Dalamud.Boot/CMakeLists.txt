set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

if (CMAKE_BUILD_TYPE STREQUAL "Release")
    target_link_options(Dalamud.Boot PRIVATE
        "/OPT:ICF"  # EnableCOMDATFolding = true
        "/OPT:REF") # OptimizeReferences = true
endif()

add_library(Dalamud.Boot SHARED
    DalamudStartInfo.cpp
    error_info.cpp
    hooks.cpp
    ../shared/logging.cpp
    ntdll.cpp
    ../shared/unicode.cpp
    xivfixes.cpp
    utils.cpp
    pch_nmd_assembly_impl.cpp
    pch.cpp
    dllmain.cpp
    rewrite_entrypoint_thunks.asm
    rewrite_entrypoint.cpp
    veh.cpp
    Dalamud.Boot.rc
    dalamud.ico
    themes.manifest
    module.def)

target_precompile_headers(Dalamud.Boot PRIVATE
    pch.h)

target_link_libraries(Dalamud.Boot PRIVATE
    Version
    Shlwapi
    CoreCLR
    minhook)

target_link_options(Dalamud.Boot PRIVATE
    "/DEF:${CMAKE_CURRENT_SOURCE_DIR}/module.def"
    "/MANIFESTUAC:level='requireAdministrator' uiAccess='false'") # EnableUAC = false

add_custom_command(TARGET Dalamud.Boot POST_BUILD
  COMMAND "${CMAKE_COMMAND}" -E copy
     "${CMAKE_CURRENT_SOURCE_DIR}/../lib/CoreCLR/nethost/nethost.dll"
     "${CMAKE_RUNTIME_OUTPUT_DIRECTORY}/$<CONFIGURATION>/nethost.dll"
  COMMENT "Copying nethost.dll to output directory")
