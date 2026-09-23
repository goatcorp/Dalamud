@echo off
rem Generates the native Visual Studio projects into build\.
rem Usage: generate.cmd [vs2026|vs2022] default: newest installed Visual Studio)
setlocal
cd /d "%~dp0"

call "%~dp0tools\find-cmake.cmd" || exit /b 1

set "PRESET=%~1"
if not "%PRESET%"=="" goto configure

rem TODO v143
set "PRESET=vs2022"
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
for /f "usebackq delims=" %%v in (`"%VSWHERE%" -latest -prerelease -property catalog_productLineVersion`) do (
    if "%%v"=="18" set "PRESET=vs2026"
)

:configure
cmake --preset %PRESET%
