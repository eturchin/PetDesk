using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using PetDesk.App.Presentation;
using PetDesk.Core.Appearance;
using PetDesk.Core.Behavior;
using PetDesk.Core.Localization;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;

namespace PetDesk.App.ViewModels;

/// <summary>Presentation state and commands for the single tray panel.</summary>
public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly Action<AppSettings> _apply;
    private readonly Action<PetEntry> _select;
    private readonly Action<bool> _setAutostart;
    private readonly List<RelayCommand> _commands = [];
    private readonly LocalizationService _localization = LocalizationService.Current;
    private AppSettings _settings;
    private PetItem? _selectedPet;
    private BitmapSource? _preview;
    private PaletteEditorViewModel? _editor;
    private DashboardSection _section;
    private string _statusKey = "Status.Saved";
    private bool _autostartEnabled;
    private bool _statusIsError;
    public SettingsViewModel(AppSettings settings, Action<AppSettings> apply, Action<PetEntry> select, Action<bool> autostart)
    {
        _settings = settings;
        _apply = apply;
        _select = select;
        _setAutostart = autostart;
        ToggleVisibilityCommand = CreateCommand(_ => PetVisible = !PetVisible);
        SetMovementCommand = CreateCommand(SetMovement);
        NavigateCommand = CreateCommand(parameter => Navigate(parameter is DashboardSection section && Enum.IsDefined(section) ? section : DashboardSection.Dashboard));
        ImportFileCommand = CreateCommand(_ => ImportFileRequested?.Invoke());
        ImportFolderCommand = CreateCommand(_ => ImportFolderRequested?.Invoke());
        OpenFolderCommand = CreateCommand(parameter => FolderRequested?.Invoke(parameter?.ToString() == "app" ? "app" : "pets"));
        DeletePetCommand = CreateCommand(RequestPetDeletion, parameter => parameter is PetItem { Entry: not null, IsBuiltIn: false });
        UsePaletteCommand = CreateCommand(UsePalette);
        CreatePaletteCommand = CreateCommand(_ => BeginEditor(copy: true));
        EditPaletteCommand = CreateCommand(_ => BeginEditor(copy: false), _ => CanEditPalette);
        SavePaletteCommand = CreateCommand(_ => SavePalette(), _ => Editor?.IsValid == true);
        DeletePaletteCommand = CreateCommand(_ => DeleteSelectedPalette(), _ => CanEditPalette);
        PickColorCommand = CreateCommand(parameter =>
        {
            if (parameter is string role)
            {
                PickColorRequested?.Invoke(role);
            }
        });
        ResetAppearanceCommand = CreateCommand(_ => Update(_settings with { Theme = "Dark", PaletteId = BuiltInPalettes.Default.Id }));
        ResetPositionCommand = CreateCommand(_ => ResetPositionRequested?.Invoke());
        ExitCommand = CreateCommand(_ => ExitRequested?.Invoke());
        WaveCommand = CreateCommand(_ => ReactRequested?.Invoke(PetState.Waving));
        JumpCommand = CreateCommand(_ => ReactRequested?.Invoke(PetState.Jumping));
        _localization.PropertyChanged += LanguageChanged;
    }

    public event Action? ImportFileRequested;
    public event Action? ImportFolderRequested;
    public event Action<string>? FolderRequested;
    public event Action<PetEntry>? DeletePetRequested;
    public event Action<PaletteDefinition>? SavePaletteRequested;
    public event Action<string>? DeletePaletteRequested;
    public event Action<string>? PickColorRequested;
    public event Action? ResetPositionRequested;
    public event Action? ExitRequested;
    public event Action<PetState>? ReactRequested;
    public RelayCommand ToggleVisibilityCommand { get; }
    public RelayCommand SetMovementCommand { get; }
    public RelayCommand NavigateCommand { get; }
    public RelayCommand ImportFileCommand { get; }
    public RelayCommand ImportFolderCommand { get; }
    public RelayCommand OpenFolderCommand { get; }
    public RelayCommand DeletePetCommand { get; }
    public RelayCommand UsePaletteCommand { get; }
    public RelayCommand CreatePaletteCommand { get; }
    public RelayCommand EditPaletteCommand { get; }
    public RelayCommand SavePaletteCommand { get; }
    public RelayCommand DeletePaletteCommand { get; }
    public RelayCommand PickColorCommand { get; }
    public RelayCommand ResetAppearanceCommand { get; }
    public RelayCommand ResetPositionCommand { get; }
    public RelayCommand ExitCommand { get; }
    public RelayCommand WaveCommand { get; }
    public RelayCommand JumpCommand { get; }
    public ObservableCollection<PetItem> Pets { get; } = [];
    public ObservableCollection<PetItem> LibraryPets { get; } = [];
    public ObservableCollection<PaletteChoice> Palettes { get; } = [];

    public PetItem? SelectedPet
    {
        get => _selectedPet;
        set
        {
            if (value is null)
            {
                return;
            }

            if (value.IsAdd)
            {
                ImportFileRequested?.Invoke();
                OnPropertyChanged();
                return;
            }

            if (value.Entry is { } pet && pet.Id != _selectedPet?.Entry?.Id)
            {
                _select(pet);
            }
        }
    }

    public PaletteChoice? SelectedPalette => Palettes.FirstOrDefault(palette => palette.Id == _settings.PaletteId) ?? Palettes.FirstOrDefault();
    public BitmapSource? Preview { get => _preview; set => SetProperty(ref _preview, value); }
    public string Status => _localization[_statusKey];
    public bool StatusIsError => _statusIsError;
    public string PetName => _selectedPet?.Name ?? _localization["Pet.None"];
    public string PetStatus => _localization[PetVisible ? "Dashboard.Active" : "Dashboard.Hidden"];
    public string PetDetails => _selectedPet?.Entry is { } pet ? string.Format(_localization["Library.Details"], pet.Manifest.FrameWidth, pet.Manifest.FrameHeight, pet.Manifest.Animations.Count) : string.Empty;
    public string AppVersion => string.Format(_localization["About.Version"], typeof(SettingsViewModel).Assembly.GetName().Version?.ToString(3));
    public DashboardSection CurrentSection => _section;
    public bool IsDashboard => _section == DashboardSection.Dashboard;
    public bool IsSettings => _section == DashboardSection.Settings;
    public bool IsEditor => _section == DashboardSection.PaletteEditor;
    public bool IsAbout => _section == DashboardSection.About;
    public bool IsSubpage => !IsDashboard;
    public string PageTitle => _localization[_section switch
    {
        DashboardSection.PaletteEditor => "Palette.Editor",
        DashboardSection.About => "Menu.About",
        _ => "Dashboard.Settings"
    }];
    public PaletteEditorViewModel? Editor => _editor;
    private bool CanEditPalette => SelectedPalette is { IsBuiltIn: false };
    public double ScalePercent { get => _settings.Scale * 100; set => Update(_settings with { Scale = value / 100 }); }
    public double OpacityPercent { get => _settings.Opacity * 100; set => Update(_settings with { Opacity = value / 100 }); }
    public double SpeedPercent { get => _settings.Speed / AppSettings.DefaultSpeed * 100; set => Update(_settings with { Speed = value / 100 * AppSettings.DefaultSpeed }); }
    public bool PetVisible { get => _settings.Visible; set => Update(_settings with { Visible = value }); }
    public bool IsFree => _settings.Movement == MovementMode.Free;
    public bool IsFixed => _settings.Movement == MovementMode.Fixed;
    public bool IsTaskbar => _settings.Movement == MovementMode.Taskbar;
    public double ActivityPercent { get => _settings.Activity * 100; set => Update(_settings with { Activity = value / 100 }); }
    public bool StartHidden { get => _settings.StartHidden; set => Update(_settings with { StartHidden = value }); }
    public bool RestorePosition { get => _settings.RestorePosition; set => Update(_settings with { RestorePosition = value }); }

    public int ThemeIndex
    {
        get => _settings.Theme switch
        {
            "Light" => 1,
            "Dark" => 2,
            _ => 0
        };
        set
        {
            if (value >= 0)
            {
                Update(_settings with
                {
                    Theme = value switch
                    {
                        1 => "Light",
                        2 => "Dark",
                        _ => "System"
                    }
                });
            }
        }
    }

    public int LanguageIndex
    {
        get => _settings.Language == "en" ? 1 : 0;
        set
        {
            if (value >= 0)
            {
                Update(_settings with { Language = value == 1 ? "en" : "ru" });
            }
        }
    }

    public bool Autostart
    {
        get => _autostartEnabled;
        set
        {
            if (value != _autostartEnabled)
            {
                _setAutostart(value);
            }
        }
    }

    public void SetAutostart(bool enabled)
    {
        _autostartEnabled = enabled;
        OnPropertyChanged(nameof(Autostart));
    }

    public void SetStatus(string key, bool error = false)
    {
        _statusKey = key;
        _statusIsError = error;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusIsError));
    }

    public void Sync(AppSettings settings)
    {
        _settings = settings;
        foreach (var palette in Palettes)
        {
            palette.IsSelected = palette.Id == settings.PaletteId;
        }

        OnPropertyChanged(string.Empty);
        RefreshCommands();
    }

    public void ReplacePets(IEnumerable<PetEntry> pets)
    {
        var selectedId = _selectedPet?.Entry?.Id ?? _settings.SelectedPetId;
        Pets.Clear();
        LibraryPets.Clear();
        foreach (var entry in pets)
        {
            var item = new PetItem(entry);
            Pets.Add(item);
            LibraryPets.Add(item);
        }

        Pets.Add(new PetItem(null));
        _selectedPet = LibraryPets.FirstOrDefault(pet => pet.Entry?.Id == selectedId);
        NotifyPetSelection();
    }

    public void ReplacePalettes(IEnumerable<PaletteDefinition> palettes)
    {
        Palettes.Clear();
        foreach (var palette in palettes)
        {
            Palettes.Add(new PaletteChoice(palette) { IsSelected = palette.Id == _settings.PaletteId });
        }

        OnPropertyChanged(nameof(SelectedPalette));
        RefreshCommands();
    }

    public void SetSelected(PetEntry entry)
    {
        _selectedPet = LibraryPets.FirstOrDefault(pet => pet.Entry?.Id == entry.Id);
        NotifyPetSelection();
    }

    public void Navigate(DashboardSection section)
    {
        _section = section;
        OnPropertyChanged(string.Empty);
    }

    private RelayCommand CreateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        var command = new RelayCommand(execute, canExecute);
        _commands.Add(command);
        return command;
    }

    private void RefreshCommands()
    {
        foreach (var command in _commands)
        {
            command.Refresh();
        }
    }

    private void Update(AppSettings settings)
    {
        settings = settings.Normalize();
        if (settings != _settings)
        {
            _apply(settings);
        }
    }

    private void SetMovement(object? parameter)
    {
        if (Enum.TryParse<MovementMode>(parameter?.ToString(), out var mode) && Enum.IsDefined(mode))
        {
            Update(_settings with { Movement = mode });
        }
    }

    private void RequestPetDeletion(object? parameter)
    {
        if (parameter is PetItem { Entry: { } entry })
        {
            DeletePetRequested?.Invoke(entry);
        }
    }

    private void UsePalette(object? parameter)
    {
        if (parameter is PaletteChoice palette)
        {
            Update(_settings with { PaletteId = palette.Id });
        }
    }

    private void SavePalette()
    {
        if (Editor?.Candidate is { } palette)
        {
            SavePaletteRequested?.Invoke(palette);
        }
    }

    private void DeleteSelectedPalette()
    {
        if (SelectedPalette is { IsBuiltIn: false } palette)
        {
            DeletePaletteRequested?.Invoke(palette.Id);
        }
    }

    private void BeginEditor(bool copy)
    {
        if (_editor is not null)
        {
            _editor.PropertyChanged -= EditorChanged;
        }

        _editor = new PaletteEditorViewModel(SelectedPalette?.Definition ?? BuiltInPalettes.Default, copy);
        _editor.PropertyChanged += EditorChanged;
        Navigate(DashboardSection.PaletteEditor);
        OnPropertyChanged(nameof(Editor));
        SavePaletteCommand.Refresh();
    }

    private void NotifyPetSelection()
    {
        OnPropertyChanged(nameof(SelectedPet));
        OnPropertyChanged(nameof(PetName));
        OnPropertyChanged(nameof(PetDetails));
    }

    private void EditorChanged(object? sender, PropertyChangedEventArgs args)
    {
        SavePaletteCommand.Refresh();
        OnPropertyChanged(nameof(Editor));
    }

    private void LanguageChanged(object? sender, PropertyChangedEventArgs args)
    {
        foreach (var pet in Pets)
        {
            pet.RefreshLanguage();
        }

        OnPropertyChanged(string.Empty);
    }

    public void Dispose()
    {
        _localization.PropertyChanged -= LanguageChanged;
        if (_editor is not null)
        {
            _editor.PropertyChanged -= EditorChanged;
        }
    }
}
