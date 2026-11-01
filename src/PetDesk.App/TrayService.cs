using System.Drawing;
using System.Windows.Forms;
using PetDesk.Core.Behavior;
using PetDesk.Core.Localization;
using PetDesk.Infrastructure.Windows;

namespace PetDesk.App;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu = new ContextMenuStrip();
    private readonly Icon _ownedIcon;
    public TrayService(DesktopApplication host)
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/PetDesk.ico"));
        using (resource.Stream)
        using (var loaded = new Icon(resource.Stream))
            _ownedIcon = (Icon)loaded.Clone();
        _icon = new NotifyIcon
        {
            Text = LocalizationService.Current["Tray.Tooltip"],
            Icon = _ownedIcon,
            ContextMenuStrip = _menu,
            Visible = true
        };
        _icon.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            NativeMethods.GetCursorPos(out var cursor);
            host.ToggleDashboard(new Position(cursor.X, cursor.Y));
        };
        _menu.Opening += (_, _) =>
        {
            var l = LocalizationService.Current;
            foreach (var old in _menu.Items.Cast<ToolStripItem>().ToArray())
            {
                old.Dispose();
            }

            _menu.Items.Clear();
            _menu.Items.Add(l["Tray.Dashboard"], null, (_, _) => host.ShowDashboard());
            _menu.Items.Add(l[host.Settings.Visible ? "Tray.Hide" : "Tray.Show"], null, (_, _) => host.ToggleVisible());
            var pets = new ToolStripMenuItem(l["Tray.Pets"]);
            foreach (var choice in host.ViewModel.LibraryPets)
            {
                if (choice.Entry is { } pet)
                {
                    var item = new ToolStripMenuItem(pet.Name)
                    {
                        Checked = pet.Id == host.Settings.SelectedPetId
                    };
                    item.Click += (_, _) => host.SelectPet(pet);
                    pets.DropDownItems.Add(item);
                }
            }

            _menu.Items.Add(pets);
            var movement = new ToolStripMenuItem(l["Dashboard.Movement"]);
            foreach (var (key, mode) in new[]
            {
                ("Mode.Free", MovementMode.Free),
                ("Mode.FixedFull", MovementMode.Fixed),
                ("Mode.Taskbar", MovementMode.Taskbar)
            }

            )
            {
                var item = new ToolStripMenuItem(l[key])
                {
                    Checked = host.Settings.Movement == mode
                };
                item.Click += (_, _) => host.SetMovement(mode);
                movement.DropDownItems.Add(item);
            }

            _menu.Items.Add(movement);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(l["Dashboard.Settings"], null, (_, _) => host.ShowDashboard(settingsPage: true));
            _menu.Items.Add(l["Menu.Exit"], null, (_, _) => host.Exit());
        };
        LocalizationService.Current.PropertyChanged += LanguageChanged;
    }

    private void LanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => _icon.Text = LocalizationService.Current["Tray.Tooltip"];
    public void Dispose()
    {
        LocalizationService.Current.PropertyChanged -= LanguageChanged;
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
        _ownedIcon.Dispose();
    }
}
