using System.Text.Json;
using Microsoft.Extensions.Logging;
using PetDesk.Core.Models;

namespace PetDesk.Infrastructure.Configuration;

public sealed class SettingsStore(AppPaths paths, ILogger<SettingsStore> logger)
{
    public string? RecoveryNotice { get; private set; }

    public AppSettings Load()
    {
        if (!File.Exists(paths.Settings))
        {
            return new AppSettings();
        }

        try
        {
            return Read(paths.Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or PetFormatException)
        {
            logger.LogWarning(new EventId(101, "SettingsRecovery"), "Configuration could not be read ({ErrorType})", ex.GetType().Name);
            RecoveryNotice = "Status.RecoveredDefaults";
            try
            {
                File.Copy(paths.Settings, paths.Settings + $".corrupt-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}", false);
                if (File.Exists(paths.Settings + ".bak"))
                {
                    var backup = Read(paths.Settings + ".bak");
                    RecoveryNotice = "Status.Recovered";
                    return backup;
                }
            }
            catch (Exception recovery) when (recovery is IOException or UnauthorizedAccessException or JsonException or PetFormatException)
            {
            }

            return new AppSettings();
        }
    }

    private static AppSettings Read(string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024)
        {
            throw new PetFormatException("Error.SettingsLarge");
        }

        return (JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), PetJson.Options) ?? throw new JsonException()).Normalize();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(paths.Root);
        var temporary = paths.Settings + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, settings.Normalize(), PetJson.Options);
                stream.Flush(true);
            }

            if (File.Exists(paths.Settings))
            {
                File.Replace(temporary, paths.Settings, paths.Settings + ".bak");
            }
            else
            {
                File.Move(temporary, paths.Settings);
            }
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
