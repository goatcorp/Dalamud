# Example: .\build.ps1 -Config Debug -Clean
[CmdletBinding()]
Param(
    [ValidateSet("Debug", "Release")]
    [String]$Config = "Release",

    [Switch]$Clean,

    [Parameter(Position=1, Mandatory=$false, ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# CONFIGURATION
###########################################################################

$BinDirectory = "$PSScriptRoot\\bin"
$BuildDirectory = "$PSScriptRoot\\build"

$DotNetGlobalFile = "$PSScriptRoot\\global.json"
$DotNetInstallUrl = "https://dot.net/v1/dotnet-install.ps1"
$DotNetChannel = "Current"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0

###########################################################################
# EXECUTION
###########################################################################

if ($Clean -and (Test-Path $BinDirectory)) {
    Write-Output "Cleaning up previous bin directory..."
    Remove-Item -Path $BinDirectory -Recurse -Force
}

if ($Clean -and (Test-Path $BuildDirectory)) {
    Write-Output "Cleaning up previous build directory..."
    Remove-Item -Path $BuildDirectory -Recurse -Force
}

# Ensure build directory exists
if (!(Test-Path $BuildDirectory)) {
    New-Item -ItemType Directory -Path $BuildDirectory -Force | Out-Null
}

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If dotnet CLI is installed globally and it matches requested version, use for execution
if ($null -ne (Get-Command "dotnet" -ErrorAction SilentlyContinue) -and `
     $(dotnet --version) -and $LASTEXITCODE -eq 0) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
}
else {
    # Download install script
    $DotNetInstallFile = "$BuildDirectory\dotnet-install.ps1"
    New-Item -ItemType Directory -Path $BuildDirectory -Force | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallUrl, $DotNetInstallFile)

    # If global.json exists, load expected version
    if (Test-Path $DotNetGlobalFile) {
        $DotNetGlobal = $(Get-Content $DotNetGlobalFile | Out-String | ConvertFrom-Json)
        if ($DotNetGlobal.PSObject.Properties["sdk"] -and $DotNetGlobal.sdk.PSObject.Properties["version"]) {
            $DotNetVersion = $DotNetGlobal.sdk.version
        }
    }

    # Install by channel or version
    $DotNetDirectory = "$BuildDirectory\dotnet-win"
    if (!(Test-Path variable:DotNetVersion)) {
        ExecSafe { & $DotNetInstallFile -InstallDir $DotNetDirectory -Channel $DotNetChannel -NoPath }
    } else {
        ExecSafe { & $DotNetInstallFile -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath }
    }
    $env:DOTNET_EXE = "$DotNetDirectory\dotnet.exe"
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version)"

Push-Location $BuildDirectory
try {
    cmake .. -A x64
    cmake --build . --config $Config --parallel $env:NUMBER_OF_PROCESSORS @BuildArguments
} finally {
    Pop-Location
}

ExecSafe { & $env:DOTNET_EXE build Dalamud.Injector/Dalamud.Injector.csproj -c $Config }
ExecSafe { & $env:DOTNET_EXE build Dalamud/Dalamud.csproj -c $Config }
