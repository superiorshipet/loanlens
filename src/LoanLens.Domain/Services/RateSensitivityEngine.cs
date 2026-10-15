using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class RateSensitivityEngine : IRateSensitivityEngine
{
    private readonly IFinancialCalculator _calculator;

    public RateSensitivityEngine(IFinancialCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public RateSensitivitySummary AnalyzeSensitivity(
        LoanParameters loan,
        decimal currentMonthlyInstallment,
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(loan);

        var isAr = LoanLensLanguage.IsArabic(language);
        var baseSchedule = _calculator.CalculateSchedule(loan);
        var baseRate = loan.AnnualInterestRatePercentage;

        var shifts = new[]
        {
            (DeltaPct: 1.0m, LabelAr: "رفع الفائدة +100 نقطة أساس (+1%)", LabelEn: "+100 bps Rate Hike (+1%)"),
            (DeltaPct: 2.0m, LabelAr: "رفع الفائدة +200 نقطة أساس (+2%)", LabelEn: "+200 bps Rate Hike (+2%)"),
            (DeltaPct: 3.0m, LabelAr: "رفع الفائدة +300 نقطة أساس (+3%) - صدمة تضخمية", LabelEn: "+300 bps Rate Hike (+3%) - Inflation Shock"),
            (DeltaPct: -1.0m, LabelAr: "خفض الفائدة -100 نقطة أساس (-1%)", LabelEn: "-100 bps Rate Cut (-1%)"),
            (DeltaPct: -2.0m, LabelAr: "خفض الفائدة -200 نقطة أساس (-2%) - تيسير نقدي", LabelEn: "-200 bps Rate Cut (-2%) - Monetary Easing")
        };

        var scenarioItems = new List<RateSensitivityScenarioItem>();

        foreach (var (deltaPct, labelAr, labelEn) in shifts)
        {
            var newRate = Math.Max(1.0m, baseRate + deltaPct);
            var scenarioLoan = new LoanParameters(
                Principal: loan.Principal,
                TenorMonths: loan.TenorMonths,
                AnnualInterestRatePercentage: newRate,
                CalculationMethod: loan.CalculationMethod,
                RateType: loan.RateType,
                AdminFeePercentage: loan.AdminFeePercentage
            );

            var scenarioSchedule = _calculator.CalculateSchedule(scenarioLoan);
            var monthlyDelta = scenarioSchedule.MonthlyInstallment - baseSchedule.MonthlyInstallment;
            var interestDelta = scenarioSchedule.TotalInterest - baseSchedule.TotalInterest;

            string severity;
            if (deltaPct > 0)
            {
                if (monthlyDelta > baseSchedule.MonthlyInstallment * 0.15m)
                    severity = isAr ? "تأثير حرج على السيولة" : "Critical Impact";
                else if (monthlyDelta > baseSchedule.MonthlyInstallment * 0.08m)
                    severity = isAr ? "تأثير مرتفع" : "High Impact";
                else
                    severity = isAr ? "تأثير معتدل" : "Moderate Impact";
            }
            else
            {
                severity = isAr ? "وفر إيجابي لصالحك" : "Positive Savings";
            }

            scenarioItems.Add(new RateSensitivityScenarioItem(
                ShiftLabel: isAr ? labelAr : labelEn,
                ShiftPercentagePoints: deltaPct,
                ResultingAnnualRatePercentage: newRate,
                MonthlyInstallment: scenarioSchedule.MonthlyInstallment,
                MonthlyInstallmentDelta: monthlyDelta,
                TotalInterestPaid: scenarioSchedule.TotalInterest,
                TotalInterestDelta: interestDelta,
                TotalRepaymentAmount: scenarioSchedule.TotalPayment,
                ImpactSeverity: severity
            ));
        }

        string advice = isAr
            ? "في القروض متغيرة الفائدة المرتبطة بسعر الكوريدور في البنك المركزي المصري؛ يؤدي رفع الفائدة بمقدار 2% إلى زيادة قسطك الشهري بمقدار يتطلب تخصيص هامش أمان إضافي في ميزانيتك الشهرية."
            : "For variable-rate loans linked to CBE Corridor benchmark rates, a 2% rate hike increases your monthly installment significantly, requiring an extra monthly liquidity buffer.";

        return new RateSensitivitySummary(
            BaseAnnualRatePercentage: baseRate,
            BaseMonthlyInstallment: baseSchedule.MonthlyInstallment,
            BaseTotalInterest: baseSchedule.TotalInterest,
            Scenarios: scenarioItems,
            KeyTakeawayAdvice: advice
        );
    }
}
