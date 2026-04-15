#!/bin/bash
# Helper script to deal with VSCode trying to shove a sh script through the pipe.

. .vscode/wine-common.sh

if [ "$1" == "sh -s" ]; then
    # Assume VSCode is trying to run remoteProcessPickerScript, which expects
    # uname
    # ps -axww -o pid=,flags=,comm=aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa,args=
    pid=$(get-ffxiv-wpid)
    if [ -z "$pid" ]; then
        exit 1
    fi

    echo "Linux"
    echo "${pid//?/ }   aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa "
    echo "${pid} 0 ffxiv_dx11.exe                                    ffxiv_dx11.exe"
    exit 0
fi

exec xlcore wine "$(get-vsdbg)" --interpreter=vscode
