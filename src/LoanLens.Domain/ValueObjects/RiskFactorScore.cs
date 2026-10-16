namespace LoanLens.Domain.ValueObjects;

public record RiskFactorScore(
    string FactorName,
    decimal Score,
    decimal MaxWeight,
    string Details);
