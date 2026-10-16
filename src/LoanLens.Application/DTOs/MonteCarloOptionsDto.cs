using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.DTOs;

public record MonteCarloOptionsDto(
    int Iterations = 5000,
    decimal? CustomIncomeVolatilityPercentage = null,
    decimal? CustomEmergencyAnnualProbability = null,
    decimal? CustomEmergencyCost = null)
{
    public MonteCarloSimulationOptions ToDomain() => new(
        Iterations: Iterations,
        CustomIncomeVolatilitySigma: CustomIncomeVolatilityPercentage.HasValue ? (double)(CustomIncomeVolatilityPercentage.Value / 100m) : null,
        CustomEmergencyProbability: CustomEmergencyAnnualProbability.HasValue ? (double)(CustomEmergencyAnnualProbability.Value / 100m) : null,
        CustomEmergencyCost: CustomEmergencyCost
    );
}
