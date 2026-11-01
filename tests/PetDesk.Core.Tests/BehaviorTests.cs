using PetDesk.Core.Behavior;
using Xunit;

namespace PetDesk.Core.Tests;

public class BehaviorTests
{
    private static readonly DesktopBounds Area = new DesktopBounds(-1000, -200, 5000, 2000);
    private sealed class PredictableRandom : Random
    {
        public override double NextDouble() => .5;
        public override int Next(int maxValue) => 0;
    }

    private static PetBehavior Walking()
    {
        var pet = new PetBehavior(new PredictableRandom());
        pet.Place(new Position(2000, 500));
        for (var i = 0; i < 21; i++)
        {
            pet.Tick(.1, MovementMode.Free, Area, 192, 208, 100, 1);
        }

        Assert.Equal(PetState.WalkingLeft, pet.State);
        return pet;
    }

    [Fact]
    public void FsmWalksThenRests()
    {
        var pet = Walking();
        for (var i = 0; i < 60; i++)
        {
            pet.Tick(.1, MovementMode.Free, Area, 192, 208, 100, 1);
        }

        Assert.Equal(PetState.Idle, pet.State);
    }

    [Fact]
    public void MovementDependsOnElapsedTimeNotTickCount()
    {
        var first = Walking();
        var second = Walking();
        var start = first.Position;
        for (var i = 0; i < 20; i++)
        {
            first.Tick(.05, MovementMode.Free, Area, 192, 208, 100, 1);
        }

        for (var i = 0; i < 50; i++)
        {
            second.Tick(.02, MovementMode.Free, Area, 192, 208, 100, 1);
        }

        Assert.Equal(start.X - 100, first.Position.X, 6);
        Assert.Equal(first.Position.X, second.Position.X, 6);
        Assert.Equal(first.Position.Y, second.Position.Y, 6);
    }

    [Fact]
    public void FixedModeKeepsPositionEvenDuringJump()
    {
        var pet = new PetBehavior(new PredictableRandom());
        var start = new Position(100, 200);
        pet.Place(start);
        pet.React(PetState.Jumping);
        for (var i = 0; i < 100; i++)
        {
            pet.Tick(.1, MovementMode.Fixed, Area, 192, 208, 100, 1);
        }

        Assert.Equal(start, pet.Position);
    }

    [Fact]
    public void SwitchingToFixedStopsWalking()
    {
        var pet = Walking();
        var start = pet.Position;
        pet.Tick(.1, MovementMode.Fixed, Area, 192, 208, 100, 1);
        Assert.Equal(start, pet.Position);
        Assert.Equal(PetState.Idle, pet.State);
    }

    [Fact]
    public void DragSuspendsBehaviorAndMovement()
    {
        var pet = Walking();
        var start = pet.Position;
        pet.IsDragging = true;
        pet.Tick(.1, MovementMode.Free, Area, 192, 208, 100, 1);
        Assert.Equal(start, pet.Position);
    }

    [Fact]
    public void WorkAreaClampsBothAxesOnNegativeMonitor()
    {
        var area = new DesktopBounds(-1920, -100, 1920, 1080);
        Assert.Equal(new Position(-1920, -100), area.Clamp(new Position(-5000, -800), 192, 208));
        Assert.Equal(new Position(-192, 772), area.Clamp(new Position(10, 2000), 192, 208));
    }

    [Fact]
    public void ReachesBoundaryWithoutLeavingVisibleArea()
    {
        var pet = Walking();
        pet.Place(new Position(Area.Left + 1, Area.Top));
        pet.Tick(.1, MovementMode.Free, Area, 192, 208, 200, 1);
        Assert.Equal(Area.Left, pet.Position.X);
        Assert.Equal(PetState.Idle, pet.State);
    }

    [Theory]
    [InlineData(DesktopEdge.Bottom)]
    [InlineData(DesktopEdge.Left)]
    [InlineData(DesktopEdge.Top)]
    [InlineData(DesktopEdge.Right)]
    public void TaskbarApproachDoesNotTeleport(DesktopEdge edge)
    {
        var pet = new PetBehavior();
        var start = new Position(500, 500);
        pet.Place(start);
        pet.Tick(.1, MovementMode.Taskbar, new DesktopBounds(0, 0, 1920, 1080), 192, 208, 100, 0, edge);
        var distance = Math.Abs(start.X - pet.Position.X) + Math.Abs(start.Y - pet.Position.Y);
        Assert.InRange(distance, 0, 10.001);
    }

    [Fact]
    public void ResumeGapCannotTeleport()
    {
        var pet = Walking();
        var start = pet.Position;
        pet.Tick(3600, MovementMode.Free, Area, 192, 208, 100, 0);
        Assert.InRange(Math.Abs(start.X - pet.Position.X), 0, 10.001);
    }
}
