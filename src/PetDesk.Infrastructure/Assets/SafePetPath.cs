using PetDesk.Core.Models;

namespace PetDesk.Infrastructure.Assets;

public static class SafePetPath
{
    public static string Resolve(string directory, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath) || relativePath.Contains(':') || relativePath.Contains('\0'))
        {
            throw new PetFormatException("Error.RelativePath");
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new PetFormatException("Error.PathEscape");
        }

        var current = new FileInfo(path) as FileSystemInfo;
        while (current is not null)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new PetFormatException("Error.Symlink");
            }

            if (string.Equals(Path.TrimEndingDirectorySeparator(current.FullName), root, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            current = current is FileInfo file ? file.Directory : ((DirectoryInfo)current).Parent;
        }

        return path;
    }
}
