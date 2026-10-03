param(
    [string]$Version = 'local'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $repoRoot 'scripts\build-driver.ps1'
if (-not (Test-Path $buildScript)) {
    throw "Build script not found: $buildScript"
}

& $buildScript
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$packageRoot = Join-Path $repoRoot 'x64\Release\EinkVirtualPen'
if (-not (Test-Path $packageRoot)) {
    throw "Driver package not found: $packageRoot"
}

foreach ($requiredFile in @('EinkVirtualPen.inf', 'EinkVirtualPen.sys', 'einkvirtualpen.cat')) {
    $filePath = Join-Path $packageRoot $requiredFile
    if (-not (Test-Path $filePath)) {
        throw "Required driver package file not found: $filePath"
    }
}

$certificatePath = Join-Path $repoRoot 'x64\Release\EinkVirtualPen.cer'
if (Test-Path $certificatePath) {
    Copy-Item -Path $certificatePath -Destination $packageRoot -Force
}

$zipName = "EinkVirtualPen-$Version.zip"
$archivePath = Join-Path $repoRoot $zipName

Compress-Archive -Path (Join-Path $packageRoot '*') -DestinationPath $archivePath -Force
Write-Host "Release archive created: $archivePath"
