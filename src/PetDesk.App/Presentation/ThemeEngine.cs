using PetDesk.Core.Appearance;

namespace PetDesk.App.Presentation;

public static class ThemeEngine
{
    public static ResourceDictionary Create(PaletteDefinition palette, bool dark)
    {
        var colors = palette.Validate().ForMode(dark);
        var resources = new ResourceDictionary();
        string text = PaletteDefinition.ReadableText(colors.Background);
        string cardText = PaletteDefinition.ReadableText(colors.Surface);

        AddBrush(resources, "PageBrush", colors.Background, 250);
        AddBrush(resources, "PanelBrush", colors.Surface, 240);
        AddBrush(resources, "SideBrush", PaletteDefinition.Mix(colors.Background, colors.Surface, .5));
        AddBrush(resources, "TextBrush", text);
        AddBrush(resources, "MutedBrush", MutedText(text, colors.Background));
        AddBrush(resources, "CardTextBrush", cardText);
        AddBrush(resources, "CardMutedBrush", MutedText(cardText, colors.Surface));
        AddBrush(resources, "LineBrush", PaletteDefinition.Mix(colors.Surface, colors.Accent, dark ? .35 : .2));

        AddBrush(resources, "AccentBrush", colors.Accent);
        AddBrush(resources, "PrimaryBrush", colors.Primary);
        AddBrush(resources, "GlowBrush", colors.Glow);
        AddBrush(resources, "AccentTextBrush", PaletteDefinition.ReadableText(colors.Accent));
        AddBrush(resources, "TrackBrush", PaletteDefinition.Mix(colors.Surface, cardText, .18));
        AddBrush(resources, "PreviewBrush", PaletteDefinition.Mix(colors.Background, colors.Primary, .2));
        AddBrush(resources, "HoverBrush", colors.Accent, 28);
        AddBrush(resources, "SelectedBrush", PaletteDefinition.Mix(colors.Primary, colors.Surface, dark ? .25 : .83));
        AddBrush(resources, "ActiveBrush", dark ? "#59EC93" : "#167843");
        AddBrush(resources, "ErrorBrush", dark ? "#FFAAC2" : "#A02C53");

        resources["AccentColor"] = ParseColor(colors.Accent);
        resources["GlowColor"] = ParseColor(colors.Glow);
        resources["PanelGradient"] = CreateGradient(
            PaletteDefinition.Mix(colors.Background, colors.Primary, .16), colors.Background, 55);
        resources["CardGradient"] = CreateGradient(
            PaletteDefinition.Mix(colors.Surface, colors.Primary, .1), colors.Surface, 70, .94);
        resources["RingBrush"] = CreateGradient(colors.Glow, colors.Accent, 35);
        resources["MotionDuration"] = new Duration(
            TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? 160 : 0));
        return resources;
    }

    public static void Apply(PaletteDefinition palette, bool dark)
    {
        var resources = Create(palette, dark);
        foreach (var key in resources.Keys)
        {
            Application.Current.Resources[key] = resources[key];
        }
    }

    private static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    private static void AddBrush(ResourceDictionary resources, string key, string hex, byte alpha = 255)
    {
        var color = ParseColor(hex);
        color.A = alpha;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        resources[key] = brush;
    }

    private static LinearGradientBrush CreateGradient(string start, string end, double angle, double opacity = 1)
    {
        var brush = new LinearGradientBrush(ParseColor(start), ParseColor(end), angle) { Opacity = opacity };
        brush.Freeze();
        return brush;
    }

    private static string MutedText(string foreground, string background)
    {
        string candidate = PaletteDefinition.Mix(foreground, background, .3);
        return PaletteDefinition.Contrast(candidate, background) >= 4.5 ? candidate : foreground;
    }
}
