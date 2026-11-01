using System.Threading;
using PetDesk.App.Views;
using PetDesk.Infrastructure.Assets;
using PetDesk.Infrastructure.Configuration;
using PetDesk.Core.Localization;
using PetDesk.Infrastructure.Windows;
using PetDesk.App.Presentation;
using PetDesk.Core.Appearance;

namespace PetDesk.App;

public partial class App : Application
{
    private DesktopApplication? _desktop;
    private Mutex? _mutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _registration;
    private EventWaitHandle? _exitEvent;
    private RegisteredWaitHandle? _exitRegistration;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--hit-test-target"))
        {
            ThemeEngine.Apply(BuiltInPalettes.Default, true);
            var diagnostic = new HitTestWindow(e.Args.Contains("--virtual-desktop"))
            {
                Title = "PetDesk — подложка другого процесса"
            };
            MainWindow = diagnostic;
            diagnostic.Closed += (_, _) => Shutdown();
            diagnostic.Show();
            return;
        }

        try
        {
            var index = Array.IndexOf(e.Args, "--data-dir");
            if (index >= 0 && index + 1 >= e.Args.Length)
            {
                throw new ArgumentException("Error.Args");
            }

            var paths = new AppPaths(index >= 0 ? e.Args[index + 1] : null);
            if (e.Args.Contains("--unregister-startup"))
            {
                new AutostartService().RemoveIfOwned(Path.Combine(AppContext.BaseDirectory, "PetDesk.exe"));
                Shutdown();
                return;
            }

            var instanceId = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(paths.Root.ToUpperInvariant())))[..20];
            _mutex = new Mutex(true, "Local\\PetDesk-" + instanceId, out var firstInstance);
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\PetDesk-" + instanceId + "-show");
            _exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\PetDesk-" + instanceId + "-exit");
            if (e.Args.Contains("--exit"))
            {
                if (!firstInstance)
                {
                    _exitEvent.Set();
                    try
                    {
                        if (!_mutex.WaitOne(TimeSpan.FromSeconds(10)))
                        {
                            Shutdown(2);
                            return;
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                    }

                    _mutex.ReleaseMutex();
                }

                Shutdown();
                return;
            }

            if (!firstInstance)
            {
                _showEvent.Set();
                Shutdown();
                return;
            }

            paths.EnsureDirectories();
            _desktop = new DesktopApplication(this, paths, e.Args.Contains("--ui-test"));
            _registration = ThreadPool.RegisterWaitForSingleObject(_showEvent, (_, _) => Dispatcher.BeginInvoke(() => _desktop.ShowDashboard()), null, Timeout.Infinite, false);
            _exitRegistration = ThreadPool.RegisterWaitForSingleObject(_exitEvent, (_, _) => Dispatcher.BeginInvoke(_desktop.Exit), null, Timeout.Infinite, false);
            DispatcherUnhandledException += (_, args) =>
            {
                _desktop.LogError(args.Exception);
                args.Handled = true;
                if (PetLibrary.IsImportError(args.Exception))
                {
                    _desktop.ShowError(args.Exception);
                }
                else
                {
                    MessageBox.Show(LocalizationService.Current["Error.Fatal"], "PetDesk", MessageBoxButton.OK, MessageBoxImage.Error);
                    _desktop.Exit();
                }
            };
            _desktop.Start(e.Args.Contains("--startup"), e.Args.Contains("--diagnostics"), e.Args.Contains("--virtual-desktop"));
        }
        catch (Exception ex)
        {
            MessageBox.Show(LocalizationService.Current["Error.Launch"] + "\n\n" + LocalizationService.Current[DesktopApplication.ErrorKey(ex)], "PetDesk", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _registration?.Unregister(null);
        _exitRegistration?.Unregister(null);
        _showEvent?.Dispose();
        _exitEvent?.Dispose();
        _desktop?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
