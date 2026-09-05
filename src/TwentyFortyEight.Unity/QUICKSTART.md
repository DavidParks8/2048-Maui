# Quick Start Guide - Unity 2048 Project

This guide helps you get the Unity 2048 project up and running quickly.

## Prerequisites

Before you begin, ensure you have:

1. **Unity Hub** installed
   - Download from: https://unity.com/download
2. **Unity 2022.3.57f1 LTS**
   - Install via Unity Hub
   - Include these modules:
     - WebGL Build Support (for web builds)
     - Your platform's build support (Windows/Mac/Linux)

## Step 1: Open the Project

1. Launch Unity Hub
2. Click **Add** → **Add project from disk**
3. Navigate to `src/TwentyFortyEight.Unity/`
4. Click **Open** or **Select Folder**
5. Unity Hub will detect the project and show Unity version 2022.3.57f1
6. Click on the project to open it in Unity

**First Time Opening:** Unity will import assets and compile scripts. This may take 2-5 minutes.

## Step 2: Open the Main Scene

1. In Unity Editor, go to **Project** window (bottom)
2. Navigate to `Assets/Scenes/`
3. Double-click `MainScene.unity`
4. The scene should open in the Scene view

## Step 3: Test in Play Mode

1. Click the **Play** button (▶) at the top center of Unity Editor
2. The game should start in the Game view
3. Test controls:
   - **Arrow Keys** or **WASD** to move tiles
   - **Mouse**: Click and drag to swipe

**Note:** If you see a blank screen, check the Console window (Window > General > Console) for errors.

## Step 4: Build the Game

### Desktop Build

1. Go to **File** > **Build Settings**
2. Select **PC, Mac & Linux Standalone**
3. Click **Switch Platform** (if needed)
4. Click **Build** and choose a folder
5. Run the executable from the build folder

### WebGL Build

1. Go to **File** > **Build Settings**
2. Select **WebGL**
3. Click **Switch Platform** (wait for platform switch to complete)
4. Click **Build** and choose a folder
5. Open the folder and run a local web server:
   ```bash
   # Python
   python -m http.server 8000
   
   # Node.js
   npx http-server
   ```
6. Open browser to `http://localhost:8000`

## Troubleshooting

### "The following errors were found when importing the Core DLL"

**Solution:** Rebuild the Core DLL:
```bash
cd ../../TwentyFortyEight.Core
dotnet build -c Release
cp bin/Release/net10.0/TwentyFortyEight.Core.dll ../TwentyFortyEight.Unity/Assets/Plugins/
```
Then return to Unity and let it reimport the DLL.

### "Missing scripts" warnings

**Solution:** 
1. Check that all .cs files in `Assets/Scripts/` are present
2. Go to **Assets** > **Reimport All**
3. Wait for compilation to complete

### Play mode shows blank screen

**Solution:**
1. Open **Window** > **General** > **Console**
2. Look for error messages
3. Common issues:
   - Core DLL not found → See "Missing DLL" solution above
   - Scene not saved → Open MainScene.unity and press Play again

### Input doesn't work

**Solution:**
1. Ensure the Game view has focus (click in it)
2. Check that InputHandler script is attached to GameManager
3. Verify Input System is installed (should be automatic)

### WebGL build fails

**Solution:**
1. Ensure WebGL Build Support is installed in Unity Hub
2. Go to **Edit** > **Project Settings** > **Player** > **WebGL**
3. Check "Memory Size" is at least 16MB
4. Try building with "Development Build" checked first

## Next Steps

Once the project is working:

1. **Modify Game Settings**: 
   - Select GameManager in Scene Hierarchy
   - In Inspector, change Board Size or Win Tile value
   
2. **Customize Visuals**:
   - Edit `BoardRenderer.cs` to change tile colors
   - Modify `TileView.cs` for different tile appearance

3. **Add Features**:
   - See main README.md for architecture details
   - Modify core logic in `TwentyFortyEight.Core` project
   - Rebuild and copy DLL to update Unity project

4. **Deploy**:
   - Build for your target platform
   - For WebGL, host on GitHub Pages, itch.io, or any web server

## Getting Help

- Check [Unity Documentation](https://docs.unity3d.com/)
- Review the full [README.md](README.md) in this folder
- Check [main repository README](../../README.md) for core logic details
- Open an issue on GitHub for bugs or questions

## Quick Command Reference

```bash
# Build Core DLL (from repo root)
cd src/TwentyFortyEight.Core
dotnet build -c Release

# Copy DLL to Unity (macOS/Linux)
cp bin/Release/net10.0/TwentyFortyEight.Core.dll ../TwentyFortyEight.Unity/Assets/Plugins/

# Copy DLL to Unity (Windows PowerShell)
Copy-Item bin\Release\net10.0\TwentyFortyEight.Core.dll ..\TwentyFortyEight.Unity\Assets\Plugins\

# Run WebGL build locally
cd build/output/folder
python -m http.server 8000
# Then open http://localhost:8000 in browser
```

Happy coding! 🎮
