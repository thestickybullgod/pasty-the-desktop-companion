#!/usr/bin/env pwsh
# Builds release packages for every supported platform into ./dist.
#   pwsh packaging/publish.ps1                  # all platforms
#   pwsh packaging/publish.ps1 -Runtimes linux-x64
param(
    [string[]] $Runtimes = @('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64')
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Pasty.Desktop/Pasty.Desktop.csproj'
$dist = Join-Path $root 'dist'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1

Write-Host "Running tests..." -ForegroundColor Cyan
dotnet test (Join-Path $root 'tests/Pasty.Core.Tests') --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Tests failed; not packaging.' }

New-Item -ItemType Directory -Force $dist | Out-Null

foreach ($rid in $Runtimes) {
    Write-Host "Publishing Pasty $version for $rid..." -ForegroundColor Cyan
    $out = Join-Path $dist "staging/$rid"
    Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
    # Self-contained single file: nothing to install, no .NET runtime needed.
    dotnet publish $project -c Release -r $rid -o $out --nologo -v quiet --self-contained `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $rid" }
    # Debug symbols (including native ones from NuGet packages) don't belong in a release download.
    Get-ChildItem $out -Filter *.pdb | Remove-Item

    Copy-Item (Join-Path $root 'README.md') $out
    Copy-Item (Join-Path $root 'RELEASE_NOTES.md') $out
    Copy-Item (Join-Path $root 'LICENSE') $out
    $name = "Pasty-$version-$rid"

    if ($rid -like 'win-*') {
        $zip = Join-Path $dist "$name.zip"
        Remove-Item $zip -ErrorAction SilentlyContinue
        Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
        Write-Host "  -> $zip" -ForegroundColor Green
    }
    else {
        Copy-Item (Join-Path $root 'packaging/linux/install.sh') $out
        Copy-Item (Join-Path $root 'assets/pasty.png') $out
        $tar = Join-Path $dist "$name.tar.gz"
        Remove-Item $tar -ErrorAction SilentlyContinue
        # Put files under a top-level folder so extracting doesn't spill into the current directory.
        $folder = Join-Path $dist "staging/$name"
        Remove-Item -Recurse -Force $folder -ErrorAction SilentlyContinue
        Move-Item $out $folder
        tar -czf $tar -C (Join-Path $dist 'staging') $name
        if ($LASTEXITCODE -ne 0) { throw "tar failed for $rid" }
        Write-Host "  -> $tar" -ForegroundColor Green
    }
}

Remove-Item -Recurse -Force (Join-Path $dist 'staging')
Get-ChildItem $dist | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($_.Name)"
} | Where-Object { $_ -notlike '*SHA256SUMS*' } | Set-Content (Join-Path $dist 'SHA256SUMS.txt')
Write-Host "Done. Pasty has entered the chat." -ForegroundColor Magenta
