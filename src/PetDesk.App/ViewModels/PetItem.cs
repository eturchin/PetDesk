using System.Windows.Media.Imaging;
using PetDesk.App.Presentation;
using PetDesk.Core.Localization;
using PetDesk.Infrastructure.Assets;

namespace PetDesk.App.ViewModels;

public sealed class PetItem(PetEntry? entry) : ObservableObject
{
    public PetEntry? Entry { get; } = entry;
    public bool IsAdd => Entry is null;
    public bool IsBuiltIn => Entry?.IsBuiltIn ?? false;
    public string Name => Entry?.Name ?? LocalizationService.Current["Library.Add"];
    public BitmapSource? Thumbnail { get; } = entry is null ? null : ThumbnailLoader.Load(entry);
    public string KindLabel => LocalizationService.Current[IsBuiltIn ? "Library.BuiltIn" : "Library.Imported"];

    public void RefreshLanguage() => OnPropertyChanged(string.Empty);
    public override string ToString() => Name;
}
