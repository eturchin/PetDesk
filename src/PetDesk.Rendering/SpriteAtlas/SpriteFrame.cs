using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PetDesk.Core.Models;

namespace PetDesk.Rendering.SpriteAtlas;

public sealed record SpriteFrame(BitmapSource Image, byte[] Alpha, int Width, int Height)
{
    public bool Hit(double normalizedX, double normalizedY) => normalizedX >= 0 && normalizedX < 1 && normalizedY >= 0 && normalizedY < 1 && Alpha[(int)(normalizedY * Height) * Width + (int)(normalizedX * Width)] > 0;
}
