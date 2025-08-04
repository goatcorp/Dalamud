$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$namespaceDefPattern = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList '(?:^\s*)namespace\s+(?<namespace>[\w.]+)\b', 'Compiled,Multiline,Singleline'
$usingPattern = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList '(?:^|;)\s*using\s+(?<using>\w+)\s*;', 'Compiled,Multiline,Singleline'
$classDefPattern = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList '(?<indent>^\s*)(?<visibility>public\s+|internal\s+|protected\s+|private\s+)?(?<static>static\s+)?(?<unsafe>unsafe\s+)?(?<partial>partial\s+)?(?<type>class\s+|struct\s+)(?<name>\w+)\b', 'Compiled,Multiline,Singleline'
$methodPattern = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList '(?:^\s+?\[.*?\](?:\r\n|\r|\n))?(?<indent>^\s*)(?<prototype>(?<visibility>public\s+|internal\s+|protected\s+|private\s+)?(?<static>static\s+)?(?<unsafe>unsafe\s+)?(?<return>(?!public|internal|protected|private|static|unsafe)\w+(?:\s*<\s*\w+?(?:<\s*\w+\s*>?)?\s*>)?(?:\s*\*+)?\s+)(?<name>\w+)(?<args>\s*\([^)]*\)))(?:\r\n|\r|\n)[\s\S]+?(?:^\k<indent>}(?:\r\n|\r|\n))', 'Compiled,Multiline,Singleline'
$referNativeFunction = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList '(?<!\.\s*)\b(\w+)Native(?=\()', 'Compiled'
$referNativeFunctionQualified = New-Object -TypeName System.Text.RegularExpressions.Regex -ArgumentList '\b(\w+)\s*\.\s*(\w+)Native(?=\()', 'Compiled'

$sourcePaths = (
    "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Generated\Functions",
    "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Generated\Structs",
    "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Internals\Functions",
    "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Manual\Functions",
    # "$PSScriptRoot\imgui\Dalamud.Bindings.ImPlot\Generated\Functions",
    # "$PSScriptRoot\imgui\Dalamud.Bindings.ImPlot\Generated\Structs",
    $null
)

# replace "ImGuiKey.GamepadStart"
$tmp = Get-Content -Path "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Generated\Enums\ImGuiKeyPrivate.cs" -Raw
$tmp = $tmp.Replace("unchecked((int)GamepadStart)", "unchecked((int)ImGuiKey.GamepadStart)").Trim()
$tmp | Set-Content -Path "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Generated\Enums\ImGuiKeyPrivate.cs" -Encoding ascii

try
{
    Remove-Item -Path "$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\Generated\Handles\ImTextureID.cs" -Force
}
catch [System.Management.Automation.ItemNotFoundException]
{
    # pass
}

