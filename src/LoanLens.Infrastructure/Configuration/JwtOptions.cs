namespace LoanLens.Infrastructure.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = "LoanLensSuperSecretKeyForJwtSigningMustBeVeryLongAndSecure2026!";
    public string Issuer { get; set; } = "LoanLens.Api";
    public string Audience { get; set; } = "LoanLens.Client";
    public int ExpiryHours { get; set; } = 24;
}
