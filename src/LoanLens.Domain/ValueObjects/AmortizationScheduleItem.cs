namespace LoanLens.Domain.ValueObjects;

public record AmortizationScheduleItem(
    int Month,
    decimal BeginningBalance,
    decimal InstallmentAmount,
    decimal PrincipalPortion,
    decimal InterestPortion,
    decimal EndingBalance);
