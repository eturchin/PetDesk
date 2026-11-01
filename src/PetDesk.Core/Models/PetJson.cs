using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetDesk.Core.Models;

public static class PetJson
{
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        },
        MaxDepth = 24
    };
}
