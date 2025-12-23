#!/bin/bash
# Post-create script - runs after container is created
set -e

echo "Restoring NuGet packages..."
cd /workspaces/2048-Maui
dotnet restore

echo ""
echo "=========================================="
echo "Development environment ready!"
echo "=========================================="
echo ""
echo "Build for Android:"
echo "  dotnet build src/TwentyFortyEight.Maui -f net10.0-android"
echo ""
