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

    [Fact]
    public void LoanParameters_Calculates_EarlySettlement_And_LatePenalty_Accurately()
    {
        var loan = new LoanParameters(
            Principal: 100_000m,
            TenorMonths: 36,
            AnnualInterestRatePercentage: 22m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            RateType: InterestRateType.Fixed,
            AdminFeePercentage: 1.5m,
            EarlyRepaymentFeePercentage: 3.0m,
            LatePaymentPenaltyPercentage: 2.0m
        );

        // Prepayment of 40,000 with 3% fee
        var earlyFee = loan.CalculateEarlyRepaymentFee(40_000m);
        Assert.Equal(1_200m, earlyFee);

        // Overdue installment of 5,000 with 2% penalty
        var latePenalty = loan.CalculateLatePenalty(5_000m);
        Assert.Equal(100m, latePenalty);
    }

    [Fact]
    public void RecommendationEngine_Catalog_ContainsTopEgyptianBanksAndConsumerLenders()
    {
        var catalogEn = _recommendationEngine.GetCatalog("en");
        var catalogAr = _recommendationEngine.GetCatalog("ar");

        Assert.True(catalogEn.Count >= 11);
        Assert.True(catalogAr.Count >= 11);

        // Check key commercial banks
        Assert.Contains(catalogEn, p => p.Id == "nbe-personal" && p.ProviderName.Contains("National Bank of Egypt"));
        Assert.Contains(catalogEn, p => p.Id == "banque-misr-easy" && p.ProviderName.Contains("Banque Misr"));
        Assert.Contains(catalogEn, p => p.Id == "cib-personal" && p.ProviderName.Contains("Commercial International Bank"));
        Assert.Contains(catalogEn, p => p.Id == "qnb-personal" && p.ProviderName.Contains("QNB"));
        Assert.Contains(catalogEn, p => p.Id == "aaib-personal" && p.ProviderName.Contains("AAIB"));
        Assert.Contains(catalogEn, p => p.Id == "alexbank-mobasher" && p.ProviderName.Contains("Alexandria"));
        Assert.Contains(catalogEn, p => p.Id == "bdc-personal" && p.ProviderName.Contains("Banque du Caire"));
        Assert.Contains(catalogEn, p => p.Id == "fabmisr-personal" && p.ProviderName.Contains("FABMISR"));

        // Check consumer finance lenders
        Assert.Contains(catalogEn, p => p.Id == "valu-consumer" && p.ProviderName.Contains("valU"));
        Assert.Contains(catalogEn, p => p.Id == "aman-cash" && p.ProviderName.Contains("Aman"));
        Assert.Contains(catalogEn, p => p.Id == "contact-personal" && p.ProviderName.Contains("Contact"));

        // Check Arabic naming
        Assert.Contains(catalogAr, p => p.Id == "nbe-personal" && p.ProviderName.Contains("الأهلي"));
        Assert.Contains(catalogAr, p => p.Id == "valu-consumer" && p.ProviderName.Contains("ڤاليو"));
    }

    [Fact]
    public void RecommendationEngine_BuildsAllFourRepaymentStrategiesWithNetBenefits()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 35_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 12_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 70_000m,
            DependentsCount: 1,
            Age: 32
        );

        var loan = new LoanParameters(
            Principal: 150_000m,
            TenorMonths: 36,
            AnnualInterestRatePercentage: 21m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            EarlyRepaymentFeePercentage: 3.0m
        );

        var schedule = _calculator.CalculateSchedule(loan);
        var affordability = new AffordabilityAnalyzer().Analyze(profile, schedule.MonthlyInstallment);
        var stress = _stressEngine.RunAllScenarios(profile, loan, schedule.MonthlyInstallment);
        var mc = _monteCarloEngine.RunSimulation(profile, loan, schedule.MonthlyInstallment, iterations: 500);
        var risk = _riskEngine.Evaluate(profile, affordability, stress, mc);

        var decision = _recommendationEngine.GenerateDecision(profile, loan, schedule, affordability, risk, "en");

        Assert.Equal(4, decision.Strategies.Count);

        var standard = decision.Strategies[0];
        Assert.Equal(36, standard.TotalMonthsToPayoff);
        Assert.Equal(0m, standard.EarlySettlementFeePaid);

        var accelerated = decision.Strategies[1];
        Assert.True(accelerated.TotalMonthsToPayoff < 36);
        Assert.True(accelerated.EarlySettlementFeePaid > 0m);
        Assert.True(accelerated.NetFinancialBenefit > 0m);

        var annualBonus = decision.Strategies[2];
        Assert.True(annualBonus.TotalMonthsToPayoff < 36);
        Assert.True(annualBonus.EarlySettlementFeePaid > 0m);
        Assert.True(annualBonus.NetFinancialBenefit > 0m);

        var paymentHoliday = decision.Strategies[3];
        Assert.True(paymentHoliday.TotalMonthsToPayoff > 36);
        Assert.True(paymentHoliday.NetFinancialBenefit < 0m, "Payment holiday must reflect capitalized extra interest cost");
    }

    [Fact]
    public void MonteCarlo_SupportsCustomTunableParameters()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 25_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 10_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 40_000m,
            DependentsCount: 1,
            Age: 30
        );

        var loan = new LoanParameters(120_000m, 36, 22m, InterestCalculationMethod.Reducing);
        var installment = _calculator.CalculateMonthlyInstallment(loan);

        var customOptions = new MonteCarloSimulationOptions(
            Iterations: 1500,
            CustomIncomeVolatilitySigma: 0.35,
            CustomEmergencyProbability: 0.40,
            CustomEmergencyCost: 20_000m
        );

        var result = _monteCarloEngine.RunSimulation(profile, loan, installment, iterations: 1500, options: customOptions);

        Assert.Equal(1500, result.TotalIterations);
        Assert.True(result.ElapsedMilliseconds < 2000);
    }
}
