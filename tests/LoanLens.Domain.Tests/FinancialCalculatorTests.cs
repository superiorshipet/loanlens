using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;
using Xunit;

namespace LoanLens.Domain.Tests;

public class FinancialCalculatorTests
{
    private readonly FinancialCalculator _calculator = new();

    [Fact]
    public void CalculateMonthlyInstallment_FlatRate_ReturnsExpectedFormula()
    {
        // Loan: 100,000 EGP, 12 months, 12% Flat
        // Total interest = 100,000 * 0.12 * 1 = 12,000
        // Total payment = 112,000
        // Monthly installment = 112,000 / 12 = 9333.33
        var loan = new LoanParameters(
            Principal: 100_000m,
            TenorMonths: 12,
            AnnualInterestRatePercentage: 12m,
            CalculationMethod: InterestCalculationMethod.Flat
        );

        var installment = _calculator.CalculateMonthlyInstallment(loan);

        Assert.Equal(9333.33m, installment);
    }

    [Fact]
    public void CalculateSchedule_FlatRate_EndsWithZeroBalance()
    {
        var loan = new LoanParameters(
            Principal: 50_000m,
            TenorMonths: 24,
            AnnualInterestRatePercentage: 10m,
            CalculationMethod: InterestCalculationMethod.Flat
        );

        var schedule = _calculator.CalculateSchedule(loan);

        Assert.Equal(24, schedule.Items.Count);
        Assert.Equal(0m, schedule.Items.Last().EndingBalance);
        Assert.Equal(50_000m, schedule.TotalPrincipal);
        Assert.Equal(10_000m, schedule.TotalInterest); // 50k * 10% * 2 years = 10k
        Assert.Equal(60_000m, schedule.TotalPayment);
    }

    [Fact]
    public void CalculateEffectiveRateFromFlat_ConvertsAccuratelyToApr()
    {
        // 100,000 over 60 months (5 years) at 12% Flat
        // Flat monthly = (100k + 60k) / 60 = 2666.67
        // Nominal APR = 20.31%
        // Effective Annual Rate (EAR) = 22.31%
        var nominalApr = _calculator.CalculateEffectiveRateFromFlat(100_000m, 60, 12m);
        var ear = _calculator.CalculateEffectiveAnnualRateFromFlat(100_000m, 60, 12m);

        Assert.Equal(20.31m, nominalApr);
        Assert.Equal(22.31m, ear);
    }

    [Fact]
    public void CalculateSchedule_ReducingRate_EndsWithZeroBalanceAndAccurateTotals()
    {
        // 100,000 EGP, 12 months, 18% Reducing
        var loan = new LoanParameters(
            Principal: 100_000m,
            TenorMonths: 12,
            AnnualInterestRatePercentage: 18m,
            CalculationMethod: InterestCalculationMethod.Reducing
        );

        var schedule = _calculator.CalculateSchedule(loan);

        Assert.Equal(12, schedule.Items.Count);
        Assert.Equal(0m, schedule.Items.Last().EndingBalance);
        Assert.True(schedule.MonthlyInstallment > 9000m && schedule.MonthlyInstallment < 9500m);
        // Total interest for 18% reducing on 100k 1yr is approx 10,000 EGP (vs 18,000 if flat)
        Assert.True(schedule.TotalInterest < 11_000m);
    }
}
