using System.Buffers.Binary;
using PetDesk.Core.Models;

namespace PetDesk.Rendering.SpriteAtlas;

public static class ImageHeaderReader
{
    public const long MaximumFileBytes = 32 * 1024 * 1024;
    public static (int Width, int Height) Read(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is < 30 or > MaximumFileBytes)
        {
            throw new PetFormatException("Error.FileSize");
        }

        Span<byte> header = stackalloc byte[30];
        stream.ReadExactly(header);
        int width, height;
        if (header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && header[12..16].SequenceEqual("IHDR"u8))
        {
            width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header[16..20]));
            height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header[20..24]));
        }
        else if (header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
        {
            if (header[12..16].SequenceEqual("VP8X"u8))
            {
                width = 1 + Read24(header[24..27]);
                height = 1 + Read24(header[27..30]);
            }
            else if (header[12..16].SequenceEqual("VP8L"u8) && header[20] == 0x2f)
            {
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(header[21..25]);
                width = (int)(bits & 0x3fff) + 1;
                height = (int)((bits >> 14) & 0x3fff) + 1;
            }
            else if (header[12..16].SequenceEqual("VP8 "u8) && header[23..26].SequenceEqual(new byte[] { 0x9d, 1, 0x2a }))
            {
                width = BinaryPrimitives.ReadUInt16LittleEndian(header[26..28]) & 0x3fff;
                height = BinaryPrimitives.ReadUInt16LittleEndian(header[28..30]) & 0x3fff;
            }
            else
            {
                throw new PetFormatException("Error.WebpHeader");
            }
        }
        else
        {
            throw new PetFormatException("Error.ImageHeader");
        }

        if (width is < 1 or > 8192 || height is < 1 or > 8192 || (long)width * height > ManifestValidator.MaximumPixels)
        {
            throw new PetFormatException("Error.ImageSize");
        }

        return (width, height);
    }

    private static int Read24(ReadOnlySpan<byte> bytes) => bytes[0] | bytes[1] << 8 | bytes[2] << 16;
}
