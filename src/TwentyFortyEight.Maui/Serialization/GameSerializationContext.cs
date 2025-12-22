using System.Text.Json.Serialization;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Serialization;

/// <summary>
/// JSON serialization context for GameStateDto.
/// </summary>
[JsonSerializable(typeof(GameStateDto))]
internal partial class GameSerializationContext : JsonSerializerContext
{
}
