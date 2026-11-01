using PetDesk.Core.Localization;

namespace PetDesk.App.Views;

/// <summary>App dialogs stay centered on the tray panel and use the selected app language.</summary>
public sealed class ActionDialog : Window
{
    private ActionDialog(Window owner, string title, string message, bool confirmation)
    {
        Owner = owner;
        Title = title;
        Width = 376;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        FontFamily = new FontFamily("Segoe UI");
        FontSize = 13;
        SetResourceReference(ForegroundProperty, "TextBrush");
        var body = new StackPanel();
        body.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 14) });
        body.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, LineHeight = 21, MaxHeight = 260 });
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 22, 0, 0)
        };
        var cancel = new Button
        {
            Content = LocalizationService.Current[confirmation ? "Common.Cancel" : "Common.Done"],
            IsCancel = true,
            MinWidth = 90
        };
        cancel.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(cancel);
        if (confirmation)
        {
            var confirm = new Button
            {
                Content = LocalizationService.Current["Common.Delete"],
                MinWidth = 90,
                Margin = new Thickness(8, 0, 0, 0),
                Style = (Style)FindResource("PrimaryButton")
            };
            confirm.Click += (_, _) => DialogResult = true;
            buttons.Children.Add(confirm);
        }

        body.Children.Add(buttons);
        var shell = new Border
        {
            Margin = new Thickness(6),
            Padding = new Thickness(22),
            CornerRadius = new CornerRadius(18),
            BorderThickness = new Thickness(1),
            Child = body
        };
        shell.SetResourceReference(Border.BackgroundProperty, "PanelGradient");
        shell.SetResourceReference(Border.BorderBrushProperty, "RingBrush");
        Content = shell;
        Loaded += (_, _) => cancel.Focus();
    }

    public static bool Confirm(Window owner, string message) => new ActionDialog(owner, LocalizationService.Current["Common.Confirm"], message, true).ShowDialog() == true;
    public static void ShowMessage(Window owner, string title, string message) => new ActionDialog(owner, title, message, false).ShowDialog();
}
