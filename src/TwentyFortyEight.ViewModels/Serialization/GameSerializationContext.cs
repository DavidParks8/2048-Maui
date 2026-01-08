using System.Text.Json.Serialization;
using TwentyFortyEight.Core;
// Alias to avoid conflict with Apple's GameKit.GameSave namespace on iOS/Mac Catalyst.
using CoreGameSave = TwentyFortyEight.Core.GameSave;

namespace TwentyFortyEight.ViewModels.Serialization;

/// <summary>
/// JSON serialization context for GameStateDto.
/// </summary>
[JsonSerializable(typeof(GameStateDto))]
[JsonSerializable(typeof(CoreGameSave))]
[JsonSerializable(typeof(MoveRecord))]
[JsonSerializable(typeof(WallSegment))]
[JsonSerializable(typeof(GameConfig))]
internal partial class GameSerializationContext : JsonSerializerContext { }
