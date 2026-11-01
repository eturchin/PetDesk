namespace PetDesk.App.Views.Pages;

public partial class PaletteEditorView : UserControl
{
    public PaletteEditorView() => InitializeComponent();
    public void UpdatePreview(PetDesk.Core.Appearance.PaletteDefinition palette, bool dark) => PalettePreview.Resources = PetDesk.App.Presentation.ThemeEngine.Create(palette, dark);
}
