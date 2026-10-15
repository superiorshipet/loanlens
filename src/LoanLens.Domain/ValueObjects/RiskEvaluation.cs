using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record RiskEvaluation(
    decimal TotalRiskScore, // 0 (Safest) to 100 (Most Dangerous)
    RiskLevel Category,
    string RiskLabel,
    IReadOnlyList<RiskFactorScore> Breakdown,
    string ComprehensiveSummary);
