using LoanLens.Domain.Enums;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class RecommendationEngine : IRecommendationEngine
{
    private readonly IFinancialCalculator _calculator;

    public RecommendationEngine(IFinancialCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    private static readonly List<LoanProduct> MarketCatalog =
    [
        new LoanProduct(
            Id: "nbe-personal",
            ProviderName: "البنك الأهلي المصري (NBE)",
            ProductName: "القرض الشخصي للموظفين وأصحاب الأعمال",
            Category: "بنوك تجارية",
            MinPrincipal: 15_000m,
            MaxPrincipal: 3_000_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 21.5m, // Reducing APR
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 2.0m,
            MinMonthlyIncomeRequired: 3_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: "سعر فائدة تنافسي، بدون ضامن، فترات سداد حتى 7 سنوات مع وثيقة تأمين مجانية."
        ),
        new LoanProduct(
            Id: "cib-personal",
            ProviderName: "بنك CIB مصر",
            ProductName: "قرض شخصي مرن",
            Category: "بنوك تجارية",
            MinPrincipal: 20_000m,
            MaxPrincipal: 5_000_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 96,
            AnnualInterestRatePercentage: 22.0m, // Reducing APR
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 1.5m,
            MinMonthlyIncomeRequired: 5_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: "تأمين على الحياة، إمكانية السداد المعجل، وموافقة سريعة خلال 48 ساعة."
        ),
        new LoanProduct(
            Id: "banque-misr-easy",
            ProviderName: "بنك مصر",
            ProductName: "قرض إكسبريس السريع",
            Category: "بنوك تجارية",
            MinPrincipal: 10_000m,
            MaxPrincipal: 1_500_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 60,
            AnnualInterestRatePercentage: 22.5m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 2.0m,
            MinMonthlyIncomeRequired: 3_000m,
            MaxAgeAllowed: 65,
            FeaturesSummary: "أبسط الإجراءات وأسرع وقت صرف في السوق المصري."
        ),
        new LoanProduct(
            Id: "valu-consumer",
            ProviderName: "ڤاليو (valU)",
            ProductName: "تمويل استهلاكي / نقدي فوري",
            Category: "تمويل استهلاكي",
            MinPrincipal: 5_000m,
            MaxPrincipal: 250_000m,
            MinTenorMonths: 6,
            MaxTenorMonths: 36,
            AnnualInterestRatePercentage: 14.0m, // Flat rate
            CalculationMethod: InterestCalculationMethod.Flat,
            AdminFeePercentage: 3.5m,
            MinMonthlyIncomeRequired: 4_000m,
            MaxAgeAllowed: 65,
            FeaturesSummary: "صرف رقمي لحظي من الموبايل بدون تعقيدات بنكية، مع مرونة في فترات السداد."
        ),
        new LoanProduct(
            Id: "aman-cash",
            ProviderName: "أمان للتمويل الاستهلاكي",
            ProductName: "قرض كاش مباشر",
            Category: "تمويل استهلاكي",
            MinPrincipal: 5_000m,
            MaxPrincipal: 150_000m,
            MinTenorMonths: 6,
            MaxTenorMonths: 36,
            AnnualInterestRatePercentage: 15.0m, // Flat rate
            CalculationMethod: InterestCalculationMethod.Flat,
            AdminFeePercentage: 4.0m,
            MinMonthlyIncomeRequired: 3_500m,
            MaxAgeAllowed: 65,
            FeaturesSummary: "فروع في كل محافظات مصر، استلام نقدية فوري للسلع والخدمات."
        )
    ];

    public IReadOnlyList<LoanProduct> GetCatalog() => MarketCatalog;

    public FinalAdvisorDecision GenerateDecision(
        FinancialProfile profile, 
        LoanParameters loan, 
        AmortizationSchedule schedule, 
        AffordabilityMetrics affordability, 
        RiskEvaluation riskEvaluation)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(loan);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(affordability);
        ArgumentNullException.ThrowIfNull(riskEvaluation);

        var strategies = BuildStrategies(loan, schedule);
        var alternatives = FindAlternatives(profile, loan, schedule);

        RecommendationVerdict verdict;
        string headline;
        string reasoning;
        var adviceList = new List<string>();

        decimal targetEmergencyBuffer = (profile.FixedMonthlyExpenses + profile.CurrentMonthlyInstallments + schedule.MonthlyInstallment) * 3m;
        decimal emergencyShortfall = Math.Max(0m, targetEmergencyBuffer - profile.Savings);

        var bestAlternative = alternatives.FirstOrDefault(a => a.IsCheaperThanCurrent);

        if (riskEvaluation.Category == RiskLevel.Critical || affordability.DisposableIncomeAfterInstallment < 0)
        {
            verdict = RecommendationVerdict.AvoidLoan;
            headline = "قرار حاسم: لا تأخذ هذا القرض في الوقت الحالي (Avoid Loan)";
            reasoning = "البيانات المالية تؤكد أن هذا القرض سيؤدي إلى عجز مالي شهري أو تجاوز خطير للحد الأقصى للمديونية (50% DTI). السداد سيتعثر حتماً عند أول ظرف طارئ.";
            
            adviceList.Add($"قم أولاً ببناء صندوق طوارئ نقدي لا يقل عن {targetEmergencyBuffer:N0} ج.م (ناقص لديك حالياً {emergencyShortfall:N0} ج.م).");
            adviceList.Add("سدد جزءاً من الأقساط الحالية أولاً لتخفيض عبء الدين قبل الالتزام بدين جديد.");
            if (loan.Principal > 50_000m)
            {
                adviceList.Add($"فكر في خفض مبلغ القرض المطلوب إلى نصف القيمة الحالية ({loan.Principal * 0.5m:N0} ج.م).");
            }
        }
        else if (bestAlternative != null && bestAlternative.CostDifferenceComparedToCurrent >= 3000m)
        {
            verdict = RecommendationVerdict.TakeAlternative;
            headline = $"اقتراح ذكي: خذ بديلاً أرخص ({bestAlternative.Product.ProviderName}) يوفر لك {bestAlternative.CostDifferenceComparedToCurrent:N0} ج.م";
            reasoning = $"القرض الذي تنوي أخذه مكلف نسبياً؛ وجدنا منتجاً بديلاً من '{bestAlternative.Product.ProviderName}' بفايدة متناقصة توفر لك إجمالي فوائد {bestAlternative.CostDifferenceComparedToCurrent:N0} ج.م مع نفس فترة السداد تقريباً.";
            
            adviceList.Add($"قارن شروط '{bestAlternative.Product.ProductName}' من {bestAlternative.Product.ProviderName}.");
            adviceList.Add("انتبه للرسوم الإدارية ورسوم السداد المبكر قبل التوقيع.");
        }
        else if (riskEvaluation.Category == RiskLevel.High || affordability.DtiPercentage > 40m)
        {
            verdict = RecommendationVerdict.AdjustTerms;
            headline = "توصية: عدّل شروط القرض لخفض القسط الشهري (Adjust Terms)";
            reasoning = "القرض الحالي يضعك في منطقة الخطر العالية لأن القسط يبتلع أكثر من 40% من دخلك. يمكنك أخذ القرض بأمان إذا قمت بمد فترة السداد أو تقليل المبلغ.";

            int recommendedTenor = Math.Min(84, loan.TenorMonths + 24);
            adviceList.Add($"قم بزيادة مدة القرض من {loan.TenorMonths} شهر إلى {recommendedTenor} شهر لخفض القسط الشهري بحوالي 20-30%.");
            adviceList.Add($"أو خفض المبلغ المقترض إلى {loan.Principal * 0.75m:N0} ج.م ليبقى القسط ضمن النطاق الآمن.");
            if (emergencyShortfall > 0)
            {
                adviceList.Add($"ادخر مبلغ {emergencyShortfall:N0} ج.م إضافية كصندوق أمان قبل إتمام القرض.");
            }
        }
        else
        {
            verdict = RecommendationVerdict.TakeLoan;
            headline = "قرار إيجابي: توكل على الله، القرض مدروس وآمن (Take Loan)";
            reasoning = "نسبة عبء الدين (DTI) في النطاق الآمن جداً، والسيولة النقدية لديك توفر وسادة حماية مريحة ضد تقلبات الدخل ومصاريف الطوارئ.";

            adviceList.Add("التزم بدفع القسط في موعده لتفادي غرامات التأخير وحماية تقريرك الائتماني (I-Score).");
            adviceList.Add("إذا توفرت لك سيولة إضافية (مكافأة سنوية أو دخل ثانوي)، استغلها في السداد المبكر لتوفير الفوائد.");
            if (strategies.Count > 1)
            {
                adviceList.Add($"استراتيجية السداد المسرع ({strategies[1].StrategyName}) يمكن أن توفر لك {strategies[1].InterestSavedComparedToStandard:N0} ج.م.");
            }
        }

        return new FinalAdvisorDecision(
            Verdict: verdict,
            VerdictHeadline: headline,
            DetailedReasoning: reasoning,
            KeyActionableAdvice: adviceList,
            Strategies: strategies,
            TopAlternatives: alternatives
        );
    }

    private List<RepaymentStrategyOption> BuildStrategies(LoanParameters loan, AmortizationSchedule schedule)
    {
        var list = new List<RepaymentStrategyOption>
        {
            new RepaymentStrategyOption(
                StrategyName: "الخطة القياسية (Standard Fixed)",
                Description: "سداد القسط الشهري الثابت في موعده بدون أي مبالغ إضافية حتى نهاية المدة.",
                MonthlyPayment: schedule.MonthlyInstallment,
                TotalInterestPaid: schedule.TotalInterest,
                InterestSavedComparedToStandard: 0m,
                TotalMonthsToPayoff: loan.TenorMonths
            )
        };

        // Strategy 2: Accelerated (+15% monthly extra payment)
        if (loan.CalculationMethod == InterestCalculationMethod.Reducing)
        {
            var extraPayment = Math.Round(schedule.MonthlyInstallment * 0.15m, 2);
            var acceleratedMonthly = schedule.MonthlyInstallment + extraPayment;
            var monthlyRate = loan.AnnualInterestRatePercentage / 100m / 12m;

            decimal balance = loan.Principal;
            int months = 0;
            decimal totalInterest = 0m;

            while (balance > 0 && months < loan.TenorMonths)
            {
                months++;
                var interest = Math.Round(balance * monthlyRate, 2);
                var principalPart = Math.Min(balance, acceleratedMonthly - interest);
                totalInterest += interest;
                balance -= principalPart;
            }

            var saved = Math.Max(0m, schedule.TotalInterest - totalInterest);

            list.Add(new RepaymentStrategyOption(
                StrategyName: "الخطة السريعة (+15% زيادة شهرية)",
                Description: $"إضافة {extraPayment:N0} ج.م شهرياً مع القسط، لسداد القرض في وقت أسرع وتخفيض إجمالي الفوائد.",
                MonthlyPayment: acceleratedMonthly,
                TotalInterestPaid: totalInterest,
                InterestSavedComparedToStandard: saved,
                TotalMonthsToPayoff: months
            ));
        }

        return list;
    }

    private List<AlternativeComparison> FindAlternatives(FinancialProfile profile, LoanParameters loan, AmortizationSchedule currentSchedule)
    {
        var alternatives = new List<AlternativeComparison>();

        foreach (var prod in MarketCatalog)
        {
            // Check eligibility
            if (profile.TotalNetIncome < prod.MinMonthlyIncomeRequired) continue;
            if (profile.Age > prod.MaxAgeAllowed) continue;
            if (loan.Principal < prod.MinPrincipal || loan.Principal > prod.MaxPrincipal) continue;

            int tenor = Math.Clamp(loan.TenorMonths, prod.MinTenorMonths, prod.MaxTenorMonths);

            var altLoanParams = new LoanParameters(
                Principal: loan.Principal,
                TenorMonths: tenor,
                AnnualInterestRatePercentage: prod.AnnualInterestRatePercentage,
                CalculationMethod: prod.CalculationMethod,
                AdminFeePercentage: prod.AdminFeePercentage
            );

            var altSchedule = _calculator.CalculateSchedule(altLoanParams);
            var altTotalCost = altSchedule.TotalPayment + altLoanParams.AdminFeeAmount;

            var currentTotalCost = currentSchedule.TotalPayment + loan.AdminFeeAmount;
            var diff = currentTotalCost - altTotalCost;
            bool isCheaper = diff > 0;

            alternatives.Add(new AlternativeComparison(
                Product: prod,
                MonthlyInstallment: altSchedule.MonthlyInstallment,
                TotalInterest: altSchedule.TotalInterest,
                TotalCost: altTotalCost,
                EffectiveAprPercentage: altSchedule.EffectiveAnnualRatePercentage,
                CostDifferenceComparedToCurrent: Math.Round(Math.Abs(diff), 2),
                IsCheaperThanCurrent: isCheaper
            ));
        }

        return alternatives.OrderByDescending(a => a.IsCheaperThanCurrent).ThenBy(a => a.TotalCost).ToList();
    }
}
