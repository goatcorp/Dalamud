#!/bin/bash
echo 'Builds the native and managed components of Dalamud into bin/<Config>/.'
echo 'Usage: build.sh [Debug|Release] (default: Debug)'
set -euo pipefail
cd "$(dirname "$0")"

# Just in case those bits get lost and someone decides to chmod a+x this file,
# but none of the other files. Been there, done that.
# -jade
chmod a+x tools/linux/build-jwasm.sh
chmod a+x generate.sh

CONFIG='Debug'
if [ $# -ge 1 ] && [ -n "$1" ]; then
    CONFIG="$1"
fi

if ! [ -f 'tools/linux/JWasm/build/jwasm' ]; then
    ./tools/linux/build-jwasm.sh
fi

if ! [ -f 'build/CMakeCache.txt' ]; then
    ./generate.sh
fi

cmake --build build --config $CONFIG
dotnet build Dalamud/Dalamud.csproj -c $CONFIG
dotnet build Dalamud.Injector/Dalamud.Injector.csproj -c $CONFIG
