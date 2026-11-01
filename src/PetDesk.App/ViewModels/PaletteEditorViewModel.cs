using System.ComponentModel;
using System.Runtime.CompilerServices;
using PetDesk.Core.Appearance;
using PetDesk.Core.Localization;

namespace PetDesk.App.ViewModels;

/// <summary>An editable draft; changes do not affect the active palette until saved.</summary>
public sealed class PaletteEditorViewModel : ObservableObject, IDataErrorInfo
{
    private readonly Dictionary<string, string> _colors = [];
    private string _name;
    public PaletteEditorViewModel(PaletteDefinition palette, bool copy)
    {
        Id = copy ? "custom-" + Guid.NewGuid().ToString("N") : palette.Id;
        _name = copy ? LocalizationService.Current["Palette.NewName"] : palette.Name;
        Background = palette.Background;
        Surface = palette.Surface;
        Primary = palette.Primary;
        Accent = palette.Accent;
        Glow = palette.Glow;
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            NotifyDraftChanged();
        }
    }

    public string Background { get => GetColor(); set => SetColorValue(value); }
    public string Surface { get => GetColor(); set => SetColorValue(value); }
    public string Primary { get => GetColor(); set => SetColorValue(value); }
    public string Accent { get => GetColor(); set => SetColorValue(value); }
    public string Glow { get => GetColor(); set => SetColorValue(value); }
    private bool HasValidName => !string.IsNullOrWhiteSpace(Name) && Name.Length <= 40 && !Name.Any(char.IsControl);
    public bool IsValid => HasValidName && _colors.Values.All(PaletteDefinition.IsHex);
    public PaletteDefinition? Candidate => IsValid ? new PaletteDefinition(Id, Name, Background, Surface, Primary, Accent, Glow).Validate() : null;
    public string Error => IsValid ? string.Empty : LocalizationService.Current["Error.PaletteInvalid"];

    public string this[string propertyName]
    {
        get
        {
            bool invalid = propertyName == nameof(Name) ? !HasValidName : _colors.TryGetValue(propertyName, out var color) && !PaletteDefinition.IsHex(color);
            return invalid ? LocalizationService.Current["Error.PaletteInvalid"] : string.Empty;
        }
    }

    public void SetColor(string role, string value)
    {
        if (_colors.ContainsKey(role))
        {
            SetColorValue(value, role);
        }
    }

    private string GetColor([CallerMemberName] string propertyName = "") => _colors.GetValueOrDefault(propertyName, string.Empty);
    private void SetColorValue(string value, [CallerMemberName] string propertyName = "")
    {
        _colors[propertyName] = value;
        NotifyDraftChanged(propertyName);
    }

    private void NotifyDraftChanged([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(Candidate));
    }
}
