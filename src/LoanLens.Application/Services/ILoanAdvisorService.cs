using LoanLens.Application.DTOs;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.Services;

public interface ILoanAdvisorService
{
    AmortizationSchedule CalculateSchedule(LoanInputDto loanDto);
    AffordabilityMetrics AnalyzeAffordability(UserProfileDto profileDto, LoanInputDto loanDto);
    StressTestSummary RunStressTest(UserProfileDto profileDto, LoanInputDto loanDto, string language = "en");
    MonteCarloSimulationResult RunMonteCarlo(UserProfileDto profileDto, LoanInputDto loanDto, int iterations = 5000);
    ComprehensiveLoanReportDto GenerateFullReport(LoanAnalysisRequestDto request, string language = "en");
    IReadOnlyList<LoanProduct> GetLoanCatalog(string language = "en");
}
