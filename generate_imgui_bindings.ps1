# Store initial directory
$initialDirectory = Get-Location

# CD to the directory of this script
Set-Location -Path $PSScriptRoot

# Copy cimgui files from the cimgui repository to Hexa.NET.ImGui
Copy-Item -Path "lib/cimgui/cimgui.h" -Destination "lib/Hexa.NET.ImGui/Generator/cimgui" -Force
Copy-Item -Path "lib/cimgui/generator/output/definitions.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimgui" -Force
Copy-Item -Path "lib/cimgui/generator/output/structs_and_enums.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimgui" -Force
Copy-Item -Path "lib/cimgui/generator/output/typedefs_dict.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimgui" -Force

# Copy cimplot.h and cimguizmo.h
Copy-Item -Path "lib/cimplot/cimplot.h" -Destination "lib/Hexa.NET.ImGui/Generator/cimplot" -Force
Copy-Item -Path "lib/cimplot/generator/output/definitions.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimplot" -Force
Copy-Item -Path "lib/cimplot/generator/output/structs_and_enums.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimplot" -Force
Copy-Item -Path "lib/cimplot/generator/output/typedefs_dict.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimplot" -Force

Copy-Item -Path "lib/cimguizmo/cimguizmo.h" -Destination "lib/Hexa.NET.ImGui/Generator/cimguizmo" -Force
Copy-Item -Path "lib/cimguizmo/generator/output/definitions.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimguizmo" -Force
Copy-Item -Path "lib/cimguizmo/generator/output/structs_and_enums.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimguizmo" -Force
#Copy-Item -Path "lib/cimguizmo/generator/output/typedefs_dict.json" -Destination "lib/Hexa.NET.ImGui/Generator/cimguizmo" -Force

# Find the first `#ifdef CIMGUI_DEFINE_ENUMS_AND_STRUCTS` in cimgui.h and insert `#define CIMGUI_DEFINE_ENUMS_AND_STRUCTS` before it
function InsertDefine {
    param (
        [string]$filePath
    )

    $lines = Get-Content $filePath
    $inserted = $false

    foreach ($line in $lines) {
        if ($line -match "#ifdef CIMGUI_DEFINE_ENUMS_AND_STRUCTS") {
            $index = [Array]::IndexOf($lines, $line)
            if ($index -gt 0 -and $lines[$index - 1] -ne "#define CIMGUI_DEFINE_ENUMS_AND_STRUCTS") {
                $lines = $lines[0..($index - 1)] + "#define CIMGUI_DEFINE_ENUMS_AND_STRUCTS" + "`r`n" + $lines[$index..($lines.Length - 1)]
                $inserted = $true
            }
            break
        }
    }

    if (-not $inserted) {
        Write-Host "CIMGUI_DEFINE_ENUMS_AND_STRUCTS not found in $filePath. Exiting."
        exit 1
    }

    # Write the modified lines back to the file
    Set-Content -Path $filePath -Value $lines
}

# Insert the define line into all relevant header files
InsertDefine -filePath "lib/Hexa.NET.ImGui/Generator/cimgui/cimgui.h"
InsertDefine -filePath "lib/Hexa.NET.ImGui/Generator/cimplot/cimplot.h"
InsertDefine -filePath "lib/Hexa.NET.ImGui/Generator/cimguizmo/cimguizmo.h"

# Copy modified cimgui.h to cimplot and cimguizmo directories
Copy-Item -Path "lib/Hexa.NET.ImGui/Generator/cimgui/cimgui.h" -Destination "lib/Hexa.NET.ImGui/Generator/cimplot/cimgui.h" -Force
Copy-Item -Path "lib/Hexa.NET.ImGui/Generator/cimgui/cimgui.h" -Destination "lib/Hexa.NET.ImGui/Generator/cimguizmo/cimgui.h" -Force


Set-Location -Path "lib/Hexa.NET.ImGui"
#dotnet workload restore
#dotnet restore

# CD to generator directory
Set-Location -Path "Generator"

# Build generator
dotnet build

# Run generator
Read-Host -Prompt "Press any key to generate" | Out-Null
Set-Location -Path "bin/Debug/net9.0"
.\Generator.exe

# Restore initial directory
Set-Location -Path $initialDirectory

& "$PSScriptRoot\filter_imgui_bindings.ps1"
