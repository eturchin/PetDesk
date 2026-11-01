using PetDesk.Core.Behavior;

namespace PetDesk.Core.Models;

public sealed record SavedPosition(string Monitor, double OffsetX, double OffsetY, double Dpi = 96);
