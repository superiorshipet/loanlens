using LoanLens.Application.DTOs;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.Services;

public class LoanAdvisorService : ILoanAdvisorService
{
    private readonly IFinancialCalculator _calculator;
    private readonly IAffordabilityAnalyzer _affordabilityAnalyzer;
    private readonly IStressTestingEngine _stressTestingEngine;
    private readonly IMonteCarloEngine _monteCarloEngine;
    private readonly IRiskScoringEngine _riskScoringEngine;
    private readonly IRecommendationEngine _recommendationEngine;

    public LoanAdvisorService(
        IFinancialCalculator calculator,
        IAffordabilityAnalyzer affordabilityAnalyzer,
        IStressTestingEngine stressTestingEngine,
        IMonteCarloEngine monteCarloEngine,
        IRiskScoringEngine riskScoringEngine,
        IRecommendationEngine recommendationEngine)
    {
        _calculator = calculator;
        _affordabilityAnalyzer = affordabilityAnalyzer;
        _stressTestingEngine = stressTestingEngine;
        _monteCarloEngine = monteCarloEngine;
        _riskScoringEngine = riskScoringEngine;
        _recommendationEngine = recommendationEngine;
    }

    public AmortizationSchedule CalculateSchedule(LoanInputDto loanDto)
    {
        ArgumentNullException.ThrowIfNull(loanDto);
        return _calculator.CalculateSchedule(loanDto.ToDomain());
    }

    public AffordabilityMetrics AnalyzeAffordability(UserProfileDto profileDto, LoanInputDto loanDto)
    {
        ArgumentNullException.ThrowIfNull(profileDto);
        ArgumentNullException.ThrowIfNull(loanDto);

        var installment = _calculator.CalculateMonthlyInstallment(loanDto.ToDomain());
        return _affordabilityAnalyzer.Analyze(profileDto.ToDomain(), installment);
    }

    public StressTestSummary RunStressTest(UserProfileDto profileDto, LoanInputDto loanDto)
    {
        ArgumentNullException.ThrowIfNull(profileDto);
        ArgumentNullException.ThrowIfNull(loanDto);

        var loan = loanDto.ToDomain();
        var installment = _calculator.CalculateMonthlyInstallment(loan);
        return _stressTestingEngine.RunAllScenarios(profileDto.ToDomain(), loan, installment);
    }

    public MonteCarloSimulationResult RunMonteCarlo(UserProfileDto profileDto, LoanInputDto loanDto, int iterations = 5000)
    {
        ArgumentNullException.ThrowIfNull(profileDto);
        ArgumentNullException.ThrowIfNull(loanDto);

        var loan = loanDto.ToDomain();
        var installment = _calculator.CalculateMonthlyInstallment(loan);
        return _monteCarloEngine.RunSimulation(profileDto.ToDomain(), loan, installment, iterations);
    }

    public ComprehensiveLoanReportDto GenerateFullReport(LoanAnalysisRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = request.Profile.ToDomain();
        var loan = request.Loan.ToDomain();

        // 1. Calculate Amortization
        var schedule = _calculator.CalculateSchedule(loan);

        // 2. Analyze Affordability
        var affordability = _affordabilityAnalyzer.Analyze(profile, schedule.MonthlyInstallment);

        // 3. Stress Test
        var stressTest = _stressTestingEngine.RunAllScenarios(profile, loan, schedule.MonthlyInstallment);

        // 4. Monte Carlo Simulation
        var monteCarlo = _monteCarloEngine.RunSimulation(profile, loan, schedule.MonthlyInstallment, request.MonteCarloIterations);

        // 5. Evaluate Composite Risk
        var risk = _riskScoringEngine.Evaluate(profile, affordability, stressTest, monteCarlo);

        // 6. Generate Recommendations & Alternative Comparisons
        var recommendation = _recommendationEngine.GenerateDecision(profile, loan, schedule, affordability, risk);

        return new ComprehensiveLoanReportDto(
            LoanDetails: loan,
            UserProfile: profile,
            AmortizationSchedule: schedule,
            Affordability: affordability,
            StressTest: stressTest,
            MonteCarlo: monteCarlo,
            RiskEvaluation: risk,
            Recommendation: recommendation,
            GeneratedAtUtc: DateTime.UtcNow
        );
    }

    public IReadOnlyList<LoanProduct> GetLoanCatalog()
    {
        return _recommendationEngine.GetCatalog();
    }
}
