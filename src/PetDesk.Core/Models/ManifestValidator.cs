using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetDesk.Core.Models;

public static class ManifestValidator
{
    public const long MaximumPixels = 16_777_216;
    public static void Validate(PetManifest manifest, int atlasWidth, int atlasHeight)
    {
        if (manifest.SchemaVersion != 1)
        {
            throw new PetFormatException("Error.Schema");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id) || manifest.Id.Length > 80 || manifest.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_'))
        {
            throw new PetFormatException("Error.Id");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 100 || manifest.Name.Any(char.IsControl))
        {
            throw new PetFormatException("Error.Name");
        }

        if (manifest.FrameWidth is < 1 or > 4096 || manifest.FrameHeight is < 1 or > 4096 || manifest.Columns is < 1 or > 64 || manifest.Rows is < 1 or > 64)
        {
            throw new PetFormatException("Error.Grid");
        }

        if ((long)atlasWidth * atlasHeight > MaximumPixels || atlasWidth <= 0 || atlasHeight <= 0 || (long)manifest.FrameWidth * manifest.Columns != atlasWidth || (long)manifest.FrameHeight * manifest.Rows != atlasHeight)
        {
            throw new PetFormatException("Error.AtlasBounds");
        }

        if (manifest.Animations is null || !manifest.Animations.ContainsKey("idle") || manifest.Animations.Count > 64)
        {
            throw new PetFormatException("Error.Idle");
        }

        long framePixels = 0;
        foreach (var (name, animation) in manifest.Animations)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64 || name.Any(char.IsControl) || animation is null || animation.Row < 0 || animation.Row >= manifest.Rows || animation.StartColumn < 0 || animation.Frames < 1 || (long)animation.StartColumn + animation.Frames > manifest.Columns || !double.IsFinite(animation.Fps) || animation.Fps is < 1 or > 60)
            {
                throw new PetFormatException("Error.Animation");
            }

            if (animation.FrameDurationsMs is { } durations && (durations.Length != animation.Frames || durations.Any(d => d is < 16 or > 10000)))
            {
                throw new PetFormatException("Error.Durations");
            }

            framePixels += (long)animation.Frames * manifest.FrameWidth * manifest.FrameHeight;
        }

        if (framePixels > MaximumPixels)
        {
            throw new PetFormatException("Error.FrameMemory");
        }
    }

    public static FrameRect GetFrame(PetManifest manifest, string animation, int frame)
    {
        var definition = manifest.Animations[animation];
        if (frame < 0 || frame >= definition.Frames)
        {
            throw new ArgumentOutOfRangeException(nameof(frame));
        }

        return new FrameRect((definition.StartColumn + frame) * manifest.FrameWidth, definition.Row * manifest.FrameHeight, manifest.FrameWidth, manifest.FrameHeight);
    }
}
