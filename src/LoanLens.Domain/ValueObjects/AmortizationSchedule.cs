namespace LoanLens.Domain.ValueObjects;

public record AmortizationScheduleItem(
    int Month,
    decimal BeginningBalance,
    decimal InstallmentAmount,
    decimal PrincipalPortion,
    decimal InterestPortion,
    decimal EndingBalance);

public record AmortizationSchedule(
    IReadOnlyList<AmortizationScheduleItem> Items,
    decimal MonthlyInstallment,
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalPayment,
    decimal EffectiveAnnualRatePercentage);
