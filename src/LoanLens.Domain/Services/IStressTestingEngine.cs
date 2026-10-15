using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IStressTestingEngine
{
    StressTestSummary RunAllScenarios(FinancialProfile profile, LoanParameters loan, decimal monthlyInstallment, string language = "en");
}
