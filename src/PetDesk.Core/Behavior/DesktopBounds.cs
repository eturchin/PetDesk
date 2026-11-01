namespace PetDesk.Core.Behavior;

public readonly record struct DesktopBounds(double Left, double Top, double Width, double Height)
{
    public Position Clamp(Position position, double width, double height) => new Position(Math.Clamp(position.X, Left, Left + Math.Max(0, Width - width)), Math.Clamp(position.Y, Top, Top + Math.Max(0, Height - height)));
}
