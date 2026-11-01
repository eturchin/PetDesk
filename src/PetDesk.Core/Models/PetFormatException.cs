using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetDesk.Core.Models;

public sealed class PetFormatException(string message) : Exception(message);
