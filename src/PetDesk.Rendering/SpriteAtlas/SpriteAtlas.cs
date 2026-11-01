using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PetDesk.Core.Models;

namespace PetDesk.Rendering.SpriteAtlas;

public sealed class SpriteAtlas(PetManifest manifest, Dictionary<string, SpriteFrame[]> animations) : IDisposable
{
    public PetManifest Manifest { get; } = manifest;
    public IReadOnlyDictionary<string, SpriteFrame[]> Animations => animations;

    public void Dispose() => animations.Clear();
}
