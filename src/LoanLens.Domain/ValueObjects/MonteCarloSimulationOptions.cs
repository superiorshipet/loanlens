namespace LoanLens.Domain.ValueObjects;

public record MonteCarloSimulationOptions(
    int Iterations = 5000,
    double? CustomIncomeVolatilitySigma = null,
    double? CustomEmergencyProbability = null,
    decimal? CustomEmergencyCost = null);
