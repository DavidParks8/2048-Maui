using System.Text.Json.Serialization;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Serialization;

/// <summary>
/// JSON serialization context for GameStateDto.
/// </summary>
[JsonSerializable(typeof(GameStateDto))]
[JsonSerializable(typeof(WallSegment))]
[JsonSerializable(typeof(GameConfig))]
internal partial class GameSerializationContext : JsonSerializerContext { }
