using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.DTOs;

public record ConsolidationTargetOfferDto(
    string ProviderName,
    decimal AnnualInterestRatePercentage,
    int TenorMonths,
    decimal AdminFeePercentage = 1.5m,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing)
{
    public ConsolidationTargetOffer ToDomain() => new(
        ProviderName: ProviderName,
        AnnualInterestRatePercentage: AnnualInterestRatePercentage,
        TenorMonths: TenorMonths,
        AdminFeePercentage: AdminFeePercentage,
        CalculationMethod: CalculationMethod
    );
}
