param([string]$AppDirectory=(Join-Path (Split-Path $PSScriptRoot -Parent) 'app'),[string]$CacheDirectory)
$ErrorActionPreference='Stop'
[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
$resourceDirectory=Join-Path (Split-Path $PSScriptRoot -Parent) 'offline'
if(-not(Test-Path -LiteralPath (Join-Path $resourceDirectory 'assets.json'))){$resourceDirectory=Join-Path $PSScriptRoot 'Offline'}
$manifest=Get-Content -LiteralPath (Join-Path $resourceDirectory 'assets.json') -Raw | ConvertFrom-Json
$offlineHome=Join-Path ([IO.Path]::GetFullPath($AppDirectory)) 'Offline'
New-Item -ItemType Directory -Path $offlineHome -Force|Out-Null
foreach($fileName in @('translate_worker.py','assets.json','MODEL-NOTICE.txt')){
    $source=Join-Path $resourceDirectory $fileName
    $target=Join-Path $offlineHome $fileName
    if([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($target)){Copy-Item -LiteralPath $source -Destination $target -Force}
}
if((Test-Path -LiteralPath (Join-Path $offlineHome 'ready.json')) -and (Test-Path -LiteralPath (Join-Path $offlineHome 'runtime\python.exe'))){
    try{
        $existingCheck=& (Join-Path $offlineHome 'runtime\python.exe') -I (Join-Path $offlineHome 'translate_worker.py') --check 2>$null
        if($LASTEXITCODE -eq 0 -and ($existingCheck|ConvertFrom-Json).ready){Write-Output 'Offline model already installed and verified.';return}
    }catch{}
}
if(Test-Path -LiteralPath (Join-Path $offlineHome 'ready.json')){Remove-Item -LiteralPath (Join-Path $offlineHome 'ready.json') -Force}
if([string]::IsNullOrWhiteSpace($CacheDirectory)){$CacheDirectory=Join-Path $offlineHome 'downloads'}
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Expand-VerifiedArchive([string]$Archive,[string]$Destination){
    $root=[IO.Path]::GetFullPath($Destination).TrimEnd('\')+'\'
    New-Item -ItemType Directory -Path $root -Force|Out-Null
    $zip=[IO.Compression.ZipFile]::OpenRead($Archive)
    try{
        foreach($entry in $zip.Entries){
            $target=[IO.Path]::GetFullPath((Join-Path $root $entry.FullName))
            if(-not $target.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Archive entry outside destination'}
            if([string]::IsNullOrEmpty($entry.Name)){New-Item -ItemType Directory -Path $target -Force|Out-Null;continue}
            New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force|Out-Null
            $sourceStream=$entry.Open();$targetStream=[IO.File]::Open($target,[IO.FileMode]::Create)
            try{$sourceStream.CopyTo($targetStream)}finally{$sourceStream.Dispose();$targetStream.Dispose()}
        }
    }finally{$zip.Dispose()}
}
foreach($asset in $manifest.assets){
    $cached=Join-Path $CacheDirectory $asset.cache
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($cached)) -Force|Out-Null
    if(-not(Test-Path -LiteralPath $cached) -or (Get-FileHash -LiteralPath $cached -Algorithm SHA256).Hash -ne $asset.sha256){
        Write-Output ('Downloading '+$asset.name)
        $temporary=$cached+'.download'
        try{
            Invoke-WebRequest -UseBasicParsing -Uri $asset.url -OutFile $temporary -TimeoutSec 300
            if((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $asset.sha256){throw ('Checksum mismatch: '+$asset.name)}
            Move-Item -LiteralPath $temporary -Destination $cached -Force
        }finally{if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}}
    }
    $destination=if($asset.kind -eq 'python'){Join-Path $offlineHome 'runtime'}elseif($asset.kind -eq 'wheel'){Join-Path $offlineHome 'runtime\Lib\site-packages'}else{Join-Path $offlineHome 'models'}
    Write-Output ('Installing '+$asset.name)
    Expand-VerifiedArchive $cached $destination
}
[IO.File]::WriteAllText((Join-Path $offlineHome 'runtime\python312._pth'),"python312.zip`r`n.`r`nLib/site-packages`r`n",[Text.Encoding]::ASCII)
$check=& (Join-Path $offlineHome 'runtime\python.exe') -I (Join-Path $offlineHome 'translate_worker.py') --check
if($LASTEXITCODE -ne 0){throw 'Offline model validation failed'}
$result=$check|ConvertFrom-Json
if(-not $result.ready -or -not $result.networkBlocked){throw 'Offline model validation failed'}
[IO.File]::WriteAllText((Join-Path $offlineHome 'ready.json'),$check,[Text.UTF8Encoding]::new($false))
Write-Output 'Offline English-to-Chinese translation is ready. No key or daily character quota.'
