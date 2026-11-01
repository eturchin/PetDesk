using System.Text.Json;
using Microsoft.Extensions.Logging;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Rendering.SpriteAtlas;

namespace PetDesk.Infrastructure.Assets;

public sealed class PetLibrary(AppPaths paths, ILogger<PetLibrary> logger)
{
    public static IReadOnlySet<string> BuiltInIds { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "builtin-mochi-cat",
        "builtin-pixel-fox"
    };
    public string LibraryDirectory => paths.Pets;

    public PetEntry EnsureBuiltIn(string source, string id)
    {
        if (!BuiltInIds.Contains(id))
        {
            throw new PetFormatException("Error.PetProtected");
        }

        var target = Path.Combine(paths.Pets, id, "pet.json");
        if (File.Exists(target))
        {
            return ReadManifest(target);
        }

        return ImportCore(source, null, id);
    }

    public void Delete(PetEntry entry)
    {
        if (entry.IsBuiltIn || BuiltInIds.Contains(entry.Id))
        {
            throw new PetFormatException("Error.PetProtected");
        }

        var expected = Path.GetFullPath(Path.Combine(paths.Pets, entry.Id));
        if (!string.Equals(Path.GetFullPath(entry.Directory), expected, StringComparison.OrdinalIgnoreCase) || !expected.StartsWith(Path.GetFullPath(paths.Pets) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new PetFormatException("Error.LibraryPath");
        }

        SafePetPath.Resolve(paths.Pets, entry.Id + "/pet.json");
        var pending = new Stack<string>();
        pending.Push(expected);
        while (pending.TryPop(out var directory))
            foreach (var child in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                if ((child.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new PetFormatException("Error.Symlink");
                }

                if (child is DirectoryInfo)
                {
                    pending.Push(child.FullName);
                }
            }

        System.IO.Directory.Delete(expected, true);
    }

    public IReadOnlyList<PetEntry> List()
    {
        var entries = new List<PetEntry>();
        foreach (var directory in System.IO.Directory.EnumerateDirectories(paths.Pets).Where(d => !Path.GetFileName(d).StartsWith('.')))
        {
            try
            {
                entries.Add(ReadManifest(Path.Combine(directory, "pet.json")));
            }
            catch (Exception ex) when (IsImportError(ex))
            {
                logger.LogWarning(new EventId(202, "LibraryEntrySkipped"), "Invalid pet entry skipped ({ErrorType})", ex.GetType().Name);
            }
        }

        return entries.OrderBy(p => p.Name, StringComparer.CurrentCulture).ToArray();
    }

    public static PetEntry ReadManifest(string path)
    {
        if (new FileInfo(path).Length > 1024 * 1024)
        {
            throw new PetFormatException("Error.ManifestLarge");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        SafePetPath.Resolve(directory, Path.GetFileName(path));
        var manifest = JsonSerializer.Deserialize<PetManifest>(File.ReadAllText(path), PetJson.Options) ?? throw new PetFormatException("Error.ManifestEmpty");
        var imagePath = SafePetPath.Resolve(directory, manifest.SpriteSheet);
        var (width, height) = ImageHeaderReader.Read(imagePath);
        ManifestValidator.Validate(manifest, width, height);
        return new PetEntry(directory, manifest);
    }

    public PetEntry Import(string source, string? displayName = null) => ImportCore(source, displayName, null);
    private PetEntry ImportCore(string source, string? displayName, string? builtInId)
    {
        if (System.IO.Directory.Exists(source))
        {
            var manifestFile = Path.Combine(source, "pet.json");
            if (File.Exists(manifestFile))
            {
                source = manifestFile;
            }
            else
            {
                var candidates = System.IO.Directory.EnumerateFiles(source).Where(IsImage).Take(2).ToArray();
                if (candidates.Length != 1)
                {
                    throw new PetFormatException("Error.Folder");
                }

                source = candidates[0];
            }
        }

        source = Path.GetFullPath(source);
        PetManifest manifest;
        string imagePath;
        if (Path.GetExtension(source).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            var entry = ReadManifest(source);
            manifest = entry.Manifest;
            imagePath = entry.ImagePath;
        }
        else
        {
            if (!IsImage(source))
            {
                throw new PetFormatException("Error.Extension");
            }

            SafePetPath.Resolve(Path.GetDirectoryName(source)!, Path.GetFileName(source));
            var (width, height) = ImageHeaderReader.Read(source);
            manifest = KnownPetAdapter.Detect(width, height, displayName ?? PetDesk.Core.Localization.LocalizationService.Current["Pet.ImportedName"], Path.GetFileName(source));
            imagePath = source;
        }

        var id = builtInId ?? "pet-" + Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        if (extension is not (".png" or ".webp"))
        {
            throw new PetFormatException("Error.SheetExtension");
        }

        manifest = manifest with
        {
            Id = id,
            Name = displayName ?? manifest.Name,
            SpriteSheet = "spritesheet" + extension
        };
        var staging = Path.Combine(paths.Pets, ".import-" + Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(paths.Pets, id);
        System.IO.Directory.CreateDirectory(staging);
        try
        {
            var copy = Path.Combine(staging, manifest.SpriteSheet);
            // Validate copied bytes, so changes to an external source cannot bypass validation.
            using (var input = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var output = new FileStream(copy, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                if (input.Length > ImageHeaderReader.MaximumFileBytes)
                {
                    throw new PetFormatException("Error.FileLarge");
                }

                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = input.Read(buffer)) > 0)
                {
                    total += read;
                    if (total > ImageHeaderReader.MaximumFileBytes)
                    {
                        throw new PetFormatException("Error.FileLarge");
                    }

                    output.Write(buffer, 0, read);
                }
            }

            using (var atlas = SpriteAtlasLoader.Load(manifest, copy))
            {
            }

            File.WriteAllText(Path.Combine(staging, "pet.json"), JsonSerializer.Serialize(manifest, PetJson.Options));
            System.IO.Directory.Move(staging, destination);
            logger.LogInformation(new EventId(201, "PetImported"), "Pet imported successfully");
            return new PetEntry(destination, manifest);
        }
        finally
        {
            if (System.IO.Directory.Exists(staging))
            {
                System.IO.Directory.Delete(staging, true);
            }
        }
    }

    private static bool IsImage(string path) => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".webp";
    public static bool IsImportError(Exception ex) => ex is PetFormatException or IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException or OverflowException;
}
