using System.Diagnostics;
using System.Windows.Interop;
using System.Windows.Threading;
using PetDesk.Core.Animation;
using PetDesk.Core.Behavior;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Windows;
using PetDesk.Rendering.SpriteAtlas;

namespace PetDesk.App.Views;

public sealed class PetWindow : Window
{
    private readonly Image _image = new Image
    {
        Stretch = Stretch.Fill,
        SnapsToDevicePixels = true
    };
    private readonly DispatcherTimer _timer = new DispatcherTimer(DispatcherPriority.Render);
    private readonly Stopwatch _clock = new Stopwatch();
    private readonly MonitorService _monitors;
    private readonly PetBehavior _behavior = new PetBehavior();
    private readonly AnimationPlayer _player = new AnimationPlayer();
    private PetAnimationController? _controller;
    private SpriteAtlas? _atlas;
    private SpriteFrame? _frame;
    private DesktopMonitor _monitor;
    private AppSettings _settings = new AppSettings();
    private nint _handle;
    private Position _dragOffset;
    private HwndSource? _source;
    private bool _closing;
    private bool _previewActive;
    public event Action<SpriteFrame>? FrameChanged;
    public SpriteFrame? CurrentFrame => _frame;

    private (int X, int Y, int Width, int Height)? _lastPlacement;
    public event Action<SavedPosition>? PositionSaved;
    public event Action? SettingsRequested;
    public event Action<string>? DragActivityChanged;
    public PetState CurrentState => _behavior.State;
    public Position PhysicalPosition => _behavior.Position;

    public PetWindow(MonitorService monitors)
    {
        _monitors = monitors;
        _monitor = monitors.Primary;
        Title = "PetDesk — питомец";
        Width = 192;
        Height = 208;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        Content = _image;
        SourceInitialized += (_, _) =>
        {
            _handle = new WindowInteropHelper(this).Handle;
            NativeMethods.SetWindowLongPtr(_handle, NativeMethods.GwlExStyle, NativeMethods.GetWindowLongPtr(_handle, NativeMethods.GwlExStyle) | NativeMethods.WsExToolWindow);
            _source = HwndSource.FromHwnd(_handle);
            _source.AddHook(WindowProc);
        };
        _timer.Tick += Tick;
        _timer.Interval = TimeSpan.FromMilliseconds(40);
        Closing += (_, e) =>
        {
            if (!_closing)
            {
                e.Cancel = true;
            }
        };
        IsVisibleChanged += (_, _) => UpdateTimer();
    }

    public void SetAtlas(SpriteAtlas atlas)
    {
        _image.Source = null;
        _frame = null;
        _atlas = atlas;
        _controller = new PetAnimationController(atlas.Manifest);
        _player.Play("idle", atlas.Manifest.Animations["idle"], true);
        _behavior.Reset();
        UpdateFrame();
    }

    public void Apply(AppSettings settings, bool restorePosition = false)
    {
        var modeChanged = settings.Movement != _settings.Movement;
        _settings = settings;
        if (modeChanged)
        {
            _behavior.Reset();
        }

        if (restorePosition)
        {
            _monitor = _monitors.ById(settings.Position?.Monitor);
            _behavior.Place(settings.Position is { } saved ? MonitorService.Restore(_monitor, saved) : new Position(_monitor.Work.Left + _monitor.Work.Width * .65, _monitor.Work.Top + _monitor.Work.Height - 240 * _monitor.Scale));
        }

        ResizeForMonitor();
        Opacity = settings.Opacity;
        _behavior.Place(_monitor.Work.Clamp(_behavior.Position, PixelWidth, PixelHeight));
        if (settings.Visible)
        {
            if (!IsVisible)
            {
                Show();
            }

            MoveNative();
        }
        else
        {
            EndDrag();
            Hide();
        }
    }

    public void React(PetState state)
    {
        _actionPlayed = null;
        _behavior.React(state);
    }

    public void SetPreviewActive(bool active)
    {
        _previewActive = active;
        UpdateTimer();
    }

    private void UpdateTimer()
    {
        if (IsVisible || _previewActive)
        {
            if (!_timer.IsEnabled)
            {
                _clock.Restart();
                _timer.Start();
            }
        }
        else
        {
            _timer.Stop();
            _clock.Reset();
        }
    }

    public void PlaceForDiagnostics(Position point)
    {
        _monitor = _monitors.Nearest(point);
        _behavior.Place(_monitor.Work.Clamp(point, PixelWidth, PixelHeight));
        MoveNative();
    }

    public SavedPosition CapturePosition() => MonitorService.Save(_monitor, _behavior.Position);
    private double PixelWidth => Width * _monitor.Scale;
    private double PixelHeight => Height * _monitor.Scale;

    private void Tick(object? sender, EventArgs e)
    {
        var dt = _clock.Elapsed.TotalSeconds;
        _clock.Restart();
        if (IsVisible)
        {
            _behavior.Tick(dt, _settings.Movement, _monitor.Work, PixelWidth, PixelHeight, _settings.Speed * _monitor.Scale, _settings.Activity, _monitor.Edge);
            MoveNative();
        }

        if (_atlas is null || _controller is null)
        {
            return;
        }

        var animation = _controller.Resolve(IsVisible ? _behavior.Animation : "idle");
        // A non-looping action holds idle after completion until the FSM leaves its action state.
        if (_player.Completed && _player.Name == animation)
        {
            animation = "idle";
        }

        if (_player.Name == "idle" && animation != "idle" && _behavior.State is PetState.Jumping or PetState.Waving && _actionPlayed == _behavior.State)
        {
            animation = "idle";
        }

        if (_behavior.State is not (PetState.Jumping or PetState.Waving))
        {
            _actionPlayed = null;
        }
        else if (animation != "idle")
        {
            _actionPlayed = _behavior.State;
        }

        _player.Play(animation, _atlas.Manifest.Animations[animation]);
        _player.Advance(dt);
        UpdateFrame();
        var positionInterval = _behavior.State is PetState.WalkingLeft or PetState.WalkingRight || _settings.Movement == MovementMode.Taskbar ? 1d / 30 : .1;
        _timer.Interval = TimeSpan.FromSeconds(Math.Clamp(Math.Min(positionInterval, _player.SecondsUntilNextFrame), 1d / 120, .1));
    }

