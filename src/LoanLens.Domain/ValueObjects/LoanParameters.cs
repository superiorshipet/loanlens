using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record LoanParameters(
    decimal Principal,
    int TenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod,
    InterestRateType RateType = InterestRateType.Fixed,
    decimal AdminFeePercentage = 0m,
    decimal EarlyRepaymentFeePercentage = 3.0m,
    decimal LatePaymentPenaltyPercentage = 2.0m)
{
    public decimal AdminFeeAmount => Math.Round(Principal * (AdminFeePercentage / 100m), 2);
    public decimal DisbursedAmount => Principal - AdminFeeAmount;

    public decimal CalculateEarlyRepaymentFee(decimal prepaidPrincipal) =>
        Math.Round(prepaidPrincipal * (EarlyRepaymentFeePercentage / 100m), 2);

    public decimal CalculateLatePenalty(decimal installmentAmount) =>
        Math.Round(installmentAmount * (LatePaymentPenaltyPercentage / 100m), 2);
}
