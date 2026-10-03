$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'driver\EinkVirtualPen.vcxproj'

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'vswhere.exe not found. Visual Studio Build Tools / VS 2022 are required.'
}

$vsInstallPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.ComponentGroup.NativeDesktop.Core -property installationPath
if (-not $vsInstallPath) {
    throw 'No Visual Studio installation with desktop C++ support was found.'
}

$msbuild = Join-Path $vsInstallPath 'MSBuild\Current\Bin\MSBuild.exe'
if (-not (Test-Path $msbuild)) {
    throw "MSBuild.exe not found under $vsInstallPath"
}

$kitsRoot = 'C:\Program Files (x86)\Windows Kits\10'
if (-not (Test-Path $kitsRoot)) {
    throw 'Windows Kits directory not found.'
}

$includeRoot = Join-Path $kitsRoot 'Include'
if (-not (Test-Path $includeRoot)) {
    throw 'Windows SDK include root not found.'
}

$latestSdk = Get-ChildItem $includeRoot -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1

if (-not $latestSdk) {
    throw 'No Windows SDK version was found under the Windows Kits include directory.'
}

$env:WDKContentRoot = $kitsRoot
$env:WindowsSdkDir = $kitsRoot + '\'
$env:WindowsSDKVersion = $latestSdk.Name + '\'
$env:TargetPlatformVersion = $latestSdk.Name

Write-Host "Using VS: $vsInstallPath"
Write-Host "Using WDK/SDK root: $kitsRoot"
Write-Host "Using SDK version: $($latestSdk.Name)"

& $msbuild $projectPath /restore /m /nologo /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=WindowsKernelModeDriver10.0
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host 'Build succeeded.'
