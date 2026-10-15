using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class StressTestingEngine : IStressTestingEngine
{
    public StressTestSummary RunAllScenarios(FinancialProfile profile, LoanParameters loan, decimal monthlyInstallment, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(loan);

        var isAr = LoanLensLanguage.IsArabic(language);

        var results = new List<StressScenarioResult>
        {
            SimulateMedicalEmergency(profile, loan.TenorMonths, monthlyInstallment, 25_000m, isAr),
            SimulateIncomeInterruption(profile, loan.TenorMonths, monthlyInstallment, disruptionMonths: 1, isAr),
            SimulateIncomeInterruption(profile, loan.TenorMonths, monthlyInstallment, disruptionMonths: 3, isAr),
            SimulateIncomeInterruption(profile, loan.TenorMonths, monthlyInstallment, disruptionMonths: 6, isAr),
            SimulateIncomeDrop(profile, loan.TenorMonths, monthlyInstallment, dropPercentage: 25m, isAr),
            SimulateInflationOrRateSurge(profile, loan, monthlyInstallment, isAr),
            SimulateCombinedCrisis(profile, loan.TenorMonths, monthlyInstallment, isAr)
        };

        var total = results.Count;
        var survived = results.Count(r => r.IsSurviving);
        var isResilient = survived >= total - 1;

        string assessment;
        if (isAr)
        {
            assessment = survived switch
            {
                >= 6 => "مرونة مالية ممتازة: الخطة تتحمل معظم الصدمات العنيفة دون تعثر بفضل المدخرات.",
                >= 4 => "مرونة مالية متوسطة: الخطة تتحمل الصدمات الخفيفة، لكن انقطاع الدخل لأكثر من شهرين سيؤدي للتعثر.",
                _ => "هشاشة مالية عالية: الخطة تفشل تحت أي ظرف طارئ بسيط بسبب انعدام هامش الأمان النقدي."
            };
        }
        else
        {
            assessment = survived switch
            {
                >= 6 => "Excellent financial resilience: Plan survives severe shocks without defaulting thanks to adequate reserves.",
                >= 4 => "Moderate financial resilience: Survives minor shocks, but income interruption over 2 months triggers default.",
                _ => "High financial vulnerability: Plan defaults under minor emergency shocks due to lack of cash safety buffer."
            };
        }

        return new StressTestSummary(
            Scenarios: results,
            TotalScenariosTested: total,
            ScenariosSurvived: survived,
            IsResilient: isResilient,
            OverallAssessment: assessment
        );
    }

    private static StressScenarioResult SimulateMedicalEmergency(FinancialProfile profile, int tenor, decimal installment, decimal medicalCost, bool isAr)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;

        for (int m = 1; m <= tenor; m++)
        {
            var income = profile.TotalNetIncome;
            var outflow = profile.FixedMonthlyExpenses + profile.CurrentMonthlyInstallments + installment;
            
            if (m == 3)
            {
                outflow += medicalCost;
            }

            var netCashFlow = income - outflow;
            savings += netCashFlow;

            if (savings < 0)
            {
                var deficit = Math.Abs(savings);
                if (deficit > maxDeficit) maxDeficit = deficit;
                defaultMonth ??= m;
            }
        }

        bool survived = defaultMonth is null;
        string name = isAr
            ? $"مصاريف علاج مفاجئة ({medicalCost:N0} ج.م)"
            : $"Emergency Medical Expense ({medicalCost:N0} EGP)";

        string desc = survived
            ? (isAr 
                ? $"نجاح: تحمل مصاريف علاجية طارئة بقيمة {medicalCost:N0} ج.م مع بقاء مدخرات {Math.Max(0, savings):N0} ج.م."
                : $"Success: Absorbed unexpected medical expense of {medicalCost:N0} EGP with {Math.Max(0, savings):N0} EGP savings remaining.")
            : (isAr
                ? $"تعثر في الشهر {defaultMonth}: المصاريف الطارئة استنزفت المدخرات بالكامل وحدث عجز أقصى قدره {maxDeficit:N0} ج.م."
                : $"Default in month {defaultMonth}: Emergency medical costs depleted all savings with peak deficit of {maxDeficit:N0} EGP.");

        return new StressScenarioResult(name, survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateIncomeInterruption(FinancialProfile profile, int tenor, decimal installment, int disruptionMonths, bool isAr)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;

        for (int m = 1; m <= tenor; m++)
        {
            var income = (m <= disruptionMonths) ? 0m : profile.TotalNetIncome;
            var outflow = profile.FixedMonthlyExpenses + profile.CurrentMonthlyInstallments + installment;
            var netCashFlow = income - outflow;
            savings += netCashFlow;

            if (savings < 0)
            {
                var deficit = Math.Abs(savings);
                if (deficit > maxDeficit) maxDeficit = deficit;
                defaultMonth ??= m;
            }
        }

        bool survived = defaultMonth is null;
        string name = isAr
            ? $"فقدان كامل للدخل لمدة {disruptionMonths} أشهر"
            : $"Complete Income Interruption ({disruptionMonths} {(disruptionMonths == 1 ? "Month" : "Months")})";

        string desc = survived
            ? (isAr
                ? $"نجاح: تحمل انقطاع الدخل لمدة {disruptionMonths} شهر مع استمرار دفع الأقساط والمصاريف."
                : $"Success: Sustained income loss for {disruptionMonths} {(disruptionMonths == 1 ? "month" : "months")} while keeping up with obligations.")
            : (isAr
                ? $"تعثر في الشهر {defaultMonth}: نفاد المدخرات قبل عودة الدخل، مع عجز متراكم قدره {maxDeficit:N0} ج.م."
                : $"Default in month {defaultMonth}: Savings exhausted before income resumption, with cumulative deficit of {maxDeficit:N0} EGP.");

        return new StressScenarioResult(name, survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateIncomeDrop(FinancialProfile profile, int tenor, decimal installment, decimal dropPercentage, bool isAr)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;
        var reducedIncome = profile.TotalNetIncome * (1m - (dropPercentage / 100m));

        for (int m = 1; m <= tenor; m++)
        {
            var outflow = profile.FixedMonthlyExpenses + profile.CurrentMonthlyInstallments + installment;
            var netCashFlow = reducedIncome - outflow;
            savings += netCashFlow;

            if (savings < 0)
            {
                var deficit = Math.Abs(savings);
                if (deficit > maxDeficit) maxDeficit = deficit;
                defaultMonth ??= m;
            }
        }

        bool survived = defaultMonth is null;
        string name = isAr
            ? $"انخفاض دائم في الدخل بنسبة {dropPercentage}%"
            : $"Permanent Income Drop (-{dropPercentage}%)";

        string desc = survived
            ? (isAr
                ? $"نجاح: تحمل انخفاض دائم في الدخل بنسبة {dropPercentage}% مع الحفاظ على مدخرات إيجابية."
                : $"Success: Sustained permanent {dropPercentage}% income reduction while preserving positive reserves.")
            : (isAr
                ? $"تعثر تدريجي في الشهر {defaultMonth}: الانخفاض في الدخل أدى لاستنزاف مستمر للمدخرات."
                : $"Gradual default in month {defaultMonth}: Income reduction steadily eroded reserves until exhaustion.");

        return new StressScenarioResult(name, survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateInflationOrRateSurge(FinancialProfile profile, LoanParameters loan, decimal installment, bool isAr)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;

        decimal adjustedInstallment = installment;
        if (loan.RateType == InterestRateType.Variable)
        {
            var hikeRate = loan.AnnualInterestRatePercentage + 3m;
            var monthlyRate = hikeRate / 100m / 12m;
            var n = loan.TenorMonths;
            var factor = Math.Pow((double)(1m + monthlyRate), n);
            var payment = (double)loan.Principal * ((double)monthlyRate * factor) / (factor - 1.0);
            adjustedInstallment = Math.Round((decimal)payment, 2);
        }

        var inflatedExpenses = profile.FixedMonthlyExpenses * 1.20m;

        for (int m = 1; m <= loan.TenorMonths; m++)
        {
            var outflow = inflatedExpenses + profile.CurrentMonthlyInstallments + adjustedInstallment;
            var netCashFlow = profile.TotalNetIncome - outflow;
            savings += netCashFlow;

            if (savings < 0)
            {
                var deficit = Math.Abs(savings);
                if (deficit > maxDeficit) maxDeficit = deficit;
                defaultMonth ??= m;
            }
        }

        bool survived = defaultMonth is null;
        string name = isAr
            ? "موجة تضخم عالية (+20% مصاريف) ورفع فائدة"
            : "High Inflation Shock (+20% Living Costs) & Rate Surge";

        string desc = survived
            ? (isAr
                ? $"نجاح: تحمل تضخم بنسبة 20% في مصاريف المعيشة{(loan.RateType == InterestRateType.Variable ? " مع زيادة 3% في الفائدة" : "")}."
                : $"Success: Sustained 20% living expense inflation{(loan.RateType == InterestRateType.Variable ? " with 3% rate hike" : "")}.")
            : (isAr
                ? $"تعثر في الشهر {defaultMonth}: التضخم وارتفاع التكاليف ابتلعا الفائض المالي."
                : $"Default in month {defaultMonth}: Inflation surge and rising costs consumed net cash flow.");

        return new StressScenarioResult(name, survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateCombinedCrisis(FinancialProfile profile, int tenor, decimal installment, bool isAr)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;
        var reducedIncome = profile.TotalNetIncome * 0.80m;
        var inflatedExpenses = profile.FixedMonthlyExpenses * 1.15m;

        for (int m = 1; m <= tenor; m++)
        {
            var outflow = inflatedExpenses + profile.CurrentMonthlyInstallments + installment;
            if (m == 4) outflow += 15_000m;

            var netCashFlow = reducedIncome - outflow;
            savings += netCashFlow;

            if (savings < 0)
            {
                var deficit = Math.Abs(savings);
                if (deficit > maxDeficit) maxDeficit = deficit;
                defaultMonth ??= m;
            }
        }

        bool survived = defaultMonth is null;
        string name = isAr
            ? "أزمة مركبة (تراجع دخل + تضخم + طوارئ مفاجئة)"
            : "Combined Crisis (Income Drop + Inflation + Sudden Shock)";

        string desc = survived
            ? (isAr
                ? "نجاح استثنائي: الخطة صمدت أمام صدمة مركبة (انخفاض 20% بالدخل + تضخم 15% + طوارئ 15 ألف)."
                : "Outstanding resilience: Plan held firm under combined pressure (-20% income, +15% inflation, 15k emergency).")
            : (isAr
                ? $"تعثر حتمي في الشهر {defaultMonth}: الأزمة المركبة تجاوزت كل الاحتياطيات النقدية."
                : $"Inevitable default in month {defaultMonth}: Multi-faceted crisis exceeded all available cash reserves.");

        return new StressScenarioResult(name, survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }
}
