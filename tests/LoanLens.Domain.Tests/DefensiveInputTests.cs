using LoanLens.Application.DTOs;
using LoanLens.Application.Validation;
using LoanLens.Domain.Enums;
using Xunit;

namespace LoanLens.Domain.Tests;

public class DefensiveInputTests
{
    [Fact]
    public void Validator_RejectsNegativeValuesAndInvalidEnums_WithClearErrors()
    {
        // Negative & invalid dummy payload sent by Postman
        var profileDto = new UserProfileDto(
            MonthlyIncome: -5000m,
            Stability: (IncomeStability)7384,
            FixedMonthlyExpenses: -2000m,
            CurrentMonthlyInstallments: -78m,
            Savings: -6109m,
            DependentsCount: -5,
            Age: -61772865,
            HasHealthInsurance: false,
            SecondaryIncome: -100m
        );

        var loanDto = new LoanInputDto(
            Principal: -50000m,
            TenorMonths: -95,
            AnnualInterestRatePercentage: -12m,
            CalculationMethod: (InterestCalculationMethod)999,
            RateType: (InterestRateType)888,
            AdminFeePercentage: -5m
        );

        var errors = LoanRequestValidator.Validate(profileDto, loanDto);

        Assert.NotEmpty(errors);
        Assert.True(errors.ContainsKey("loan.principal"));
        Assert.True(errors.ContainsKey("loan.tenorMonths"));
        Assert.True(errors.ContainsKey("loan.annualInterestRatePercentage"));
        Assert.True(errors.ContainsKey("profile.monthlyIncome"));
        Assert.True(errors.ContainsKey("profile.fixedMonthlyExpenses"));
        Assert.True(errors.ContainsKey("profile.savings"));
        Assert.True(errors.ContainsKey("profile.age"));
        Assert.True(errors.ContainsKey("profile.stability"));
    }

    [Fact]
    public void Validator_Passes_WhenInputIsValid()
    {
        var profileDto = new UserProfileDto(
            MonthlyIncome: 20000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 7000m,
            CurrentMonthlyInstallments: 1000m,
            Savings: 35000m,
            DependentsCount: 1,
            Age: 30,
            HasHealthInsurance: true,
            SecondaryIncome: 0m
        );

        var loanDto = new LoanInputDto(
            Principal: 100000m,
            TenorMonths: 36,
            AnnualInterestRatePercentage: 22m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            RateType: InterestRateType.Fixed,
            AdminFeePercentage: 1.5m
        );

        var errors = LoanRequestValidator.Validate(profileDto, loanDto);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validator_RejectsNegativeBankingFees_AndInvalidMonteCarloOptions()
    {
        var profileDto = new UserProfileDto(
            MonthlyIncome: 20000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 7000m,
            CurrentMonthlyInstallments: 0m,
            Savings: 35000m,
            DependentsCount: 1,
            Age: 30
        );

        var loanDto = new LoanInputDto(
            Principal: 100000m,
            TenorMonths: 36,
            AnnualInterestRatePercentage: 22m,
            EarlyRepaymentFeePercentage: -3.0m,
            LatePaymentPenaltyPercentage: -2.0m
        );

        var mcOptions = new MonteCarloOptionsDto(
            Iterations: -500,
            CustomIncomeVolatilityPercentage: -10m,
            CustomEmergencyAnnualProbability: 150m,
            CustomEmergencyCost: -5000m
        );

        // Test in English
        var errorsEn = LoanRequestValidator.Validate(profileDto, loanDto, mcOptions, iterations: -500, language: "en");
        Assert.True(errorsEn.ContainsKey("loan.earlyRepaymentFeePercentage"));
        Assert.True(errorsEn.ContainsKey("loan.latePaymentPenaltyPercentage"));
        Assert.True(errorsEn.ContainsKey("iterations"));
        Assert.True(errorsEn.ContainsKey("monteCarloOptions.customIncomeVolatilityPercentage"));
        Assert.True(errorsEn.ContainsKey("monteCarloOptions.customEmergencyAnnualProbability"));
        Assert.True(errorsEn.ContainsKey("monteCarloOptions.customEmergencyCost"));

        // Test in Arabic
        var errorsAr = LoanRequestValidator.Validate(profileDto, loanDto, mcOptions, iterations: -500, language: "ar");
        Assert.Contains("غرامة السداد المعجل", errorsAr["loan.earlyRepaymentFeePercentage"][0]);
        Assert.Contains("غرامة التأخير", errorsAr["loan.latePaymentPenaltyPercentage"][0]);
        Assert.Contains("لا يمكن أن تكون سالبة", errorsAr["monteCarloOptions.customEmergencyCost"][0]);
    }
}
