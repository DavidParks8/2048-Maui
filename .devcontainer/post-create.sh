#!/bin/bash
# Post-create script - runs after container is created
set -e

echo "Restoring NuGet packages..."
cd /workspaces/2048-Maui
dotnet restore apps/twenty-forty-eight/TwentyFortyEight.slnx --disable-build-servers
dotnet build apps/twenty-forty-eight/src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj \
    -t:InstallAndroidDependencies -f net11.0-android \
    -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory=/home/vscode/.jdk \
    -p:AcceptAndroidSdkLicenses=True --disable-build-servers
"$ANDROID_HOME/cmdline-tools/latest/bin/sdkmanager" --install "cmdline-tools;21.0"

echo ""
echo "=========================================="
echo "Development environment ready!"
echo "=========================================="
echo ""
echo "Build for Android:"
echo "  dotnet build apps/twenty-forty-eight/src/TwentyFortyEight.Maui -f net11.0-android"
echo ""
