using LoanLens.Domain.Localization;
using Microsoft.AspNetCore.Mvc;

namespace LoanLens.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string ResolveLanguage(string? explicitLang = null, params string?[] textHints)
    {
        // 1. Explicit query parameter ?lang=
        if (Request.Query.TryGetValue("lang", out var queryLang) && !string.IsNullOrWhiteSpace(queryLang))
            return LoanLensLanguage.IsArabic(queryLang.ToString()) ? LoanLensLanguage.Arabic : LoanLensLanguage.English;

        // 2. Explicit language parameter in payload
        if (!string.IsNullOrWhiteSpace(explicitLang))
            return LoanLensLanguage.IsArabic(explicitLang) ? LoanLensLanguage.Arabic : LoanLensLanguage.English;

        // 3. Intelligent auto-detection from text hints (Arabic question, Arabic customer name, etc.)
        if (textHints != null)
        {
            foreach (var hint in textHints)
            {
                if (LoanLensLanguage.ContainsArabic(hint))
                    return LoanLensLanguage.Arabic;
            }
        }

        // 4. Accept-Language header
        var acceptLang = Request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrWhiteSpace(acceptLang) && LoanLensLanguage.IsArabic(acceptLang))
            return LoanLensLanguage.Arabic;

        return LoanLensLanguage.English;
    }
}
