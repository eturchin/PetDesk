namespace PetDesk.App.Views;

/// <summary>Opt-in QA surface: test hit testing without capturing unrelated apps or user files.</summary>
public sealed class HitTestWindow : Window
{
    public HitTestWindow(bool virtualDesktop = false)
    {
        Title = "PetDesk — проверка прозрачности";
        Width = 800;
        Height = 620;
        Left = 30;
        Top = 40;
        if (virtualDesktop)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
        }

        Background = new SolidColorBrush(Color.FromRgb(24, 48, 68));
        Foreground = Brushes.White;
        var grid = new Grid
        {
            Margin = new Thickness(24)
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var heading = new TextBlock
        {
            Text = "Проверка прозрачности",
            FontSize = 24,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 24)
        };
        var button = new Button
        {
            Content = "Кликов под питомцем: 0",
            Background = Brushes.LightSkyBlue,
            Foreground = Brushes.Black,
            FontSize = 18
        };
        var clicks = 0;
        button.Click += (_, _) => button.Content = $"Кликов под питомцем: {++clicks}";
        var hint = new TextBlock
        {
            Text = "Прозрачный угол должен пропускать клики к голубой кнопке.\nВидимая часть питомца должна перетаскиваться, не нажимая кнопку.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 24, 0, 0)
        };
        Grid.SetRow(button, 1);
        Grid.SetRow(hint, 2);
        grid.Children.Add(heading);
        grid.Children.Add(button);
        grid.Children.Add(hint);
        Content = grid;
    }
}
