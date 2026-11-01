using System.Globalization;

namespace PetDesk.Core.Appearance;

public static class BuiltInPalettes
{
    public static IReadOnlyList<PaletteDefinition> All { get; } = Array.AsReadOnly(new[] { new PaletteDefinition("amethyst", "Amethyst", "#21152F", "#342044", "#6E3196", "#D05BF2", "#B956F0"), new PaletteDefinition("ocean", "Ocean", "#101E32", "#192E46", "#215D9B", "#48B8FF", "#439BE8"), new PaletteDefinition("emerald", "Emerald", "#102923", "#1B3B34", "#17745E", "#47D7A5", "#38B990"), new PaletteDefinition("rose", "Rose", "#301A30", "#46233E", "#93446F", "#F477BC", "#E866AB"), new PaletteDefinition("amber", "Amber", "#302218", "#47301C", "#956121", "#FFC352", "#E5A938") });
    public static PaletteDefinition Default => All[0];

    public static bool IsBuiltIn(string id) => All.Any(p => p.Id == id);
}
