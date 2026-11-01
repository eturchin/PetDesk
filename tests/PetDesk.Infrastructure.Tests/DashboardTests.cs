using PetDesk.App.ViewModels;
using PetDesk.Core.Appearance;
using PetDesk.Core.Behavior;
using PetDesk.Core.Models;
using PetDesk.Infrastructure.Assets;
using Xunit;

namespace PetDesk.Infrastructure.Tests;

public class DashboardTests
{
    [Fact]
    public void DashboardCommandsUseExistingSettingsContract()
    {
        var state = new AppSettings();
        SettingsViewModel? vm = null;
        vm = new SettingsViewModel(state, settings =>
        {
            state = settings;
            vm!.Sync(settings);
        }, _ =>
        {
        }, _ =>
        {
        });
        using var disposal = vm;
        vm.ToggleVisibilityCommand.Execute(null);
        Assert.False(state.Visible);
        vm.SetMovementCommand.Execute("Free");
        Assert.Equal(MovementMode.Free, state.Movement);
        vm.ScalePercent = 150;
        vm.OpacityPercent = 60;
        vm.SpeedPercent = 150;
        Assert.Equal(1.5, state.Scale);
        Assert.Equal(.6, state.Opacity);
        Assert.Equal(112.5, state.Speed);
        vm.LanguageIndex = 1;
        Assert.Equal("en", state.Language);
    }

    [Fact]
    public void PetSelectionAndAddItemDispatchCorrectActions()
    {
        string? selected = null;
        var imported = false;
        using var vm = new SettingsViewModel(new AppSettings(), _ =>
        {
        }, entry => selected = entry.Id, _ =>
        {
        });
        var pet = new PetEntry(Path.GetTempPath(), new PetManifest { Id = "pet-existing", Name = "Existing pet" });
        vm.ReplacePets(new[] { pet });
        vm.ImportFileRequested += () => imported = true;
        vm.SelectedPet = vm.Pets[0];
        Assert.Equal(pet.Id, selected);
        vm.SelectedPet = vm.Pets[^1];
        Assert.True(imported);
        Assert.True(vm.Pets[^1].IsAdd);
        Assert.Single(vm.LibraryPets);
    }

    [Fact]
    public void CustomEditorRefusesBadHexAndBuiltInDeletion()
    {
        var state = new AppSettings();
        SettingsViewModel? vm = null;
        vm = new SettingsViewModel(state, settings =>
        {
            state = settings;
            vm!.Sync(settings);
        }, _ =>
        {
        }, _ =>
        {
        });
        using var disposal = vm;
        vm.ReplacePalettes(BuiltInPalettes.All);
        Assert.False(vm.DeletePaletteCommand.CanExecute(null));
        vm.CreatePaletteCommand.Execute(null);
        Assert.True(vm.IsEditor);
        Assert.True(vm.SavePaletteCommand.CanExecute(null));
        vm.Editor!.Accent = "#WRONG!";
        Assert.False(vm.SavePaletteCommand.CanExecute(null));
        Assert.Null(vm.Editor.Candidate);
        vm.Editor.Accent = "#336699";
        Assert.True(vm.SavePaletteCommand.CanExecute(null));
        PaletteDefinition? saved = null;
        vm.SavePaletteRequested += palette => saved = palette;
        vm.SavePaletteCommand.Execute(null);
        Assert.NotNull(saved);
        Assert.StartsWith("custom-", saved.Id);
        vm.UsePaletteCommand.Execute(new PaletteChoice(BuiltInPalettes.All[1]));
        Assert.Equal("ocean", state.PaletteId);
        vm.ResetAppearanceCommand.Execute(null);
        Assert.Equal("amethyst", state.PaletteId);
        Assert.Equal("Dark", state.Theme);
    }
}
