$ErrorActionPreference = 'Stop'
$applicationDirectory = Join-Path $PSScriptRoot 'app'
& (Join-Path $PSScriptRoot 'scripts\Get-Fonts.ps1') -FontDirectory (Join-Path $applicationDirectory 'Fonts')
Start-Process -FilePath (Join-Path $applicationDirectory '界读.exe') -WorkingDirectory $applicationDirectory -WindowStyle Normal

