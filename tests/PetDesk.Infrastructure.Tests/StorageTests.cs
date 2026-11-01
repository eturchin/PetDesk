using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PetDesk.Core.Behavior;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Infrastructure.Windows;
using Xunit;

namespace PetDesk.Infrastructure.Tests;

public class StorageTests
{
    [Theory]
    [InlineData(-1920, -100)]
    [InlineData(2500, 1300)]
    public void NativeMessageCoordinatesPreserveSignedScreenPosition(int x, int y)
    {
        var message = (ushort)x | ((long)(ushort)y << 16);
        var point = NativeMethods.UnpackCoordinates(message);
        Assert.Equal(x, point.X);
        Assert.Equal(y, point.Y);
    }

    [Fact]
    public void SavesSettingsAtomicallyAndRoundTripsAllUserChoices()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(dir.Root);
        paths.EnsureDirectories();
        var store = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
        var settings = new AppSettings
        {
            Scale = 1.6,
            Opacity = .55,
            Movement = MovementMode.Fixed,
            Speed = 95,
            Theme = "Dark",
            Position = new SavedPosition("DISPLAY2", 24, 30, 144)
        };
        store.Save(settings);
        Assert.Equal(settings, store.Load());
        store.Save(settings with { Scale = .8 });
        Assert.True(File.Exists(paths.Settings + ".bak"));
        Assert.Empty(Directory.GetFiles(dir.Root, "*.tmp-*"));
        Assert.Equal(1.6, JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(paths.Settings + ".bak"), PetJson.Options)!.Scale);
    }

    [Fact]
    public void CorruptSettingsRecoverFromLastBackup()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(dir.Root);
        var store = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
        store.Save(new AppSettings { Scale = .8 });
        store.Save(new AppSettings { Scale = 1.4 });
        File.WriteAllText(paths.Settings, "{broken");
        Assert.Equal(.8, store.Load().Scale);
        Assert.NotNull(store.RecoveryNotice);
        Assert.Single(Directory.GetFiles(dir.Root, "*.corrupt-*"));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":99}")]
    public void CorruptOrUnsupportedSettingsUseDefaults(string content)
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(dir.Root);
        File.WriteAllText(paths.Settings, content);
        var store = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
        Assert.Equal(new AppSettings(), store.Load());
        Assert.NotNull(store.RecoveryNotice);
    }

    [Fact]
    public void SaveFailureLeavesNoTemporaryFiles()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(dir.Root);
        Directory.CreateDirectory(paths.Settings);
        var store = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
        Assert.ThrowsAny<IOException>(() => store.Save(new AppSettings()));
        Assert.Empty(Directory.GetFiles(dir.Root, "*.tmp-*"));
    }

    [Fact]
    public void NormalizesOutOfRangeSettings()
    {
        var normalized = new AppSettings
        {
            Scale = 900,
            Opacity = .01,
            Speed = double.NaN,
            Movement = (MovementMode)99,
            Position = new SavedPosition("display", double.NaN, 0)
        }.Normalize();
        Assert.Equal(2, normalized.Scale);
        Assert.Equal(.3, normalized.Opacity);
        Assert.Equal(75, normalized.Speed);
        Assert.Null(normalized.Position);
        Assert.Equal(MovementMode.Taskbar, normalized.Movement);
    }

    [Theory]
    [InlineData("../escape.png")]
    [InlineData("..\\escape.png")]
    [InlineData("C:\\outside.png")]
    [InlineData("\\\\server\\pet.png")]
    [InlineData("spritesheet.png:stream")]
    [InlineData("/root.png")]
    [InlineData("")]
    public void PreventsManifestPathEscape(string relative)
    {
        using var dir = new TestDirectory();
        Assert.Throws<PetFormatException>(() => SafePetPath.Resolve(dir.Root, relative));
    }

    [Fact]
    public void AllowsOrdinarySubdirectoryWithinPet()
    {
        using var dir = new TestDirectory();
        Assert.Equal(Path.Combine(dir.Root, "images", "sheet.png"), SafePetPath.Resolve(dir.Root, "images/sheet.png"));
    }

    [Fact]
    public void AutostartQuotesSpacesAndNeverNeedsAdminKey()
    {
        Assert.Equal("\"C:\\My Pets\\PetDesk.exe\" --startup", AutostartService.BuildCommand(@"C:\My Pets\PetDesk.exe"));
        Assert.Throws<ArgumentException>(() => AutostartService.BuildCommand("relative.exe"));
        Assert.Throws<ArgumentException>(() => AutostartService.BuildCommand("C:\\pet\".exe"));
    }

    [Fact]
    public void PositionRestoresAcrossDpiChangeAndNegativeOrigin()
    {
        var monitor = new DesktopMonitor("secondary", new DesktopBounds(-1920, -200, 1920, 1040), new DesktopBounds(-1920, -200, 1920, 1080), 144, false, DesktopEdge.Bottom);
        var saved = MonitorService.Save(monitor, new Position(-1770, -50));
        Assert.Equal(100, saved.OffsetX);
        Assert.Equal(100, saved.OffsetY);
        var changed = monitor with
        {
            Dpi = 192,
            Work = new DesktopBounds(-2560, -200, 2560, 1400)
        };
        Assert.Equal(new Position(-2360, 0), MonitorService.Restore(changed, saved));
    }
}
