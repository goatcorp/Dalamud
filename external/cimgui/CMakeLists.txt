set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

if (CMAKE_BUILD_TYPE STREQUAL "Release")
    target_link_options(cimgui PRIVATE
        "/OPT:ICF"  # EnableCOMDATFolding = true
        "/OPT:REF") # OptimizeReferences = true
endif()

include_directories(
    ../../lib/cimgui/imgui
    ../../lib/cimgui)

add_library(cimgui SHARED
    ../../lib/cimgui/cimgui.cpp
    ../../lib/cimgui/cimgui_impl.cpp
    ../../lib/cimgui/imgui/imgui.cpp
    ../../lib/cimgui/imgui/imgui_demo.cpp
    ../../lib/cimgui/imgui/imgui_draw.cpp
    ../../lib/cimgui/imgui/imgui_tables.cpp
    ../../lib/cimgui/imgui/imgui_widgets.cpp)

target_compile_definitions(cimgui PRIVATE
    UNICODE
    _UNICODE
    CIMGUI_EXPORTS
    IMGUI_DISABLE_OBSOLETE_FUNCTIONS=1
    IMGUI_USER_CONFIG="cimgui_user.h")

target_link_options(cimgui PRIVATE
    "/MANIFESTUAC:level='requireAdministrator' uiAccess='false'") # EnableUAC = false
