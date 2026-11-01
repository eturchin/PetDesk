using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PetDesk.Core.Behavior;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Infrastructure.Windows;
using Xunit;

namespace PetDesk.Infrastructure.Tests;

public sealed class TestDirectory : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "PetDesk.Tests", Guid.NewGuid().ToString("N"));

    public TestDirectory() => Directory.CreateDirectory(Root);
    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, true);
        }
    }
}
