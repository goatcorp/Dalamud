#!/bin/bash
# Wrapper around wine dotnet for CI tests specifically.

export WINEDEBUG=-all,+debugstr
export DOTNET_NOLOGO=true
export DOTNET_ROOT=null
export DOTNET_ROOT_X64=null

if [ ! -f "$1" ]; then
    echo "Must run as test with file"
    exit 1
fi

exec wine dotnet test --no-restore --no-build "$(winepath -w -- "$1")" "${@:2}"
