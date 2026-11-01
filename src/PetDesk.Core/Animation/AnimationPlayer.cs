using PetDesk.Core.Models;

namespace PetDesk.Core.Animation;

public sealed class AnimationPlayer
{
    private AnimationDefinition? _definition;
    private double _elapsed;
    public int Frame { get; private set; }
    public bool Completed { get; private set; }
    public string Name { get; private set; } = "";

    public double SecondsUntilNextFrame
    {
        get
        {
            if (_definition is null || Completed)
            {
                return double.PositiveInfinity;
            }

            double start = 0;
            for (var i = 0; i < Frame; i++)
            {
                start += _definition.Duration(i);
            }

            return Math.Max(.001, start + _definition.Duration(Frame) - _elapsed);
        }
    }

    public void Play(string name, AnimationDefinition definition, bool restart = false)
    {
        if (!restart && Name == name && ReferenceEquals(_definition, definition))
        {
            return;
        }

        Name = name;
        _definition = definition;
        _elapsed = 0;
        Frame = 0;
        Completed = false;
    }

    public void Advance(double deltaSeconds)
    {
        if (_definition is null || Completed || !double.IsFinite(deltaSeconds) || deltaSeconds <= 0)
        {
            return;
        }

        _elapsed += deltaSeconds;
        if (_definition.Loop)
        {
            _elapsed %= _definition.TotalDuration;
        }
        else if (_elapsed >= _definition.TotalDuration)
        {
            Frame = _definition.Frames - 1;
            Completed = true;
            return;
        }

        var remaining = _elapsed;
        Frame = 0;
        while (Frame < _definition.Frames - 1 && remaining + 1e-9 >= _definition.Duration(Frame))
            remaining -= _definition.Duration(Frame++);
    }
}
