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
    pch_nmd_assembly_impl.cpp
    rewrite_entrypoint.cpp
    rewrite_entrypoint_thunks.asm
    safetyhook_api.cpp
    utils.cpp
    veh.cpp
    xivfixes.cpp
    Dalamud.Boot.rc
    module.def
    themes.manifest)

# TODO v143: Use target_compile_features with cxx_std_23 instead
target_compile_options(Dalamud.Boot PRIVATE $<$<COMPILE_LANGUAGE:CXX>:/std:c++23preview>)
target_compile_definitions(Dalamud.Boot PRIVATE CPPDLLTEMPLATE_EXPORTS _USRDLL)
target_include_directories(Dalamud.Boot PRIVATE "${LIB_DIR}/mimalloc/include" "${LIB_DIR}/safetyhook")
target_link_libraries(Dalamud.Boot PRIVATE Version Shlwapi)

# TODO PCH: Must match pch.h due to stupid std::mutex abi break
set_source_files_properties("${LIB_DIR}/safetyhook/safetyhook.cpp" PROPERTIES
    COMPILE_DEFINITIONS _DISABLE_CONSTEXPR_MUTEX_CONSTRUCTOR)

add_custom_command(TARGET Dalamud.Boot POST_BUILD
    COMMAND "${CMAKE_COMMAND}" -E copy_if_different
        "${LIB_DIR}/CoreCLR/nethost/nethost.dll" "$<TARGET_FILE_DIR:Dalamud.Boot>")
