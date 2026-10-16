using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record ConsolidationTargetOffer(
    string ProviderName,
    decimal AnnualInterestRatePercentage,
    int TenorMonths,
    decimal AdminFeePercentage = 1.5m,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing);
