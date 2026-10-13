# Get the certificate and password from environment variables
$certificateBase64 = $env:CODESIGN_CERT_PFX
$certificatePassword = $env:CODESIGN_CERT_PASSWORD

# Write the certificate to a file
$certificatePath = Join-Path -Path $env:TEMP -ChildPath 'certificate.pfx'
$certificateBytes = [Convert]::FromBase64String($certificateBase64)
[System.IO.File]::WriteAllBytes($certificatePath, $certificateBytes)

# Define the function to find the path to signtool.exe
function Get-SignToolPath {
    # Array of common installation directories for Windows SDK
    $sdkInstallationDirs = @(
        "$env:ProgramFiles (x86)\Windows Kits\10\bin\x64",
        "$env:ProgramFiles\Windows Kits\10\bin\x64",
        "$env:ProgramFiles (x86)\Windows Kits\10\App Certification Kit"
    )

    foreach ($dir in $sdkInstallationDirs) {
        $path = Join-Path -Path $dir -ChildPath 'signtool.exe'
        #Write-Host $path
        if (Test-Path -Path $path) {
            return $path
        }
    }

    throw "Could not find signtool.exe. Make sure the Windows SDK is installed."
}

# Find the path to signtool.exe
$signtoolPath = Get-SignToolPath

# Define the function to code-sign a file
function Sign-File {
    param (
        [Parameter(Mandatory=$true)]
        [String]$FilePath
    )

    # Check if the file is already code-signed
    $signature = Get-AuthenticodeSignature -FilePath $FilePath -ErrorAction SilentlyContinue
    if ($signature.status -ne "NotSigned") {
        Write-Host "File '$FilePath' is already code-signed. Skipping."
        return
    }

    # Code-sign the file using signtool
    Write-Host "Code-signing file '$FilePath'..."
    & $signtoolPath sign /tr http://timestamp.digicert.com /td sha256 /v /fd sha256 /f $certificatePath /p $certificatePassword $FilePath
}

# Define the function to recursively code-sign files in a directory
function Sign-FilesRecursively {
    param (
        [Parameter(Mandatory=$true)]
        [String]$DirectoryPath
    )

    Write-Host $DirectoryPath

    # Get all exe and dll files recursively
    dir $DirectoryPath -recurse | where {$_.extension -in ".exe",".dll"} | ForEach-Object {
        Sign-File -FilePath $_.FullName
    }
}

# Usage: Provide the directory path as an argument to sign files recursively
Sign-FilesRecursively -DirectoryPath $args[0]

# Remove the temporary certificate file
Remove-Item -Path $certificatePath
