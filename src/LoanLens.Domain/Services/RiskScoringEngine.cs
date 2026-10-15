using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class RiskScoringEngine : IRiskScoringEngine
{
    public RiskEvaluation Evaluate(
        FinancialProfile profile, 
        AffordabilityMetrics affordability, 
        StressTestSummary stressTest, 
        MonteCarloSimulationResult monteCarlo)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(affordability);
        ArgumentNullException.ThrowIfNull(stressTest);
        ArgumentNullException.ThrowIfNull(monteCarlo);

        var factors = new List<RiskFactorScore>();

        // 1. DTI & Cashflow Burden (Max 35 points)
        decimal dtiScore;
        string dtiDetail;
        if (affordability.DisposableIncomeAfterInstallment < 0)
        {
            dtiScore = 35m;
            dtiDetail = $"عجز نقدي فوري: الدخل المتبقي بالسالب ({affordability.DisposableIncomeAfterInstallment:N0} ج.م).";
        }
        else if (affordability.DtiPercentage > 50m)
        {
            dtiScore = 35m;
            dtiDetail = $"تجاوز سقف البنك المركزي المصري: نسبة الدين {affordability.DtiPercentage}% (الحد الأقصى 50%).";
        }
        else if (affordability.DtiPercentage > 40m)
        {
            dtiScore = 26m;
            dtiDetail = $"نسبة عبء الدين عالية ({affordability.DtiPercentage}%): تتجاوز حاجز الأمان 40%.";
        }
        else if (affordability.DtiPercentage > 30m)
        {
            dtiScore = 14m;
            dtiDetail = $"نسبة عبء الدين متوسطة ({affordability.DtiPercentage}%): مقبولة ولكن تتطلب انضباطاً.";
        }
        else
        {
            dtiScore = 4m;
            dtiDetail = $"نسبة عبء الدين ممتازة ومريحة جداً ({affordability.DtiPercentage}%).";
        }
        factors.Add(new RiskFactorScore("عبء الدين والتدفق النقدي (DTI)", dtiScore, 35m, dtiDetail));

        // 2. Liquidity & Emergency Cushion (Max 25 points)
        decimal runwayScore;
        string runwayDetail;
        if (affordability.EmergencyRunwayMonths < 1.0m)
        {
            runwayScore = 25m;
            runwayDetail = $"انعدام السيولة: المدخرات تغطي {affordability.EmergencyRunwayMonths} شهر فقط (خطر فوري عند أي تأخير بالراتب).";
        }
        else if (affordability.EmergencyRunwayMonths < 3.0m)
        {
            runwayScore = 18m;
            runwayDetail = $"صندوق طوارئ هش: {affordability.EmergencyRunwayMonths} شهور (أقل من الحد الأدنى الموصى به 3-6 شهور).";
        }
        else if (affordability.EmergencyRunwayMonths < 6.0m)
        {
            runwayScore = 8m;
            runwayDetail = $"صندوق طوارئ جيد: يغطي {affordability.EmergencyRunwayMonths} شهور من المصاريف والأقساط.";
        }
        else
        {
            runwayScore = 0m;
            runwayDetail = $"وسادة أمان مالية صلبة: المدخرات تغطي {affordability.EmergencyRunwayMonths} شهور.";
        }
        factors.Add(new RiskFactorScore("السيولة وصندوق الطوارئ", runwayScore, 25m, runwayDetail));

        // 3. Monte Carlo Stochastic Default Risk (Max 25 points)
        decimal mcScore;
        string mcDetail;
        if (monteCarlo.DefaultProbabilityPercentage > 30m)
        {
            mcScore = 25m;
            mcDetail = $"احتمالية تعثر حرجة في المحاكاة العشوائية ({monteCarlo.DefaultProbabilityPercentage}%).";
        }
        else if (monteCarlo.DefaultProbabilityPercentage > 15m)
        {
            mcScore = 18m;
            mcDetail = $"احتمالية تعثر مرتفعة ({monteCarlo.DefaultProbabilityPercentage}%)، وأخطر شهر محتمل هو الشهر {monteCarlo.PeakRiskMonth ?? 1}.";
        }
        else if (monteCarlo.DefaultProbabilityPercentage > 5m)
        {
            mcScore = 10m;
            mcDetail = $"احتمالية تعثر طفيفة ({monteCarlo.DefaultProbabilityPercentage}%) تحت الصدمات العشوائية.";
        }
        else
        {
            mcScore = 0m;
            mcDetail = $"احتمالية تعثر شبه منعدمة ({monteCarlo.DefaultProbabilityPercentage}%) في 5000 سيناريو محاكى.";
        }
        factors.Add(new RiskFactorScore("محاكاة التعثر العشوائية (Monte Carlo)", mcScore, 25m, mcDetail));

        // 4. Volatility, Stability & Profile Risk (Max 15 points)
        decimal profileScore = 0m;
        var profileNotes = new List<string>();

        if (profile.Stability == IncomeStability.Freelance)
        {
            profileScore += 7m;
            profileNotes.Add("طبيعة العمل الحر تنطوي على تذبذب تدفقات الدخل");
        }
        else if (profile.Stability == IncomeStability.BusinessOwner)
        {
            profileScore += 8m;
            profileNotes.Add("نشاط تجاري/عمل خاص معرض لدورات السوق");
        }

        if (profile.DependentsCount >= 3)
        {
            profileScore += 4m;
            profileNotes.Add($"عدد المعالين ({profile.DependentsCount}) يرفع الالتزامات الإلزامية");
        }

        if (profile.Age >= 55)
        {
            profileScore += 3m;
            profileNotes.Add($"السن ({profile.Age}) يقترب من سن التقاعد");
        }

        if (!profile.HasHealthInsurance)
        {
            profileScore += 2m;
            profileNotes.Add("عدم وجود تأمين صحي يعرض الخطة لمخاطر العلاج المفاجئ");
        }

        profileScore = Math.Min(15m, profileScore);
        string profileSummary = profileNotes.Count > 0 
            ? string.Join("، ", profileNotes) + "."
            : "ملف وظيفي واجتماعي مستقر ومنخفض المخاطر.";
        factors.Add(new RiskFactorScore("عوامل استقرار الدخل والظروف الشخصية", profileScore, 15m, profileSummary));

        // Total Composite Score
        decimal totalScore = factors.Sum(f => f.Score);
        totalScore = Math.Clamp(totalScore, 0m, 100m);

        RiskLevel category;
        string label;
        string summary;

        if (totalScore <= 25m)
        {
            category = RiskLevel.Safe;
            label = "آمن (Safe)";
            summary = "القرض آمن تماماً ويتناسب مع دخلك ومدخراتك دون تشكيل أي تهديد لسلامتك المالية.";
        }
        else if (totalScore <= 50m)
        {
            category = RiskLevel.Moderate;
            label = "متوسط (Moderate)";
            summary = "القرض مقبول ولكن يتطلب الحذر ومراقبة المصاريف الشهرية لتجنب أي ضغط غير متوقع.";
        }
        else if (totalScore <= 75m)
        {
            category = RiskLevel.High;
            label = "عالي المخاطر (High Risk)";
            summary = "مخاطرة مرتفعة: أي طارئ بسيط (مرض، تأخر راتب) قد يدفعك للتعثر أو الاستدانة لسداد القسط.";
        }
        else
        {
            category = RiskLevel.Critical;
            label = "خطر وشيك (Critical)";
            summary = "خطر تعثر وشيك: هذا القرض غير مناسب على الإطلاق لبياناتك المالية الحالية وينذر بأزمة ديون خانقة.";
        }

        return new RiskEvaluation(
            TotalRiskScore: Math.Round(totalScore, 1),
            Category: category,
            RiskLabel: label,
            Breakdown: factors,
            ComprehensiveSummary: summary
        );
    }
}
