using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PetDesk.Infrastructure.Tests;

/// <summary>Deterministic Codex-v2 geometry without depending on anyone's private artwork.</summary>
public sealed class CodexAtlasFixture : IDisposable
{
    private const int Columns = 8;
    private const int Rows = 11;
    private const int FrameWidth = 192;
    private const int FrameHeight = 208;
    private readonly TestDirectory _directory = new();
    public string ImagePath { get; }

    public CodexAtlasFixture()
    {
        ImagePath = Path.Combine(_directory.Root, "codex-v2.png");
        int width = Columns * FrameWidth;
        int height = Rows * FrameHeight;
        int stride = width * 4;
        byte[] pixels = new byte[stride * height];
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                // Each frame has a transparent corner, an opaque center and a distinct alpha mask.
                for (int y = 32; y < FrameHeight - 32; y++)
                {
                    for (int x = 24 + column; x < FrameWidth - 24; x++)
                    {
                        int offset = (row * FrameHeight + y) * stride + (column * FrameWidth + x) * 4;
                        pixels[offset] = (byte)(80 + column * 12);
                        pixels[offset + 1] = (byte)(100 + row * 10);
                        pixels[offset + 2] = 180;
                        pixels[offset + 3] = 255;
                    }
                }
            }
        }

        var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(ImagePath);
        encoder.Save(output);
    }

    public void Dispose() => _directory.Dispose();
}
