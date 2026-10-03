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

$msbuild = Join-Path $vsInstallPath 'MSBuild\Current\Bin\amd64\MSBuild.exe'
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

$compatibleSdk = Get-ChildItem $includeRoot -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
    Where-Object {
        $version = $_.Name
        (Test-Path (Join-Path $_.FullName 'shared\sdkddkver.h')) -and
        (Test-Path (Join-Path $kitsRoot "Lib\$version\um\x64\gdi32.lib")) -and
        (Test-Path (Join-Path $kitsRoot "Build\$version\WindowsDriver.Common.props")) -and
        (Test-Path (Join-Path $kitsRoot "Build\$version\x64\ImportAfter\WDK.x64.WindowsKernelModeDriver.Platform.props")) -and
        (Test-Path (Join-Path $kitsRoot "Lib\$version\km\x64\vhfkm.lib"))
    } |
    Sort-Object { [version]$_.Name } -Descending |
    Select-Object -First 1

if (-not $compatibleSdk) {
    throw 'No complete matching Windows SDK and WDK installation was found. Install the Windows SDK version matching the WDK, including the x64 desktop libraries.'
}

$latestSdk = $compatibleSdk
$env:WDKContentRoot = $kitsRoot + '\'
$env:WindowsSdkDir = $kitsRoot + '\'
$env:WindowsSDKVersion = $latestSdk.Name + '\'
$env:WindowsTargetPlatformVersion = $latestSdk.Name
$env:TargetPlatformVersion = $latestSdk.Name

Write-Host "Using VS: $vsInstallPath"
Write-Host "Using WDK/SDK root: $kitsRoot"
Write-Host "Using SDK version: $($latestSdk.Name)"

& $msbuild $projectPath /restore /m /nologo /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=WindowsKernelModeDriver10.0 "/p:SolutionDir=$repoRoot\"
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host 'Build succeeded.'
