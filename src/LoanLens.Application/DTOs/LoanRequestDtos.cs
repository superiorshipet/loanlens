using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.DTOs;

public record UserProfileDto(
    decimal MonthlyIncome,
    IncomeStability Stability,
    decimal FixedMonthlyExpenses,
    decimal CurrentMonthlyInstallments,
    decimal Savings,
    int DependentsCount,
    int Age,
    bool HasHealthInsurance = false,
    decimal SecondaryIncome = 0m)
{
    public FinancialProfile ToDomain() => new(
        MonthlyIncome,
        Stability,
        FixedMonthlyExpenses,
        CurrentMonthlyInstallments,
        Savings,
        DependentsCount,
        Age,
        HasHealthInsurance,
        SecondaryIncome
    );
}

public record LoanInputDto(
    decimal Principal,
    int TenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing,
    InterestRateType RateType = InterestRateType.Fixed,
    decimal AdminFeePercentage = 0m)
{
    public LoanParameters ToDomain() => new(
        Principal,
        TenorMonths,
        AnnualInterestRatePercentage,
        CalculationMethod,
        RateType,
        AdminFeePercentage
    );
}

public record LoanAnalysisRequestDto(
    UserProfileDto Profile,
    LoanInputDto Loan,
    int MonteCarloIterations = 5000);
