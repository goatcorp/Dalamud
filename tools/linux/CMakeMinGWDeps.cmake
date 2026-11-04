# Invoked by main CMakeLists.txt on MinGW builds with all native target files.
# "-DFILES=${_collected_files}"
# "-DDEST=${CMAKE_RUNTIME_OUTPUT_DIRECTORY}"

cmake_minimum_required(VERSION 4.2)

include("${CMAKE_CURRENT_LIST_DIR}/CMakeMinGW.cmake")

file(GET_RUNTIME_DEPENDENCIES
    EXECUTABLES ${FILES}
    PRE_EXCLUDE_REGEXES "^api-ms-" "^ext-ms-"
    RESOLVED_DEPENDENCIES_VAR _resolved
    UNRESOLVED_DEPENDENCIES_VAR _unresolved
    DIRECTORIES ${MINGW_DLL_DIRS})

message(STATUS "Resolved: ${_resolved}")
file(INSTALL
    DESTINATION ${DEST}
    FOLLOW_SYMLINK_CHAIN
    FILES ${_resolved}
)

message(STATUS "Unresolved: ${_unresolved}")
