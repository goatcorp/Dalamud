$hashes = @{}

Get-ChildItem $args[0] -Exclude dalamud.txt,*.zip,*.pdb,*.ipdb | Foreach-Object {
    $hashes.Add($_.Name, (Get-FileHash $_.FullName -Algorithm MD5).Hash)
}

ConvertTo-Json $hashes | Out-File -FilePath (Join-Path $args[0] "hashes.json")