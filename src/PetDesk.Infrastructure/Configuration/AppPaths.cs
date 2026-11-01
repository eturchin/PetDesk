namespace PetDesk.Infrastructure.Configuration;

public sealed class AppPaths(string? root = null)
{
    public string Root { get; } = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PetDesk")));
    public string Pets => Path.Combine(Root, "Pets");
    public string Logs => Path.Combine(Root, "Logs");
    public string Settings => Path.Combine(Root, "settings.json");

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Pets);
        Directory.CreateDirectory(Logs);
    }
}
