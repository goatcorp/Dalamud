# Store initial directory
$initialDirectory = Get-Location

# CD to the repository root
Set-Location -Path "$PSScriptRoot/.."

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

# cimgui emits by-value structs as `X_c` with `typedef struct X_c X;`.
# HexaGen names them `XC` while the definitions refer to `X`.
# Rename them back to `X`.
$convStructPattern = '(?m)^typedef struct (\w+)_c \1;\r?\n'
$convStructNames = New-Object -TypeName "System.Collections.Generic.HashSet[string]"
foreach ($filePath in "lib/Hexa.NET.ImGui/Generator/cimgui/cimgui.h", "lib/Hexa.NET.ImGui/Generator/cimplot/cimplot.h", "lib/Hexa.NET.ImGui/Generator/cimguizmo/cimguizmo.h") {
    foreach ($m in [Regex]::Matches((Get-Content $filePath -Raw), $convStructPattern)) {
        $null = $convStructNames.Add($m.Groups[1].Value)
    }
}

function RemoveConvStructSuffix {
    param (
        [string]$filePath
    )

    $text = Get-Content $filePath -Raw
    $text = [Regex]::Replace($text, $convStructPattern, '')
    foreach ($name in $convStructNames) {
        $text = [Regex]::Replace($text, "\b$($name)_c\b", $name)
    }
    Set-Content -Path $filePath -Value $text -NoNewline -Encoding ascii
}

RemoveConvStructSuffix -filePath "lib/Hexa.NET.ImGui/Generator/cimgui/cimgui.h"
RemoveConvStructSuffix -filePath "lib/Hexa.NET.ImGui/Generator/cimplot/cimplot.h"
RemoveConvStructSuffix -filePath "lib/Hexa.NET.ImGui/Generator/cimguizmo/cimguizmo.h"

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
# Developer prompts set Platform=x64, which moves the output away from the bin/Debug/net9.0 run below.
Remove-Item -Path Env:Platform -ErrorAction SilentlyContinue
dotnet build

# Run generator
Read-Host -Prompt "Press any key to generate" | Out-Null
Set-Location -Path "bin/Debug/net9.0"
.\Generator.exe

Set-Location -Path "$PSScriptRoot/.."
foreach ($file in Get-ChildItem -Path "imgui/Dalamud.Bindings.ImPlot/Generated/Structs" -Filter "*.cs") {
    $text = Get-Content $file.FullName -Raw
    $fixed = $text.Replace("ImPlot.TagCollectionAppendNative(", "ImPlot.AppendNative(").Replace("ImPlot.AnnotationCollectionAppendNative(", "ImPlot.AppendNative(")
    if ($fixed -ne $text) {
        Set-Content -Path $file.FullName -Value $fixed -NoNewline -Encoding ascii
    }
}

# Restore initial directory
Set-Location -Path $initialDirectory

& "$PSScriptRoot\filter_imgui_bindings.ps1"
