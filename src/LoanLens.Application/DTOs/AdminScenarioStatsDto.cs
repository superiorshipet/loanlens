namespace LoanLens.Application.DTOs;

public record AdminScenarioStatsDto(
    int TotalScenarios,
    int ScenariosLast7Days,
    int ScenariosLast30Days,
    decimal TotalPrincipalSimulated,
    decimal AverageLoanAmount,
    int AverageTenorMonths,
    decimal AverageDtiPercentage,
    decimal AverageRiskScore,
    IReadOnlyDictionary<string, int> VerdictBreakdown,
    IReadOnlyDictionary<string, int> RiskCategoryBreakdown
);
