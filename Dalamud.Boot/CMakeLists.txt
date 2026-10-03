enable_language(ASM_MASM)

set(LIB_DIR "${PROJECT_SOURCE_DIR}/lib")
set(SHARED_DIR "${PROJECT_SOURCE_DIR}/shared")

set(THIRD_PARTY_SOURCES
    "${LIB_DIR}/mimalloc/src/alloc.c"
    "${LIB_DIR}/mimalloc/src/alloc-aligned.c"
    "${LIB_DIR}/mimalloc/src/alloc-posix.c"
    "${LIB_DIR}/mimalloc/src/arena.c"
    "${LIB_DIR}/mimalloc/src/bitmap.c"
    "${LIB_DIR}/mimalloc/src/heap.c"
    "${LIB_DIR}/mimalloc/src/init.c"
    "${LIB_DIR}/mimalloc/src/libc.c"
    "${LIB_DIR}/mimalloc/src/options.c"
    "${LIB_DIR}/mimalloc/src/os.c"
    "${LIB_DIR}/mimalloc/src/page.c"
    "${LIB_DIR}/mimalloc/src/random.c"
    "${LIB_DIR}/mimalloc/src/segment.c"
    "${LIB_DIR}/mimalloc/src/segment-map.c"
    "${LIB_DIR}/mimalloc/src/stats.c"
    "${LIB_DIR}/mimalloc/src/prim/prim.c"
    "${LIB_DIR}/libdeflate/lib/deflate_decompress.c"
    "${LIB_DIR}/libdeflate/lib/utils.c"
    "${LIB_DIR}/libdeflate/lib/x86/cpu_features.c"
    "${LIB_DIR}/CoreCLR/boot.cpp"
    "${LIB_DIR}/CoreCLR/CoreCLR.cpp"
    "${LIB_DIR}/TsudaKageyu-minhook/src/buffer.c"
    "${LIB_DIR}/TsudaKageyu-minhook/src/HDE/hde64.c"
    "${LIB_DIR}/TsudaKageyu-minhook/src/hook.c"
    "${LIB_DIR}/TsudaKageyu-minhook/src/trampoline.c"
    "${LIB_DIR}/safetyhook/Zydis.c"
    "${LIB_DIR}/safetyhook/safetyhook.cpp")

add_library(Dalamud.Boot SHARED
    ${THIRD_PARTY_SOURCES}
    "${SHARED_DIR}/hardware_info.cpp"
    "${SHARED_DIR}/logging.cpp"
    "${SHARED_DIR}/unicode.cpp"
    DalamudStartInfo.cpp
    dllmain.cpp
    error_info.cpp
    hooks.cpp
    ntdll.cpp
    nmd_assembly_impl.cpp
    rewrite_entrypoint.cpp
    rewrite_entrypoint_thunks.asm
    safetyhook_api.cpp
    utils.cpp
    veh.cpp
    xivfixes.cpp
    Dalamud.Boot.rc
    module.def
    themes.manifest)

# TODO: use same standard for all projects (here cxx_std_23)
target_compile_options(Dalamud.Boot PRIVATE $<$<COMPILE_LANGUAGE:CXX>:/std:c++23preview>)
target_compile_definitions(Dalamud.Boot PRIVATE CPPDLLTEMPLATE_EXPORTS _USRDLL)

# TODO: Get rid of this please, put it in a header, make the thirdparties static libs
target_compile_definitions(Dalamud.Boot PRIVATE WIN32_LEAN_AND_MEAN= NOMINMAX=)
target_include_directories(Dalamud.Boot PRIVATE
    "${SHARED_DIR}"
    "${LIB_DIR}/CoreCLR"
    "${LIB_DIR}/libdeflate"
    "${LIB_DIR}/mimalloc/include"
    "${LIB_DIR}/nlohmann-json"
    "${LIB_DIR}/Nomade040-nmd"
    "${LIB_DIR}/safetyhook"
    "${LIB_DIR}/srell3_009/single-header"
    "${LIB_DIR}/TsudaKageyu-minhook/include")
target_link_libraries(Dalamud.Boot PRIVATE Version Shlwapi Comctl32 Dxgi)

add_custom_command(TARGET Dalamud.Boot POST_BUILD
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different
        "${LIB_DIR}/CoreCLR/nethost/nethost.dll" "$<TARGET_FILE_DIR:Dalamud.Boot>")
