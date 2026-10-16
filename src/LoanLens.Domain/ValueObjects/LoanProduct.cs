using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record LoanProduct(
    string Id,
    string ProviderName,
    string ProductName,
    string Category,
    decimal MinPrincipal,
    decimal MaxPrincipal,
    int MinTenorMonths,
    int MaxTenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod,
    decimal AdminFeePercentage,
    decimal MinMonthlyIncomeRequired,
    int MaxAgeAllowed,
    string FeaturesSummary,
    decimal EarlySettlementFeePercentage = 3.0m,
    decimal LatePenaltyPercentage = 2.0m);
