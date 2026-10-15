using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;
using LoanLens.Infrastructure.Services;
using Xunit;

namespace LoanLens.Domain.Tests;

public class AdvancedBackendFeaturesTests
{
    private readonly FinancialCalculator _calculator;
    private readonly BorrowingCapacityEngine _borrowingEngine;
    private readonly DebtConsolidationEngine _consolidationEngine;
    private readonly RateSensitivityEngine _rateSensitivityEngine;
    private readonly LoanExcelExportService _excelExportService;

    public AdvancedBackendFeaturesTests()
    {
        _calculator = new FinancialCalculator();
        _borrowingEngine = new BorrowingCapacityEngine();
        _consolidationEngine = new DebtConsolidationEngine(_calculator);
        _rateSensitivityEngine = new RateSensitivityEngine(_calculator);
        _excelExportService = new LoanExcelExportService();
    }

    [Fact]
    public void BorrowingCapacity_CalculatesAccurateCeilings_UnderCbe50AndSafe35()
    {
        // Monthly income: 40,000 EGP. Current installments: 5,000 EGP.
        // CBE 50% max installment = 20,000 - 5,000 = 15,000 EGP.
        // Safe 35% max installment = 14,000 - 5,000 = 9,000 EGP.
        var profile = new FinancialProfile(
            MonthlyIncome: 40_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 12_000m,
            CurrentMonthlyInstallments: 5_000m,
            Savings: 60_000m,
            DependentsCount: 1,
            Age: 30
        );

        var result = _borrowingEngine.CalculateCapacity(
            profile,
            targetInterestRate: 20m,
            targetTenorMonths: 60,
            calculationMethod: InterestCalculationMethod.Reducing,
            language: "en"
        );

        Assert.Equal(15_000m, result.LegalMaxMonthlyInstallment);
        Assert.Equal(9_000m, result.SafeMonthlyInstallment);
        Assert.True(result.LegalMaxPrincipal > result.SafeRecommendedPrincipal);
        Assert.True(result.LegalMaxPrincipal > 500_000m);
        Assert.Equal(50.0m, result.LegalMaxDtiPercentage);
        Assert.Equal(35.0m, result.SafeDtiPercentage);
        Assert.False(string.IsNullOrWhiteSpace(result.AdviceExplanation));
    }

    [Fact]
    public void BorrowingCapacity_CapsTenor_WhenBorrowerNearsRetirement()
    {
        // Employee age 58. Retirement age = 60. Remaining months = 24.
        // Even if requesting 60 months, tenor should cap at 24.
        var profile = new FinancialProfile(
            MonthlyIncome: 30_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 10_000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 40_000m,
            DependentsCount: 0,
            Age: 58
        );

        var result = _borrowingEngine.CalculateCapacity(
            profile,
            targetInterestRate: 20m,
            targetTenorMonths: 60,
            calculationMethod: InterestCalculationMethod.Reducing,
            language: "ar"
        );

        Assert.Equal(24, result.TenorMonthsUsed);
        Assert.Contains("التقاعد", result.AdviceExplanation);
    }

