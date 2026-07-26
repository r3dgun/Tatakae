Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Restoring solution..." -ForegroundColor Cyan
dotnet restore .\Tatakae.sln

Write-Host "Building solution..." -ForegroundColor Cyan
dotnet build .\Tatakae.sln --no-restore

Write-Host "Running tests..." -ForegroundColor Cyan
dotnet test .\Tatakae.sln --no-build --logger "console;verbosity=detailed"
