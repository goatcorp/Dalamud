@echo off

where cmake >nul 2>nul && exit /b 0

set "FIND_CMAKE_VS="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" goto missing

for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -prerelease -requires Microsoft.VisualStudio.Component.VC.CMake.Project -property installationPath`) do (
    set "FIND_CMAKE_VS=%%i"
)
if "%FIND_CMAKE_VS%"=="" goto missing

set "FIND_CMAKE_BIN=%FIND_CMAKE_VS%\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin"
if not exist "%FIND_CMAKE_BIN%\cmake.exe" goto missing

set "PATH=%FIND_CMAKE_BIN%;%PATH%"
exit /b 0

:missing
echo CMake 4.0 or newer is required. Install it from https://cmake.org/download/
echo or add the "C++ CMake tools for Windows" component to Visual Studio 2026.
exit /b 1
