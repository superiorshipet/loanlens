using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class FinancialCalculator : IFinancialCalculator
{
    public decimal CalculateMonthlyInstallment(LoanParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var principal = Math.Max(1_000m, Math.Abs(parameters.Principal));
        var tenor = Math.Clamp(Math.Abs(parameters.TenorMonths) == 0 ? 12 : Math.Abs(parameters.TenorMonths), 1, 360);
        var rate = Math.Max(0m, Math.Abs(parameters.AnnualInterestRatePercentage));

        if (parameters.CalculationMethod == InterestCalculationMethod.Flat)
        {
            var totalInterest = principal * (rate / 100m) * (tenor / 12m);
            var totalPayment = principal + totalInterest;
            return Math.Round(totalPayment / tenor, 2);
        }
        else
        {
            if (rate <= 0)
                return Math.Round(principal / tenor, 2);

            var monthlyRate = (double)(rate / 100m / 12m);
            var factor = Math.Pow(1.0 + monthlyRate, tenor);
            var monthlyPaymentDouble = (double)principal * (monthlyRate * factor) / (factor - 1.0);
            return Math.Round((decimal)monthlyPaymentDouble, 2);
        }
    }

    public AmortizationSchedule CalculateSchedule(LoanParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var safeParams = new LoanParameters(
            Principal: Math.Max(1_000m, Math.Abs(parameters.Principal)),
            TenorMonths: Math.Clamp(Math.Abs(parameters.TenorMonths) == 0 ? 12 : Math.Abs(parameters.TenorMonths), 1, 360),
            AnnualInterestRatePercentage: Math.Max(0m, Math.Abs(parameters.AnnualInterestRatePercentage)),
            CalculationMethod: Enum.IsDefined(typeof(InterestCalculationMethod), parameters.CalculationMethod) ? parameters.CalculationMethod : InterestCalculationMethod.Reducing,
            RateType: Enum.IsDefined(typeof(InterestRateType), parameters.RateType) ? parameters.RateType : InterestRateType.Fixed,
            AdminFeePercentage: Math.Max(0m, Math.Abs(parameters.AdminFeePercentage)),
            EarlyRepaymentFeePercentage: Math.Max(0m, Math.Abs(parameters.EarlyRepaymentFeePercentage))
        );

        if (safeParams.CalculationMethod == InterestCalculationMethod.Flat)
        {
            return CalculateFlatSchedule(safeParams);
        }

        return CalculateReducingSchedule(safeParams);
    }

    public decimal CalculateEffectiveRateFromFlat(decimal principal, int tenorMonths, decimal annualFlatRatePercentage)
    {
        if (principal <= 0 || tenorMonths <= 0 || annualFlatRatePercentage <= 0)
            return 0m;

        var totalInterest = principal * (annualFlatRatePercentage / 100m) * (tenorMonths / 12m);
        var installment = (principal + totalInterest) / tenorMonths;

        // Bisection to solve for monthly rate `i`:
        // Present Value of annuity: PV(i) = installment * (1 - (1+i)^-n) / i == principal
        double p = (double)principal;
        double a = (double)installment;
        int n = tenorMonths;

        double low = 0.00001;
        double high = 1.0;

        for (int iter = 0; iter < 60; iter++)
        {
            double mid = (low + high) / 2.0;
            double pv = a * (1.0 - Math.Pow(1.0 + mid, -n)) / mid;

            if (pv > p)
                low = mid;
            else
                high = mid;
        }

        double monthlyRate = (low + high) / 2.0;
        // Annual Percentage Rate (Nominal APR compounded monthly: i * 12)
        double nominalApr = monthlyRate * 12.0 * 100.0;
        return Math.Round((decimal)nominalApr, 2);
    }

    public decimal CalculateEffectiveAnnualRateFromFlat(decimal principal, int tenorMonths, decimal annualFlatRatePercentage)
    {
        if (principal <= 0 || tenorMonths <= 0 || annualFlatRatePercentage <= 0)
            return 0m;

        var totalInterest = principal * (annualFlatRatePercentage / 100m) * (tenorMonths / 12m);
        var installment = (principal + totalInterest) / tenorMonths;

        double p = (double)principal;
        double a = (double)installment;
        int n = tenorMonths;

        double low = 0.00001;
        double high = 1.0;

        for (int iter = 0; iter < 60; iter++)
        {
            double mid = (low + high) / 2.0;
            double pv = a * (1.0 - Math.Pow(1.0 + mid, -n)) / mid;

            if (pv > p)
                low = mid;
            else
                high = mid;
        }

        double monthlyRate = (low + high) / 2.0;
        // Effective Annual Rate (EAR / APY: (1 + i)^12 - 1)
        double ear = (Math.Pow(1.0 + monthlyRate, 12.0) - 1.0) * 100.0;
        return Math.Round((decimal)ear, 2);
    }

    private AmortizationSchedule CalculateFlatSchedule(LoanParameters parameters)
    {
        var tenor = parameters.TenorMonths;
        var totalInterest = Math.Round(parameters.Principal * (parameters.AnnualInterestRatePercentage / 100m) * (tenor / 12m), 2);
        var totalPayment = parameters.Principal + totalInterest;
        var monthlyInstallment = Math.Round(totalPayment / tenor, 2);

        var monthlyPrincipal = Math.Round(parameters.Principal / tenor, 2);
        var monthlyInterest = Math.Round(totalInterest / tenor, 2);

        var items = new List<AmortizationScheduleItem>(tenor);
        var remainingPrincipal = parameters.Principal;

        for (int month = 1; month <= tenor; month++)
        {
            var beginningBalance = remainingPrincipal;
            decimal principalPortion;
            decimal interestPortion;
            decimal installment;

            if (month == tenor)
            {
                // Balance adjustment on last month to eliminate rounding drift
                principalPortion = remainingPrincipal;
                interestPortion = totalInterest - (monthlyInterest * (tenor - 1));
                installment = principalPortion + interestPortion;
                remainingPrincipal = 0m;
            }
            else
            {
                principalPortion = monthlyPrincipal;
                interestPortion = monthlyInterest;
                installment = monthlyInstallment;
                remainingPrincipal -= principalPortion;
            }

            items.Add(new AmortizationScheduleItem(
                Month: month,
                BeginningBalance: beginningBalance,
                InstallmentAmount: installment,
                PrincipalPortion: principalPortion,
                InterestPortion: interestPortion,
                EndingBalance: Math.Max(0m, remainingPrincipal)
            ));
        }

        var effectiveApr = CalculateEffectiveRateFromFlat(parameters.Principal, tenor, parameters.AnnualInterestRatePercentage);

        return new AmortizationSchedule(
            Items: items,
            MonthlyInstallment: monthlyInstallment,
            TotalPrincipal: parameters.Principal,
            TotalInterest: totalInterest,
            TotalPayment: totalPayment,
            EffectiveAnnualRatePercentage: effectiveApr
        );
    }

    private AmortizationSchedule CalculateReducingSchedule(LoanParameters parameters)
    {
        var tenor = parameters.TenorMonths;
        var monthlyInstallment = CalculateMonthlyInstallment(parameters);
        var monthlyRate = parameters.AnnualInterestRatePercentage / 100m / 12m;

        var items = new List<AmortizationScheduleItem>(tenor);
        var currentBalance = parameters.Principal;
        decimal accumulatedInterest = 0m;
        decimal accumulatedPrincipal = 0m;

        for (int month = 1; month <= tenor; month++)
        {
            var beginningBalance = currentBalance;
            var interestPortion = Math.Round(beginningBalance * monthlyRate, 2);
            decimal principalPortion;
            decimal installment;

            if (month == tenor || beginningBalance + interestPortion <= monthlyInstallment)
            {
                // Final month cleanup
                principalPortion = beginningBalance;
                installment = principalPortion + interestPortion;
                currentBalance = 0m;
            }
            else
            {
                principalPortion = monthlyInstallment - interestPortion;
                installment = monthlyInstallment;
                currentBalance -= principalPortion;
            }

            accumulatedInterest += interestPortion;
            accumulatedPrincipal += principalPortion;

            items.Add(new AmortizationScheduleItem(
                Month: month,
                BeginningBalance: beginningBalance,
                InstallmentAmount: installment,
                PrincipalPortion: principalPortion,
                InterestPortion: interestPortion,
                EndingBalance: Math.Max(0m, currentBalance)
            ));

            if (currentBalance <= 0m) break;
        }

        return new AmortizationSchedule(
            Items: items,
            MonthlyInstallment: monthlyInstallment,
            TotalPrincipal: accumulatedPrincipal,
            TotalInterest: accumulatedInterest,
            TotalPayment: accumulatedPrincipal + accumulatedInterest,
            EffectiveAnnualRatePercentage: parameters.AnnualInterestRatePercentage
        );
    }
}
