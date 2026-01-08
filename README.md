# 2048-Maui

The classic 2048 game built with .NET MAUI.

## Overview

This is a fully-featured implementation of the classic 2048 puzzle game, built with .NET MAUI for cross-platform support (Android, iOS, Mac Catalyst, Windows). The project follows a clean architecture with a testable core engine and MVVM pattern for the UI.

## Features

- 🎮 Classic 2048 gameplay with smooth animations
- 🔄 Undo functionality
- 💾 Auto-save and resume game state
- 🏆 Best score tracking
- 🧩 Multiple board sizes (3x3 through 8x8)
- 🧱 Multiple game modes (Classic + Walltastrophy)
- 🧠 Optional Move Coach (recommended direction + reason)
- 🛟 Coach Nudges when you're stuck (optional)
- 👆 Swipe preview (slow-drag previews a move before committing)
- 🍎 Game Center integration on iOS + Mac Catalyst (leaderboards and achievements)
- 🎨 Light and dark theme support
- ⚙️ Gameplay settings (coach, nudges, haptics, undo button visibility)
- ♿ Accessibility features: semantic descriptions + screen reader announcements
- 🗣️ Voice Control friendly directional buttons (shown only when Voice Control / Narrator / TalkBack is enabled)
- ⌨️ Keyboard support (arrow keys + WASD)
- 🎮 Gamepad support (where supported)
- 👆 Touch gestures (swipe to move)
- 📱 Responsive layout for phones, tablets, and desktops

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET MAUI workload

Platform notes:

- iOS / Mac Catalyst: requires macOS + Xcode
- Android: requires Android SDK + emulators/device

## Setup

1. Install .NET MAUI workload:

   ```bash
   dotnet workload install maui
   ```

2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Build:

   ```bash
   dotnet build
   ```

   Tip: MAUI builds can be slow if you build multiple platforms. For a faster loop, build a specific target framework (examples in the next section), or set `MAUI_TARGET_PLATFORM` when building the solution (this is what CI uses).

4. Run tests:

   ```bash
   dotnet test
   ```

## Running the App

### VS Code (recommended)

This repo includes launch/task configs for debugging:

- macOS: Mac Catalyst debug/run
- Windows: Windows debug/run
- Android: Android debug/run

See `.vscode/launch.json` and `.vscode/tasks.json`.

### Windows

```bash
dotnet build src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet run --project src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-windows10.0.19041.0
```

### Android

```bash
dotnet build src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-android
dotnet run --project src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-android
```

### Mac Catalyst

```bash
dotnet build src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-maccatalyst -c Debug
dotnet run --project src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-maccatalyst -c Debug
```

### iOS

Note: Building for iOS requires a Mac with Xcode installed.

```bash
dotnet build src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-ios -c Debug
dotnet run --project src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-ios -c Debug
```

## Architecture

The project is organized into three main components:

### 1. Core Engine (TwentyFortyEight.Core)

A fully-testable, UI-independent game engine that implements the classic 2048 rules:

- Game2048Engine: move logic, merge rules, win/game-over detection
- GameState: immutable state representation for undo/redo
- GameConfig: configurable board size + win conditions
- GameMode: ruleset variants (Classic, Walltastrophy)
- IRandomSource: abstraction for deterministic testing
- GameStateDto: JSON-friendly serialization for persistence
- MoveAnalyzer / HeuristicMoveAdvisor: platform-agnostic move analysis and coaching

### 2. MAUI App (TwentyFortyEight.Maui)

Cross-platform UI built with .NET MAUI using MVVM pattern:

- GameViewModel: observable game state, commands, persistence
- TileViewModel: tile representation
- MainPage: responsive board with gesture and keyboard input

### 3. Tests (TwentyFortyEight.Core.Tests, TwentyFortyEight.ViewModels.Tests)

Comprehensive test suite using MSTest covering:

- Move/merge correctness for all directions
- Spawn behavior with deterministic RNG
- Win and game-over detection
- Undo/redo and serialization
- Move analysis / coach heuristics
- Ruleset identifiers (board size + mode)

## Project Structure

- slnx format: New XML-based solution file format for .NET 10
- Central Package Management (CPM): package versions in `Directory.Packages.props`
- Consolidated props: common build properties in `Directory.Build.props`
- src/
  - TwentyFortyEight.Core
  - TwentyFortyEight.Maui
  - TwentyFortyEight.ViewModels
- test/
  - TwentyFortyEight.Core.Tests
  - TwentyFortyEight.ViewModels.Tests

## Technologies

- .NET 10
- .NET MAUI
- MSTest
- CommunityToolkit.Mvvm

## Game Rules

1. Objective: Combine tiles to create a tile with the value 2048
2. Movement: Swipe or use arrow keys to move all tiles in that direction
3. Merging: Adjacent tiles with the same value merge into one (value doubles)
4. Scoring: Score increases by the value of each merged tile
5. New Tiles: After each move, a new tile (2 or 4) appears in a random empty spot
6. Winning: Reach the 2048 tile (game can continue after winning)
7. Game Over: No more valid moves available

## CI/CD

GitHub Actions builds and tests the solution on pushes and pull requests.

## License

See LICENSE for details.
