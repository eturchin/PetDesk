using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetDesk.Core.Models;

public sealed record AnimationDefinition
{
    public int Row { get; init; }
    public int StartColumn { get; init; }
    public int Frames { get; init; } = 1;
    public double Fps { get; init; } = 8;
    public bool Loop { get; init; } = true;
    public int[]? FrameDurationsMs { get; init; }

    public double Duration(int index) => FrameDurationsMs is { } durations ? durations[index] / 1000d : 1d / Fps;
    [JsonIgnore]
    public double TotalDuration => FrameDurationsMs is { } durations ? durations.Sum() / 1000d : Frames / Fps;
}
