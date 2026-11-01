namespace PetDesk.Core.Models;

/// <summary>Grid adapter verified against the local Pets animation-row reference and a real v2 PNG.</summary>
public static class KnownPetAdapter
{
    public static PetManifest Detect(int width, int height, string name = "Pet", string spriteSheet = "spritesheet.png")
    {
        if (width != 1536 || height is not (1872 or 2288))
        {
            throw new PetFormatException("Error.UnknownGrid");
        }

        var definitions = new Dictionary<string, AnimationDefinition>(StringComparer.Ordinal);
        void Add(string key, int row, bool loop, params int[] durations) => definitions.Add(key, new AnimationDefinition { Row = row, Frames = durations.Length, FrameDurationsMs = durations, Loop = loop });
        Add("idle", 0, true, 280, 110, 110, 140, 140, 320);
        Add("walkRight", 1, true, 120, 120, 120, 120, 120, 120, 120, 220);
        Add("walkLeft", 2, true, 120, 120, 120, 120, 120, 120, 120, 220);
        Add("wave", 3, false, 140, 140, 140, 280);
        Add("jump", 4, false, 140, 140, 140, 140, 280);
        Add("failed", 5, false, 140, 140, 140, 140, 140, 140, 140, 240);
        Add("waiting", 6, true, 150, 150, 150, 150, 150, 260);
        Add("running", 7, true, 120, 120, 120, 120, 120, 220);
        Add("review", 8, false, 150, 150, 150, 150, 150, 280);
        if (height == 2288)
        {
            for (var i = 0; i < 16; i++)
            {
                definitions.Add($"look{i * 22.5:000.0}".Replace(',', '.'), new AnimationDefinition { Row = 9 + i / 8, StartColumn = i % 8, Frames = 1, Fps = 1 });
            }
        }

        return new PetManifest
        {
            Id = "pet-" + Guid.NewGuid().ToString("N"),
            Name = name,
            SpriteSheet = spriteSheet,
            FrameWidth = 192,
            FrameHeight = 208,
            Columns = 8,
            Rows = height / 208,
            Animations = definitions
        };
    }
}
