$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Project root: $root" -ForegroundColor Cyan
if ($root.Length -gt 80) {
    Write-Warning "The project path is still long ($($root.Length) characters). Move the T folder to a short location such as H:\T before building."
}

# Release file locks held by prior MSBuild/Roslyn compiler servers.
dotnet build-server shutdown | Out-Host

# Remove stale Visual Studio and generated state. The project now uses .b/.o,
# but older versions may still have per-project bin/obj directories.
$generatedNames = @('bin', 'obj')
Get-ChildItem -Path $root -Recurse -Directory -Force |
    Where-Object { $generatedNames -contains $_.Name } |
    Sort-Object FullName -Descending |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

@('.b', '.o', '.vs') | ForEach-Object {
    $path = Join-Path $root $_
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    }
}

Write-Host 'Restoring packages...' -ForegroundColor Cyan
dotnet restore .\Tatakae.sln --disable-parallel

Write-Host 'Building solution...' -ForegroundColor Cyan
dotnet build .\Tatakae.sln --no-restore -m:1

Write-Host 'Running tests...' -ForegroundColor Cyan
dotnet test .\Tatakae.sln --no-build -m:1
