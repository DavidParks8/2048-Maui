# 2048-Maui
The classic 2048 game built with .NET MAUI

## Overview

This is a fully-featured implementation of the classic 2048 puzzle game, built with .NET MAUI for cross-platform support (Android, Windows, iOS). The project follows a clean architecture with a testable core engine and MVVM pattern for the UI.

## Features

- 🎮 Classic 2048 gameplay with smooth animations
- 🔄 Undo functionality (up to 50 moves)
- 💾 Auto-save and resume game state
- 🏆 Best score tracking
- 🎨 Light and dark theme support
- ♿ Accessibility features with semantic descriptions
- ⌨️ Keyboard support (arrow keys + WASD)
- 👆 Touch gestures (swipe to move)
- 📱 Responsive layout for phones, tablets, and desktops

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- .NET MAUI workload

## Setup

1. **Install .NET MAUI workload:**
   ```bash
   dotnet workload install maui
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore 2048-Maui.slnx
   ```

3. **Build the solution:**
   ```bash
   dotnet build 2048-Maui.slnx
   ```

4. **Run tests:**
   ```bash
   dotnet run --project test/TwentyFortyEight.Tests/TwentyFortyEight.Tests.csproj
   ```

## Running the App

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

### iOS/Mac Catalyst

**Note:** Building for iOS/Mac Catalyst requires a Mac with Xcode installed.

```bash
dotnet build src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-ios
dotnet run --project src/TwentyFortyEight.Maui/TwentyFortyEight.Maui.csproj -f net10.0-ios
```

## Architecture

The project is organized into three main components:

### 1. Core Engine (`TwentyFortyEight.Core`)

A fully-testable, UI-independent game engine that implements the classic 2048 rules:

- **Game2048Engine**: Main engine with move logic, merge rules, win/game-over detection
- **GameState**: Immutable state representation for easy undo/redo
- **GameConfig**: Configurable board size and win conditions
- **IRandomSource**: Abstraction for deterministic testing
- **GameStateDto**: JSON-friendly serialization for persistence

**Key Features:**
- Correct merge behavior (e.g., `[2,2,2,2]` → `[4,4,0,0]`)
- No-op moves don't spawn tiles or change score
- 90% chance of spawning 2, 10% chance of spawning 4
- Bounded undo/redo history (50 moves)

### 2. MAUI App (`TwentyFortyEight.Maui`)

Cross-platform UI built with .NET MAUI using MVVM pattern:

- **GameViewModel**: Observable game state, commands, and persistence
- **TileViewModel**: Individual tile representation with colors and values
- **MainPage**: Responsive game board with gesture and keyboard input

**Input Methods:**
- Touch: Swipe gestures (up/down/left/right)
- Keyboard: Arrow keys or WASD

**UI Features:**
- Score and best score display
- Visual feedback for game state (won/game over)
- Tile colors that match classic 2048 design
- Undo/New Game buttons

### 3. Tests (`TwentyFortyEight.Tests`)

Comprehensive test suite using MSTest:

- Move/merge correctness for all directions
- No-op move validation
- Spawn behavior with deterministic RNG
- Win and game-over detection
- Undo/redo functionality
- State serialization

## Project Structure

This project uses modern .NET scaffolding:

- **slnx format**: New XML-based solution file format for .NET 10
- **Central Package Management (CPM)**: Package versions managed centrally in `Directory.Packages.props`
- **Consolidated props**: Common build properties defined in `Directory.Build.props`
- **MSTest.Sdk**: Modern test project format with integrated test runner
- **src/**: Source code projects
  - **TwentyFortyEight.Core**: Core game engine library
  - **TwentyFortyEight.Maui**: .NET MAUI application
- **test/**: Test projects
  - **TwentyFortyEight.Tests**: Unit tests for the core engine

## Technologies

- .NET 10
- .NET MAUI for cross-platform UI
- MSTest for unit testing
- MVVM pattern for clean separation of concerns
- JSON serialization for game state persistence
- Preferences API for local storage

## Game Rules

1. **Objective**: Combine tiles to create a tile with the value 2048
2. **Movement**: Swipe or use arrow keys to move all tiles in that direction
3. **Merging**: Adjacent tiles with the same value merge into one (value doubles)
4. **Scoring**: Score increases by the value of each merged tile
5. **New Tiles**: After each move, a new tile (2 or 4) appears in a random empty spot
6. **Winning**: Reach the 2048 tile (game can continue after winning)
7. **Game Over**: No more valid moves available (board full with no merges possible)

## CI/CD

The project includes a GitHub Actions workflow that automatically builds and tests the solution on every push and pull request.

## License

See LICENSE file for details.
