using System.Windows.Data;
using System.Windows.Markup;
using PetDesk.Core.Localization;

namespace PetDesk.App.Presentation;

public sealed class LocExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => new Binding($"[{key}]")
    {
        Source = LocalizationService.Current,
        Mode = BindingMode.OneWay
    }.ProvideValue(serviceProvider);
}
