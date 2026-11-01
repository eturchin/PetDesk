using PetDesk.App.Views;

namespace PetDesk.App.Presentation;

/// <summary>Keeps the tray panel open while a child popup temporarily owns focus.</summary>
public static class PopupLifetime
{
    public static readonly DependencyProperty TrackProperty = DependencyProperty.RegisterAttached("Track", typeof(bool), typeof(PopupLifetime), new PropertyMetadata(false, TrackChanged));
    private static readonly DependencyProperty LeaseProperty = DependencyProperty.RegisterAttached("Lease", typeof(IDisposable), typeof(PopupLifetime));
    public static bool GetTrack(DependencyObject element) => (bool)element.GetValue(TrackProperty);
    public static void SetTrack(DependencyObject element, bool value) => element.SetValue(TrackProperty, value);
    private static void TrackChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is ComboBox comboBox)
        {
            if ((bool)args.NewValue)
            {
                comboBox.DropDownOpened += Opened;
                comboBox.DropDownClosed += Closed;
            }
            else
            {
                comboBox.DropDownOpened -= Opened;
                comboBox.DropDownClosed -= Closed;
            }
        }
        else if (element is ContextMenu menu)
        {
            if ((bool)args.NewValue)
            {
                menu.Opened += Opened;
                menu.Closed += Closed;
            }
            else
            {
                menu.Opened -= Opened;
                menu.Closed -= Closed;
            }
        }

        if (!(bool)args.NewValue)
        {
            Release(element);
        }
    }

    private static void Opened(object? sender, EventArgs args)
    {
        if (sender is not DependencyObject element)
        {
            return;
        }

        Release(element);
        var owner = sender is ContextMenu menu ? menu.PlacementTarget : element;
        if (owner is not null && Window.GetWindow(owner) is SettingsWindow window)
        {
            element.SetValue(LeaseProperty, window.TrackPopup());
        }
    }

    private static void Closed(object? sender, EventArgs args)
    {
        if (sender is DependencyObject element)
        {
            Release(element);
        }
    }

    private static void Release(DependencyObject element)
    {
        var lease = element.GetValue(LeaseProperty) as IDisposable;
        element.ClearValue(LeaseProperty);
        lease?.Dispose();
    }
}
