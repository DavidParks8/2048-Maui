# Building the Core DLL for Unity

This script builds the TwentyFortyEight.Core project and copies the DLL to the Unity project's Plugins folder.

## Usage

### Windows (PowerShell)

```powershell
# From repository root
cd src\TwentyFortyEight.Core
dotnet build -c Release
Copy-Item bin\Release\net10.0\TwentyFortyEight.Core.dll ..\TwentyFortyEight.Unity\Assets\Plugins\
```

### macOS/Linux (Bash)

```bash
# From repository root
cd src/TwentyFortyEight.Core
dotnet build -c Release
cp bin/Release/net10.0/TwentyFortyEight.Core.dll ../TwentyFortyEight.Unity/Assets/Plugins/
```

## When to Rebuild

Rebuild and copy the Core DLL when:

1. You modify any code in `src/TwentyFortyEight.Core/`
2. You add new features to the core game logic
3. You fix bugs in the core engine
4. You change game rules or behavior

After copying the new DLL, Unity will automatically reload it when you return to the editor.

## Troubleshooting

**"DLL not found" in Unity**
- Ensure the DLL is in `Assets/Plugins/TwentyFortyEight.Core.dll`
- Check that the build succeeded without errors
- Verify the DLL is not corrupted (should be ~50KB)

**"Type not found" errors in Unity**
- Ensure you're using .NET Standard 2.1 compatible code in Core
- Check that all required namespaces are imported in Unity scripts
- Verify the Core project built for the correct target framework

**Unity doesn't reload the DLL**
- Close and reopen Unity
- Or use Assets > Refresh (Ctrl+R / Cmd+R)
- Check Unity console for import errors
