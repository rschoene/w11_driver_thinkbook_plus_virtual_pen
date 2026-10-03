$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'bridge\EinkPenInjector\EinkPenInjector.csproj'
$out = Join-Path $repoRoot 'dist\single'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

dotnet build $project -c Release -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Keep exactly one file: the executable (.NET Framework 4.8 ships with Windows 10/11).
Get-ChildItem $out | Where-Object { $_.Name -ne 'EinkPenInjector.exe' } | Remove-Item -Recurse -Force
Write-Host "Single binary: $(Join-Path $out 'EinkPenInjector.exe')"
