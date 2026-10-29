param([string]$FontDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'Fonts'))
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
New-Item -ItemType Directory -Path $FontDirectory -Force | Out-Null
$assets = @(
    @{ Name='LXGWWenKaiScreen.ttf'; Url='https://github.com/lxgw/LxgwWenKai-Screen/releases/download/v1.530/LXGWWenKaiScreen.ttf'; Hash='ABD13364163E3CD84372B333F2FDCEE9EFB848634668F1C0CE5788DCD059EE61' },
    @{ Name='SourceSerif4-It.otf'; Url='https://api.github.com/repos/adobe-fonts/source-serif/git/blobs/550c8547051aefd8a40632307e6e3433dbd103b3'; Hash='F8062257F4693E438A8E317C9FE2B4393F0D4EED84D897EDCBFD733C30A1C1A1' },
    @{ Name='SourceSerif4-Regular.otf'; Url='https://api.github.com/repos/adobe-fonts/source-serif/git/blobs/fc9f84e94b2537b3cc10aaebcab08a0bc2a0e16b'; Hash='EDF160D0D584DEEE8A3BB2C3371B2A7624CA63580FBE02C57C1F4C91E84D8787' }
)
foreach ($asset in $assets) {
    $destination = Join-Path $FontDirectory $asset.Name
    if ((Test-Path -LiteralPath $destination) -and (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -eq $asset.Hash) { continue }
    $temporary = $destination + '.download'
    try {
        Write-Host ('Downloading ' + $asset.Name)
        Invoke-WebRequest -UseBasicParsing -Uri $asset.Url -Headers @{ Accept='application/vnd.github.raw+json'; 'User-Agent'='ReadAlong-FontSetup' } -OutFile $temporary -TimeoutSec 120
        if ((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $asset.Hash) { throw ('Font checksum mismatch: ' + $asset.Name) }
        Move-Item -LiteralPath $temporary -Destination $destination -Force
    } finally {
        if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    }
}
$licenseDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) 'Fonts'
foreach ($licenseName in @('LXGWWenKai-OFL.txt','SourceSerif-OFL.md')) {
    $source = Join-Path $licenseDirectory $licenseName
    $target = Join-Path $FontDirectory $licenseName
    if ([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($target)) { Copy-Item -LiteralPath $source -Destination $target -Force }
}
Write-Host 'Reading fonts ready (SHA-256 verified).'

