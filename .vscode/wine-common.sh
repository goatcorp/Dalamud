#!/bin/bash

filter-stderr() {
    sed '/^[ef]sync: up and running/d'
}

xlcore-wine() {
    xlcore wine $@ 2> >(filter-stderr >&2)
}

dotnetsdk-wine() {
    xlcore wine $@ 2> >(filter-stderr >&2)
}

export WINEDEBUG=-all,+debugstr
export DOTNET_NOLOGO=true
export DOTNET_ROOT="$(xlcore-wine winepath -w ~/.xlcore/runtime/)"
export DALAMUD_RUNTIME="$DOTNET_ROOT"

get-wpid() {
    xlcore-wine tasklist | awk "/$1/ "'{print $2}'
}

get-netcoredbg() {
    xlcore-wine winepath -w tools/linux/netcoredbg/netcoredbg.exe
}

get-vsdbg() {
    xlcore-wine winepath -w tools/linux/vsdbg/vsdbg.exe
}

if [ -f tools/linux/ffxivrc.sh ]; then
    . tools/linux/ffxivrc.sh
fi
