include_directories(
    ../../lib/cimgui/imgui
    ../../lib/cimplot/implot
    ../../lib/cimgui)

add_library(cimplot SHARED
    ../../lib/cimplot/cimplot.cpp
    ../../lib/cimplot/cimplot_impl.cpp
    ../../lib/cimplot/implot/implot.cpp
    ../../lib/cimplot/implot/implot_demo.cpp
    ../../lib/cimplot/implot/implot_items.cpp
    $<TARGET_OBJECTS:imgui>)

target_compile_definitions(cimplot PRIVATE
    UNICODE
    _UNICODE
    CIMPLOT_EXPORTS
    IMGUI_USER_CONFIG="cimgui_user.h")

target_link_options(cimplot PRIVATE
    "/MANIFESTUAC:level='requireAdministrator' uiAccess='false'") # EnableUAC = false