foreach ($sourcePath in $sourcePaths)
{
    if (!$sourcePath)
    {
        continue
    }

    $targetPath = "$( Split-Path $( Split-Path $sourcePath ) )/Custom/Generated/$( Split-Path $( Split-Path $sourcePath ) -Leaf )/$( Split-Path $sourcePath -Leaf )"
    $null = New-Item -Path $targetPath -Type Container -Force

    $namespace = $null
    $classes = New-Object -TypeName "System.Collections.Generic.Dictionary[string, System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[System.Text.RegularExpressions.Match]]]"
    $imports = New-Object -TypeName "System.Collections.Generic.SortedSet[string]"
    $null = $imports.Add("System.Diagnostics")
    $null = $imports.Add("System.Runtime.CompilerServices")
    $null = $imports.Add("System.Runtime.InteropServices")
    $null = $imports.Add("System.Numerics")
    $null = $imports.Add("HexaGen.Runtime")

    if (!$sourcePath.StartsWith("$PSScriptRoot\imgui\Dalamud.Bindings.ImGui\"))
    {
        $null = $imports.Add("Dalamud.Bindings.ImGui")
    }

    $husks = New-Object -TypeName "System.Text.StringBuilder"
    foreach ($file in (Get-ChildItem -Path $sourcePath))
    {
        $fileData = Get-Content -Path $file.FullName -Raw
        $fileData = [Regex]::Replace($fileData, '#else\s*$[\s\S]*?^\s*#endif\s*$', '#endif', 'Multiline')
        $fileData = [Regex]::Replace($fileData, '^\s*(#if(?:def)?\s+.*|#endif\s*)$', '', 'Multiline')
        $namespace = $namespaceDefPattern.Match($fileData).Groups["namespace"].Value
        $classDefMatches = $classDefPattern.Matches($fileData)
        foreach ($using in $usingPattern.Matches($fileData))
        {
            $null = $imports.Add($using.Groups["using"])
        }

        $null = $husks.Append("/* $( $file.Name ) */`r`n").Append($methodPattern.Replace($fileData, ""))

        foreach ($i in (0..($classDefMatches.Count - 1)))
        {
            $classGroup = $classDefMatches[$i].Groups
            $className = "$($classGroup["type"].Value.Trim() ) $($classGroup["name"].Value.Trim() )"
            if ( $className.EndsWith("Union"))
            {
                $className = $className.Substring(0, $className.Length - 5)
            }
            $methods = $nativeMethods = $null

            $methodMatches = $methodPattern.Matches($(
            if (!$classes.TryGetValue($className, [ref]$methods))
            {
                $methods = New-Object -TypeName "System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[System.Text.RegularExpressions.Match]]"
                $classes.Add($className, $methods)
            }
            If ($i -eq ($classDefMatches.Count - 1))
            {
                $fileData.Substring($classDefMatches[$i].Index)
            }
            Else
            {
                $fileData.Substring($classDefMatches[$i].Index, $classDefMatches[$i + 1].Index - $classDefMatches[$i].Index)
            }
            ))

            foreach ($methodMatch in $methodMatches)
            {
                $methodName = $methodMatch.Groups["name"].Value
                if ( $methodMatch.Groups["args"].Value.Contains("stbtt_pack_context"))
                {
                    continue
                }
                $overload = $null
                $methodContainer = $( If ( $methodName.EndsWith("Native"))
                {
                    if ($null -eq $nativeMethods -and !$classes.TryGetValue("$( $className )Native", [ref]$nativeMethods))
                    {
                        $nativeMethods = New-Object -TypeName "System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[System.Text.RegularExpressions.Match]]"
                        $classes.Add("$( $className )Native", $nativeMethods)
                    }
                    $nativeMethods
                }
                else
                {
                    $methods
                } )
                if (!$methodContainer.TryGetValue($methodName, [ref]$overload))
                {
                    $overload = New-Object -TypeName "System.Collections.Generic.List[System.Text.RegularExpressions.Match]"
                    $methodContainer.Add($methodName, $overload)
                }

                $overload.Add($methodMatch)
            }
        }
    }

    $husks = $husks.ToString()
    $husks = [Regex]::Replace($husks, '^\s*//.*\r\n', '', 'Multiline')
    $husks = [Regex]::Replace($husks, '^\s*using .*;\r\n', '', 'Multiline')
    # $husks = $emptyClassPattern.Replace($husks, '')
    # $husks = $emptyNamespacePattern.Replace($husks, '')
    $husks = [Regex]::Replace($husks, '\s*\r\n', "`r`n", 'Multiline').Trim()
    if ($husks -ne '')
    {
        $husks = [Regex]::Replace($husks, '\}\s*' + [Regex]::Escape("namespace $namespace") + '\s*\{', "", 'Multiline').Trim()
        $husks = $husks.Replace("public unsafe struct", "public unsafe partial struct")
        $husks = $referNativeFunctionQualified.Replace($husks, '$1Native.$2')
        $husks = "// <auto-generated/>`r`n`r`nusing $([string]::Join(";`r`nusing ", $imports) );`r`n`r`n$husks"
        $husks | Set-Content -Path "$targetPath.gen.cs" -Encoding ascii
    }

    $husks = "// <auto-generated/>`r`n`r`nusing $([string]::Join(";`r`nusing ", $imports) );`r`n`r`nnamespace $namespace;`r`n`r`n"

    $sb = New-Object -TypeName "System.Text.StringBuilder"
    $discardMethods = New-Object -TypeName "System.Collections.Generic.SortedSet[string]"
    $null = $discardMethods.Add("ImFontAtlasBuildPackCustomRectsNative")
    foreach ($classDef in $classes.Keys)
    {
        $null = $sb.Clear().Append($husks).Append("public unsafe partial $classDef`r`n{`r`n")
        $className = $classDef.Split(" ")[1]
        $isNative = $className.EndsWith("Native")
        $discardMethods.Clear()

        if (!$isNative)
        {
            foreach ($methods in $classes[$classDef].Values)
            {
                $methodName = $methods[0].Groups["name"].Value;

                # discard Drag/Slider functions
                if ($methodName.StartsWith("Drag") -or
                    $methodName.StartsWith("Slider") -or
                    $methodName.StartsWith("VSlider") -or
                    $methodName.StartsWith("InputFloat") -or
                    $methodName.StartsWith("InputInt") -or
                    $methodName.StartsWith("InputDouble") -or
                    $methodName.StartsWith("InputScalar") -or
                    $methodName.StartsWith("ColorEdit") -or
                    $methodName.StartsWith("ColorPicker"))
                {
                    $null = $discardMethods.Add($methodName)
                    continue
                }

                # discard specific functions
                if ($methodName.StartsWith("ImGuiTextRange") -or
                    $methodName.StartsWith("AddInputCharacter"))
                {
                    $null = $discardMethods.Add($methodName)
                    continue
                }

                # discard Get...Ref functions
                if ($methodName.StartsWith("Get") -And
                    $methodName.EndsWith("Ref"))
                {
                    $null = $discardMethods.Add($methodName)
                    continue
                }

                foreach ($overload in $methods)
                {
                    $returnType = $overload.Groups["return"].Value.Trim()
                    $argDef = $overload.Groups["args"].Value

                    # discard functions returning a string of some sort
                    if ($returnType -eq "string" -and
                        $methodName.EndsWith("S"))
                    {
                        $null = $discardMethods.Add($methodName.Substring(0, $methodName.Length - 1))
                        $null = $discardMethods.Add($methodName)
                        break
                    }

                    # discard formatting functions or functions accepting (begin, end) or (data, size) pairs
                    if ($argDef.Contains("fmt") -or
                        $argDef -match "\btext\b" -or
                        # $argDef.Contains("byte* textEnd") -or
                        $argDef.Contains("str") -or
                        # $argDef.Contains("byte* strEnd") -or
                        # $argDef.Contains("byte* strIdEnd") -or
                        $argDef.Contains("label") -or
                        $argDef.Contains("name") -or
                        $argDef.Contains("prefix") -or
                        $argDef.Contains("byte* shortcut") -or
                        $argDef.Contains("byte* type") -or
                        $argDef.Contains("byte* iniData") -or
                        $argDef.Contains("int dataSize") -or
                        $argDef.Contains("values, int valuesCount") -or
                        $argDef.Contains("data, int itemCount") -or
                        $argDef.Contains("pData, int components") -or
                        $argDef.Contains("ushort* glyphRanges") -or
                        $argDef.Contains("nuint args"))
                    {
                        $null = $discardMethods.Add($methodName)
                        break
                    }
                }
            }
        }

        foreach ($methods in $classes[$classDef].Values)
        {
            $methodName = $methods[0].Groups["name"];

            if ( $discardMethods.Contains($methodName))
            {
                continue
            }

            foreach ($overload in $methods)
            {
                if ($isNative)
                {
                    $null = $sb.Append($overload.Groups[0].Value.Replace("internal ", "public ").Replace("Native(", "(").Replace("funcTable", "$($className.Substring(0, $className.Length - 6) ).funcTable"))
                    continue
                }

                $tmp = $overload.Groups[0].Value
                $tmp = $referNativeFunction.Replace($tmp, "$( $className )Native.`$1")
                $tmp = $referNativeFunctionQualified.Replace($tmp, '$1Native.$2')
                $null = $sb.Append($tmp)
            }
        }

        $null = $sb.Append("}`r`n")

        $nativeMethods = $null
        if (!$classes.TryGetValue($classDef + "Native", [ref]$nativeMethods))
        {
            $nativeMethods = $null
        }

        foreach ($methodName in $discardMethods)
        {
            if ($nativeMethods -ne $null)
            {
                $overloads = $null
                if ($nativeMethods.TryGetValue($methodName + "Native", [ref]$overloads))
                {
                    foreach ($overload in $overloads)
                    {
                        $null = $sb.Append("// DISCARDED: $( $overload.Groups["prototype"].Value )`r`n")
                    }
                    continue
                }
            }

            $null = $sb.Append("// DISCARDED: $methodName`r`n")
        }

        $sb.ToString() | Set-Content -Path "$targetPath/$className.gen.cs" -Encoding ascii
    }
}
