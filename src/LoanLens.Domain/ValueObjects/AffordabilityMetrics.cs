using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record AffordabilityMetrics(
    decimal ProposedInstallment,
    decimal ExistingInstallments,
    decimal TotalDebtObligations,
    decimal NetMonthlyIncome,
    decimal DtiPercentage,
    decimal DisposableIncomeAfterInstallment,
    decimal EmergencyRunwayMonths,
    RiskLevel AffordabilityRisk,
    string SummaryMessage,
    IReadOnlyList<string> RiskFactors);
