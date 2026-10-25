@echo off
rem Generates the native Visual Studio projects into build\.
rem Usage: generate.cmd [preset] (default: vs2026)
setlocal
cd /d "%~dp0"

call "%~dp0tools\find-cmake.cmd" || exit /b 1

set "PRESET=%~1"
if "%PRESET%"=="" set "PRESET=vs2026"

cmake --preset %PRESET%
