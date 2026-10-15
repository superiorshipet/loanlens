using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class StressTestingEngine : IStressTestingEngine
{
    public StressTestSummary RunAllScenarios(FinancialProfile profile, LoanParameters loan, decimal monthlyInstallment)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(loan);

        var results = new List<StressScenarioResult>
        {
            SimulateMedicalEmergency(profile, loan.TenorMonths, monthlyInstallment, 25_000m),
            SimulateIncomeInterruption(profile, loan.TenorMonths, monthlyInstallment, disruptionMonths: 1),
            SimulateIncomeInterruption(profile, loan.TenorMonths, monthlyInstallment, disruptionMonths: 3),
            SimulateIncomeInterruption(profile, loan.TenorMonths, monthlyInstallment, disruptionMonths: 6),
            SimulateIncomeDrop(profile, loan.TenorMonths, monthlyInstallment, dropPercentage: 25m),
            SimulateInflationOrRateSurge(profile, loan, monthlyInstallment),
            SimulateCombinedCrisis(profile, loan.TenorMonths, monthlyInstallment)
        };

        var total = results.Count;
        var survived = results.Count(r => r.IsSurviving);
        var isResilient = survived >= total - 1; // Resilient if surviving at least all but worst combined

        string assessment = survived switch
        {
            >= 6 => "مرونة مالية ممتازة: الخطة تتحمل معظم الصدمات العنيفة دون تعثر بفضل المدخرات.",
            >= 4 => "مرونة مالية متوسطة: الخطة تتحمل الصدمات الخفيفة، لكن انقطاع الدخل لأكثر من شهرين سيؤدي للتعثر.",
            _ => "هشاشة مالية عالية: الخطة تفشل تحت أي ظرف طارئ بسيط بسبب انعدام هامش الأمان النقدي."
        };

        return new StressTestSummary(
            Scenarios: results,
            TotalScenariosTested: total,
            ScenariosSurvived: survived,
            IsResilient: isResilient,
            OverallAssessment: assessment
        );
    }

    private static StressScenarioResult SimulateMedicalEmergency(FinancialProfile profile, int tenor, decimal installment, decimal medicalCost)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;

        for (int m = 1; m <= tenor; m++)
        {
            var income = profile.TotalNetIncome;
            var outflow = profile.FixedMonthlyExpenses + profile.CurrentMonthlyInstallments + installment;
            
            if (m == 3) // Medical shock hits at month 3
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
        string desc = survived
            ? $"نجاح: تحمل مصاريف علاجية طارئة بقيمة {medicalCost:N0} ج.م مع بقاء مدخرات {Math.Max(0, savings):N0} ج.م."
            : $"تعثر في الشهر {defaultMonth}: المصاريف الطارئة استنزفت المدخرات بالكامل وحدث عجز أقصى قدره {maxDeficit:N0} ج.م.";

        return new StressScenarioResult("مصاريف علاج مفاجئة (25,000 ج.م)", survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateIncomeInterruption(FinancialProfile profile, int tenor, decimal installment, int disruptionMonths)
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
        string desc = survived
            ? $"نجاح: تحمل انقطاع الدخل لمدة {disruptionMonths} شهر مع استمرار دفع الأقساط والمصاريف."
            : $"تعثر في الشهر {defaultMonth}: نفاد المدخرات قبل عودة الدخل، مع عجز متراكم قدره {maxDeficit:N0} ج.م.";

        return new StressScenarioResult($"فقدان كامل للدخل لمدة {disruptionMonths} أشهر", survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateIncomeDrop(FinancialProfile profile, int tenor, decimal installment, decimal dropPercentage)
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
        string desc = survived
            ? $"نجاح: تحمل انخفاض دائم في الدخل بنسبة {dropPercentage}% مع الحفاظ على مدخرات إيجابية."
            : $"تعثر تدريجي في الشهر {defaultMonth}: الانخفاض في الدخل أدى لاستنزاف مستمر للمدخرات.";

        return new StressScenarioResult($"انخفاض دائم في الدخل بنسبة {dropPercentage}%", survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateInflationOrRateSurge(FinancialProfile profile, LoanParameters loan, decimal installment)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;

        // Either rate hike on variable rate (+3%) or general inflation raising living expenses by 20%
        decimal adjustedInstallment = installment;
        if (loan.RateType == InterestRateType.Variable)
        {
            // +3% interest rate hike
            var hikeRate = loan.AnnualInterestRatePercentage + 3m;
            var monthlyRate = hikeRate / 100m / 12m;
            var n = loan.TenorMonths;
            var factor = Math.Pow((double)(1m + monthlyRate), n);
            var payment = (double)loan.Principal * ((double)monthlyRate * factor) / (factor - 1.0);
            adjustedInstallment = Math.Round((decimal)payment, 2);
        }

        var inflatedExpenses = profile.FixedMonthlyExpenses * 1.20m; // 20% inflation shock

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
        string desc = survived
            ? $"نجاح: تحمل تضخم بنسبة 20% في مصاريف المعيشة{(loan.RateType == InterestRateType.Variable ? " مع زيادة 3% في الفائدة" : "")}."
            : $"تعثر في الشهر {defaultMonth}: التضخم وارتفاع التكاليف ابتلعا الفائض المالي.";

        return new StressScenarioResult("موجة تضخم عالية (+20% مصاريف) ورفع فائدة", survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }

    private static StressScenarioResult SimulateCombinedCrisis(FinancialProfile profile, int tenor, decimal installment)
    {
        var savings = profile.Savings;
        int? defaultMonth = null;
        decimal maxDeficit = 0m;
        var reducedIncome = profile.TotalNetIncome * 0.80m; // 20% drop
        var inflatedExpenses = profile.FixedMonthlyExpenses * 1.15m; // 15% inflation

        for (int m = 1; m <= tenor; m++)
        {
            var outflow = inflatedExpenses + profile.CurrentMonthlyInstallments + installment;
            if (m == 4) outflow += 15_000m; // Emergency expense at month 4

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
        string desc = survived
            ? "نجاح استثنائي: الخطة صمدت أمام صدمة مركبة (انخفاض 20% بالدخل + تضخم 15% + طوارئ 15 ألف)."
            : $"تعثر حتمي في الشهر {defaultMonth}: الأزمة المركبة تجاوزت كل الاحتياطيات النقدية.";

        return new StressScenarioResult("أزمة مركبة (تراجع دخل + تضخم + طوارئ مفاجئة)", survived, defaultMonth, Math.Round(savings, 2), Math.Round(maxDeficit, 2), desc);
    }
}
