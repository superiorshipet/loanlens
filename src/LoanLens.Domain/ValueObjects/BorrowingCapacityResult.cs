namespace LoanLens.Domain.ValueObjects;

public record BorrowingCapacityResult(
    decimal LegalMaxPrincipal,
    decimal SafeRecommendedPrincipal,
    decimal LegalMaxMonthlyInstallment,
    decimal SafeMonthlyInstallment,
    int TenorMonthsUsed,
    decimal AnnualInterestRatePercentage,
    decimal CurrentDtiPercentage,
    decimal LegalMaxDtiPercentage,
    decimal SafeDtiPercentage,
    decimal RemainingDisposableIncomeAtMax,
    decimal RemainingDisposableIncomeAtSafe,
    string AdviceExplanation);
