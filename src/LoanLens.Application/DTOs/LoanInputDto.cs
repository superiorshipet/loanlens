using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.DTOs;

public record LoanInputDto(
    decimal Principal,
    int TenorMonths,
    decimal AnnualInterestRatePercentage,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing,
    InterestRateType RateType = InterestRateType.Fixed,
    decimal AdminFeePercentage = 0m,
    decimal EarlyRepaymentFeePercentage = 3.0m,
    decimal LatePaymentPenaltyPercentage = 2.0m)
{
    public LoanParameters ToDomain() => new(
        Principal: Principal,
        TenorMonths: TenorMonths,
        AnnualInterestRatePercentage: AnnualInterestRatePercentage,
        CalculationMethod: CalculationMethod,
        RateType: RateType,
        AdminFeePercentage: AdminFeePercentage,
        EarlyRepaymentFeePercentage: EarlyRepaymentFeePercentage,
        LatePaymentPenaltyPercentage: LatePaymentPenaltyPercentage
    );
}
