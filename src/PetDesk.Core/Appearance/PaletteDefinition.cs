using System.Globalization;

namespace PetDesk.Core.Appearance;

public sealed record PaletteDefinition(string Id, string Name, string Background, string Surface, string Primary, string Accent, string Glow)
{
    public int SchemaVersion { get; init; } = 1;

    public static bool IsHex(string? value) => value is { Length: 7 } && value[0] == '#' && value[1..].All(Uri.IsHexDigit);
    public static bool IsSafeId(string? id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 80 && id.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
    public PaletteDefinition Validate()
    {
        if (SchemaVersion != 1 || !IsSafeId(Id) || string.IsNullOrWhiteSpace(Name) || Name.Length > 40 || Name.Any(char.IsControl) || !new[]
        {
            Background,
            Surface,
            Primary,
            Accent,
            Glow
        }.All(IsHex))
        {
            throw new Models.PetFormatException("Error.PaletteInvalid");
        }

        return this with
        {
            Name = Name.Trim(),
            Background = Background.ToUpperInvariant(),
            Surface = Surface.ToUpperInvariant(),
            Primary = Primary.ToUpperInvariant(),
            Accent = Accent.ToUpperInvariant(),
            Glow = Glow.ToUpperInvariant()
        };
    }

    public static string Mix(string first, string second, double weight)
    {
        int Channel(string text, int offset) => int.Parse(text.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        int Blend(int offset) => (int)Math.Round(Channel(first, offset) * (1 - weight) + Channel(second, offset) * weight);
        return $"#{Blend(1):X2}{Blend(3):X2}{Blend(5):X2}";
    }

    private static double Luminance(string hex)
    {
        double Channel(int offset)
        {
            var c = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;
            return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);
        }

        return .2126 * Channel(1) + .7152 * Channel(3) + .0722 * Channel(5);
    }

    public static double Contrast(string first, string second)
    {
        double a = Luminance(first), b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }

    public static string ReadableText(string background)
    {
        const string dark = "#171320", light = "#FCFAFF";
        var preferred = Contrast(background, dark) >= Contrast(background, light) ? dark : light;
        return Contrast(background, preferred) >= 4.5 ? preferred : Contrast(background, "#000000") >= Contrast(background, "#FFFFFF") ? "#000000" : "#FFFFFF";
    }

    public PaletteDefinition ForMode(bool dark) => dark ? this : this with
    {
        Background = Mix(Background, "#FFFFFF", .94),
        Surface = Mix(Surface, "#FFFFFF", .96),
        Accent = Mix(Primary, "#000000", .12),
        Glow = Mix(Glow, "#FFFFFF", .35)
    };
}
