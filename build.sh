#!/usr/bin/env bash

bash --version 2>&1 | head -n 1

set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

###########################################################################
# CONFIGURATION
###########################################################################

BIN_DIRECTORY="$SCRIPT_DIR/bin"
BUILD_DIRECTORY="$SCRIPT_DIR/build"

DOTNET_GLOBAL_FILE="$SCRIPT_DIR/global.json"
DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
DOTNET_CHANNEL="Current"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_MULTILEVEL_LOOKUP=0

###########################################################################
# PARSE ARGUMENTS
###########################################################################

CLEAN=0
CONFIG="Release"

while [[ "$#" -gt 0 ]]; do
    case $1 in
        -c|--config) CONFIG="$2"; shift ;;
        -C|--clean) CLEAN=1 ;;
        *)
            # Break out of flag parsing to collect remaining build arguments
            # If you want to pass extra flags to CMake/dotnet, they will be captured here.
            break
            ;;
    esac
    shift
done

# Store any trailing arguments that should be passed down to build tools
REMAINING_ARGS=("$@")

###########################################################################
# EXECUTION
###########################################################################

if [ "$CLEAN" -eq 1 ] && [ -d "$BIN_DIRECTORY" ]; then
    echo "Cleaning up previous bin directory..."
    rm -r "$BIN_DIRECTORY"
fi

if [ "$CLEAN" -eq 1 ] && [ -d "$BUILD_DIRECTORY" ]; then
    echo "Cleaning up previous build directory..."
    rm -r "$BUILD_DIRECTORY"
fi

mkdir -p "$BUILD_DIRECTORY"

function FirstJsonValue {
    perl -nle 'print $1 if m{"'"$1"'": "([^"]+)",?}' <<< "${@:2}"
}

# If dotnet CLI is installed globally and it matches requested version, use for execution
if [ -x "$(command -v dotnet)" ] && dotnet --version &>/dev/null; then
    export DOTNET_EXE="$(command -v dotnet)"
else
    # Download install script
    DOTNET_INSTALL_FILE="$BUILD_DIRECTORY/dotnet-install.sh"
    curl -Lsfo "$DOTNET_INSTALL_FILE" "$DOTNET_INSTALL_URL"
    chmod +x "$DOTNET_INSTALL_FILE"

    # If global.json exists, load expected version
    if [[ -f "$DOTNET_GLOBAL_FILE" ]]; then
        DOTNET_VERSION=$(FirstJsonValue "version" "$(cat "$DOTNET_GLOBAL_FILE")")
        if [[ "$DOTNET_VERSION" == ""  ]]; then
            unset DOTNET_VERSION
        fi
    fi

    # Install by channel or version
    DOTNET_DIRECTORY="$BUILD_DIRECTORY/dotnet-unix"
    if [[ -z ${DOTNET_VERSION+x} ]]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --no-path
    else
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    fi
    export DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"
fi

echo "Microsoft (R) .NET Core SDK version $("$DOTNET_EXE" --version)"

cd "$BUILD_DIRECTORY"
cmake .. -A x64
cmake --build . --config "$CONFIG" --parallel $(nproc) "${REMAINING_ARGS[@]}"
cd "$SCRIPT_DIR"

"$DOTNET_EXE" build Dalamud.Injector/Dalamud.Injector.csproj -c "$CONFIG"
"$DOTNET_EXE" build Dalamud/Dalamud.csproj -c "$CONFIG"
