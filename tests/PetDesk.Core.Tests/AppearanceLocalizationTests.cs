using System.Text.Json;
using PetDesk.Core.Appearance;
using PetDesk.Core.Behavior;
using PetDesk.Core.Localization;
using PetDesk.Core.Models;
using Xunit;

namespace PetDesk.Core.Tests;

public class AppearanceLocalizationTests
{
    [Fact]
    public void AutomaticTextMaintainsContrastEvenOnMidGrayCustomSurfaces()
    {
        for (var value = 0; value < 256; value++)
        {
            var color = $"#{value:X2}{value:X2}{value:X2}";
            Assert.True(PaletteDefinition.Contrast(color, PaletteDefinition.ReadableText(color)) >= 4.5);
        }
    }

    [Fact]
    public void AllFivePresetsValidateAndHaveDistinctSemanticColors()
    {
        Assert.Equal(new[] { "amethyst", "ocean", "emerald", "rose", "amber" }, BuiltInPalettes.All.Select(p => p.Id));
        foreach (var palette in BuiltInPalettes.All)
        {
            Assert.Equal(palette, palette.Validate());
            Assert.NotEqual(palette.Background, palette.Surface);
            Assert.NotEqual(palette.Primary, palette.Accent);
        }

        Assert.Equal("amethyst", BuiltInPalettes.Default.Id);
    }

    [Theory]
    [InlineData("#00aaFF", true)]
    [InlineData("#123456", true)]
    [InlineData("#123", false)]
    [InlineData("#XX1122", false)]
    [InlineData("112233", false)]
    [InlineData("#11223344", false)]
    public void HexValidationRejectsUnsupportedForms(string value, bool expected) => Assert.Equal(expected, PaletteDefinition.IsHex(value));
    [Fact]
    public void LightVariantChangesSurfacesWithoutDestroyingPreset()
    {
        var dark = BuiltInPalettes.Default;
        var light = dark.ForMode(false);
        Assert.NotEqual(dark.Background, light.Background);
        Assert.NotEqual(dark.Surface, light.Surface);
        Assert.Equal(dark, dark.ForMode(true));
        Assert.Equal(dark.Id, light.Id);
    }

    [Fact]
    public void BothLanguagesHaveEveryKeyAndFallbackIsSafe()
    {
        var ru = LocalizationService.Catalog("ru");
        var en = LocalizationService.Catalog("en");
        Assert.True(ru.Count > 100);
        Assert.Equal(ru.Keys.Order(), en.Keys.Order());
        Assert.All(en.Values, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        Assert.Equal(ru["Dashboard.Settings"], LocalizationService.Translate("Dashboard.Settings", "unknown"));
        Assert.Equal("unknown-key", LocalizationService.Translate("unknown-key", "en"));
    }

    [Fact]
    public void LanguageChangesNotifyLiveBindings()
    {
        var service = new LocalizationService();
        var changes = 0;
        service.PropertyChanged += (_, _) => changes++;
        service.SetLanguage("en");
        Assert.Equal("Settings", service["Dashboard.Settings"]);
        Assert.True(changes > 0);
        service.SetLanguage("bad");
        Assert.Equal("ru", service.Language);
    }

    [Fact]
    public void ExistingSchemaOneSettingsKeepPetBehaviorAndGainDefaults()
    {
        const string old = "{\"schemaVersion\":1,\"selectedPetId\":\"pet-existing\",\"scale\":1.5,\"opacity\":0.6,\"movement\":\"Fixed\",\"speed\":95,\"theme\":\"System\"}";
        var settings = JsonSerializer.Deserialize<AppSettings>(old, PetJson.Options)!.Normalize();
        Assert.Equal("pet-existing", settings.SelectedPetId);
        Assert.Equal(1.5, settings.Scale);
        Assert.Equal(.6, settings.Opacity);
        Assert.Equal(95, settings.Speed);
        Assert.Equal(MovementMode.Fixed, settings.Movement);
        Assert.Equal("System", settings.Theme);
        Assert.Equal("ru", settings.Language);
        Assert.Equal("amethyst", settings.PaletteId);
        Assert.True(settings.RestorePosition);
    }

    [Theory]
    [InlineData(DesktopEdge.Bottom)]
    [InlineData(DesktopEdge.Top)]
    [InlineData(DesktopEdge.Left)]
    [InlineData(DesktopEdge.Right)]
    public void PopupFitsNegativeMonitorAtHighDpi(DesktopEdge edge)
    {
        var area = new DesktopBounds(-1920, 200, 1920, 1032);
        var rectangle = PopupPlacement.Calculate(area, new Position(-50, 1200), 512, 708, 192, edge);
        Assert.True(rectangle.X >= area.Left);
        Assert.True(rectangle.Y >= area.Top);
        Assert.True(rectangle.X + rectangle.Width <= area.Left + area.Width);
        Assert.True(rectangle.Y + rectangle.Height <= area.Top + area.Height);
    }
}
