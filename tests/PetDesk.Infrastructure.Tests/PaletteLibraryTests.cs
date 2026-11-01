using Microsoft.Extensions.Logging.Abstractions;
using PetDesk.Core.Appearance;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;
using PetDesk.Infrastructure.Configuration;
using Xunit;

namespace PetDesk.Infrastructure.Tests;

public class PaletteLibraryTests(CodexAtlasFixture fixture) : IClassFixture<CodexAtlasFixture>
{
    [Fact]
    public void CustomPaletteSurvivesRestartThenDeletesWithoutAffectingPresets()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(dir.Root);
        var store = new PaletteStore(paths);
        var palette = BuiltInPalettes.Default with
        {
            Id = "custom-mytheme",
            Name = "My theme",
            Accent = "#AA55FF"
        };
        store.Save(palette);
        Assert.Contains(palette, new PaletteStore(paths).Load());
        var settings = new SettingsStore(paths, NullLogger<SettingsStore>.Instance);
        settings.Save(new AppSettings { PaletteId = palette.Id, Language = "en" });
        Assert.Equal(palette.Id, settings.Load().PaletteId);
        Assert.Equal("en", settings.Load().Language);
        store.Delete(palette.Id);
        Assert.Equal(5, store.Load().Count);
        Assert.Empty(Directory.GetFiles(store.DirectoryPath));
    }

    [Fact]
    public void CorruptPaletteCannotReplaceBuiltInTheme()
    {
        using var dir = new TestDirectory();
        var store = new PaletteStore(new AppPaths(dir.Root));
        store.Load();
        File.WriteAllText(Path.Combine(store.DirectoryPath, "custom-broken.json"), "{bad");
        Assert.Equal(5, store.Load().Count);
        Assert.Equal(1, store.InvalidFiles);
        Assert.Throws<PetFormatException>(() => store.Save(BuiltInPalettes.Default));
        Assert.Throws<PetFormatException>(() => store.Delete("amethyst"));
        Assert.Equal(5, store.Load().Count);
    }

    [Theory]
    [InlineData("../../other")]
    [InlineData("custom-../other")]
    [InlineData("C:\\other")]
    public void PalettePathsStayInThemeFolder(string id)
    {
        using var dir = new TestDirectory();
        var store = new PaletteStore(new AppPaths(dir.Root));
        Assert.Throws<PetFormatException>(() => store.Save(BuiltInPalettes.Default with { Id = id }));
    }

    [Fact]
    public void DeletingImportedCopyPreservesSourceAndBuiltIns()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(Path.Combine(dir.Root, "data"));
        paths.EnsureDirectories();
        var library = new PetLibrary(paths, NullLogger<PetLibrary>.Instance);
        var source = fixture.ImagePath;
        var imported = library.Import(source);
        library.Delete(imported);
        Assert.True(File.Exists(source));
        Assert.Empty(library.List());
        var builtin = library.EnsureBuiltIn(Path.Combine(AppContext.BaseDirectory, "BuiltIn", "mochi-cat"), "builtin-mochi-cat");
        Assert.True(builtin.IsBuiltIn);
        Assert.Throws<PetFormatException>(() => library.Delete(builtin));
        Assert.Single(library.List());
        Assert.Equal(builtin.Id, library.EnsureBuiltIn(Path.Combine(AppContext.BaseDirectory, "BuiltIn", "mochi-cat"), "builtin-mochi-cat").Id);
    }

    [Theory]
    [InlineData("mochi-cat")]
    [InlineData("pixel-fox")]
    public void OriginalDemoAtlasesHaveVisibleIdleAndDirectionalMovement(string name)
    {
        var entry = PetLibrary.ReadManifest(Path.Combine(AppContext.BaseDirectory, "BuiltIn", name, "pet.json"));
        using var atlas = PetDesk.Rendering.SpriteAtlas.SpriteAtlasLoader.Load(entry.Manifest, entry.ImagePath);
        Assert.Equal(3, atlas.Animations.Count);
        Assert.Equal(4, atlas.Animations["idle"].Length);
        Assert.Equal(6, atlas.Animations["walkRight"].Length);
        Assert.All(atlas.Animations.Values.SelectMany(x => x), frame =>
        {
            Assert.Contains((byte)0, frame.Alpha);
            Assert.Contains(frame.Alpha, a => a == 255);
        });
        Assert.False(atlas.Animations["walkRight"][0].Alpha.SequenceEqual(atlas.Animations["walkRight"][1].Alpha));
    }
}
