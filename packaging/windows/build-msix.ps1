#!/usr/bin/env pwsh
# Builds Microsoft Store packages (MSIX per architecture + one .msixbundle) into ./dist/msix.
#
# Store build (values from Partner Center → your app → Product identity):
#   pwsh packaging/windows/build-msix.ps1 -IdentityName "12345Publisher.PastytheDesktopCompanion" `
#       -Publisher "CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" -PublisherDisplayName "Your Name"
#
# Local test build, signed with a self-signed certificate (created on first use in your user store):
#   pwsh packaging/windows/build-msix.ps1 -Test
# Installing it needs that certificate trusted once, from an elevated PowerShell:
#   Import-Certificate -FilePath dist\msix\PastyLocalTest.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
# (Windows won't install unsigned full-trust desktop apps at all.)
param(
    [string] $IdentityName,
    [string] $Publisher,
    [string] $PublisherDisplayName,
    [string[]] $Runtimes = @('win-x64', 'win-arm64'),
    [switch] $Test
)
$ErrorActionPreference = 'Stop'

if ($Test) {
    $IdentityName = 'PastyTheDesktopCompanion.LocalTest'
    $Publisher = 'CN=Pasty Local Test'
    $PublisherDisplayName = 'Pasty (local test build)'
}
elseif (-not ($IdentityName -and $Publisher -and $PublisherDisplayName)) {
    throw 'Pass -IdentityName, -Publisher and -PublisherDisplayName from Partner Center (or -Test for a local build).'
}

$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$project = Join-Path $root 'src/Pasty.Desktop/Pasty.Desktop.csproj'
$tfm = 'net10.0-windows10.0.19041.0'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$msixVersion = "$version.0"   # the Store requires the fourth field to be 0
$out = Join-Path $root 'dist/msix'
$staging = Join-Path $out 'staging'

$sdkBin = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName 'x64\makeappx.exe') } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (-not $sdkBin) { throw 'Windows SDK (makeappx/makepri) not found. Install the Windows 10/11 SDK.' }
$makeappx = Join-Path $sdkBin.FullName 'x64\makeappx.exe'
$makepri = Join-Path $sdkBin.FullName 'x64\makepri.exe'
$signtool = Join-Path $sdkBin.FullName 'x64\signtool.exe'

$testCert = $null
if ($Test) {
    $testCert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $Publisher -and $_.NotAfter -gt (Get-Date) } | Select-Object -First 1
    if (-not $testCert) {
        $testCert = New-SelfSignedCertificate -Type Custom -Subject $Publisher -KeyUsage DigitalSignature `
            -FriendlyName 'Pasty local MSIX test signing' -CertStoreLocation Cert:\CurrentUser\My `
            -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}') -NotAfter (Get-Date).AddYears(1)
    }
}

Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $staging, (Join-Path $out 'bundle') | Out-Null

foreach ($rid in $Runtimes) {
    $arch = $rid.Split('-')[1]
    $app = Join-Path $staging $arch
    Write-Host "Publishing Pasty $version for $rid..." -ForegroundColor Cyan
    dotnet publish $project -c Release -f $tfm -r $rid --self-contained -o $app --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $rid" }
    Get-ChildItem $app -Filter *.pdb | Remove-Item

    Copy-Item (Join-Path $PSScriptRoot 'Images') (Join-Path $app 'Images') -Recurse
    Copy-Item (Join-Path $root 'LICENSE') $app

    $manifest = (Get-Content (Join-Path $PSScriptRoot 'AppxManifest.template.xml') -Raw).
        Replace('$(IdentityName)', [Security.SecurityElement]::Escape($IdentityName)).
        Replace('$(Publisher)', [Security.SecurityElement]::Escape($Publisher)).
        Replace('$(PublisherDisplayName)', [Security.SecurityElement]::Escape($PublisherDisplayName)).
        Replace('$(Version)', $msixVersion).
        Replace('$(Arch)', $arch)
    Set-Content (Join-Path $app 'AppxManifest.xml') $manifest -Encoding utf8

    # resources.pri lets Windows pick the right logo scale (scale-100/200/400, targetsize-*).
    $priConfig = Join-Path $staging "priconfig-$arch.xml"
    & $makepri createconfig /cf $priConfig /dq en-US /pv 10.0.0 /o | Out-Null
    & $makepri new /pr $app /cf $priConfig /mn (Join-Path $app 'AppxManifest.xml') /of (Join-Path $app 'resources.pri') /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "makepri failed for $arch" }

    $msix = Join-Path $out "PastyTheDesktopCompanion_${msixVersion}_$arch.msix"
    & $makeappx pack /d $app /p $msix /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "makeappx pack failed for $arch" }
    if ($testCert) {
        & $signtool sign /fd SHA256 /sha1 $testCert.Thumbprint /s My $msix | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "signing failed for $arch" }
    }
    Copy-Item $msix (Join-Path $out 'bundle')
    Write-Host "  -> $msix" -ForegroundColor Green
}

$bundle = Join-Path $out "PastyTheDesktopCompanion_$msixVersion.msixbundle"
& $makeappx bundle /d (Join-Path $out 'bundle') /p $bundle /bv $msixVersion /o | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'makeappx bundle failed' }
Remove-Item -Recurse -Force (Join-Path $out 'bundle')
Write-Host "  -> $bundle" -ForegroundColor Green
if ($Test) {
    & $signtool sign /fd SHA256 /sha1 $testCert.Thumbprint /s My $bundle | Out-Null
    Export-Certificate -Cert $testCert -FilePath (Join-Path $out 'PastyLocalTest.cer') | Out-Null
    Write-Host 'Test build signed with a local certificate. Trust it once (elevated), then Add-AppxPackage <file>.msix' -ForegroundColor Yellow
}
else {
    Write-Host 'Upload the .msixbundle in Partner Center → Packages. The Store signs it.' -ForegroundColor Magenta
}
