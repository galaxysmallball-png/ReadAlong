param([switch]$Verify)
$ErrorActionPreference = 'Stop'
$applicationDirectory = Join-Path $PSScriptRoot 'dist\界读'
& (Join-Path $PSScriptRoot 'scripts\Get-Fonts.ps1')
& (Join-Path $PSScriptRoot 'src\build.ps1') -OutputDirectory $applicationDirectory
New-Item -ItemType Directory -Path (Join-Path $applicationDirectory 'Fonts') -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Fonts') -File | Copy-Item -Destination (Join-Path $applicationDirectory 'Fonts') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'scripts\Setup-Offline.ps1') -Destination $applicationDirectory -Force
& (Join-Path $PSScriptRoot 'scripts\Setup-Offline.ps1') -AppDirectory $applicationDirectory
if ($Verify) { & (Join-Path $PSScriptRoot 'src\verify.ps1') -AppDirectory $applicationDirectory -OutputDirectory (Join-Path $PSScriptRoot 'qa') -TestOffline }

