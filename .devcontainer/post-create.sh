#!/bin/bash
# Post-create script - runs after container is created
set -e

echo "Restoring NuGet packages..."
cd /workspaces/2048-Maui
dotnet restore apps/twenty-forty-eight/TwentyFortyEight.slnx

echo ""
echo "=========================================="
echo "Development environment ready!"
echo "=========================================="
echo ""
echo "Build for Android:"
echo "  dotnet build apps/twenty-forty-eight/src/TwentyFortyEight.Maui -f net10.0-android"
echo ""
