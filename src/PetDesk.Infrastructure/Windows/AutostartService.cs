using Microsoft.Win32;

namespace PetDesk.Infrastructure.Windows;

public sealed class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static string BuildCommand(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || executable.Contains('"') || executable.Any(char.IsControl) || !executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Error.StartupPath");
        }

        var command = $"\"{executable}\" --startup";
        if (command.Length > 260)
        {
            throw new ArgumentException("Error.StartupLong");
        }

        return command;
    }

    public bool IsEnabled(string executable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return string.Equals(key?.GetValue("PetDesk") as string, BuildCommand(executable), StringComparison.OrdinalIgnoreCase);
    }

    public void RemoveIfOwned(string executable)
    {
        if (IsEnabled(executable))
        {
            SetEnabled(false, executable);
        }
    }

    public void SetEnabled(bool enabled, string executable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true) ?? throw new UnauthorizedAccessException("Error.StartupAccess");
        if (enabled)
        {
            key.SetValue("PetDesk", BuildCommand(executable), RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue("PetDesk", false);
        }

        if (IsEnabled(executable) != enabled)
        {
            throw new IOException("Error.StartupVerify");
        }
    }
}
