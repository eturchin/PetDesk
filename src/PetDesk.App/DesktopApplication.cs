using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using PetDesk.App.Presentation;
using PetDesk.App.Services;
using PetDesk.App.ViewModels;
using PetDesk.App.Views;
using PetDesk.Core.Appearance;
using PetDesk.Core.Behavior;
using PetDesk.Core.Localization;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Infrastructure.Logging;
using PetDesk.Infrastructure.Windows;
using PetDesk.Rendering.SpriteAtlas;

namespace PetDesk.App;

public sealed class DesktopApplication : IDisposable
{
    private readonly Application _application;
    private readonly ILoggerFactory _logging;
    private readonly ILogger _logger;
    private readonly SettingsStore _store;
    private readonly PetLibrary _library;
    private readonly PaletteStore _paletteStore;
    private IReadOnlyList<PaletteDefinition> _palettes;
    private readonly AutostartService _autostart = new AutostartService();
    private readonly PetWindow _pet;
    private readonly SettingsWindow _window;
    private readonly DispatcherTimer _saveTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(450)
    };
    private readonly string _executable = Path.Combine(AppContext.BaseDirectory, "PetDesk.exe");
    private TrayService? _tray;
    private SpriteAtlas? _atlas;
    private HitTestWindow? _diagnostic;
    private bool _exiting, _disposed, _starting = true;
    private LocalizationService L => LocalizationService.Current;
    public SettingsViewModel ViewModel { get; }
    public AppSettings Settings { get; private set; }
    public MonitorService Monitors { get; }
    public bool IsDark { get; private set; }

    public DesktopApplication(Application application, AppPaths paths, bool uiTest = false)
    {
        _application = application;
        _logging = LoggerFactory.Create(b => b.AddProvider(new LocalLoggerProvider(paths.Logs)));
        _logger = _logging.CreateLogger<DesktopApplication>();
        _store = new SettingsStore(paths, _logging.CreateLogger<SettingsStore>());
        Settings = _store.Load();
        L.SetLanguage(Settings.Language);
        _library = new PetLibrary(paths, _logging.CreateLogger<PetLibrary>());
        _paletteStore = new PaletteStore(paths);
        _palettes = _paletteStore.Load();
        if (!_palettes.Any(p => p.Id == Settings.PaletteId))
        {
            Settings = Settings with
            {
                PaletteId = "amethyst"
            };
        }

        ViewModel = new SettingsViewModel(Settings, Apply, SelectPet, SetAutostart);
        ViewModel.ReplacePalettes(_palettes);
        ApplyTheme();
        Monitors = new MonitorService();
        _pet = new PetWindow(Monitors);
        _window = new SettingsWindow(this, uiTest);
        application.MainWindow = _window;
        _pet.PositionSaved += position => Apply(Settings with { Position = position });
        _pet.SettingsRequested += () => ShowDashboard();
        _pet.FrameChanged += frame =>
        {
            if (_window.IsVisible)
            {
                ViewModel.Preview = frame.Image;
            }
        };
        ViewModel.FolderRequested += folder => OpenFolder(folder == "app" ? AppContext.BaseDirectory : paths.Pets);
        ViewModel.DeletePetRequested += DeletePet;
        ViewModel.SavePaletteRequested += SavePalette;
        ViewModel.DeletePaletteRequested += DeletePalette;
        ViewModel.ResetPositionRequested += ResetPosition;
        ViewModel.ExitRequested += Exit;
        ViewModel.ReactRequested += React;
        _saveTimer.Tick += (_, _) => SaveNow();
        SystemEvents.UserPreferenceChanged += UserPreferenceChanged;
        SystemEvents.SessionEnding += SessionEnding;
    }

    public void Start(bool startup, bool diagnostics, bool virtualDesktop = false)
    {
        foreach (var name in new[]
        {
            "mochi-cat",
            "pixel-fox"
        }

        )
        {
            _library.EnsureBuiltIn(Path.Combine(AppContext.BaseDirectory, "Assets", "BuiltIn", name), "builtin-" + name);
        }

        var pets = _library.List();
        ViewModel.ReplacePets(pets);
        var selected = pets.FirstOrDefault(p => p.Id == Settings.SelectedPetId) ?? pets.First(p => p.Id == "builtin-mochi-cat");
        if (!TrySelect(selected))
        {
            foreach (var fallback in pets.Where(p => p != selected))
            {
                if (TrySelect(fallback))
                {
                    break;
                }
            }
        }

        if (_atlas is null)
        {
            throw new PetFormatException("Error.Decode");
        }

        if (startup && Settings.StartHidden)
        {
            Settings = Settings with
            {
                Visible = false
            };
        }

        ViewModel.Sync(Settings);
        _pet.Apply(Settings.RestorePosition ? Settings : Settings with { Position = null }, true);
        _starting = false;
        RefreshAutostart();
        _tray = new TrayService(this);
        _logger.LogInformation(new EventId(1, "Started"), "PetDesk started");
        if (_store.RecoveryNotice is not null)
        {
            ViewModel.SetStatus(_store.RecoveryNotice, true);
        }

        if (_paletteStore.InvalidFiles > 0)
        {
            ViewModel.SetStatus("Status.InvalidPalettes", true);
        }

        if (!startup)
        {
            ShowDashboard();
        }

        if (diagnostics)
        {
            _diagnostic = new HitTestWindow(virtualDesktop);
            _diagnostic.Show();
            PlaceDiagnosticPet();
        }

        QueueSave();
    }

    private bool TrySelect(PetEntry entry)
    {
        try
        {
            if (_atlas?.Manifest.Id == entry.Id)
            {
                return true;
            }

            var next = SpriteAtlasLoader.Load(entry.Manifest, entry.ImagePath);
            var old = _atlas;
            _atlas = next;
            _pet.SetAtlas(next);
            ViewModel.Preview = next.Animations["idle"][0].Image;
            ViewModel.SetSelected(entry);
            old?.Dispose();
            Settings = Settings with
            {
                SelectedPetId = entry.Id
            };
            if (!_starting)
            {
                _pet.Apply(Settings);
            }

            ViewModel.Sync(Settings);
            QueueSave();
            return true;
        }
        catch (Exception ex) when (PetLibrary.IsImportError(ex))
        {
            ShowError(ex);
            return false;
        }
    }

    public void SelectPet(PetEntry entry) => TrySelect(entry);
    public void Import(string source)
    {
        _window.Cursor = System.Windows.Input.Cursors.Wait;
        try
        {
            var name = File.Exists(source) && Path.GetExtension(source).ToLowerInvariant() is ".png" or ".webp" ? L["Pet.ImportedName"] : null;
            var entry = _library.Import(source, name);
            ViewModel.ReplacePets(_library.List());
            SelectPet(entry);
            ViewModel.SetStatus("Status.Imported");
        }
        catch (Exception ex) when (PetLibrary.IsImportError(ex))
        {
            ShowError(ex);
        }
        finally
        {
            _window.Cursor = null;
        }
    }

    private void DeletePet(PetEntry entry)
    {
        if (entry.IsBuiltIn)
        {
            ShowError(new PetFormatException("Error.PetProtected"));
            return;
        }

        using var modal = _window.KeepOpen();
        if (!ActionDialog.Confirm(_window, string.Format(L["Library.DeleteConfirm"], entry.Name)))
        {
            return;
        }

        try
        {
            if (Settings.SelectedPetId == entry.Id)
            {
                SelectPet(_library.List().First(p => p.IsBuiltIn));
            }

            _library.Delete(entry);
            ViewModel.ReplacePets(_library.List());
            ViewModel.SetSelected(_library.List().First(p => p.Id == Settings.SelectedPetId));
            ViewModel.SetStatus("Status.Deleted");
        }
        catch (Exception ex) when (PetLibrary.IsImportError(ex))
        {
            ShowError(ex);
        }
    }

    private void SavePalette(PaletteDefinition palette)
    {
        try
        {
            _paletteStore.Save(palette);
            _palettes = _paletteStore.Load();
            ViewModel.ReplacePalettes(_palettes);
            ApplyCore(Settings with { PaletteId = palette.Id }, true);
            ViewModel.Navigate(DashboardSection.Settings);
            ViewModel.SetStatus("Status.PaletteSaved");
        }
        catch (Exception ex) when (PetLibrary.IsImportError(ex))
        {
            ShowError(ex);
        }
    }

    private void DeletePalette(string id)
    {
        var palette = _palettes.FirstOrDefault(p => p.Id == id);
        if (palette is null || BuiltInPalettes.IsBuiltIn(id))
        {
            return;
        }

        using var modal = _window.KeepOpen();
        if (!ActionDialog.Confirm(_window, string.Format(L["Palette.DeleteConfirm"], palette.Name)))
        {
            return;
        }

        try
        {
            _paletteStore.Delete(id);
            _palettes = _paletteStore.Load();
            ViewModel.ReplacePalettes(_palettes);
            ApplyCore(Settings with { PaletteId = Settings.PaletteId == id ? "amethyst" : Settings.PaletteId }, true);
            ViewModel.SetStatus("Status.PaletteDeleted");
        }
        catch (Exception ex) when (PetLibrary.IsImportError(ex))
        {
            ShowError(ex);
        }
    }

    public void Apply(AppSettings settings) => ApplyCore(settings, false);
    private void ApplyCore(AppSettings settings, bool forceAppearance)
    {
        settings = settings.Normalize();
        var appearance = forceAppearance || settings.Theme != Settings.Theme || settings.PaletteId != Settings.PaletteId;
        var language = settings.Language != Settings.Language;
        Settings = settings;
        if (language)
        {
            L.SetLanguage(settings.Language);
        }

        _pet.Apply(Settings);
        ViewModel.Sync(Settings);
        if (appearance)
        {
            ApplyTheme();
        }

        ViewModel.SetStatus("Status.Pending");
        QueueSave();
    }

    public void SetMovement(MovementMode mode) => Apply(Settings with { Movement = mode });
    public void ToggleVisible() => Apply(Settings with { Visible = !Settings.Visible });
    public void React(PetState state) => _pet.React(state);
    public void SetPreviewActive(bool active)
    {
        _pet.SetPreviewActive(active);
        if (active && _pet.CurrentFrame is { } frame)
        {
            ViewModel.Preview = frame.Image;
        }
    }

    public void ResetPosition()
    {
        _pet.Apply(Settings with { Position = null, Visible = true }, true);
        Apply(Settings with { Visible = true, Position = _pet.CapturePosition() });
    }

    private void QueueSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    public void SaveNow()
    {
        _saveTimer.Stop();
        try
        {
            Settings = Settings with
            {
                Position = _pet.CapturePosition()
            };
            _store.Save(Settings);
            if (!ViewModel.StatusIsError)
            {
                ViewModel.SetStatus("Status.Saved");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(new EventId(102, "SettingsSaveFailed"), "Configuration save failed ({ErrorType})", ex.GetType().Name);
            ViewModel.SetStatus("Error.Save", true);
        }
    }

    public void ShowDashboard(Position? anchor = null, bool settingsPage = false)
    {
        if (_exiting)
        {
            return;
        }

        RefreshAutostart();
        _window.ShowPanel(anchor, settingsPage ? DashboardSection.Settings : DashboardSection.Dashboard);
    }

    public void ToggleDashboard(Position anchor)
    {
        if (_window.IsVisible)
        {
            _window.HidePanel();
        }
        else if (!_window.WasJustAutoHidden)
        {
            ShowDashboard(anchor);
        }
    }

    public static string ErrorKey(Exception ex)
    {
        if (LocalizationService.Catalog("ru").ContainsKey(ex.Message))
        {
            return ex.Message;
        }

        return ex switch
        {
            FileNotFoundException => "Error.Missing",
            UnauthorizedAccessException => "Error.Access",
            System.Text.Json.JsonException => "Error.Json",
            IOException => "Error.Io",
            _ => "Error.Generic"
        };
    }

    public void ShowError(Exception ex)
    {
        _logger.LogWarning(new EventId(203, "UserFileError"), "User operation failed ({ErrorType})", ex.GetType().Name);
        var key = ErrorKey(ex);
        ViewModel.SetStatus(key, true);
        if (_window.IsVisible)
        {
            using var modal = _window.KeepOpen();
            ActionDialog.ShowMessage(_window, L["Error.Title"], L[key]);
        }
    }

    public void LogError(Exception ex) => _logger.LogError(new EventId(500, "UnhandledError"), "Unexpected error ({ErrorType})", ex.GetType().Name);
    private void RefreshAutostart()
    {
        try
        {
            ViewModel.SetAutostart(_autostart.IsEnabled(_executable));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or ArgumentException)
        {
            ViewModel.SetStatus("Error.StartupRead", true);
            ViewModel.SetAutostart(false);
        }
    }

    private void SetAutostart(bool enabled)
    {
        try
        {
            _autostart.SetEnabled(enabled, _executable);
            ViewModel.SetAutostart(enabled);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or ArgumentException)
        {
            RefreshAutostart();
            ViewModel.SetStatus("Error.Startup", true);
        }
    }

    public void ApplyTheme()
    {
        IsDark = SystemAppearance.IsDark(Settings.Theme);
        ThemeEngine.Apply(_palettes.FirstOrDefault(p => p.Id == Settings.PaletteId) ?? BuiltInPalettes.Default, IsDark);
    }

    private void OpenFolder(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            ShowError(ex);
        }
    }

    private void UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => _application.Dispatcher.BeginInvoke(ApplyTheme);
    private void SessionEnding(object sender, SessionEndingEventArgs e) => _application.Dispatcher.Invoke(SaveNow);
    public void PlaceDiagnosticPet()
    {
        if (_diagnostic is null)
        {
            return;
        }

        var work = Monitors.Primary.Work;
        Apply(Settings with { Movement = MovementMode.Fixed, Visible = true });
        _pet.PlaceForDiagnostics(new Position(work.Left + 400, work.Top + 230));
        SaveNow();
    }

    public void Exit()
    {
        if (_exiting)
        {
            return;
        }

        _exiting = true;
        _window.AllowShutdown();
        SaveNow();
        _application.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _saveTimer.Stop();
        SystemEvents.UserPreferenceChanged -= UserPreferenceChanged;
        SystemEvents.SessionEnding -= SessionEnding;
        _tray?.Dispose();
        _pet.ShutdownWindow();
        _window.ShutdownWindow();
        if (_diagnostic?.IsLoaded == true)
        {
            _diagnostic.Close();
        }

        ViewModel.Preview = null;
        ViewModel.Dispose();
        _atlas?.Dispose();
        _logger.LogInformation(new EventId(2, "Stopped"), "PetDesk stopped");
        _logging.Dispose();
    }
}
