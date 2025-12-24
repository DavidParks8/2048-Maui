using System.Text.Json.Serialization;
using TwentyFortyEight.Core;

namespace TwentyFortyEight.Maui.Serialization;

/// <summary>
/// JSON serialization context for GameStatistics.
/// </summary>
[JsonSerializable(typeof(GameStatistics))]
internal partial class StatisticsSerializationContext : JsonSerializerContext { }
