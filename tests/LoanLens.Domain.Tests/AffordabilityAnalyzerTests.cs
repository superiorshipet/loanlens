using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;
using Xunit;

namespace LoanLens.Domain.Tests;

public class AffordabilityAnalyzerTests
{
    private readonly AffordabilityAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_SafeProfile_ReturnsSafeRisk()
    {
        // Income 20,000, Expenses 8,000, Existing debt 1,000, Proposed installment 3,000
        // Total debt = 4,000 -> DTI = 20% (< 35% safe)
        // Savings = 40,000 -> Runway = 40k / 12k = 3.3 months (safe)
        var profile = new FinancialProfile(
            MonthlyIncome: 20_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 8_000m,
            CurrentMonthlyInstallments: 1_000m,
            Savings: 40_000m,
            DependentsCount: 1,
            Age: 32
        );

        var result = _analyzer.Analyze(profile, 3_000m);

        Assert.Equal(20m, result.DtiPercentage);
        Assert.Equal(8_000m, result.DisposableIncomeAfterInstallment);
        Assert.Equal(RiskLevel.Safe, result.AffordabilityRisk);
    }

    [Fact]
    public void Analyze_HighDti_ReturnsCriticalWhenExceedingCbeCap()
    {
        // Income 10,000, Installment 5,500 -> DTI = 55% (> 50% CBE cap)
        var profile = new FinancialProfile(
            MonthlyIncome: 10_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 4_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 30_000m,
            DependentsCount: 2,
            Age: 40
        );

        var result = _analyzer.Analyze(profile, 5_500m);

        Assert.Equal(55m, result.DtiPercentage);
        Assert.Equal(RiskLevel.Critical, result.AffordabilityRisk);
        Assert.Contains(result.RiskFactors, r => r.Contains("50%"));
    }

    [Fact]
    public void Analyze_NegativeDisposableIncome_ReturnsCritical()
    {
        // Income 10,000, Expenses 7,000, Installment 4,000 -> Deficit -1,000
        var profile = new FinancialProfile(
            MonthlyIncome: 10_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 7_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 5_000m,
            DependentsCount: 1,
            Age: 28
        );

        var result = _analyzer.Analyze(profile, 4_000m);

        Assert.Equal(-1_000m, result.DisposableIncomeAfterInstallment);
        Assert.Equal(RiskLevel.Critical, result.AffordabilityRisk);
    }
}
