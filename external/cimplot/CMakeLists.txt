set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

if (CMAKE_BUILD_TYPE STREQUAL "Release")
    target_link_options(cimplot PRIVATE
        "/OPT:ICF"  # EnableCOMDATFolding = true
        "/OPT:REF") # OptimizeReferences = true
endif()

include_directories(
    ../../lib/cimgui/imgui
    ../../lib/cimplot/implot
    ../../lib/cimgui)

add_library(cimplot SHARED
    ../../lib/cimgui/imgui/imgui.cpp
    ../../lib/cimgui/imgui/imgui_demo.cpp
    ../../lib/cimgui/imgui/imgui_draw.cpp
    ../../lib/cimgui/imgui/imgui_tables.cpp
    ../../lib/cimgui/imgui/imgui_widgets.cpp
    ../../lib/cimplot/cimplot.cpp
    ../../lib/cimplot/cimplot_impl.cpp
    ../../lib/cimplot/implot/implot.cpp
    ../../lib/cimplot/implot/implot_demo.cpp
    ../../lib/cimplot/implot/implot_items.cpp)

target_compile_definitions(cimplot PRIVATE
    UNICODE
    _UNICODE
    CIMPLOT_EXPORTS
    IMGUI_USER_CONFIG="cimgui_user.h")

target_link_options(cimplot PRIVATE
    "/MANIFESTUAC:level='requireAdministrator' uiAccess='false'") # EnableUAC = false
