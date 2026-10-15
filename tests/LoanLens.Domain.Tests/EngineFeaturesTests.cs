using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;
using Xunit;

namespace LoanLens.Domain.Tests;

public class EngineFeaturesTests
{
    private readonly FinancialCalculator _calculator = new();
    private readonly StressTestingEngine _stressEngine = new();
    private readonly MonteCarloEngine _monteCarloEngine = new();
    private readonly RiskScoringEngine _riskEngine = new();
    private readonly RecommendationEngine _recommendationEngine;

    public EngineFeaturesTests()
    {
        _recommendationEngine = new RecommendationEngine(_calculator);
    }

    [Fact]
    public void StressTesting_IdentifiesDeficit_UnderSevereShocks()
    {
        // Low savings profile
        var profile = new FinancialProfile(
            MonthlyIncome: 12_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 6_000m,
            CurrentMonthlyInstallments: 1_000m,
            Savings: 5_000m, // Low savings
            DependentsCount: 1,
            Age: 30
        );

        var loan = new LoanParameters(100_000m, 24, 18m, InterestCalculationMethod.Reducing);
        var installment = _calculator.CalculateMonthlyInstallment(loan);

        var summary = _stressEngine.RunAllScenarios(profile, loan, installment);

        Assert.Equal(7, summary.TotalScenariosTested);
        Assert.True(summary.ScenariosSurvived < summary.TotalScenariosTested);
        // Medical emergency (25k) should fail when savings are only 5k
        var medicalScenario = summary.Scenarios.First(s => s.ScenarioName.Contains("Medical") || s.ScenarioName.Contains("علاج"));
        Assert.False(medicalScenario.IsSurviving);
    }

    [Fact]
    public void MonteCarlo_RunsFastAndProducesMeaningfulMetrics()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 25_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 10_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 60_000m,
            DependentsCount: 1,
            Age: 32
        );

        var loan = new LoanParameters(80_000m, 36, 18m, InterestCalculationMethod.Reducing);
        var installment = _calculator.CalculateMonthlyInstallment(loan);

        var result = _monteCarloEngine.RunSimulation(profile, loan, installment, iterations: 2000);

        Assert.Equal(2000, result.TotalIterations);
        Assert.True(result.ElapsedMilliseconds < 1000, $"Simulation took too long: {result.ElapsedMilliseconds}ms");
        Assert.True(result.DefaultProbabilityPercentage <= 10m); // Strong profile should have low default rate
        Assert.True(result.MedianEndingSavings > 0m);
    }

    [Fact]
    public void RiskScoring_ScoresSafeProfileAccurately()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 30_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 8_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 90_000m,
            DependentsCount: 0,
            Age: 29,
            HasHealthInsurance: true
        );

        var loan = new LoanParameters(50_000m, 24, 16m, InterestCalculationMethod.Reducing);
        var installment = _calculator.CalculateMonthlyInstallment(loan);
        var affordability = new AffordabilityAnalyzer().Analyze(profile, installment);
        var stress = _stressEngine.RunAllScenarios(profile, loan, installment);
        var mc = _monteCarloEngine.RunSimulation(profile, loan, installment, iterations: 1000);

        var evaluation = _riskEngine.Evaluate(profile, affordability, stress, mc);

        Assert.True(evaluation.TotalRiskScore <= 35m);
        Assert.True(evaluation.Category == RiskLevel.Safe || evaluation.Category == RiskLevel.Moderate);
    }

    [Fact]
    public void RecommendationEngine_AvoidsDangerousLoan_WhenDtiIsExcessive()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 8_000m,
            Stability: IncomeStability.Freelance,
            FixedMonthlyExpenses: 5_000m,
            CurrentMonthlyInstallments: 1_000m,
            Savings: 2_000m,
            DependentsCount: 2,
            Age: 35
        );

        // Huge loan 200k for 8k income
        var loan = new LoanParameters(200_000m, 24, 20m, InterestCalculationMethod.Reducing);
        var schedule = _calculator.CalculateSchedule(loan);
        var affordability = new AffordabilityAnalyzer().Analyze(profile, schedule.MonthlyInstallment);
        var stress = _stressEngine.RunAllScenarios(profile, loan, schedule.MonthlyInstallment);
        var mc = _monteCarloEngine.RunSimulation(profile, loan, schedule.MonthlyInstallment, iterations: 1000);
        var risk = _riskEngine.Evaluate(profile, affordability, stress, mc);

        var decision = _recommendationEngine.GenerateDecision(profile, loan, schedule, affordability, risk);

        Assert.Equal(RecommendationVerdict.AvoidLoan, decision.Verdict);
        Assert.NotEmpty(decision.KeyActionableAdvice);
    }
}
