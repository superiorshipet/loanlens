using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IRecommendationEngine
{
    FinalAdvisorDecision GenerateDecision(
        FinancialProfile profile, 
        LoanParameters loan, 
        AmortizationSchedule schedule, 
        AffordabilityMetrics affordability, 
        RiskEvaluation riskEvaluation);

    IReadOnlyList<LoanProduct> GetCatalog();
}
