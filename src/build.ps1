param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$frameworkDir = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $frameworkDir 'csc.exe'
$wpfDir = Join-Path $frameworkDir 'WPF'
$sdkDir = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata'
$winmd = Get-ChildItem -LiteralPath $sdkDir -Directory | Where-Object { $_.Name -match '^\d' } | Sort-Object Name -Descending | ForEach-Object { Join-Path $_.FullName 'Windows.winmd' } | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $winmd) { throw '构建需要 Windows 10 SDK（只影响构建，运行不需要 SDK）。' }
$facade = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\System.Runtime.dll'
$outputDir = if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { Split-Path $PSScriptRoot -Parent } else { [IO.Path]::GetFullPath($OutputDirectory) }
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
$refs = @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','UIAutomationClient.dll','UIAutomationTypes.dll') | ForEach-Object { '/reference:' + (Join-Path $wpfDir $_) }
$refs += @('/reference:System.Core.dll','/reference:System.Xaml.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll','/reference:System.Net.Http.dll','/reference:System.Security.dll',('/reference:' + $winmd),('/reference:' + $facade),('/reference:' + (Join-Path $frameworkDir 'System.Runtime.WindowsRuntime.dll')))
$sources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object { $_.FullName }
& $compiler /nologo /utf8output /nowarn:1701 /target:winexe /main:ReadAlong.Program /optimize+ /platform:x64 "/out:$outputDir\界读.exe" "/win32manifest:$PSScriptRoot\app.manifest" "/win32icon:$PSScriptRoot\app.ico" @refs @sources
if ($LASTEXITCODE -ne 0) { throw '主程序编译失败' }
$workerSources = @('Core.cs','Capture.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compiler /nologo /utf8output /nowarn:1701 /target:exe /main:ReadAlong.WorkerProgram /optimize+ /platform:x64 "/out:$outputDir\ReadAlong.Reader.exe" "/win32manifest:$PSScriptRoot\app.manifest" @refs @workerSources
if ($LASTEXITCODE -ne 0) { throw '识别组件编译失败' }
Write-Output "已生成：$outputDir\界读.exe 和 ReadAlong.Reader.exe"
