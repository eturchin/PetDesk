using PetDesk.Core.Appearance;

namespace PetDesk.App.ViewModels;

public sealed class PaletteChoice(PaletteDefinition definition) : ObservableObject
{
    private bool _isSelected;
    public PaletteDefinition Definition { get; } = definition;
    public string Id => Definition.Id;
    public string Name => Definition.Name;
    public string Background => Definition.Background;
    public string Surface => Definition.Surface;
    public string Primary => Definition.Primary;
    public string Accent => Definition.Accent;
    public bool IsBuiltIn => BuiltInPalettes.IsBuiltIn(Id);
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}
