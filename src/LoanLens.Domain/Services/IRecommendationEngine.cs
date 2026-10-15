using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IRecommendationEngine
{
    FinalAdvisorDecision GenerateDecision(
        FinancialProfile profile, 
        LoanParameters loan, 
        AmortizationSchedule schedule, 
        AffordabilityMetrics affordability, 
        RiskEvaluation riskEvaluation,
        string language = "en");

    IReadOnlyList<LoanProduct> GetCatalog(string language = "en");
}
