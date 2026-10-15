using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public enum RecommendationVerdict
{
    TakeLoan = 1,
    AdjustTerms = 2,
    TakeAlternative = 3,
    AvoidLoan = 4
}

public record LoanProduct(
    string Id,
    string ProviderName,
    string ProductName,
    string Category, // e.g. "Commercial Bank", "Consumer Finance", "Microfinance"
    decimal MinPrincipal,
    decimal MaxPrincipal,
    int MinTenorMonths,
    int MaxTenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod,
    decimal AdminFeePercentage,
    decimal MinMonthlyIncomeRequired,
    int MaxAgeAllowed,
    string FeaturesSummary);

public record AlternativeComparison(
    LoanProduct Product,
    decimal MonthlyInstallment,
    decimal TotalInterest,
    decimal TotalCost,
    decimal EffectiveAprPercentage,
    decimal CostDifferenceComparedToCurrent,
    bool IsCheaperThanCurrent);

public record RepaymentStrategyOption(
    string StrategyName,
    string Description,
    decimal MonthlyPayment,
    decimal TotalInterestPaid,
    decimal InterestSavedComparedToStandard,
    int TotalMonthsToPayoff);

public record FinalAdvisorDecision(
    RecommendationVerdict Verdict,
    string VerdictHeadline,
    string DetailedReasoning,
    IReadOnlyList<string> KeyActionableAdvice,
    IReadOnlyList<RepaymentStrategyOption> Strategies,
    IReadOnlyList<AlternativeComparison> TopAlternatives);
