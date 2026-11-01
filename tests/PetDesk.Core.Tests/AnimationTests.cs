using PetDesk.Core.Animation;
using PetDesk.Core.Models;
using Xunit;

namespace PetDesk.Core.Tests;

public class AnimationTests
{
    private static PetManifest Pet() => KnownPetAdapter.Detect(1536, 2288);
    [Fact]
    public void ComputesFrameCoordinatesIncludingOffset()
    {
        var pet = Pet();
        Assert.Equal(new FrameRect(7 * 192, 2 * 208, 192, 208), ManifestValidator.GetFrame(pet, "walkLeft", 7));
        Assert.Equal(new FrameRect(7 * 192, 10 * 208, 192, 208), ManifestValidator.GetFrame(pet, "look337.5", 0));
    }

    [Fact]
    public void RejectsFramePastAnimationEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => ManifestValidator.GetFrame(Pet(), "idle", 6));
    [Theory]
    [InlineData(1536, 1872, 9)]
    [InlineData(1536, 2288, 11)]
    public void DetectsKnownGrids(int width, int height, int rows)
    {
        var pet = KnownPetAdapter.Detect(width, height);
        ManifestValidator.Validate(pet, width, height);
        Assert.Equal(rows, pet.Rows);
    }

    [Theory]
    [InlineData(1536, 2000)]
    [InlineData(128, 128)]
    [InlineData(2288, 1536)]
    public void DoesNotGuessUnknownGrids(int width, int height) => Assert.Throws<PetFormatException>(() => KnownPetAdapter.Detect(width, height));
    [Fact]
    public void MissingAnimationFallsBackToIdle()
    {
        var pet = Pet() with
        {
            Animations = new Dictionary<string, AnimationDefinition>
            {
                ["idle"] = new AnimationDefinition
                {
                    Frames = 2
                }
            }
        };
        var controller = new PetAnimationController(pet);
        Assert.Equal("idle", controller.Resolve("wave"));
        Assert.Equal("idle", controller.Resolve("unknown"));
        Assert.Same(pet.Animations["idle"], controller.Definition("walkRight"));
    }

    [Fact]
    public void VariableDurationsLoopAndResetWithoutIntermediateFrame()
    {
        var player = new AnimationPlayer();
        var pet = Pet();
        player.Play("idle", pet.Animations["idle"]);
        player.Advance(.281);
        Assert.Equal(1, player.Frame);
        player.Advance(.11);
        Assert.Equal(2, player.Frame);
        player.Advance(2000 * pet.Animations["idle"].TotalDuration);
        Assert.Equal(2, player.Frame);
        player.Play("wave", pet.Animations["wave"]);
        Assert.Equal(0, player.Frame);
        Assert.False(player.Completed);
    }

    [Fact]
    public void OneShotFinishesAndHoldsLastFrame()
    {
        var player = new AnimationPlayer();
        var wave = Pet().Animations["wave"];
        player.Play("wave", wave);
        player.Advance(4);
        Assert.True(player.Completed);
        Assert.Equal(3, player.Frame);
        player.Advance(1);
        Assert.Equal(3, player.Frame);
        player.Play("wave", wave, true);
        Assert.Equal(0, player.Frame);
        Assert.False(player.Completed);
    }

    [Fact]
    public void InvalidDeltaCannotCorruptPlayback()
    {
        var player = new AnimationPlayer();
        player.Play("idle", Pet().Animations["idle"]);
        player.Advance(double.NaN);
        player.Advance(double.PositiveInfinity);
        player.Advance(-5);
        Assert.Equal(0, player.Frame);
    }

    [Fact]
    public void RepeatedPlayDoesNotResetCurrentAnimation()
    {
        var player = new AnimationPlayer();
        var idle = Pet().Animations["idle"];
        player.Play("idle", idle);
        player.Advance(.5);
        player.Play("idle", idle);
        Assert.Equal(3, player.Frame);
    }
}
