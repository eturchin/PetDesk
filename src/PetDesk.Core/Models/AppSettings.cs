using PetDesk.Core.Behavior;

namespace PetDesk.Core.Models;

public sealed record AppSettings
{
    public const double DefaultSpeed = 75;
    public int SchemaVersion { get; init; } = 1;
    public string? SelectedPetId { get; init; }
    public double Scale { get; init; } = 1;
    public double Opacity { get; init; } = 1;
    public bool Visible { get; init; } = true;
    public MovementMode Movement { get; init; } = MovementMode.Taskbar;
    public double Speed { get; init; } = DefaultSpeed;
    public double Activity { get; init; } = .45;
    public bool StartHidden { get; init; }
    public bool CloseSettingsToTray { get; init; } = true;
    public string Theme { get; init; } = "Dark";
    public string PaletteId { get; init; } = "amethyst";
    public string Language { get; init; } = "ru";
    public bool RestorePosition { get; init; } = true;
    public SavedPosition? Position { get; init; }

    public AppSettings Normalize()
    {
        if (SchemaVersion != 1)
        {
            throw new PetFormatException("Error.SettingsSchema");
        }

        return this with
        {
            Scale = double.IsFinite(Scale) ? Math.Clamp(Scale, .5, 2) : 1,
            Opacity = double.IsFinite(Opacity) ? Math.Clamp(Opacity, .3, 1) : 1,
            Speed = double.IsFinite(Speed) ? Math.Clamp(Speed, 10, 250) : DefaultSpeed,
            Activity = double.IsFinite(Activity) ? Math.Clamp(Activity, 0, 1) : .45,
            Movement = Enum.IsDefined(Movement) ? Movement : MovementMode.Taskbar,
            Theme = Theme is "Light" or "Dark" ? Theme : "System",
            PaletteId = Appearance.PaletteDefinition.IsSafeId(PaletteId) ? PaletteId : "amethyst",
            Language = Language == "en" ? "en" : "ru",
            Position = Position is { } p && double.IsFinite(p.OffsetX) && double.IsFinite(p.OffsetY) && p.Dpi is >= 48 and <= 768 ? p : null
        };
    }
}
