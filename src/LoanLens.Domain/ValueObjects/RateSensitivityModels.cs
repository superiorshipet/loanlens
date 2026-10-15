namespace LoanLens.Domain.ValueObjects;

public record RateSensitivityScenarioItem(
    string ShiftLabel,
    decimal ShiftPercentagePoints,
    decimal ResultingAnnualRatePercentage,
    decimal MonthlyInstallment,
    decimal MonthlyInstallmentDelta,
    decimal TotalInterestPaid,
    decimal TotalInterestDelta,
    decimal TotalRepaymentAmount,
    string ImpactSeverity);

public record RateSensitivitySummary(
    decimal BaseAnnualRatePercentage,
    decimal BaseMonthlyInstallment,
    decimal BaseTotalInterest,
    IReadOnlyList<RateSensitivityScenarioItem> Scenarios,
    string KeyTakeawayAdvice);
