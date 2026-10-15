using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.DTOs;

public record ExistingDebtItemDto(
    string DebtName,
    decimal RemainingPrincipal,
    decimal MonthlyInstallment,
    int RemainingMonths,
    decimal EarlySettlementFeePercentage = 3.0m)
{
    public ExistingDebtItem ToDomain() => new(
        DebtName: DebtName,
        RemainingPrincipal: RemainingPrincipal,
        MonthlyInstallment: MonthlyInstallment,
        RemainingMonths: RemainingMonths,
        EarlySettlementFeePercentage: EarlySettlementFeePercentage
    );
}
