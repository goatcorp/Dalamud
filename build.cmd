@echo off
rem Builds the native and managed components of Dalamud into bin\<Config>\.
rem Usage: build.cmd [Debug|Release] (default: Debug)
setlocal
cd /d "%~dp0"

set "CONFIG=%~1"
if "%CONFIG%"=="" set "CONFIG=Debug"

call "%~dp0tools\find-cmake.cmd" || exit /b 1

if not exist build\CMakeCache.txt (
    call "%~dp0generate.cmd" || exit /b 1
)

cmake --build build --config %CONFIG% || exit /b 1
dotnet build Dalamud\Dalamud.csproj -c %CONFIG% || exit /b 1
dotnet build Dalamud.Injector\Dalamud.Injector.csproj -c %CONFIG% || exit /b 1
