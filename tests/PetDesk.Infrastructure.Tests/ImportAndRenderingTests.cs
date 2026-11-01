using Microsoft.Extensions.Logging.Abstractions;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Rendering.SpriteAtlas;
using Xunit;

namespace PetDesk.Infrastructure.Tests;

public class ImportAndRenderingTests(CodexAtlasFixture fixture) : IClassFixture<CodexAtlasFixture>
{
    private string AtlasImage => fixture.ImagePath;

    [Fact]
    public void CodexV2AtlasDecodesAllNineStatesAndSixteenLooks()
    {
        var dimensions = ImageHeaderReader.Read(AtlasImage);
        Assert.Equal((1536, 2288), dimensions);
        var manifest = KnownPetAdapter.Detect(dimensions.Width, dimensions.Height, "Test companion");
        using var atlas = SpriteAtlasLoader.Load(manifest, AtlasImage);
        Assert.Equal(25, atlas.Animations.Count);
        Assert.Equal(6, atlas.Animations["idle"].Length);
        Assert.Equal(8, atlas.Animations["walkLeft"].Length);
        Assert.Equal(5, atlas.Animations["jump"].Length);
        var first = atlas.Animations["idle"][0];
        Assert.False(first.Hit(0, 0));
        Assert.True(first.Hit(.5, .5));
        Assert.False(first.Hit(1, .5));
        Assert.Equal(192, first.Image.PixelWidth);
        Assert.Equal(208, first.Image.PixelHeight);
        Assert.True(first.Image.IsFrozen);
        Assert.False(first.Alpha.SequenceEqual(atlas.Animations["idle"][1].Alpha));
    }

    [Fact]
    public void ImportCopiesSourceAndPublishesOnlyValidatedLibraryEntry()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(dir.Root);
        paths.EnsureDirectories();
        var library = new PetLibrary(paths, NullLogger<PetLibrary>.Instance);
        var originalBytes = File.ReadAllBytes(AtlasImage);
        var entry = library.Import(AtlasImage, "Test companion");
        Assert.Single(library.List());
        Assert.Equal("Test companion", entry.Name);
        Assert.Equal(originalBytes, File.ReadAllBytes(AtlasImage));
        Assert.Equal(originalBytes, File.ReadAllBytes(entry.ImagePath));
        Assert.NotEqual(Path.GetDirectoryName(AtlasImage), entry.Directory);
        Assert.Equal(entry.Id, PetLibrary.ReadManifest(Path.Combine(entry.Directory, "pet.json")).Id);
        Assert.Empty(Directory.GetDirectories(paths.Pets, ".import-*"));
    }

    [Fact]
    public void ImportFolderSupportsCustomManifestAndAnimationNames()
    {
        using var dir = new TestDirectory();
        var source = Path.Combine(dir.Root, "source");
        Directory.CreateDirectory(source);
        File.Copy(AtlasImage, Path.Combine(source, "sheet.png"));
        var manifest = KnownPetAdapter.Detect(1536, 2288, "Custom", "sheet.png");
        manifest.Animations["customPose"] = new AnimationDefinition
        {
            Row = 0,
            Frames = 1
        };
        File.WriteAllText(Path.Combine(source, "pet.json"), System.Text.Json.JsonSerializer.Serialize(manifest, PetJson.Options));
        var paths = new AppPaths(Path.Combine(dir.Root, "data"));
        paths.EnsureDirectories();
        var library = new PetLibrary(paths, NullLogger<PetLibrary>.Instance);
        var entry = library.Import(source);
        Assert.Equal("Custom", entry.Name);
        Assert.Contains("customPose", entry.Manifest.Animations.Keys);
    }

    [Fact]
    public void InvalidImportDoesNotChangeExistingLibrary()
    {
        using var dir = new TestDirectory();
        var paths = new AppPaths(Path.Combine(dir.Root, "data"));
        paths.EnsureDirectories();
        var invalid = Path.Combine(dir.Root, "broken.png");
        File.WriteAllText(invalid, "this is not a PNG");
        var library = new PetLibrary(paths, NullLogger<PetLibrary>.Instance);
        Assert.Throws<PetFormatException>(() => library.Import(invalid));
        Assert.Empty(library.List());
        Assert.Empty(Directory.GetDirectories(paths.Pets));
    }

    [Fact]
    public void DamagedPayloadWithValidDimensionsFailsDecodeAndCleansStaging()
    {
        using var dir = new TestDirectory();
        var broken = Path.Combine(dir.Root, "broken.png");
        File.WriteAllBytes(broken, File.ReadAllBytes(AtlasImage)[..40]);
        var paths = new AppPaths(Path.Combine(dir.Root, "data"));
        paths.EnsureDirectories();
        var library = new PetLibrary(paths, NullLogger<PetLibrary>.Instance);
        var error = Record.Exception(() => library.Import(broken));
        Assert.NotNull(error);
        Assert.True(PetLibrary.IsImportError(error));
        Assert.Empty(Directory.GetDirectories(paths.Pets));
    }

    [Fact]
    public void LockedSpritesheetReturnsIoError()
    {
        using var dir = new TestDirectory();
        var locked = Path.Combine(dir.Root, "locked.png");
        File.Copy(AtlasImage, locked);
        using var stream = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None);
        Assert.Throws<IOException>(() => ImageHeaderReader.Read(locked));
    }

    [Fact]
    public void HeaderRejectsDecompressionBombBeforeWicDecode()
    {
        using var dir = new TestDirectory();
        var bomb = Path.Combine(dir.Root, "bomb.png");
        var bytes = File.ReadAllBytes(AtlasImage)[..40];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), 20000);
        File.WriteAllBytes(bomb, bytes);
        Assert.Throws<PetFormatException>(() => ImageHeaderReader.Read(bomb));
    }
}