    private PetState? _actionPlayed;
    private void UpdateFrame()
    {
        if (_atlas is null)
        {
            return;
        }

        var next = _atlas.Animations[_player.Name][_player.Frame];
        if (ReferenceEquals(next, _frame))
        {
            return;
        }

        _frame = next;
        _image.Source = next.Image;
        FrameChanged?.Invoke(next);
    }

    private void MoveNative()
    {
        if (_handle == 0 || !IsVisible)
        {
            return;
        }

        var p = _behavior.Position;
        var placement = ((int)Math.Round(p.X), (int)Math.Round(p.Y), (int)Math.Ceiling(PixelWidth), (int)Math.Ceiling(PixelHeight));
        if (_lastPlacement == placement)
        {
            return;
        }

        if (NativeMethods.SetWindowPos(_handle, 0, placement.Item1, placement.Item2, placement.Item3, placement.Item4, 0x14))
        {
            _lastPlacement = placement;
        }
    }

    private void ResizeForMonitor()
    {
        var width = (_atlas?.Manifest.FrameWidth ?? 192) * _settings.Scale;
        var height = (_atlas?.Manifest.FrameHeight ?? 208) * _settings.Scale;
        var fit = Math.Min(1, Math.Min(_monitor.Work.Width / (width * _monitor.Scale), _monitor.Work.Height / (height * _monitor.Scale)));
        Width = width * fit;
        Height = height * fit;
    }

    private void BeginDrag()
    {
        if (_frame is null)
        {
            return;
        }

        var cursor = MessageCursor();
        _dragOffset = new Position(cursor.X - _behavior.Position.X, cursor.Y - _behavior.Position.Y);
        _behavior.Reset();
        _behavior.IsDragging = true;
        NativeMethods.SetCapture(_handle);
        DragActivityChanged?.Invoke("started");
    }

    private void ContinueDrag(nint buttons)
    {
        if (!_behavior.IsDragging)
        {
            return;
        }

        if (((long)buttons & 1) == 0)
        {
            EndDrag("no-button");
            return;
        }

        var cursor = MessageCursor();
        var nextMonitor = _monitors.Nearest(new Position(cursor.X, cursor.Y));
        if (nextMonitor.Id != _monitor.Id)
        {
            _monitor = nextMonitor;
            ResizeForMonitor();
        }

        _behavior.Place(_monitor.Work.Clamp(new Position(cursor.X - _dragOffset.X, cursor.Y - _dragOffset.Y), PixelWidth, PixelHeight));
        MoveNative();
    }

    private void EndDrag(string reason = "released")
    {
        if (!_behavior.IsDragging)
        {
            return;
        }

        _behavior.IsDragging = false;
        if (NativeMethods.GetCapture() == _handle)
        {
            NativeMethods.ReleaseCapture();
        }

        _behavior.Reset();
        DragActivityChanged?.Invoke("completed-" + reason);
        PositionSaved?.Invoke(CapturePosition());
    }

    private static NativeMethods.Point MessageCursor()
    {
        // GetCursorPos can already point to the end of a fast, queued drag.
        // GetMessagePos retains the physical position belonging to each mouse message.
        var packed = NativeMethods.GetMessagePos();
        return NativeMethods.UnpackCoordinates(packed);
    }

    private nint WindowProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        // Handle capture at the HWND boundary; coordinates stay in physical pixels throughout the drag.
        if (message == 0x201)
        {
            BeginDrag();
            handled = true;
            return 0;
        }

        if (message == 0x200 && _behavior.IsDragging)
        {
            ContinueDrag(wParam);
            handled = true;
            return 0;
        }

        if (message == 0x202)
        {
            EndDrag();
            handled = true;
            return 0;
        }

        if (message == 0x215)
        {
            EndDrag("capture-changed");
        }

        if (message == 0x205)
        {
            SettingsRequested?.Invoke();
            handled = true;
            return 0;
        }

        if (message == NativeMethods.WmNcHitTest && _frame is not null && !_behavior.IsDragging)
        {
            NativeMethods.GetWindowRect(hwnd, out var rect);
            var cursor = NativeMethods.UnpackCoordinates(lParam);
            int x = cursor.X, y = cursor.Y;
            handled = true;
            return _frame.Hit((x - rect.Left) / (double)Math.Max(1, rect.Width), (y - rect.Top) / (double)Math.Max(1, rect.Height)) ? 1 : -1;
        }

        if (message is NativeMethods.WmDpiChanged or NativeMethods.WmDisplayChange or NativeMethods.WmSettingChange)
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (_closing)
                {
                    return;
                }

                var saved = CapturePosition();
                _monitors.Refresh();
                _monitor = _monitors.ById(saved.Monitor);
                ResizeForMonitor();
                _lastPlacement = null;
                _behavior.Place(_monitor.Work.Clamp(MonitorService.Restore(_monitor, saved), PixelWidth, PixelHeight));
                MoveNative();
            }, DispatcherPriority.Loaded);
        }

        return 0;
    }

    public void ShutdownWindow()
    {
        _closing = true;
        _timer.Stop();
        _image.Source = null;
        _atlas = null;
        _frame = null;
        _source?.RemoveHook(WindowProc);
        Close();
    }
}
