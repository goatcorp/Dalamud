#!/bin/bash
echo 'Generates the native Visual Studio projects into build/.'
echo 'Usage: generate.sh [mingw-make-debug|mingw-make-release|mingw-ninja] default: mingw-ninja'
set -euo pipefail
cd "$(dirname "$0")"

PRESET='mingw-ninja'
if [ $# -ge 1 ] && [ -n "$1" ]; then
    PRESET="$1"
fi

cmake --preset "$PRESET"
