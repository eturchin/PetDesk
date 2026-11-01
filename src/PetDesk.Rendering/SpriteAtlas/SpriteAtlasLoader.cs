using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PetDesk.Core.Models;

namespace PetDesk.Rendering.SpriteAtlas;

public static class SpriteAtlasLoader
{
    public static SpriteAtlas Load(PetManifest manifest, string path)
    {
        var (width, height) = ImageHeaderReader.Read(path);
        ManifestValidator.Validate(manifest, width, height);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        BitmapSource source;
        try
        {
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count != 1)
            {
                throw new PetFormatException("Error.StaticSheet");
            }

            source = decoder.Frames[0];
            if (source.PixelWidth != width || source.PixelHeight != height)
            {
                throw new PetFormatException("Error.DecodeSize");
            }

            if (source.Format != PixelFormats.Bgra32)
            {
                source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            }

            source.Freeze();
            var frames = new Dictionary<string, SpriteFrame[]>(StringComparer.Ordinal);
            foreach (var (name, definition) in manifest.Animations)
            {
                var animation = new SpriteFrame[definition.Frames];
                for (var i = 0; i < animation.Length; i++)
                {
                    animation[i] = SpriteFrameExtractor.Extract(source, ManifestValidator.GetFrame(manifest, name, i));
                }

                if (animation.All(f => !f.Alpha.Any(a => a > 0)))
                {
                    throw new PetFormatException("Error.EmptyAnimation");
                }

                frames.Add(name, animation);
            }

            return new SpriteAtlas(manifest, frames);
        }
        catch (Exception ex) when (ex is NotSupportedException or FileFormatException or ArgumentException or InvalidOperationException)
        {
            throw new PetFormatException(Path.GetExtension(path).Equals(".webp", StringComparison.OrdinalIgnoreCase) ? "Error.Webp" : "Error.Decode");
        }
    }
}
