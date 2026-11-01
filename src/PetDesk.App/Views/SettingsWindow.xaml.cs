using System.ComponentModel;
using System.Threading;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using PetDesk.App.ViewModels;
using PetDesk.Core.Appearance;
using PetDesk.Core.Behavior;
using PetDesk.Core.Localization;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Windows;

namespace PetDesk.App.Views;

/// <summary>Owns panel placement, focus and native dialogs; page content lives in Views/Pages.</summary>
public partial class SettingsWindow : Window
{
    private const double PanelWidth = 512;
    private const double DashboardHeight = 668;
    private const double SettingsHeight = 708;
    private const double AboutHeight = 456;
    private const uint NoActivate = 0x0010;
    private readonly DesktopApplication _host;
    private readonly bool _uiTest;
    private int _modalDepth;
    private int _popupDepth;
    private bool _shuttingDown;
    private Position? _lastAnchor;
    private DashboardSection _lastSection;
    private DateTime _autoHiddenAt = DateTime.MinValue;
    public SettingsWindow(DesktopApplication host, bool uiTest)
    {
        _host = host;
        _uiTest = uiTest;
        InitializeComponent();
        DataContext = host.ViewModel;
        ShowInTaskbar = uiTest;
        Topmost = true;
        SourceInitialized += ConfigureNativeWindow;
        IsVisibleChanged += (_, _) => _host.SetPreviewActive(IsVisible);
        Deactivated += (_, _) => CheckOutsideClick();
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
        host.ViewModel.ImportFileRequested += ImportFile;
        host.ViewModel.ImportFolderRequested += ImportFolder;
        host.ViewModel.PickColorRequested += PickColor;
        host.ViewModel.PropertyChanged += ViewModelChanged;
    }

    public nint Handle => new WindowInteropHelper(this).Handle;
    public bool WasJustAutoHidden => (DateTime.UtcNow - _autoHiddenAt).TotalMilliseconds < 250;

    public void ShowPanel(Position? anchor = null, DashboardSection section = DashboardSection.Dashboard)
    {
        if (_shuttingDown)
        {
            return;
        }

        _host.ViewModel.Navigate(section);
        _lastAnchor = anchor ?? _lastAnchor;
        PlacePanel();
        Show();
        PlacePanel();
        Activate();
        _host.SetPreviewActive(true);
    }

    public void HidePanel()
    {
        if (!IsVisible)
        {
            return;
        }

        Hide();
        _host.SetPreviewActive(false);
        _host.SaveNow();
    }

    public IDisposable KeepOpen()
    {
        _modalDepth++;
        return new Scope(() =>
        {
            _modalDepth--;
            if (IsVisible)
            {
                Activate();
            }
        });
    }

    internal IDisposable TrackPopup()
    {
        _popupDepth++;
        return new Scope(() =>
        {
            _popupDepth--;
            CheckOutsideClick();
        });
    }

    private void ConfigureNativeWindow(object? sender, EventArgs args)
    {
        if (_uiTest)
        {
            return;
        }

        var style = NativeMethods.GetWindowLongPtr(Handle, NativeMethods.GwlExStyle);
        NativeMethods.SetWindowLongPtr(Handle, NativeMethods.GwlExStyle, style | NativeMethods.WsExToolWindow);
    }

