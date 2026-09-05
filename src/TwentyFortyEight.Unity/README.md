# TwentyFortyEight Unity Implementation

A Unity implementation of the classic 2048 game that reuses the core game logic from the existing .NET MAUI project.

## Overview

This Unity project provides a cross-platform implementation of 2048 using Unity's Universal Render Pipeline (URP). It shares the same core game logic (rules, state management, move validation) with the MAUI version, ensuring consistent gameplay across platforms.

## Unity Version

**Unity 2022.3.57f1 LTS** (Long Term Support)

This is the latest stable LTS release as of the project creation, providing:
- Long-term stability and support
- Universal Render Pipeline (URP) 14.0.11
- WebGL build support
- Cross-platform compatibility

## Prerequisites

1. **Unity Hub** (recommended) or Unity Editor directly
2. **Unity 2022.3.57f1 LTS** with the following modules:
   - WebGL Build Support (for web builds)
   - Windows Build Support (if on non-Windows)
   - macOS Build Support (if on non-macOS)
   - Linux Build Support (optional)

## Opening the Project

### Using Unity Hub

1. Open Unity Hub
2. Click "Add" and navigate to `src/TwentyFortyEight.Unity/`
3. Select the project folder
4. Click "Open" (Unity Hub will use the version specified in ProjectVersion.txt)

### Using Unity Editor Directly

1. Launch Unity 2022.3.57f1
2. Click "Open Project"
3. Navigate to `src/TwentyFortyEight.Unity/`
4. Click "Select Folder"

## Building the Project

### Desktop (Windows/Mac/Linux)

1. Open the project in Unity
2. Go to **File > Build Settings**
3. Select your target platform (PC, Mac & Linux Standalone)
4. Click "Switch Platform" if needed
5. Click "Build" or "Build And Run"

### WebGL

1. Open the project in Unity
2. Go to **File > Build Settings**
3. Select **WebGL**
4. Click "Switch Platform"
5. Click "Build" and choose an output folder
6. The build will create an `index.html` and supporting files
7. Host these files on any web server to play

**Note:** WebGL builds require a web server to run. You can use:
- Python: `python -m http.server 8000` (in the build directory)
- Node.js: `npx http-server`
- Unity's built-in "Build And Run" option (automatically starts a local server)

## Project Structure

```
src/TwentyFortyEight.Unity/
├── Assets/
│   ├── Plugins/
│   │   └── TwentyFortyEight.Core.dll    # Core game logic (from .NET project)
│   ├── Scenes/
│   │   └── MainScene.unity              # Main game scene
│   ├── Scripts/
│   │   ├── GameManager.cs               # Main game controller
│   │   ├── BoardRenderer.cs             # Board and tile rendering
│   │   ├── TileView.cs                  # Individual tile visual
│   │   ├── InputHandler.cs              # Keyboard/touch input
│   │   └── UIManager.cs                 # UI elements (score, buttons)
│   ├── Materials/                       # (Future: custom materials)
│   └── Prefabs/                         # (Future: reusable prefabs)
├── Packages/
│   ├── manifest.json                    # Package dependencies
│   └── packages-lock.json               # Locked package versions
├── ProjectSettings/                     # Unity project configuration
└── README.md                            # This file
```

## Core Logic Integration

The Unity project uses the compiled `TwentyFortyEight.Core.dll` from the .NET MAUI project. This DLL contains:

- **Game2048Engine**: Core game logic, move processing, win/lose detection
- **GameState**: Immutable state representation
- **Board**: 2D grid representation
- **Direction**: Move direction enum
- **GameConfig**: Configuration (board size, win tile, game mode)
- **GameMode**: Rule variants (Modern, Classic, etc.)

### How it Works

1. The Core DLL is copied to `Assets/Plugins/`
2. Unity scripts import `TwentyFortyEight.Core` namespace
3. GameManager creates a `Game2048Engine` instance
4. User input triggers moves via the engine
5. BoardRenderer updates visuals based on game state

### Updating the Core Logic

If the core logic changes in the MAUI project:

1. Rebuild the Core project:
   ```bash
   cd /path/to/2048-Maui
   dotnet build src/TwentyFortyEight.Core/TwentyFortyEight.Core.csproj -c Release
   ```

2. Copy the new DLL:
   ```bash
   cp src/TwentyFortyEight.Core/bin/Release/net10.0/TwentyFortyEight.Core.dll \
      src/TwentyFortyEight.Unity/Assets/Plugins/
   ```

3. Unity will automatically reload the updated DLL

## Gameplay

### Controls

