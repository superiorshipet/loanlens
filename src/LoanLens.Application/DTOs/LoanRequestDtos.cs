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
    public FinancialProfile ToDomain()
    {
        // Defensive normalization for negative numbers or out-of-range dummy data
        var safeIncome = Math.Max(0m, Math.Abs(MonthlyIncome));
        var safeSecondary = Math.Max(0m, Math.Abs(SecondaryIncome));
        var safeExpenses = Math.Max(0m, Math.Abs(FixedMonthlyExpenses));
        var safeInstallments = Math.Max(0m, Math.Abs(CurrentMonthlyInstallments));
        var safeSavings = Math.Max(0m, Math.Abs(Savings));
        var safeDependents = Math.Clamp(Math.Abs(DependentsCount), 0, 20);

        var safeAge = Age switch
        {
            <= 0 => 30,
            > 100 => 60,
            _ => Age
        };

        var safeStability = Enum.IsDefined(typeof(IncomeStability), Stability)
            ? Stability
            : IncomeStability.StableEmployed;

        return new FinancialProfile(
            MonthlyIncome: safeIncome,
            Stability: safeStability,
            FixedMonthlyExpenses: safeExpenses,
            CurrentMonthlyInstallments: safeInstallments,
            Savings: safeSavings,
            DependentsCount: safeDependents,
            Age: safeAge,
            HasHealthInsurance: HasHealthInsurance,
            SecondaryIncome: safeSecondary
        );
    }
}

public record LoanInputDto(
    decimal Principal,
    int TenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing,
    InterestRateType RateType = InterestRateType.Fixed,
    decimal AdminFeePercentage = 0m)
{
    public LoanParameters ToDomain()
    {
        // Defensive normalization for negative or zero loan parameters
        var safePrincipal = Math.Max(1_000m, Math.Abs(Principal));
        var safeTenor = Math.Clamp(Math.Abs(TenorMonths) == 0 ? 12 : Math.Abs(TenorMonths), 1, 360);
        var safeRate = Math.Max(0.01m, Math.Abs(AnnualInterestRatePercentage));
        var safeAdminFee = Math.Max(0m, Math.Abs(AdminFeePercentage));

        var safeMethod = Enum.IsDefined(typeof(InterestCalculationMethod), CalculationMethod)
            ? CalculationMethod
            : InterestCalculationMethod.Reducing;

        var safeRateType = Enum.IsDefined(typeof(InterestRateType), RateType)
            ? RateType
            : InterestRateType.Fixed;

        return new LoanParameters(
            Principal: safePrincipal,
            TenorMonths: safeTenor,
            AnnualInterestRatePercentage: safeRate,
            CalculationMethod: safeMethod,
            RateType: safeRateType,
            AdminFeePercentage: safeAdminFee
        );
    }
}

public record LoanAnalysisRequestDto(
    UserProfileDto Profile,
    LoanInputDto Loan,
    int MonteCarloIterations = 5000)
{
    public int SafeIterations => Math.Clamp(MonteCarloIterations <= 0 ? 5000 : MonteCarloIterations, 100, 20000);
}
