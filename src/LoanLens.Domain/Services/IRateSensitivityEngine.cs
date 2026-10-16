using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IRateSensitivityEngine
{
    RateSensitivitySummary AnalyzeSensitivity(
        LoanParameters loan,
        decimal currentMonthlyInstallment,
        string language = "en");
}
