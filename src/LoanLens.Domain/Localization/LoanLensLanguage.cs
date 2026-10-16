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

    public static bool ContainsArabic(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var c in text)
        {
            if (c >= '\u0600' && c <= '\u06FF' ||
                c >= '\u0750' && c <= '\u077F' ||
                c >= '\u08A0' && c <= '\u08FF' ||
                c >= '\uFB50' && c <= '\uFDFF' ||
                c >= '\uFE70' && c <= '\uFEFF')
            {
                return true;
            }
        }
        return false;
    }

    public static string Normalize(string? language) =>
        IsArabic(language) ? Arabic : English;

    public static string DetectLanguage(string? explicitLanguage, params string?[] textHints)
    {
        if (!string.IsNullOrWhiteSpace(explicitLanguage))
            return IsArabic(explicitLanguage) ? Arabic : English;

        if (textHints != null)
        {
            foreach (var hint in textHints)
            {
                if (ContainsArabic(hint))
                    return Arabic;
            }
        }

        return English;
    }
}
