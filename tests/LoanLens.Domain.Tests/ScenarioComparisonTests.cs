using LoanLens.Application.DTOs;
using LoanLens.Application.Services;
using LoanLens.Application.Validation;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using Xunit;

namespace LoanLens.Domain.Tests;

public class ScenarioComparisonTests
{
    private readonly LoanAdvisorService _advisorService;

    public ScenarioComparisonTests()
    {
        var calculator = new FinancialCalculator();
        var affordability = new AffordabilityAnalyzer();
        var stress = new StressTestingEngine();
        var monteCarlo = new MonteCarloEngine();
        var risk = new RiskScoringEngine();
        var recommendation = new RecommendationEngine(calculator);
        var borrowing = new BorrowingCapacityEngine();
        var consolidation = new DebtConsolidationEngine(calculator);
        var sensitivity = new RateSensitivityEngine(calculator);

        _advisorService = new LoanAdvisorService(
            calculator,
            affordability,
            stress,
            monteCarlo,
            risk,
            recommendation,
            borrowing,
            consolidation,
            sensitivity
        );
    }

    [Fact]
    public void CompareScenarios_IdentifiesDifferences_BetweenTwoLoanOffers()
    {
        var profile = new UserProfileDto(
            MonthlyIncome: 30_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 10_000m,
            CurrentMonthlyInstallments: 1_000m,
            Savings: 50_000m,
            DependentsCount: 1,
            Age: 32,
            HasHealthInsurance: true
        );

        // Offer A: 3-year loan at 24%
        var requestA = new LoanAnalysisRequestDto(
            Profile: profile,
            Loan: new LoanInputDto(100_000m, 36, 24m, InterestCalculationMethod.Reducing),
            MonteCarloIterations: 500
        );

        // Offer B: 2-year loan at 20%
        var requestB = new LoanAnalysisRequestDto(
            Profile: profile,
            Loan: new LoanInputDto(100_000m, 24, 20m, InterestCalculationMethod.Reducing),
            MonteCarloIterations: 500
        );

        var comparisonRequest = new ScenarioComparisonRequestDto(
            ScenarioA: requestA,
            ScenarioB: requestB,
            ScenarioAName: "Bank A (36M @ 24%)",
            ScenarioBName: "Bank B (24M @ 20%)",
            Language: "en"
        );

        var result = _advisorService.CompareScenarios(comparisonRequest, "en");

        Assert.NotNull(result);
        Assert.Equal("Bank A (36M @ 24%)", result.ScenarioAName);
        Assert.Equal("Bank B (24M @ 20%)", result.ScenarioBName);
        Assert.NotEmpty(result.KeyDifferences);

        // Bank B has higher monthly installment (shorter term), but much lower total interest
        Assert.True(result.MonthlyInstallmentDiff > 0, "Scenario B should have higher monthly payment");
        Assert.True(result.TotalInterestDiff < 0, "Scenario B should save total interest");
        Assert.NotEmpty(result.WinnerScenarioName);
        Assert.NotEmpty(result.DetailedAdvisorRationale);
    }

    [Fact]
    public void CompareScenarios_SupportsArabic_Localization()
    {
        var profile = new UserProfileDto(
            MonthlyIncome: 25_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 9_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 40_000m,
            DependentsCount: 1,
            Age: 30
        );

        var requestA = new LoanAnalysisRequestDto(
            Profile: profile,
            Loan: new LoanInputDto(80_000m, 36, 22m),
            MonteCarloIterations: 300
        );

        var requestB = new LoanAnalysisRequestDto(
            Profile: profile,
            Loan: new LoanInputDto(80_000m, 24, 21m),
            MonteCarloIterations: 300
        );

        var comparisonRequest = new ScenarioComparisonRequestDto(
            ScenarioA: requestA,
            ScenarioB: requestB,
            ScenarioAName: "بنك مصر (36 شهر)",
            ScenarioBName: "البنك الأهلي (24 شهر)",
            Language: "ar"
        );

        var result = _advisorService.CompareScenarios(comparisonRequest, "ar");

        Assert.NotNull(result);
        Assert.Contains("القسط الشهري", result.KeyDifferences.Select(k => k.MetricName));
        Assert.Contains("إجمالي الفائدة المدفوعة", result.KeyDifferences.Select(k => k.MetricName));
        Assert.NotEmpty(result.VerdictComparisonSummary);
    }

    [Fact]
    public void Validator_RejectsInvalidComparisonRequest()
    {
        var errors = LoanRequestValidator.Validate((ScenarioComparisonRequestDto)null!, "en");
        Assert.NotEmpty(errors);
        Assert.True(errors.ContainsKey("request"));
    }
}
