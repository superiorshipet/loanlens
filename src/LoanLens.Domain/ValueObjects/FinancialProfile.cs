using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record FinancialProfile(
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
    public decimal TotalNetIncome => MonthlyIncome + SecondaryIncome;
    public decimal TotalObligationsBeforeNewLoan => FixedMonthlyExpenses + CurrentMonthlyInstallments;
    public decimal CurrentSurplus => TotalNetIncome - TotalObligationsBeforeNewLoan;
}
