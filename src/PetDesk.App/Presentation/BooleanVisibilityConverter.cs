using System.Windows.Data;
using System.Windows.Markup;
using PetDesk.Core.Localization;

namespace PetDesk.App.Presentation;

public sealed class BooleanVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var show = value is true;
        if (parameter is string text && text == "invert")
        {
            show = !show;
        }

        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Binding.DoNothing;
}
