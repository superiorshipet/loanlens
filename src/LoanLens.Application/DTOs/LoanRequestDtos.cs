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
        MonthlyIncome: MonthlyIncome,
        Stability: Stability,
        FixedMonthlyExpenses: FixedMonthlyExpenses,
        CurrentMonthlyInstallments: CurrentMonthlyInstallments,
        Savings: Savings,
        DependentsCount: DependentsCount,
        Age: Age,
        HasHealthInsurance: HasHealthInsurance,
        SecondaryIncome: SecondaryIncome
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
        Principal: Principal,
        TenorMonths: TenorMonths,
        AnnualInterestRatePercentage: AnnualInterestRatePercentage,
        CalculationMethod: CalculationMethod,
        RateType: RateType,
        AdminFeePercentage: AdminFeePercentage
    );
}

public record LoanAnalysisRequestDto(
    UserProfileDto Profile,
    LoanInputDto Loan,
    int MonteCarloIterations = 5000);
