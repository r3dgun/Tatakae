#!/usr/bin/env bash
set -euo pipefail

echo "Restoring solution..."
dotnet restore ./Tatakae.sln

echo "Building solution..."
dotnet build ./Tatakae.sln --no-restore

echo "Running tests..."
dotnet test ./Tatakae.sln --no-build --logger "console;verbosity=detailed"
