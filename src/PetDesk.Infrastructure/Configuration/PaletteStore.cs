using System.Text.Json;
using PetDesk.Core.Appearance;
using PetDesk.Core.Models;

namespace PetDesk.Infrastructure.Configuration;

public sealed class PaletteStore(AppPaths paths)
{
    public int InvalidFiles { get; private set; }
    public string DirectoryPath => Path.Combine(paths.Root, "Themes");

    public IReadOnlyList<PaletteDefinition> Load()
    {
        Directory.CreateDirectory(DirectoryPath);
        InvalidFiles = 0;
        var palettes = BuiltInPalettes.All.ToList();
        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "custom-*.json").Take(100))
        {
            try
            {
                Assets.SafePetPath.Resolve(DirectoryPath, Path.GetFileName(file));
                if (new FileInfo(file).Length > 16384)
                {
                    throw new PetFormatException("Error.PaletteInvalid");
                }

                var palette = (JsonSerializer.Deserialize<PaletteDefinition>(File.ReadAllText(file), PetJson.Options) ?? throw new JsonException()).Validate();
                if (!palette.Id.StartsWith("custom-", StringComparison.Ordinal) || Path.GetFileNameWithoutExtension(file) != palette.Id || palettes.Any(p => p.Id == palette.Id))
                {
                    throw new PetFormatException("Error.PaletteInvalid");
                }

                palettes.Add(palette);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or PetFormatException or ArgumentException)
            {
                InvalidFiles++;
            }
        }

        return palettes;
    }

    public PaletteDefinition Save(PaletteDefinition palette)
    {
        palette = palette.Validate();
        if (!palette.Id.StartsWith("custom-", StringComparison.Ordinal) || BuiltInPalettes.IsBuiltIn(palette.Id))
        {
            throw new PetFormatException("Error.PaletteProtected");
        }

        Directory.CreateDirectory(DirectoryPath);
        string target = Assets.SafePetPath.Resolve(DirectoryPath, palette.Id + ".json"), temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(palette, PetJson.Options));
            File.Move(temporary, target, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }

        return palette;
    }

    public void Delete(string id)
    {
        if (BuiltInPalettes.IsBuiltIn(id) || !PaletteDefinition.IsSafeId(id) || !id.StartsWith("custom-", StringComparison.Ordinal))
        {
            throw new PetFormatException("Error.PaletteProtected");
        }

        var file = Assets.SafePetPath.Resolve(DirectoryPath, id + ".json");
        if (File.Exists(file))
        {
            File.Delete(file);
        }
    }
}
