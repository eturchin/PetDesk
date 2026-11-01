using PetDesk.Infrastructure.Windows;
using Xunit;
using Xunit.Abstractions;

namespace PetDesk.Infrastructure.Tests;

public class MonitorIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public void WindowsExposesValidMonitorWorkAreasAndDpi()
    {
        var service = new MonitorService();
        Assert.NotEmpty(service.Monitors);
        Assert.Contains(service.Primary, service.Monitors);
        output.WriteLine($"Monitor count: {service.Monitors.Count}");
        foreach (var monitor in service.Monitors)
        {
            Assert.InRange(monitor.Dpi, 48, 768);
            Assert.True(monitor.Work.Width > 0 && monitor.Work.Height > 0);
            Assert.True(monitor.Work.Left >= monitor.Full.Left && monitor.Work.Top >= monitor.Full.Top);
            Assert.True(monitor.Work.Left + monitor.Work.Width <= monitor.Full.Left + monitor.Full.Width);
            output.WriteLine($"Full={monitor.Full}; Work={monitor.Work}; DPI={monitor.Dpi}; Edge={monitor.Edge}");
        }
    }
}
