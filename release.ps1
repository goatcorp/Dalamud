param(
    [string]$VersionString
)

if (-not $VersionString) {
    Write-Error "Version string is required as the first argument."
    exit 1
}

$csprojPath = "Dalamud/Dalamud.csproj"

if (-not (Test-Path $csprojPath)) {
    Write-Error "Cannot find Dalamud.csproj at the specified path."
    exit 1
}

# Update the version in the csproj file
(Get-Content $csprojPath) -replace '<DalamudVersion>.*?</DalamudVersion>', "<DalamudVersion>$VersionString</DalamudVersion>" | Set-Content $csprojPath

# Commit the change
git add $csprojPath
git commit -m "build: $VersionString"

# Get the current branch
$currentBranch = git rev-parse --abbrev-ref HEAD

# Create a tag
git tag -a -m "v$VersionString" $VersionString

# Push atomically
git push origin $currentBranch $VersionString