- **Arrow Keys**: Up/Down/Left/Right to move tiles
- **WASD**: Alternative keyboard controls (W=Up, S=Down, A=Left, D=Right)
- **Touch/Swipe**: Swipe in any direction (mobile/touchscreen)
- **Mouse**: Click and drag to swipe (desktop)

### Features Implemented

- ✅ 4×4 game board
- ✅ Tile rendering with values and colors
- ✅ Keyboard input (arrow keys + WASD)
- ✅ Touch/swipe input
- ✅ Score tracking
- ✅ Best score persistence (via PlayerPrefs)
- ✅ New game / restart
- ✅ Undo move (if supported by core logic)
- ✅ Win detection (reaching 2048)
- ✅ Game over detection

### Future Enhancements

The project is structured to support future additions:

- **Ripple Effects**: URP is configured for screen-space post-processing effects
- **3D Tiles**: URP supports 3D rendering alongside 2D
- **Animations**: Tile merge/spawn animations
- **Particle Effects**: Visual feedback for merges
- **Sound Effects**: Audio integration
- **Multiple Board Sizes**: 3×3, 5×5, etc. (core supports this)
- **Game Modes**: Classic, Walltastrophy, Adversarial (core supports this)

## Universal Render Pipeline (URP)

This project uses URP instead of Unity's Built-in Render Pipeline because:

1. **Performance**: Better performance on mobile and web
2. **Modern Features**: Screen-space effects, post-processing
3. **Future-Proof**: Unity's recommended render pipeline
4. **Ripple Effects**: Enables future screen-space ripple post-process

### URP Configuration

- **Quality Settings**: Low, Medium, High profiles configured
- **Graphics Settings**: URP asset configured as default
- **Render Pipeline Asset**: Located in project settings

## WebGL Considerations

### Performance

- The game uses 2D rendering for optimal WebGL performance
- Minimal texture usage (procedurally generated tiles)
- Efficient UI with TextMeshPro

### Compatibility

- Tested with WebGL 2.0
- Browser requirements:
  - Chrome/Edge 90+
  - Firefox 88+
  - Safari 14+

### Build Settings

WebGL builds are configured with:
- Exception support: Full
- Compression: Gzip (default)
- Memory size: 16MB (adjustable in Player Settings)

## Development Notes

### Adding New Features

1. **Core Logic Changes**: Modify the .NET Core project and rebuild the DLL
2. **Visual Changes**: Modify Unity scripts in `Assets/Scripts/`
3. **UI Changes**: Edit the MainScene.unity or create UI prefabs

### Debugging

- Unity Editor provides real-time debugging
- Use `Debug.Log()` for console output
- Unity's Profiler helps identify performance issues
- The Core DLL can be debugged by attaching Visual Studio to Unity

### Testing

- Test in Unity Editor for quick iteration
- Test WebGL builds in actual browsers (not just Editor preview)
- Test touch input on mobile devices or using Chrome DevTools mobile emulation

## CI/CD Integration

The Unity project is designed to coexist with the existing MAUI CI/CD:

- Unity-specific files are in `src/TwentyFortyEight.Unity/`
- Unity artifacts (Library/, Temp/, etc.) are gitignored
- The Core DLL is built separately and copied in

### GitHub Actions (Future)

Consider adding Unity Cloud Build or GitHub Actions for automated builds:

```yaml
# Example: Unity WebGL build action
- uses: game-ci/unity-builder@v2
  with:
    targetPlatform: WebGL
    projectPath: src/TwentyFortyEight.Unity
```

## License

Same license as the main 2048-Maui repository (see root LICENSE file).

## Contributing

Contributions are welcome! Please:

1. Keep the Unity project separate from MAUI
2. Maintain core logic in the shared Core project
3. Test both desktop and WebGL builds
4. Follow Unity best practices and C# conventions

## Troubleshooting

### "DLL not found" error

- Ensure `TwentyFortyEight.Core.dll` is in `Assets/Plugins/`
- Check that the DLL is built for .NET Standard 2.1 or compatible

### WebGL build fails

- Check WebGL Build Support is installed
- Verify browser compatibility
- Check Unity console for specific errors

### Input not working

- Ensure InputHandler component is attached to GameManager
- Check that the scene has an EventSystem (for UI)
- Verify touch input settings in Player Settings

## Support

For issues or questions:

1. Check Unity console for error messages
2. Review Unity documentation: https://docs.unity3d.com/
3. Check the main repository README for core logic issues
4. Open an issue on the GitHub repository

---

**Note**: This Unity project is an alternative implementation alongside the MAUI version, not a replacement. Both share the same core game logic but provide different platform experiences.
