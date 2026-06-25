include_directories(
    ../../lib/cimgui/imgui
    ../../lib/cimgui)

add_library(cimgui SHARED
    ../../lib/cimgui/cimgui.cpp
    ../../lib/cimgui/cimgui_impl.cpp
    $<TARGET_OBJECTS:imgui>)

target_compile_definitions(cimgui PRIVATE
    UNICODE
    _UNICODE
    CIMGUI_EXPORTS
    IMGUI_DISABLE_OBSOLETE_FUNCTIONS=1
    IMGUI_USER_CONFIG="cimgui_user.h")

target_link_options(cimgui PRIVATE
    "/MANIFESTUAC:level='requireAdministrator' uiAccess='false'") # EnableUAC = false
