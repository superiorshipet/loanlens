using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class RiskScoringEngine : IRiskScoringEngine
{
    public RiskEvaluation Evaluate(
        FinancialProfile profile, 
        AffordabilityMetrics affordability, 
        StressTestSummary stressTest, 
        MonteCarloSimulationResult monteCarlo,
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(affordability);
        ArgumentNullException.ThrowIfNull(stressTest);
        ArgumentNullException.ThrowIfNull(monteCarlo);

        var isAr = LoanLensLanguage.IsArabic(language);
        var factors = new List<RiskFactorScore>();

        // 1. DTI & Cashflow Burden (Max 35 points)
        decimal dtiScore;
        string dtiDetail;
        if (affordability.DisposableIncomeAfterInstallment < 0)
        {
            dtiScore = 35m;
            dtiDetail = isAr 
                ? $"عجز نقدي فوري: الدخل المتبقي بالسالب ({affordability.DisposableIncomeAfterInstallment:N0} ج.م)."
                : $"Immediate cash deficit: Net disposable income is negative ({affordability.DisposableIncomeAfterInstallment:N0} EGP).";
        }
        else if (affordability.DtiPercentage > 50m)
        {
            dtiScore = 35m;
            dtiDetail = isAr
                ? $"تجاوز سقف البنك المركزي المصري: نسبة الدين {affordability.DtiPercentage}% (الحد الأقصى 50%)."
                : $"Exceeds Central Bank of Egypt (CBE) 50% cap: Debt ratio is {affordability.DtiPercentage}% (regulatory limit is 50%).";
        }
        else if (affordability.DtiPercentage > 40m)
        {
            dtiScore = 26m;
            dtiDetail = isAr
                ? $"نسبة عبء الدين عالية ({affordability.DtiPercentage}%): تتجاوز حاجز الأمان 40%."
                : $"Elevated debt burden ({affordability.DtiPercentage}%): Exceeds safe 40% threshold.";
        }
        else if (affordability.DtiPercentage > 30m)
        {
            dtiScore = 14m;
            dtiDetail = isAr
                ? $"نسبة عبء الدين متوسطة ({affordability.DtiPercentage}%): مقبولة ولكن تتطلب انضباطاً."
                : $"Moderate debt burden ({affordability.DtiPercentage}%): Manageable but requires disciplined budgeting.";
        }
        else
        {
            dtiScore = 4m;
            dtiDetail = isAr
                ? $"نسبة عبء الدين ممتازة ومريحة جداً ({affordability.DtiPercentage}%)."
                : $"Optimal debt ratio ({affordability.DtiPercentage}%): Well within healthy cash flow limits.";
        }
        factors.Add(new RiskFactorScore(
            isAr ? "عبء الدين والتدفق النقدي (DTI)" : "Debt-To-Income & Cash Flow Burden (DTI)", 
            dtiScore, 35m, dtiDetail));

        // 2. Liquidity & Emergency Cushion (Max 25 points)
        decimal runwayScore;
        string runwayDetail;
        if (affordability.EmergencyRunwayMonths < 1.0m)
        {
            runwayScore = 25m;
            runwayDetail = isAr
                ? $"انعدام السيولة: المدخرات تغطي {affordability.EmergencyRunwayMonths} شهر فقط (خطر فوري عند أي تأخير بالراتب)."
                : $"Severe illiquidity: Savings cover only {affordability.EmergencyRunwayMonths:0.0} months (immediate risk if salary is delayed).";
        }
        else if (affordability.EmergencyRunwayMonths < 3.0m)
        {
            runwayScore = 18m;
            runwayDetail = isAr
                ? $"صندوق طوارئ هش: {affordability.EmergencyRunwayMonths} شهور (أقل من الحد الأدنى الموصى به 3-6 شهور)."
                : $"Fragile emergency cushion: {affordability.EmergencyRunwayMonths:0.0} months (below recommended 3-6 months).";
        }
        else if (affordability.EmergencyRunwayMonths < 6.0m)
        {
            runwayScore = 8m;
            runwayDetail = isAr
                ? $"صندوق طوارئ جيد: يغطي {affordability.EmergencyRunwayMonths} شهور من المصاريف والأقساط."
                : $"Healthy emergency buffer: Covers {affordability.EmergencyRunwayMonths:0.0} months of expenses and installments.";
        }
        else
        {
            runwayScore = 0m;
            runwayDetail = isAr
                ? $"وسادة أمان مالية صلبة: المدخرات تغطي {affordability.EmergencyRunwayMonths} شهور."
                : $"Robust emergency buffer: Covers {affordability.EmergencyRunwayMonths:0.0} months of all liabilities.";
        }
        factors.Add(new RiskFactorScore(
            isAr ? "السيولة وصندوق الطوارئ" : "Liquidity & Emergency Cushion", 
            runwayScore, 25m, runwayDetail));

        // 3. Monte Carlo Stochastic Default Risk (Max 25 points)
        decimal mcScore;
        string mcDetail;
        if (monteCarlo.DefaultProbabilityPercentage > 30m)
        {
            mcScore = 25m;
            mcDetail = isAr
                ? $"احتمالية تعثر حرجة في المحاكاة العشوائية ({monteCarlo.DefaultProbabilityPercentage}%)."
                : $"Critical default probability in stochastic simulation ({monteCarlo.DefaultProbabilityPercentage}%).";
        }
        else if (monteCarlo.DefaultProbabilityPercentage > 15m)
        {
            mcScore = 18m;
            mcDetail = isAr
                ? $"احتمالية تعثر مرتفعة ({monteCarlo.DefaultProbabilityPercentage}%)، وأخطر شهر محتمل هو الشهر {monteCarlo.PeakRiskMonth ?? 1}."
                : $"High default probability ({monteCarlo.DefaultProbabilityPercentage}%), peak risk concentrated around month {monteCarlo.PeakRiskMonth ?? 1}.";
        }
        else if (monteCarlo.DefaultProbabilityPercentage > 5m)
        {
            mcScore = 10m;
            mcDetail = isAr
                ? $"احتمالية تعثر طفيفة ({monteCarlo.DefaultProbabilityPercentage}%) تحت الصدمات العشوائية."
                : $"Low default probability ({monteCarlo.DefaultProbabilityPercentage}%) under stochastic shocks.";
        }
        else
        {
            mcScore = 0m;
            mcDetail = isAr
                ? $"احتمالية تعثر شبه منعدمة ({monteCarlo.DefaultProbabilityPercentage}%) في {monteCarlo.TotalIterations} سيناريو محاكى."
                : $"Near-zero default probability ({monteCarlo.DefaultProbabilityPercentage}%) across {monteCarlo.TotalIterations:N0} simulated paths.";
        }
        factors.Add(new RiskFactorScore(
            isAr ? "محاكاة التعثر العشوائية (Monte Carlo)" : "Stochastic Default Risk (Monte Carlo)", 
            mcScore, 25m, mcDetail));

        // 4. Volatility, Stability & Profile Risk (Max 15 points)
        decimal profileScore = 0m;
        var profileNotes = new List<string>();

        if (profile.Stability == IncomeStability.Freelance)
        {
            profileScore += 7m;
            profileNotes.Add(isAr ? "طبيعة العمل الحر تنطوي على تذبذب تدفقات الدخل" : "Freelance income involves cash flow volatility");
        }
        else if (profile.Stability == IncomeStability.BusinessOwner)
        {
            profileScore += 8m;
            profileNotes.Add(isAr ? "نشاط تجاري/عمل خاص معرض لدورات السوق" : "Business enterprise exposed to market cycles");
        }

        if (profile.DependentsCount >= 3)
        {
            profileScore += 4m;
            profileNotes.Add(isAr ? $"عدد المعالين ({profile.DependentsCount}) يرفع الالتزامات الإلزامية" : $"High dependents count ({profile.DependentsCount}) increases non-discretionary commitments");
        }

        if (profile.Age >= 55)
        {
            profileScore += 3m;
            profileNotes.Add(isAr ? $"السن ({profile.Age}) يقترب من سن التقاعد" : $"Age ({profile.Age}) approaching retirement threshold");
        }

        if (!profile.HasHealthInsurance)
        {
            decimal insuranceRisk = profile.DependentsCount >= 2 ? 4m : (profile.DependentsCount == 1 ? 3m : 2m);
            profileScore += insuranceRisk;
            profileNotes.Add(isAr 
                ? (profile.DependentsCount > 0 
                    ? $"غياب التأمين الصحي مع وجود معالين ({profile.DependentsCount}) يرفع قابلية التعثر في الطوارئ الطبية (+{insuranceRisk:0} درجات مخاطر)" 
                    : "عدم وجود تأمين صحي يعرض الخطة لمخاطر العلاج المفاجئ (+2 درجات مخاطر)")
                : (profile.DependentsCount > 0
                    ? $"Lack of health insurance with {profile.DependentsCount} dependents elevates emergency medical vulnerability (+{insuranceRisk:0} risk pts)"
                    : "Absence of health insurance elevates unexpected medical shock risk (+2 risk pts)"));
        }
        else
        {
            profileNotes.Add(isAr 
                ? "التغطية بالتأمين الصحي توفر حماية فعالة ضد صدمات العلاج المفاجئ" 
                : "Health insurance coverage provides effective protection against unexpected medical shocks");
        }

        profileScore = Math.Min(15m, profileScore);
        string profileSummary;
        if (profileNotes.Count > 0)
        {
            profileSummary = isAr
                ? string.Join("، ", profileNotes) + "."
                : string.Join(", ", profileNotes) + ".";
        }
        else
        {
            profileSummary = isAr
                ? "ملف وظيفي واجتماعي مستقر ومنخفض المخاطر."
                : "Stable, low-risk personal and employment profile.";
        }
        factors.Add(new RiskFactorScore(
            isAr ? "عوامل استقرار الدخل والظروف الشخصية" : "Income Stability & Personal Factors", 
            profileScore, 15m, profileSummary));

        // Total Composite Score
        decimal totalScore = factors.Sum(f => f.Score);
        totalScore = Math.Clamp(totalScore, 0m, 100m);

        RiskLevel category;
        string label;
        string summary;

        if (totalScore <= 25m)
        {
            category = RiskLevel.Safe;
            label = isAr ? "آمن" : "Safe";
            summary = isAr
                ? "القرض آمن تماماً ويتناسب مع دخلك ومدخراتك دون تشكيل أي تهديد لسلامتك المالية."
                : "The loan is fully secure and comfortably fits within your cash flow and savings without threatening your financial stability.";
        }
        else if (totalScore <= 50m)
        {
            category = RiskLevel.Moderate;
            label = isAr ? "متوسط / معتدل" : "Moderate";
            summary = isAr
                ? "القرض مقبول ولكن يتطلب الحذر ومراقبة المصاريف الشهرية لتجنب أي ضغط غير متوقع."
                : "The loan is manageable but requires disciplined expense tracking to avoid unexpected pressure.";
        }
        else if (totalScore <= 75m)
        {
            category = RiskLevel.High;
            label = isAr ? "مرتفع الخطورة" : "High Risk";
            summary = isAr
                ? "مخاطرة مرتفعة: أي طارئ بسيط (مرض، تأخر راتب) قد يدفعك للتعثر أو الاستدانة لسداد القسط."
                : "High risk: Any unexpected shock (health emergency, delayed salary) will likely trigger payment delinquency.";
        }
        else
        {
            category = RiskLevel.Critical;
            label = isAr ? "حرج / غير آمن" : "Critical Risk";
            summary = isAr
                ? "خطر تعثر وشيك: هذا القرض غير مناسب على الإطلاق لبياناتك المالية الحالية وينذر بأزمة ديون خانقة."
                : "Imminent default risk: This loan is unsustainable given your current financial profile and will trigger a debt crisis.";
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
