set(LIB_DIR "${PROJECT_SOURCE_DIR}/lib")
set(IMGUI_DIR "${LIB_DIR}/cimgui/imgui")

set(IMGUI_SOURCES
    "${IMGUI_DIR}/imgui.cpp"
    "${IMGUI_DIR}/imgui_demo.cpp"
    "${IMGUI_DIR}/imgui_draw.cpp"
    "${IMGUI_DIR}/imgui_tables.cpp"
    "${IMGUI_DIR}/imgui_widgets.cpp")

function(add_cimgui_module NAME)
    cmake_parse_arguments(ARG "" "" "SOURCES;INCLUDES;DEFINES" ${ARGN})

    add_library(${NAME} SHARED ${IMGUI_SOURCES} ${ARG_SOURCES})
    target_include_directories(${NAME} PRIVATE "${IMGUI_DIR}" "${LIB_DIR}/cimgui" ${ARG_INCLUDES})
    target_compile_definitions(${NAME} PRIVATE _USRDLL "IMGUI_USER_CONFIG=\"cimgui_user.h\"" ${ARG_DEFINES})
    set_target_properties(${NAME} PROPERTIES INTERPROCEDURAL_OPTIMIZATION_RELEASE ON)
endfunction()

add_cimgui_module(cimgui
    SOURCES
        "${LIB_DIR}/cimgui/cimgui.cpp"
        "${LIB_DIR}/cimgui/cimgui_impl.cpp"
    DEFINES CIMGUI_EXPORTS IMGUI_DISABLE_OBSOLETE_FUNCTIONS=1)

add_cimgui_module(cimplot
    SOURCES
        "${LIB_DIR}/cimplot/cimplot.cpp"
        "${LIB_DIR}/cimplot/cimplot_impl.cpp"
        "${LIB_DIR}/cimplot/implot/implot.cpp"
        "${LIB_DIR}/cimplot/implot/implot_demo.cpp"
        "${LIB_DIR}/cimplot/implot/implot_items.cpp"
    INCLUDES "${LIB_DIR}/cimplot/implot"
    DEFINES CIMPLOT_EXPORTS)

add_cimgui_module(cimguizmo
    SOURCES
        "${LIB_DIR}/cimguizmo/cimguizmo.cpp"
        "${LIB_DIR}/cimguizmo/cimguizmo_impl.cpp"
        "${LIB_DIR}/cimguizmo/ImGuizmo/GraphEditor.cpp"
        "${LIB_DIR}/cimguizmo/ImGuizmo/ImCurveEdit.cpp"
        "${LIB_DIR}/cimguizmo/ImGuizmo/ImGradient.cpp"
        "${LIB_DIR}/cimguizmo/ImGuizmo/ImGuizmo.cpp"
        "${LIB_DIR}/cimguizmo/ImGuizmo/ImSequencer.cpp"
    INCLUDES "${LIB_DIR}/cimguizmo/ImGuizmo"
    DEFINES CIMGUIZMO_EXPORTS)
