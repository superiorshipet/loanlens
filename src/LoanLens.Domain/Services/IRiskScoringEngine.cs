using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IRiskScoringEngine
{
    RiskEvaluation Evaluate(
        FinancialProfile profile, 
        AffordabilityMetrics affordability, 
        StressTestSummary stressTest, 
        MonteCarloSimulationResult monteCarlo);
}
