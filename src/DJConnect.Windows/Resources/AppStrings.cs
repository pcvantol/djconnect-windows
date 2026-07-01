using System.Globalization;
using System.Resources;

namespace DJConnect.Windows.Resources;

public static class AppStrings
{
    public static readonly string[] SupportedLanguages = ["en", "nl", "de", "fr", "es"];
    private static readonly ResourceManager ResourceManager = new("DJConnect.Windows.Resources.Strings", typeof(AppStrings).Assembly);

    public static string CurrentLanguage { get; private set; } = "en";

    public static void UseLanguage(string? language)
    {
        CurrentLanguage = NormalizeLanguage(language);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(CurrentLanguage);
    }

    public static string Get(string key)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
            ?? ResourceManager.GetString(key, CultureInfo.InvariantCulture)
            ?? key;
    }

    public static string Format(string key, params object?[] args)
    {
        return string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }

    public static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var candidate = language.Trim().Split(['-', '_'], 2)[0].ToLowerInvariant();
        return SupportedLanguages.Contains(candidate, StringComparer.Ordinal) ? candidate : "en";
    }

    public static string NormalizeApiLocale(string? language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            var normalized = language.Trim().Replace('_', '-');
            var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var lang = parts[0].ToLowerInvariant();
                if (SupportedLanguages.Contains(lang, StringComparer.Ordinal))
                {
                    return $"{lang}-{parts[1].ToUpperInvariant()}";
                }
            }
        }

        return NormalizeLanguage(language) switch
        {
            "nl" => "nl-NL",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "es" => "es-ES",
            _ => "en-US"
        };
    }
}
