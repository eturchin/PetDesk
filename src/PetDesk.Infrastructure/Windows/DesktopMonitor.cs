using System.Runtime.InteropServices;
using PetDesk.Core.Behavior;
using PetDesk.Core.Models;

namespace PetDesk.Infrastructure.Windows;

public sealed record DesktopMonitor(string Id, DesktopBounds Work, DesktopBounds Full, double Dpi, bool Primary, DesktopEdge Edge)
{
    public double Scale => Dpi / 96d;
}
