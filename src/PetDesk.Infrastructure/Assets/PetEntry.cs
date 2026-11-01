using System.Text.Json;
using Microsoft.Extensions.Logging;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Rendering.SpriteAtlas;

namespace PetDesk.Infrastructure.Assets;

public sealed record PetEntry(string Directory, PetManifest Manifest)
{
    public string Name => Manifest.Name;
    public string Id => Manifest.Id;
    public bool IsBuiltIn => PetLibrary.BuiltInIds.Contains(Id) && Path.GetFileName(Directory) == Id;
    public string ImagePath => SafePetPath.Resolve(Directory, Manifest.SpriteSheet);

    public override string ToString() => Name;
}
