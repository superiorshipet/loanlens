namespace LoanLens.Domain.ValueObjects;

public record ExistingDebtItem(
    string DebtName,
    decimal RemainingPrincipal,
    decimal MonthlyInstallment,
    int RemainingMonths,
    decimal EarlySettlementFeePercentage = 3.0m);
