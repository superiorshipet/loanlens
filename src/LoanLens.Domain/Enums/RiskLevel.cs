namespace LoanLens.Domain.Enums;

public enum RiskLevel
{
    Safe = 1,       // Low risk (< 35% DTI, healthy runway)
    Moderate = 2,   // Watch zone (35% - 40% DTI)
    High = 3,       // High risk (40% - 50% DTI or low runway)
    Critical = 4    // Extreme risk (> 50% DTI / CBE limits exceeded)
}
