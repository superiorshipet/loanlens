using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class AffordabilityAnalyzer : IAffordabilityAnalyzer
{
    public AffordabilityMetrics Analyze(FinancialProfile profile, decimal proposedInstallment)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (proposedInstallment < 0) throw new ArgumentOutOfRangeException(nameof(proposedInstallment), "Proposed installment cannot be negative.");

        var netIncome = profile.TotalNetIncome;
        var totalDebtObligations = profile.CurrentMonthlyInstallments + proposedInstallment;
        
        var dti = netIncome > 0 
            ? Math.Round((totalDebtObligations / netIncome) * 100m, 2) 
            : 100m;

        var disposableIncome = netIncome - (profile.FixedMonthlyExpenses + totalDebtObligations);
        
        var totalMonthlyBurn = profile.FixedMonthlyExpenses + totalDebtObligations;
        var runwayMonths = totalMonthlyBurn > 0 
            ? Math.Round(profile.Savings / totalMonthlyBurn, 1) 
            : 0m;

        var riskFactors = new List<string>();
        RiskLevel risk;

        if (disposableIncome < 0)
        {
            risk = RiskLevel.Critical;
            riskFactors.Add("عجز مالي: الدخل المتبقي بعد سداد الأقساط والمصاريف بالسالب.");
        }
        else if (dti > 50m)
        {
            risk = RiskLevel.Critical;
            riskFactors.Add($"نسبة عبء الدين ({dti}%) تتجاوز الحد الأقصى للبنك المركزي المصري (50%).");
        }
        else if (dti > 40m)
        {
            risk = RiskLevel.High;
            riskFactors.Add($"نسبة عبء الدين ({dti}%) في منطقة الخطر العالية (أعلى من 40%).");
        }
        else if (dti > 35m)
        {
            risk = RiskLevel.Moderate;
            riskFactors.Add($"نسبة عبء الدين ({dti}%) في المنطقة المتوسطة (بين 35% و 40%).");
        }
        else
        {
            risk = RiskLevel.Safe;
        }

        if (runwayMonths < 1.0m)
        {
            risk = (RiskLevel)Math.Max((int)risk, (int)RiskLevel.Critical);
            riskFactors.Add($"صندوق الطوارئ حرج جداً: يغطي {runwayMonths} شهر فقط (الحد الأدنى الآمن 3 شهور).");
        }
        else if (runwayMonths < 3.0m)
        {
            risk = (RiskLevel)Math.Max((int)risk, (int)RiskLevel.High);
            riskFactors.Add($"صندوق الطوارئ ضعيف: يغطي {runwayMonths} شهور فقط.");
        }

        if (profile.Stability == IncomeStability.Freelance)
        {
            if (risk == RiskLevel.Safe) risk = RiskLevel.Moderate;
            riskFactors.Add("طبيعة العمل كـ Freelancer تتطلب هامش أمان أكبر لاحتمالية تذبذب الدخل.");
        }

        if (!profile.HasHealthInsurance)
        {
            if (runwayMonths < 2.5m && risk == RiskLevel.Safe)
            {
                risk = RiskLevel.Moderate;
            }
            riskFactors.Add("غياب التأمين الصحي يفرض عبئاً إضافياً على صندوق الطوارئ لمواجهة تكاليف العلاج المفاجئ.");
        }

        string summary = risk switch
        {
            RiskLevel.Safe => "القدرة المالية ممتازة: القسط ضمن الحدود الآمنة ويوجد فائض نقدي مريح.",
            RiskLevel.Moderate => "القدرة المالية مقبولة بحذر: القسط يقترب من سقف الأمان، يوصى بمراقبة المصاريف.",
            RiskLevel.High => "مخاطرة عالية: القسط يضغط بشدة على الميزانية الشهرية وصندوق الطوارئ غير كافٍ.",
            RiskLevel.Critical => "غير مستحسن إطلاقاً: القرض يشكل خطراً وشيكاً للتعثر وتجاوز معايير الإقراض المسؤولة.",
            _ => "تقييم غير محدد"
        };

        return new AffordabilityMetrics(
            ProposedInstallment: proposedInstallment,
            ExistingInstallments: profile.CurrentMonthlyInstallments,
            TotalDebtObligations: totalDebtObligations,
            NetMonthlyIncome: netIncome,
            DtiPercentage: dti,
            DisposableIncomeAfterInstallment: disposableIncome,
            EmergencyRunwayMonths: runwayMonths,
            AffordabilityRisk: risk,
            SummaryMessage: summary,
            RiskFactors: riskFactors
        );
    }
}
