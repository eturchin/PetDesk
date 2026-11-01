using PetDesk.Core.Behavior;

namespace PetDesk.Core.Models;

public static class PopupPlacement
{
    public static PopupRectangle Calculate(DesktopBounds work, Position anchor, double desiredWidth, double desiredHeight, double dpi, DesktopEdge edge)
    {
        var gap = 8 * dpi / 96;
        var width = Math.Min(desiredWidth * dpi / 96, Math.Max(1, work.Width - gap * 2));
        var height = Math.Min(desiredHeight * dpi / 96, Math.Max(1, work.Height - gap * 2));
        double x = anchor.X - width + gap, y = anchor.Y - height - gap;
        if (edge == DesktopEdge.Top)
        {
            y = work.Top + gap;
        }

        if (edge == DesktopEdge.Left)
        {
            x = work.Left + gap;
        }

        if (edge == DesktopEdge.Right)
        {
            x = work.Left + work.Width - width - gap;
        }

        var point = work.Clamp(new Position(x, y), width + gap, height + gap);
        return new PopupRectangle(Math.Max(work.Left + gap, point.X), Math.Max(work.Top + gap, point.Y), width, height);
    }
}
