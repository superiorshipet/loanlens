using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IAffordabilityAnalyzer
{
    AffordabilityMetrics Analyze(FinancialProfile profile, decimal proposedInstallment);
}
