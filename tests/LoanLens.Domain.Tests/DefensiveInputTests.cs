using LoanLens.Application.DTOs;
using LoanLens.Application.Services;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using Xunit;

namespace LoanLens.Domain.Tests;

public class DefensiveInputTests
{
    private readonly LoanAdvisorService _service;

    public DefensiveInputTests()
    {
        var calc = new FinancialCalculator();
        var aff = new AffordabilityAnalyzer();
        var stress = new StressTestingEngine();
        var mc = new MonteCarloEngine();
        var risk = new RiskScoringEngine();
        var rec = new RecommendationEngine(calc);
        _service = new LoanAdvisorService(calc, aff, stress, mc, risk, rec);
    }

    [Fact]
    public void NegativeAndDummyValues_AreDefensivelyHandled_WithoutCrashing()
    {
        // Exact dummy garbage payload sent by Postman
        var profileDto = new UserProfileDto(
            MonthlyIncome: 0m,
            Stability: (IncomeStability)7384,
            FixedMonthlyExpenses: 0.2153237m,
            CurrentMonthlyInstallments: -78m,
            Savings: -6109m,
            DependentsCount: -5,
            Age: -61772865,
            HasHealthInsurance: false,
            SecondaryIncome: 0m
        );

        var loanDto = new LoanInputDto(
            Principal: 1446486061.2235m,
            TenorMonths: -95,
            AnnualInterestRatePercentage: 8.83788215752m,
            CalculationMethod: (InterestCalculationMethod)999,
            RateType: (InterestRateType)888,
            AdminFeePercentage: -5m
        );

        // 1. Amortization Schedule should not crash
        var schedule = _service.CalculateSchedule(loanDto);
        Assert.NotNull(schedule);
        Assert.True(schedule.MonthlyInstallment > 0m);

        // 2. Affordability analysis should not crash
        var metrics = _service.AnalyzeAffordability(profileDto, loanDto);
        Assert.NotNull(metrics);
        Assert.Equal(RiskLevel.Critical, metrics.AffordabilityRisk);

        // 3. Full report should not crash
        var report = _service.GenerateFullReport(new LoanAnalysisRequestDto(profileDto, loanDto, MonteCarloIterations: 500));
        Assert.NotNull(report);
        Assert.NotNull(report.Recommendation);
    }
}