    private void PlacePanel()
    {
        _host.Monitors.Refresh();
        var primary = _host.Monitors.Primary;
        var anchor = _lastAnchor ?? new Position(primary.Work.Left + primary.Work.Width - 18 * primary.Scale, primary.Work.Top + primary.Work.Height + 12 * primary.Scale);
        var monitor = _host.Monitors.Nearest(anchor);
        double height = _host.ViewModel.CurrentSection switch
        {
            DashboardSection.Dashboard => DashboardHeight,
            DashboardSection.About => AboutHeight,
            _ => SettingsHeight
        };
        var rectangle = PopupPlacement.Calculate(monitor.Work, anchor, PanelWidth, height, monitor.Dpi, monitor.Edge);
        Width = rectangle.Width / monitor.Scale;
        Height = rectangle.Height / monitor.Scale;
        if (Handle == 0)
        {
            Left = rectangle.X / monitor.Scale;
            Top = rectangle.Y / monitor.Scale;
        }
        else
        {
            NativeMethods.SetWindowPos(Handle, -1, (int)Math.Round(rectangle.X), (int)Math.Round(rectangle.Y), (int)Math.Ceiling(rectangle.Width), (int)Math.Ceiling(rectangle.Height), NoActivate);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs args)
    {
        if (_shuttingDown)
        {
            return;
        }

        args.Cancel = true;
        HidePanel();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape && _popupDepth == 0 && _modalDepth == 0)
        {
            HidePanel();
            args.Handled = true;
        }
    }

    private void CheckOutsideClick() => Dispatcher.BeginInvoke(() =>
    {
        if (!_shuttingDown && IsVisible && !IsActive && _modalDepth == 0 && _popupDepth == 0)
        {
            _autoHiddenAt = DateTime.UtcNow;
            HidePanel();
        }
    }, DispatcherPriority.ContextIdle);
    private void More_Click(object sender, RoutedEventArgs args)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    private void ImportFile()
    {
        using var scope = KeepOpen();
        var localization = LocalizationService.Current;
        var dialog = new OpenFileDialog
        {
            Title = localization["Library.FileTitle"],
            Filter = localization["Library.FileFilter"],
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            _host.Import(dialog.FileName);
        }
    }

    private void ImportFolder()
    {
        using var scope = KeepOpen();
        var dialog = new OpenFolderDialog
        {
            Title = LocalizationService.Current["Library.FolderTitle"]
        };
        if (dialog.ShowDialog(this) == true)
        {
            _host.Import(dialog.FolderName);
        }
    }

    private void PickColor(string role)
    {
        if (_host.ViewModel.Editor is not { } editor)
        {
            return;
        }

        var value = role switch
        {
            nameof(editor.Background) => editor.Background,
            nameof(editor.Surface) => editor.Surface,
            nameof(editor.Primary) => editor.Primary,
            nameof(editor.Accent) => editor.Accent,
            nameof(editor.Glow) => editor.Glow,
            _ => string.Empty
        };
        if (value.Length == 0)
        {
            return;
        }

        using var scope = KeepOpen();
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true
        };
        if (PaletteDefinition.IsHex(value))
        {
            dialog.Color = System.Drawing.Color.FromArgb(Convert.ToInt32(value[1..], 16) | unchecked((int)0xff000000));
        }

        if (dialog.ShowDialog(new DialogOwner(Handle)) == System.Windows.Forms.DialogResult.OK)
        {
            editor.SetColor(role, $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}");
        }
    }

    private void ViewModelChanged(object? sender, PropertyChangedEventArgs args)
    {
        var viewModel = _host.ViewModel;
        if (_lastSection != viewModel.CurrentSection)
        {
            _lastSection = viewModel.CurrentSection;
            Scroller.ScrollToTop();
            if (IsVisible)
            {
                PlacePanel();
            }
        }

        if (viewModel.IsEditor && viewModel.Editor?.Candidate is { } candidate)
        {
            PaletteEditor.UpdatePreview(candidate, _host.IsDark);
        }
    }

    public void AllowShutdown() => _shuttingDown = true;
    public void ShutdownWindow()
    {
        _shuttingDown = true;
        _host.ViewModel.ImportFileRequested -= ImportFile;
        _host.ViewModel.ImportFolderRequested -= ImportFolder;
        _host.ViewModel.PickColorRequested -= PickColor;
        _host.ViewModel.PropertyChanged -= ViewModelChanged;
        DataContext = null;
        Close();
    }

    private sealed class DialogOwner(nint handle) : System.Windows.Forms.IWin32Window
    {
        public nint Handle { get; } = handle;
    }

    private sealed class Scope(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
