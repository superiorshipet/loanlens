namespace LoanLens.Domain.Localization;

public static class LoanLensLanguage
{
    public const string English = "en";
    public const string Arabic = "ar";

    public static bool IsArabic(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        var clean = language.Trim().ToLowerInvariant();
        return clean.StartsWith("ar");
    }

    public static string Normalize(string? language) =>
        IsArabic(language) ? Arabic : English;
}
