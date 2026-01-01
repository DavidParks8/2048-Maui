# Android Devcontainer Setup

This devcontainer provides a complete Android development environment for .NET MAUI, allowing you to build Android APKs without installing the Android SDK on your Windows host.

## Features

- .NET 10 SDK with MAUI Android workload
- Android SDK (API 35)
- Android Emulator with x86_64 system image
- VNC/Web desktop for viewing the emulator (slow on Windows, see below)

## Important: Emulator Performance on Windows

The Android emulator inside Docker on Windows **will be slow** because:
- KVM (hardware virtualization) is not available in Docker Desktop on Windows
- The emulator runs in full software emulation mode

**Recommended workflows:**

1. **Build in container, test on Windows host**
   - Build the APK in the devcontainer
   - Install on an emulator running on Windows: `adb install path/to/app.apk`

2. **Connect a physical Android device over network**
   - Enable wireless debugging on your Android device
   - Connect via ADB: `adb connect <device-ip>:5555`

3. **Use the container emulator for quick smoke tests**
   - It works, just expect it to be slow

## Usage

### Start the container

1. Open VS Code in this project
2. Press `Ctrl+Shift+P` → "Dev Containers: Reopen in Container"
3. Wait for setup to complete (~10-15 minutes first time)

### Build for Android

```bash
dotnet build src/TwentyFortyEight.Maui -f net10.0-android
```

The APK will be at:
```
src/TwentyFortyEight.Maui/bin/Debug/net10.0-android/com.dappermagna.twentyfortyeight-Signed.apk
```

### Start the emulator (optional, slow on Windows)

```bash
.devcontainer/start-emulator.sh
```

Then open http://localhost:6080 in your browser (password: `vscode`)

### Deploy to a running emulator/device

```bash
# List connected devices
adb devices

# Install the APK
adb install src/TwentyFortyEight.Maui/bin/Debug/net10.0-android/com.dappermagna.twentyfortyeight-Signed.apk
```

### Build and run directly

```bash
dotnet build src/TwentyFortyEight.Maui -f net10.0-android -t:Run
```

## Connecting Windows Emulator from Container

If you're running an Android emulator on Windows and want to deploy from the container:

1. Start the emulator on Windows
2. In the container, connect to the host's ADB:
   ```bash
   adb connect host.docker.internal:5555
   ```
3. Deploy as usual with `adb install`

## Troubleshooting

### Build fails with workload errors
```bash
dotnet workload restore
```

### Can't see emulator display
- Open http://localhost:6080 in browser
- Check if port 6080 is forwarded in VS Code Ports panel

### Container setup fails
- Try rebuilding the container: Command Palette → "Dev Containers: Rebuild Container"
- Check Docker Desktop is running
