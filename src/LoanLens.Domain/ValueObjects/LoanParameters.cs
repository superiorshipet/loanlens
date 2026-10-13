using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record LoanParameters(
    decimal Principal,
    int TenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod,
    InterestRateType RateType = InterestRateType.Fixed,
    decimal AdminFeePercentage = 0m,
    decimal EarlyRepaymentFeePercentage = 0m)
{
    public decimal AdminFeeAmount => Math.Round(Principal * (AdminFeePercentage / 100m), 2);
    public decimal DisbursedAmount => Principal - AdminFeeAmount;
}
