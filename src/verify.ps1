param([string]$OutputDirectory, [string]$AppDirectory, [switch]$TestNetwork, [switch]$TestOffline)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($AppDirectory)) { $AppDirectory = Split-Path $PSScriptRoot -Parent }
$appPath = Join-Path $AppDirectory '界读.exe'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) '验证记录' }
$qaPath = Join-Path $OutputDirectory ('QA-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $qaPath -Force | Out-Null
& $appPath --self-test (Join-Path $qaPath 'self.txt') | Out-Null
Get-Content -LiteralPath (Join-Path $qaPath 'self.txt')
if ((Get-Content -LiteralPath (Join-Path $qaPath 'self.txt') -Raw) -match '(?m)^FAIL(?:\s|$)') { throw '底层检查失败' }
& $appPath --demo --data-dir (Join-Path $qaPath 'data') --ui-test (Join-Path $qaPath 'ui.txt') | Out-Null
Get-Content -LiteralPath (Join-Path $qaPath 'ui.txt')
if ((Get-Content -LiteralPath (Join-Path $qaPath 'ui.txt') -Raw) -match '(?m)^FAIL(?:\s|$)') { throw '界面检查失败' }
if ($TestNetwork) {
    & $appPath --network-test (Join-Path $qaPath 'network.txt') | Out-Null
    Get-Content -LiteralPath (Join-Path $qaPath 'network.txt')
    if ((Get-Content -LiteralPath (Join-Path $qaPath 'network.txt') -Raw) -match '(?m)^FAIL(?:\s|$)') { throw '联网检查失败' }
}
if ($TestOffline) {
    & $appPath --offline-test (Join-Path $qaPath 'offline.txt') | Out-Null
    Get-Content -LiteralPath (Join-Path $qaPath 'offline.txt')
    if ((Get-Content -LiteralPath (Join-Path $qaPath 'offline.txt') -Raw) -match '(?m)^FAIL(?:\s|$)') { throw '离线翻译检查失败' }
}
Write-Output "结果与截图：$qaPath"
