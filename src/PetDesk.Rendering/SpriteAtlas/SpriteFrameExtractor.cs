using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PetDesk.Core.Models;

namespace PetDesk.Rendering.SpriteAtlas;

public static class SpriteFrameExtractor
{
    public static SpriteFrame Extract(BitmapSource atlas, FrameRect rect)
    {
        var pixels = new byte[checked(rect.Width * rect.Height * 4)];
        atlas.CopyPixels(new Int32Rect(rect.X, rect.Y, rect.Width, rect.Height), pixels, rect.Width * 4, 0);
        var alpha = new byte[rect.Width * rect.Height];
        for (var i = 0; i < alpha.Length; i++)
        {
            // Tiny alpha noise in generated atlases must not produce invisible mouse barriers.
            if (pixels[i * 4 + 3] < 8)
            {
                pixels[i * 4] = pixels[i * 4 + 1] = pixels[i * 4 + 2] = pixels[i * 4 + 3] = 0;
            }

            alpha[i] = pixels[i * 4 + 3];
        }

        var image = BitmapSource.Create(rect.Width, rect.Height, 96, 96, PixelFormats.Bgra32, null, pixels, rect.Width * 4);
        image.Freeze();
        return new SpriteFrame(image, alpha, rect.Width, rect.Height);
    }
}
