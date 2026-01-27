namespace Monolith.WebAPI;

public static class LanguageHelper
{
    public static readonly string[] ValidLanguages = ["EN", "TR"];

    public static bool IsValidLanguage(string language)
        => ValidLanguages.Contains(language.ToUpper());
}