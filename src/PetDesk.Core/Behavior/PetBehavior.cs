namespace PetDesk.Core.Behavior;

public sealed class PetBehavior
{
    private readonly Random _random;
    private double _remaining = 2;
    private double _velocityX, _velocityY;
    public PetState State { get; private set; } = PetState.Idle;
    public Position Position { get; private set; }
    public bool IsDragging { get; set; }
    public string Animation => State switch
    {
        PetState.WalkingLeft => "walkLeft",
        PetState.WalkingRight => "walkRight",
        PetState.Jumping => "jump",
        PetState.Waving => "wave",
        _ => "idle"
    };

    public PetBehavior(Random? random = null) => _random = random ?? new Random();
    public void Place(Position position) => Position = position;
    public void Reset()
    {
        State = PetState.Idle;
        _remaining = 2;
        _velocityX = _velocityY = 0;
    }

    public void React(PetState state)
    {
        State = state;
        _remaining = state == PetState.Jumping ? 1.05 : 1.2;
    }

    public void Tick(double dt, MovementMode mode, DesktopBounds area, double width, double height, double speed, double activity, DesktopEdge edge = DesktopEdge.Bottom)
    {
        if (IsDragging || !double.IsFinite(dt) || dt <= 0)
        {
            return;
        }

        // Discard long suspend/resume gaps: never jump across a desktop after sleep.
        dt = Math.Min(dt, .1);
        Position = area.Clamp(Position, width, height);
        if (mode == MovementMode.Fixed && State is PetState.WalkingLeft or PetState.WalkingRight)
        {
            Reset();
        }

        _remaining -= dt;
        if (_remaining <= 0)
        {
            if (State != PetState.Idle)
            {
                Reset();
                _remaining = 2 + _random.NextDouble() * 5;
            }
            else if (mode != MovementMode.Fixed && _random.NextDouble() > .25)
            {
                _velocityX = _random.Next(2) == 0 ? -1 : 1;
                _velocityY = mode == MovementMode.Free ? (_random.NextDouble() - .5) * .6 : 0;
                if (mode == MovementMode.Taskbar && edge is DesktopEdge.Left or DesktopEdge.Right)
                {
                    _velocityY = _velocityX;
                    _velocityX = 0;
                }

                State = _velocityX < 0 || (_velocityX == 0 && _velocityY < 0) ? PetState.WalkingLeft : PetState.WalkingRight;
                _remaining = 3 + _random.NextDouble() * 5;
            }
            else
            {
                if (_random.NextDouble() < Math.Clamp(activity, 0, 1))
                {
                    React(_random.Next(2) == 0 ? PetState.Waving : PetState.Jumping);
                }
                else
                {
                    _remaining = 2 + _random.NextDouble() * 5;
                }
            }
        }

        if (mode == MovementMode.Fixed)
        {
            return;
        }

        double x = Position.X, y = Position.Y;
        if (State is PetState.WalkingLeft or PetState.WalkingRight)
        {
            x += _velocityX * speed * dt;
            y += _velocityY * speed * dt;
        }

        if (mode == MovementMode.Taskbar)
        {
            // Approach the taskbar edge gradually, including left/top/right taskbars.
            var target = edge switch
            {
                DesktopEdge.Top => area.Top,
                DesktopEdge.Left => area.Left,
                DesktopEdge.Right => area.Left + Math.Max(0, area.Width - width),
                _ => area.Top + Math.Max(0, area.Height - height)
            };
            static double Approach(double current, double target, double amount) => current + Math.Clamp(target - current, -amount, amount);
            if (edge is DesktopEdge.Top or DesktopEdge.Bottom)
            {
                y = Approach(y, target, speed * dt);
            }
            else
            {
                x = Approach(x, target, speed * dt);
            }
        }

        var candidate = new Position(x, y);
        Position = area.Clamp(candidate, width, height);
        if (candidate != Position && State is PetState.WalkingLeft or PetState.WalkingRight)
        {
            Reset();
            _remaining = 1.5;
        }
    }
}
