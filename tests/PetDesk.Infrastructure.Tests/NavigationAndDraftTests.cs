using PetDesk.App.ViewModels;
using PetDesk.Core.Appearance;
using PetDesk.Core.Models;
using Xunit;

namespace PetDesk.Infrastructure.Tests;

public class NavigationAndDraftTests
{
    [Theory]
    [InlineData(DashboardSection.Dashboard)]
    [InlineData(DashboardSection.Settings)]
    [InlineData(DashboardSection.PaletteEditor)]
    [InlineData(DashboardSection.About)]
    public void TypedNavigationSelectsExactlyOnePanelSection(DashboardSection section)
    {
        using var viewModel = new SettingsViewModel(new AppSettings(), _ => { }, _ => { }, _ => { });

        viewModel.NavigateCommand.Execute(section);

        Assert.Equal(section, viewModel.CurrentSection);
        Assert.Single(new[] { viewModel.IsDashboard, viewModel.IsSettings, viewModel.IsEditor, viewModel.IsAbout },
            visible => visible);
    }

    [Fact]
    public void InvalidNavigationReturnsToTheDashboard()
    {
        using var viewModel = new SettingsViewModel(new AppSettings(), _ => { }, _ => { }, _ => { });
        viewModel.Navigate(DashboardSection.Settings);

        viewModel.NavigateCommand.Execute((DashboardSection)999);

        Assert.True(viewModel.IsDashboard);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Palette\nname")]
    [InlineData("This palette name is longer than forty characters")]
    public void InvalidPaletteNamesProduceAnInlineErrorAndCannotBeSaved(string name)
    {
        var editor = new PaletteEditorViewModel(BuiltInPalettes.Default, copy: true) { Name = name };

        Assert.False(editor.IsValid);
        Assert.Null(editor.Candidate);
        Assert.NotEmpty(editor[nameof(editor.Name)]);
    }
}
