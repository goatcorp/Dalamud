#!/bin/sh
# Wrapper around wine dotnet for NUKE CI tests specifically.

export WINEDEBUG=-all
export DOTNET_NOLOGO=true

if [ "$1" != "test" ] || [ ! -f "$2" ]; then
    echo "Must run as test with file"
    exit 1
fi

exec wine dotnet "$1" "$(winepath -w -- "$2")" ${@:3}
