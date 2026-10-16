namespace LoanLens.Application.DTOs;

public record SavedScenarioSummaryDto(
    Guid Id,
    DateTime CreatedAt,
    string? CustomerName,
    decimal Principal,
    int TenorMonths,
    decimal MonthlyInstallment,
    decimal DtiPercentage,
    decimal RiskScore,
    string RiskCategory,
    string VerdictHeadline);
