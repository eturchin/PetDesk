using System.Text.Json;
using PetDesk.Core.Models;
using Xunit;

namespace PetDesk.Core.Tests;

public class ManifestTests
{
    [Theory]
    [InlineData(0, 208, 8, 11)]
    [InlineData(192, -1, 8, 11)]
    [InlineData(192, 208, 9, 11)]
    [InlineData(192, 208, 8, 10)]
    public void RejectsInvalidGrid(int width, int height, int columns, int rows)
    {
        var pet = KnownPetAdapter.Detect(1536, 2288) with
        {
            FrameWidth = width,
            FrameHeight = height,
            Columns = columns,
            Rows = rows
        };
        Assert.Throws<PetFormatException>(() => ManifestValidator.Validate(pet, 1536, 2288));
    }

    [Theory]
    [InlineData(-1, 0, 1, 8)]
    [InlineData(11, 0, 1, 8)]
    [InlineData(0, 7, 2, 8)]
    [InlineData(0, 0, 0, 8)]
    [InlineData(0, 0, 1, 0)]
    [InlineData(0, 0, 1, 61)]
    public void RejectsAnimationOutsideAtlas(int row, int column, int frames, double fps)
    {
        var pet = KnownPetAdapter.Detect(1536, 2288) with
        {
            Animations = new Dictionary<string, AnimationDefinition>
            {
                ["idle"] = new AnimationDefinition
                {
                    Row = row,
                    StartColumn = column,
                    Frames = frames,
                    Fps = fps
                }
            }
        };
        Assert.Throws<PetFormatException>(() => ManifestValidator.Validate(pet, 1536, 2288));
    }

    [Fact]
    public void RequiresIdleAndKnownSchema()
    {
        var pet = KnownPetAdapter.Detect(1536, 2288);
        Assert.Throws<PetFormatException>(() => ManifestValidator.Validate(pet with { SchemaVersion = 2 }, 1536, 2288));
        Assert.Throws<PetFormatException>(() => ManifestValidator.Validate(pet with { Animations = [] }, 1536, 2288));
    }

    [Fact]
    public void AllowsCustomAnimationNamesWithoutUsingThemForBehavior()
    {
        var pet = KnownPetAdapter.Detect(1536, 2288);
        pet.Animations["customPose"] = new AnimationDefinition
        {
            Row = 3,
            Frames = 4
        };
        ManifestValidator.Validate(pet, 1536, 2288);
    }

    [Fact]
    public void RejectsInvalidDurations()
    {
        var pet = KnownPetAdapter.Detect(1536, 2288);
        pet.Animations["idle"] = pet.Animations["idle"] with
        {
            FrameDurationsMs = [140, 140]
        };
        Assert.Throws<PetFormatException>(() => ManifestValidator.Validate(pet, 1536, 2288));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("")]
    [InlineData("C:\\pets")]
    [InlineData("pet with spaces")]
    public void RejectsUnsafeId(string id)
    {
        var pet = KnownPetAdapter.Detect(1536, 2288) with
        {
            Id = id
        };
        Assert.Throws<PetFormatException>(() => ManifestValidator.Validate(pet, 1536, 2288));
    }

    [Fact]
    public void JsonRoundTripPreservesFrameTiming()
    {
        var pet = KnownPetAdapter.Detect(1536, 2288);
        var restored = JsonSerializer.Deserialize<PetManifest>(JsonSerializer.Serialize(pet, PetJson.Options), PetJson.Options)!;
        ManifestValidator.Validate(restored, 1536, 2288);
        Assert.Equal(pet.Animations["idle"].FrameDurationsMs, restored.Animations["idle"].FrameDurationsMs);
    }
}
