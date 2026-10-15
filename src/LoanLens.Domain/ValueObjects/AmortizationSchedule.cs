namespace LoanLens.Domain.ValueObjects;

public record AmortizationSchedule(
    IReadOnlyList<AmortizationScheduleItem> Items,
    decimal MonthlyInstallment,
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalPayment,
    decimal EffectiveAnnualRatePercentage);
