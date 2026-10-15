using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IMonteCarloEngine
{
    MonteCarloSimulationResult RunSimulation(
        FinancialProfile profile, 
        LoanParameters loan, 
        decimal monthlyInstallment, 
        int iterations = 5000);
}
