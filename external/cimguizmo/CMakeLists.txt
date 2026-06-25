set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

if (CMAKE_BUILD_TYPE STREQUAL "Release")
    target_link_options(cimguizmo PRIVATE
        "/OPT:ICF"  # EnableCOMDATFolding = true
        "/OPT:REF") # OptimizeReferences = true
endif()

include_directories(
    ../../lib/cimgui/imgui
    ../../lib/cimguizmo/ImGuizmo
    ../../lib/cimgui)

add_library(cimguizmo SHARED
    ../../lib/cimguizmo/cimguizmo.cpp
    ../../lib/cimguizmo/cimguizmo_impl.cpp
    ../../lib/cimguizmo/ImGuizmo/GraphEditor.cpp
    ../../lib/cimguizmo/ImGuizmo/ImCurveEdit.cpp
    ../../lib/cimguizmo/ImGuizmo/ImGradient.cpp
    ../../lib/cimguizmo/ImGuizmo/ImGuizmo.cpp
    ../../lib/cimguizmo/ImGuizmo/ImSequencer.cpp
    ../../lib/cimgui/imgui/imgui.cpp
    ../../lib/cimgui/imgui/imgui_demo.cpp
    ../../lib/cimgui/imgui/imgui_draw.cpp
    ../../lib/cimgui/imgui/imgui_tables.cpp
    ../../lib/cimgui/imgui/imgui_widgets.cpp)

target_compile_definitions(cimguizmo PRIVATE
    UNICODE
    _UNICODE
    CIMGUIZMO_EXPORTS
    IMGUI_USER_CONFIG="cimgui_user.h")

target_link_options(cimguizmo PRIVATE
    "/MANIFESTUAC:level='requireAdministrator' uiAccess='false'") # EnableUAC = false
