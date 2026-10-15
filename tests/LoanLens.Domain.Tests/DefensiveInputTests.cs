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
}
