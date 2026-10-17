#!/bin/bash
echo 'Builds JWasm, which is used as a replacement for MASM.'
echo 'Usage: build-jwasm.sh'
set -euo pipefail
cd "$(dirname "$0")"

# Ubuntu 22.04 MinGW 13 warns about some possible overflows.
export CFLAGS="-Wno-format-overflow -Wno-format ${CFLAGS:-}"

# Fails to build with C++23, or out of the box with CMake 4.
# The min ver is also ancient.
cmake -Slib/JWasm -Blib/JWasm/build \
    -DCMAKE_C_STANDARD=17 -DCMAKE_CXX_STANDARD=17 \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 \
    -Wno-deprecated

cmake --build lib/JWasm/build
