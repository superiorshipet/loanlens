namespace LoanLens.Domain.Enums;

public enum IncomeStability
{
    StableEmployed = 1,
    Freelance = 2,
    BusinessOwner = 3
}

public enum InterestCalculationMethod
{
    Reducing = 1,
    Flat = 2
}

public enum InterestRateType
{
    Fixed = 1,
    Variable = 2
}

public enum RiskLevel
{
    Safe = 1,       // Low risk (< 35% DTI, healthy runway)
    Moderate = 2,   // Watch zone (35% - 40% DTI)
    High = 3,       // High risk (40% - 50% DTI or low runway)
    Critical = 4    // Extreme risk (> 50% DTI / CBE limits exceeded)
}
