namespace LoanLens.Domain.ValueObjects;

public record RateSensitivitySummary(
    decimal BaseAnnualRatePercentage,
    decimal BaseMonthlyInstallment,
    decimal BaseTotalInterest,
    IReadOnlyList<RateSensitivityScenarioItem> Scenarios,
    string KeyTakeawayAdvice);
