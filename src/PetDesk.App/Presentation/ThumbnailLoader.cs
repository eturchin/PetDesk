using System.Windows.Media.Imaging;
using PetDesk.Infrastructure.Assets;
using PetDesk.Rendering.SpriteAtlas;

namespace PetDesk.App.Presentation;

public static class ThumbnailLoader
{
    public static BitmapSource? Load(PetEntry entry)
    {
        try
        {
            var (width, height) = ImageHeaderReader.Read(entry.ImagePath);
            using var input = File.OpenRead(entry.ImagePath);
            var small = new BitmapImage();
            small.BeginInit();
            small.CacheOption = BitmapCacheOption.OnLoad;
            small.DecodePixelWidth = Math.Min(width, Math.Max(40, entry.Manifest.Columns * 40));
            small.StreamSource = input;
            small.EndInit();
            small.Freeze();
            var rect = PetDesk.Core.Models.ManifestValidator.GetFrame(entry.Manifest, "idle", 0);
            int x = (int)(rect.X * small.PixelWidth / (double)width), y = (int)(rect.Y * small.PixelHeight / (double)height);
            int w = Math.Max(1, (int)(rect.Width * small.PixelWidth / (double)width)), h = Math.Max(1, (int)(rect.Height * small.PixelHeight / (double)height));
            var crop = new CroppedBitmap(small, new Int32Rect(x, y, Math.Min(w, small.PixelWidth - x), Math.Min(h, small.PixelHeight - y)));
            var converted = new FormatConvertedBitmap(crop, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
            converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
            var result = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, converted.PixelWidth * 4);
            result.Freeze();
            return result;
        }
        catch (Exception ex) when (PetLibrary.IsImportError(ex) || ex is InvalidOperationException)
        {
            return null;
        }
    }
}
