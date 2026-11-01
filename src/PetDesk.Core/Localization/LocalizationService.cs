using System.ComponentModel;
using System.Text.Json;

namespace PetDesk.Core.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, Dictionary<string, string>> Catalogs = LoadCatalogs();
    public static LocalizationService Current { get; } = new LocalizationService();
    public string Language { get; private set; } = "ru";

    public event PropertyChangedEventHandler? PropertyChanged;
    public string this[string key] => Translate(key, Language);
    public void SetLanguage(string language)
    {
        language = language == "en" ? "en" : "ru";
        if (Language == language)
        {
            return;
        }

        Language = language;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public static string Translate(string key, string language) => Catalogs.TryGetValue(language, out var selected) && selected.TryGetValue(key, out var text) ? text : Catalogs["ru"].TryGetValue(key, out var fallback) ? fallback : key;
    public static IReadOnlyDictionary<string, string> Catalog(string language) => new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(Catalogs[language]);
    private static IReadOnlyDictionary<string, Dictionary<string, string>> LoadCatalogs()
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        foreach (var language in new[]
        {
            "ru",
            "en"
        }

        )
        {
            using var stream = typeof(LocalizationService).Assembly.GetManifestResourceStream($"PetDesk.Core.Localization.Strings.{language}.json")!;
            result.Add(language, JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!);
        }

        return result;
    }
}
