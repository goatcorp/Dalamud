#!/bin/bash

export WINEDEBUG=-all
export DOTNET_NOLOGO=true
export DOTNET_ROOT="$(xlcore wine winepath -w ~/.xlcore/runtime/)"
export DALAMUD_RUNTIME="$DOTNET_ROOT"

get-ffxiv-wpid() {
    xlcore wine tasklist | awk '/ffxiv_dx11.exe/ {print $2}'
}

get-netcoredbg() {
    xlcore wine winepath -w linuxtools/netcoredbg/netcoredbg.exe
}

get-vsdbg() {
    xlcore wine winepath -w linuxtools/vsdbg/vsdbg.exe
}
