namespace LoanLens.Domain.ValueObjects;

public record AlternativeComparison(
    LoanProduct Product,
    decimal MonthlyInstallment,
    decimal TotalInterest,
    decimal TotalCost,
    decimal EffectiveAprPercentage,
    decimal CostDifferenceComparedToCurrent,
    bool IsCheaperThanCurrent);