    [Fact]
    public void DebtConsolidation_CalculatesBenefit_AndAccountsForEarlySettlementFees()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 35_000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 10_000m,
            CurrentMonthlyInstallments: 12_000m,
            Savings: 20_000m,
            DependentsCount: 1,
            Age: 35
        );

        // Two existing high-interest debts:
        // Debt 1: Remaining 100k, 5k/mo, 24 mo left, 3% early fee = 3,000 fee
        // Debt 2: Remaining 50k, 3k/mo, 18 mo left, 5% early fee = 2,500 fee
        // Total Old Principal = 150k. Total early fees = 5,500. Required new loan = 155,500 EGP.
        var debts = new List<ExistingDebtItem>
        {
            new("Credit Card / Personal Loan A", 100_000m, 5_000m, 24, 3.0m),
            new("Car Loan B", 50_000m, 3_000m, 18, 5.0m)
        };

        // Target consolidation offer: Bank buyout at 18% over 60 months
        var targetOffer = new ConsolidationTargetOffer(
            ProviderName: "National Bank of Egypt (NBE Buyout)",
            AnnualInterestRatePercentage: 18.0m,
            TenorMonths: 60,
            AdminFeePercentage: 1.0m,
            CalculationMethod: InterestCalculationMethod.Reducing
        );

        var result = _consolidationEngine.AnalyzeConsolidation(profile, debts, targetOffer, "en");

        Assert.Equal(150_000m, result.TotalCurrentDebtPrincipal);
        Assert.Equal(5_500m, result.TotalEarlySettlementPenalty);
        Assert.Equal(155_500m, result.RequiredNewLoanPrincipal);
        Assert.Equal(8_000m, result.CurrentTotalMonthlyInstallments);
        Assert.True(result.NewConsolidatedMonthlyInstallment < result.CurrentTotalMonthlyInstallments);
        Assert.True(result.MonthlyCashFlowFreedUp > 0);
        Assert.True(result.NewDtiPercentage < result.CurrentDtiPercentage);
        Assert.True(result.IsDtiCbeCompliant);
        Assert.NotEqual(DebtConsolidationVerdict.NotRecommended, result.Verdict);
    }

    [Fact]
    public void RateSensitivity_CalculatesCbeHikesAndCutsCorrectly()
    {
        var loan = new LoanParameters(
            Principal: 300_000m,
            TenorMonths: 60,
            AnnualInterestRatePercentage: 22.0m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            RateType: InterestRateType.Variable
        );

        var installment = _calculator.CalculateMonthlyInstallment(loan);
        var sensitivity = _rateSensitivityEngine.AnalyzeSensitivity(loan, installment, "en");

        Assert.Equal(22.0m, sensitivity.BaseAnnualRatePercentage);
        Assert.Equal(5, sensitivity.Scenarios.Count);

        // Verify +100 bps (+1%)
        var hike1 = sensitivity.Scenarios.First(s => s.ShiftPercentagePoints == 1.0m);
        Assert.Equal(23.0m, hike1.ResultingAnnualRatePercentage);
        Assert.True(hike1.MonthlyInstallmentDelta > 0);
        Assert.True(hike1.TotalInterestDelta > 0);

        // Verify +300 bps (+3%)
        var hike3 = sensitivity.Scenarios.First(s => s.ShiftPercentagePoints == 3.0m);
        Assert.Equal(25.0m, hike3.ResultingAnnualRatePercentage);
        Assert.True(hike3.MonthlyInstallmentDelta > hike1.MonthlyInstallmentDelta);

        // Verify -100 bps (-1%)
        var cut1 = sensitivity.Scenarios.First(s => s.ShiftPercentagePoints == -1.0m);
        Assert.Equal(21.0m, cut1.ResultingAnnualRatePercentage);
        Assert.True(cut1.MonthlyInstallmentDelta < 0);
        Assert.True(cut1.TotalInterestDelta < 0);
    }

    [Fact]
    public void ExcelExport_GeneratesValidSpreadsheetBytes_InBothLanguages()
    {
        var loan = new LoanParameters(
            Principal: 250_000m,
            TenorMonths: 36,
            AnnualInterestRatePercentage: 24.0m,
            CalculationMethod: InterestCalculationMethod.Reducing
        );

        var schedule = _calculator.CalculateSchedule(loan);

        // English export
        var enBytes = _excelExportService.ExportAmortizationScheduleToExcel(loan, schedule, "Ahmed Hassan", "en");
        Assert.NotNull(enBytes);
        Assert.True(enBytes.Length > 2000);
        // Valid ZIP/XLSX header signature (PK\x03\x04)
        Assert.Equal(0x50, enBytes[0]);
        Assert.Equal(0x4B, enBytes[1]);

        // Arabic export
        var arBytes = _excelExportService.ExportAmortizationScheduleToExcel(loan, schedule, "أحمد حسن", "ar");
        Assert.NotNull(arBytes);
        Assert.True(arBytes.Length > 2000);
        Assert.Equal(0x50, arBytes[0]);
        Assert.Equal(0x4B, arBytes[1]);
    }
}
