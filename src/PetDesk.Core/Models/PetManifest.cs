using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetDesk.Core.Models;

public sealed record PetManifest
{
    public int SchemaVersion { get; init; } = 1;
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string SpriteSheet { get; init; } = "spritesheet.png";
    public int FrameWidth { get; init; }
    public int FrameHeight { get; init; }
    public int Columns { get; init; }
    public int Rows { get; init; }
    public Dictionary<string, AnimationDefinition> Animations { get; init; } = new Dictionary<string, AnimationDefinition>(StringComparer.Ordinal);
}
