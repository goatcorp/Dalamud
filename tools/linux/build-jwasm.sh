#!/bin/bash
echo 'Builds JWasm, which is used as a replacement for MASM.'
echo 'Usage: build-jwasm.sh'
set -euo pipefail
cd "$(dirname "$0")"

# https://github.com/goatcorp/Dalamud/pull/3024/changes#paneldiscussion_r4319228855
git clone \
    --depth 1 \
    --revision a5c4ea03cc0545a15d81a354251b5f534bef7a1b \
    https://github.com/JWasm/JWasm.git

# Fails to build with C++23, or out of the box with CMake 4.
# The min ver is also ancient.
cmake -SJWasm -BJWasm/build \
    -DCMAKE_C_STANDARD=17 -DCMAKE_CXX_STANDARD=17 \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 \
    -Wno-deprecated

cmake --build JWasm/build
