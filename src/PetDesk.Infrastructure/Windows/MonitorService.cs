using System.Runtime.InteropServices;
using PetDesk.Core.Behavior;
using PetDesk.Core.Models;

namespace PetDesk.Infrastructure.Windows;

public sealed class MonitorService
{
    public IReadOnlyList<DesktopMonitor> Monitors { get; private set; } = [];

    public MonitorService() => Refresh();
    public void Refresh()
    {
        var monitors = new List<DesktopMonitor>();
        NativeMethods.EnumDisplayMonitors(0, 0, (handle, _, _, _) =>
        {
            var info = new NativeMethods.MonitorInfo
            {
                Size = Marshal.SizeOf<NativeMethods.MonitorInfo>(),
                Device = ""
            };
            if (!NativeMethods.GetMonitorInfo(handle, ref info))
            {
                return true;
            }

            double dpi = NativeMethods.GetDpiForMonitor(handle, 0, out var x, out _) == 0 ? x : 96;
            var work = new DesktopBounds(info.Work.Left, info.Work.Top, info.Work.Width, info.Work.Height);
            var full = new DesktopBounds(info.Monitor.Left, info.Monitor.Top, info.Monitor.Width, info.Monitor.Height);
            var gaps = new[]
            {
                info.Monitor.Bottom - info.Work.Bottom,
                info.Work.Left - info.Monitor.Left,
                info.Work.Top - info.Monitor.Top,
                info.Monitor.Right - info.Work.Right
            };
            var edge = (DesktopEdge)Array.IndexOf(gaps, gaps.Max());
            monitors.Add(new DesktopMonitor(info.Device, work, full, dpi, (info.Flags & 1) != 0, edge));
            return true;
        }, 0);
        if (monitors.Count == 0)
        {
            throw new InvalidOperationException("Error.NoMonitor");
        }

        Monitors = monitors;
    }

    public DesktopMonitor Primary => Monitors.FirstOrDefault(m => m.Primary) ?? Monitors[0];

    public DesktopMonitor ById(string? id) => Monitors.FirstOrDefault(m => m.Id == id) ?? Primary;
    public DesktopMonitor Nearest(Position point) => Monitors.MinBy(m =>
    {
        var x = Math.Clamp(point.X, m.Full.Left, m.Full.Left + m.Full.Width);
        var y = Math.Clamp(point.Y, m.Full.Top, m.Full.Top + m.Full.Height);
        return Math.Pow(x - point.X, 2) + Math.Pow(y - point.Y, 2);
    })!;
    public static SavedPosition Save(DesktopMonitor monitor, Position point) => new SavedPosition(monitor.Id, (point.X - monitor.Work.Left) / monitor.Scale, (point.Y - monitor.Work.Top) / monitor.Scale, monitor.Dpi);
    public static Position Restore(DesktopMonitor monitor, SavedPosition position) => new Position(monitor.Work.Left + position.OffsetX * monitor.Scale, monitor.Work.Top + position.OffsetY * monitor.Scale);
}
