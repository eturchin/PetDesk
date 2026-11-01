using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetDesk.Core.Models;

public readonly record struct FrameRect(int X, int Y, int Width, int Height);
