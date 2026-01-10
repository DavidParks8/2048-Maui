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
- 🧱 Multiple game modes (Modern + Classic + Walltastrophy + Adversarial)
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

## Game Center Setup (iOS / Mac Catalyst)

The game includes Game Center integration on iOS and Mac Catalyst with leaderboards and achievements.

### 1. Configure in App Store Connect

1. Sign in to [App Store Connect](https://appstoreconnect.apple.com/)
2. Navigate to your app (Bundle ID: `com.dappermagna.twentyfortyeight`)
3. Go to Features → Game Center

### 2. Create Leaderboards

Create the following leaderboards:

#### Classic Mode Leaderboards

| Leaderboard ID | Name | Score Format | Sort Order |
| --- | --- | --- | --- |
| `com.dappermagna.twentyfortyeight.highscores.3x3` | Classic 3×3 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.4x4` | Classic 4×4 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.5x5` | Classic 5×5 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.6x6` | Classic 6×6 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.7x7` | Classic 7×7 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.8x8` | Classic 8×8 High Scores | Integer | High to Low |

#### Modern Mode Leaderboards

| Leaderboard ID | Name | Score Format | Sort Order |
| --- | --- | --- | --- |
| `com.dappermagna.twentyfortyeight.highscores.modern.3x3` | Modern 3×3 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.modern.4x4` | Modern 4×4 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.modern.5x5` | Modern 5×5 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.modern.6x6` | Modern 6×6 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.modern.7x7` | Modern 7×7 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.modern.8x8` | Modern 8×8 High Scores | Integer | High to Low |

#### Walltastrophy Mode Leaderboards

| Leaderboard ID | Name | Score Format | Sort Order |
| --- | --- | --- | --- |
| `com.dappermagna.twentyfortyeight.highscores.walltastrophy.3x3` | Walltastrophy 3×3 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.walltastrophy.4x4` | Walltastrophy 4×4 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.walltastrophy.5x5` | Walltastrophy 5×5 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.walltastrophy.6x6` | Walltastrophy 6×6 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.walltastrophy.7x7` | Walltastrophy 7×7 High Scores | Integer | High to Low |
| `com.dappermagna.twentyfortyeight.highscores.walltastrophy.8x8` | Walltastrophy 8×8 High Scores | Integer | High to Low |

#### Adversarial Mode Leaderboards

Adversarial mode is scored using normal 2048 merge scoring, but the objective is inverted: **lower score is better**. Configure these leaderboards with **Low to High** sort order.

| Leaderboard ID | Name | Score Format | Sort Order |
| --- | --- | --- | --- |
| `com.dappermagna.twentyfortyeight.highscores.adversarial.3x3` | Adversarial 3×3 Low Scores | Integer | Low to High |
| `com.dappermagna.twentyfortyeight.highscores.adversarial.4x4` | Adversarial 4×4 Low Scores | Integer | Low to High |
| `com.dappermagna.twentyfortyeight.highscores.adversarial.5x5` | Adversarial 5×5 Low Scores | Integer | Low to High |
| `com.dappermagna.twentyfortyeight.highscores.adversarial.6x6` | Adversarial 6×6 Low Scores | Integer | Low to High |
| `com.dappermagna.twentyfortyeight.highscores.adversarial.7x7` | Adversarial 7×7 Low Scores | Integer | Low to High |
| `com.dappermagna.twentyfortyeight.highscores.adversarial.8x8` | Adversarial 8×8 Low Scores | Integer | Low to High |

**Note:** Only the default win tile (2048) is supported for leaderboard submissions. Custom win tiles do not submit scores to leaderboards.

### 3. Create Achievements

Create the following achievements (all should be 100% completion):

| Achievement ID | Name | Description |
| --- | --- | --- |
| `com.dappermagna.twentyfortyeight.tile128` | Tile 128 | Create a 128 tile |
| `com.dappermagna.twentyfortyeight.tile256` | Tile 256 | Create a 256 tile |
| `com.dappermagna.twentyfortyeight.tile512` | Tile 512 | Create a 512 tile |
| `com.dappermagna.twentyfortyeight.tile1024` | Tile 1024 | Create a 1024 tile |
| `com.dappermagna.twentyfortyeight.tile2048` | Tile 2048 | Create a 2048 tile |
| `com.dappermagna.twentyfortyeight.tile4096` | Tile 4096 | Create a 4096 tile |
| `com.dappermagna.twentyfortyeight.firstwin` | First Win | Reach 2048 for the first time |
| `com.dappermagna.twentyfortyeight.score10000` | Score 10,000 | Reach a score of 10,000 |
| `com.dappermagna.twentyfortyeight.score25000` | Score 25,000 | Reach a score of 25,000 |
| `com.dappermagna.twentyfortyeight.score50000` | Score 50,000 | Reach a score of 50,000 |
| `com.dappermagna.twentyfortyeight.score100000` | Score 100,000 | Reach a score of 100,000 |

### 4. Testing Game Center

- Game Center requires a real iOS device for testing (not the simulator)
- Sign in to Game Center on your device
- The app will automatically authenticate when launched
- Leaderboard and Achievement buttons will appear when Game Center is available

## Architecture

The project is organized into three main components:

### 1. Core Engine (TwentyFortyEight.Core)

A fully-testable, UI-independent game engine that implements the classic 2048 rules:

- Game2048Engine: move logic, merge rules, win/game-over detection
- GameState: immutable state representation for undo/redo
- GameConfig: configurable board size + win conditions
- GameMode: ruleset variants (Modern, Classic, Walltastrophy, Adversarial)
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
5. New Tiles: After each move, a new tile appears in a random empty spot (value depends on game mode)
6. Winning: Reach the 2048 tile (game can continue after winning)
7. Game Over: No more valid moves available

### Adversarial Mode

Adversarial mode flips the role: **you place tiles to try to block the AI**.

- Input: Tap an empty cell to spawn a random `2` or `4`, then the AI automatically makes one move.
- Win: The AI has no legal moves (board is locked).
- Loss: The AI creates the win tile (2048 by default).
- Scoring: Standard 2048 merge scoring (non-negative), but **lower final score is better**.

## CI/CD

GitHub Actions builds and tests the solution on pushes and pull requests.

## License

See LICENSE for details.
