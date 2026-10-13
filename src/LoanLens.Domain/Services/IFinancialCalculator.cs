using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IFinancialCalculator
{
    AmortizationSchedule CalculateSchedule(LoanParameters parameters);
    decimal CalculateEffectiveRateFromFlat(decimal principal, int tenorMonths, decimal annualFlatRatePercentage);
    decimal CalculateEffectiveAnnualRateFromFlat(decimal principal, int tenorMonths, decimal annualFlatRatePercentage);
    decimal CalculateMonthlyInstallment(LoanParameters parameters);
}
