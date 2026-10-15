using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class RecommendationEngine : IRecommendationEngine
{
    private readonly IFinancialCalculator _calculator;

    public RecommendationEngine(IFinancialCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    private static List<LoanProduct> GetMarketCatalog(bool isAr) =>
    [
        new LoanProduct(
            Id: "nbe-personal",
            ProviderName: isAr ? "البنك الأهلي المصري (NBE)" : "National Bank of Egypt (NBE)",
            ProductName: isAr ? "القرض الشخصي للموظفين وأصحاب الأعمال" : "Personal Loan for Employees & Business Owners",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 15_000m,
            MaxPrincipal: 3_000_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 21.5m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 2.0m,
            MinMonthlyIncomeRequired: 3_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: isAr 
                ? "سعر فائدة تنافسي، بدون ضامن، فترات سداد حتى 7 سنوات مع وثيقة تأمين مجانية."
                : "Competitive interest rate, no guarantor required, repayment up to 7 years with free life insurance."
        ),
        new LoanProduct(
            Id: "cib-personal",
            ProviderName: isAr ? "بنك CIB مصر" : "Commercial International Bank (CIB)",
            ProductName: isAr ? "قرض شخصي مرن" : "Flexible Personal Loan",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 20_000m,
            MaxPrincipal: 5_000_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 96,
            AnnualInterestRatePercentage: 22.0m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 1.5m,
            MinMonthlyIncomeRequired: 5_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: isAr 
                ? "تأمين على الحياة، إمكانية السداد المعجل، وموافقة سريعة خلال 48 ساعة."
                : "Life insurance, early settlement flexibility, and rapid approval within 48 hours."
        ),
        new LoanProduct(
            Id: "banque-misr-easy",
            ProviderName: isAr ? "بنك مصر" : "Banque Misr",
            ProductName: isAr ? "قرض إكسبريس السريع" : "Express Cash Loan",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 10_000m,
            MaxPrincipal: 1_500_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 60,
            AnnualInterestRatePercentage: 22.5m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 2.0m,
            MinMonthlyIncomeRequired: 3_000m,
            MaxAgeAllowed: 65,
            FeaturesSummary: isAr 
                ? "أبسط الإجراءات وأسرع وقت صرف في السوق المصري."
                : "Simplest procedures and rapid disbursement in the Egyptian market."
        ),
        new LoanProduct(
            Id: "valu-consumer",
            ProviderName: isAr ? "ڤاليو (valU)" : "valU Consumer Finance",
            ProductName: isAr ? "تمويل استهلاكي / نقدي فوري" : "Instant Consumer & Cash Financing",
            Category: isAr ? "تمويل استهلاكي" : "Consumer Finance",
            MinPrincipal: 5_000m,
            MaxPrincipal: 250_000m,
            MinTenorMonths: 6,
            MaxTenorMonths: 36,
            AnnualInterestRatePercentage: 14.0m,
            CalculationMethod: InterestCalculationMethod.Flat,
            AdminFeePercentage: 3.5m,
            MinMonthlyIncomeRequired: 4_000m,
            MaxAgeAllowed: 65,
            FeaturesSummary: isAr 
                ? "صرف رقمي لحظي من الموبايل بدون تعقيدات بنكية، مع مرونة في فترات السداد."
                : "Instant mobile disbursement without bank bureaucracy, flexible tenor options."
        ),
        new LoanProduct(
            Id: "aman-cash",
            ProviderName: isAr ? "أمان للتمويل الاستهلاكي" : "Aman Consumer Finance",
            ProductName: isAr ? "قرض كاش مباشر" : "Direct Cash Loan",
            Category: isAr ? "تمويل استهلاكي" : "Consumer Finance",
            MinPrincipal: 5_000m,
            MaxPrincipal: 150_000m,
            MinTenorMonths: 6,
            MaxTenorMonths: 36,
            AnnualInterestRatePercentage: 15.0m,
            CalculationMethod: InterestCalculationMethod.Flat,
            AdminFeePercentage: 4.0m,
            MinMonthlyIncomeRequired: 3_500m,
            MaxAgeAllowed: 65,
            FeaturesSummary: isAr 
                ? "فروع في كل محافظات مصر، استلام نقدية فوري للسلع والخدمات."
                : "Extensive nationwide branch network across Egypt, instant cash pickup."
        )
    ];

    public IReadOnlyList<LoanProduct> GetCatalog(string language = "en") => 
        GetMarketCatalog(LoanLensLanguage.IsArabic(language));

    public FinalAdvisorDecision GenerateDecision(
        FinancialProfile profile, 
        LoanParameters loan, 
        AmortizationSchedule schedule, 
        AffordabilityMetrics affordability, 
        RiskEvaluation riskEvaluation,
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(loan);
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(affordability);
        ArgumentNullException.ThrowIfNull(riskEvaluation);

        var isAr = LoanLensLanguage.IsArabic(language);
        var catalog = GetMarketCatalog(isAr);
        var alternatives = EvaluateAlternatives(profile, loan, schedule, catalog);
        var strategies = BuildStrategies(loan, schedule, isAr);

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
            headline = isAr
                ? "قرار حاسم: لا تأخذ هذا القرض في الوقت الحالي (Avoid Loan)"
                : "Decisive Verdict: Avoid taking this loan at this time (Avoid Loan)";
            reasoning = isAr
                ? "البيانات المالية تؤكد أن هذا القرض سيؤدي إلى عجز مالي شهري أو تجاوز خطير للحد الأقصى للمديونية (50% DTI). السداد سيتعثر حتماً عند أول ظرف طارئ."
                : "Financial assessment indicates this loan will cause monthly cash deficits or breach regulatory debt limits (50% DTI). Repayment will likely fail under minor shocks.";
            
            adviceList.Add(isAr
                ? $"قم أولاً ببناء صندوق طوارئ نقدي لا يقل عن {targetEmergencyBuffer:N0} ج.م (ناقص لديك حالياً {emergencyShortfall:N0} ج.م)."
                : $"Build an emergency cash reserve of at least {targetEmergencyBuffer:N0} EGP first (current shortfall: {emergencyShortfall:N0} EGP).");
            adviceList.Add(isAr
                ? "سدد جزءاً من الأقساط الحالية أولاً لتخفيض عبء الدين قبل الالتزام بدين جديد."
                : "Pay down existing debt balances first to free up debt-to-income capacity before borrowing anew.");
            if (loan.Principal > 50_000m)
            {
                adviceList.Add(isAr
                    ? $"فكر في خفض مبلغ القرض المطلوب إلى نصف القيمة الحالية ({loan.Principal * 0.5m:N0} ج.م)."
                    : $"Consider reducing requested loan principal by half ({loan.Principal * 0.5m:N0} EGP).");
            }
        }
        else if (bestAlternative != null && bestAlternative.CostDifferenceComparedToCurrent >= 3000m)
        {
            verdict = RecommendationVerdict.TakeAlternative;
            headline = isAr
                ? $"اقتراح ذكي: خذ بديلاً أرخص ({bestAlternative.Product.ProviderName}) يوفر لك {bestAlternative.CostDifferenceComparedToCurrent:N0} ج.م"
                : $"Smart Choice: Switch to a cheaper alternative ({bestAlternative.Product.ProviderName}) saving {bestAlternative.CostDifferenceComparedToCurrent:N0} EGP";
            reasoning = isAr
                ? $"القرض الذي تنوي أخذه مكلف نسبياً؛ وجدنا منتجاً بديلاً من '{bestAlternative.Product.ProviderName}' بفايدة متناقصة توفر لك إجمالي فوائد {bestAlternative.CostDifferenceComparedToCurrent:N0} ج.م مع نفس فترة السداد تقريباً."
                : $"The requested loan is relatively costly. An alternative from '{bestAlternative.Product.ProviderName}' with reducing interest saves you {bestAlternative.CostDifferenceComparedToCurrent:N0} EGP over approximately the same tenure.";
            
            adviceList.Add(isAr
                ? $"قارن شروط '{bestAlternative.Product.ProductName}' من {bestAlternative.Product.ProviderName}."
                : $"Compare terms for '{bestAlternative.Product.ProductName}' from {bestAlternative.Product.ProviderName}.");
            adviceList.Add(isAr
                ? "انتبه للرسوم الإدارية ورسوم السداد المبكر قبل التوقيع."
                : "Verify early settlement and admin fee conditions before signing.");
        }
        else if (riskEvaluation.Category == RiskLevel.High || affordability.DtiPercentage > 40m)
        {
            verdict = RecommendationVerdict.AdjustTerms;
            headline = isAr
                ? "توصية: عدّل شروط القرض لخفض القسط الشهري (Adjust Terms)"
                : "Recommendation: Adjust loan terms to lower monthly installments (Adjust Terms)";
            reasoning = isAr
                ? "القرض الحالي يضعك في منطقة الخطر العالية لأن القسط يبتلع أكثر من 40% من دخلك. يمكنك أخذ القرض بأمان إذا قمت بمد فترة السداد أو تقليل المبلغ."
                : "The current loan places you in a high-risk zone as the installment absorbs over 40% of income. You can safely proceed by extending tenor or reducing principal.";

            int recommendedTenor = Math.Min(84, loan.TenorMonths + 24);
            adviceList.Add(isAr
                ? $"قم بزيادة مدة القرض من {loan.TenorMonths} شهر إلى {recommendedTenor} شهر لخفض القسط الشهري بحوالي 20-30%."
                : $"Extend loan tenor from {loan.TenorMonths} to {recommendedTenor} months to lower monthly installment by 20-30%.");
            adviceList.Add(isAr
                ? $"أو خفض المبلغ المقترض إلى {loan.Principal * 0.75m:N0} ج.م ليبقى القسط ضمن النطاق الآمن."
                : $"Or reduce loan principal to {loan.Principal * 0.75m:N0} EGP to keep payment within safe boundaries.");
            if (emergencyShortfall > 0)
            {
                adviceList.Add(isAr
                    ? $"ادخر مبلغ {emergencyShortfall:N0} ج.م إضافية كصندوق أمان قبل إتمام القرض."
                    : $"Accumulate {emergencyShortfall:N0} EGP in extra emergency savings prior to signing.");
            }
        }
        else
        {
            verdict = RecommendationVerdict.TakeLoan;
            headline = isAr
                ? "قرار إيجابي: توكل على الله، القرض مدروس وآمن (Take Loan)"
                : "Positive Verdict: Proceed with confidence, loan is sound and affordable (Take Loan)";
            reasoning = isAr
                ? "نسبة عبء الدين (DTI) في النطاق الآمن جداً، والسيولة النقدية لديك توفر وسادة حماية مريحة ضد تقلبات الدخل ومصاريف الطوارئ."
                : "Your debt-to-income ratio (DTI) is well within safe thresholds, and your cash reserves provide a comfortable safety cushion against unexpected shocks.";

            adviceList.Add(isAr
                ? "التزم بدفع القسط في موعده لتفادي غرامات التأخير وحماية تقريرك الائتماني (I-Score)."
                : "Set up automated monthly payments to avoid late penalty fees and protect your I-Score rating.");
            adviceList.Add(isAr
                ? "إذا توفرت لك سيولة إضافية (مكافأة سنوية أو دخل ثانوي)، استغلها في السداد المبكر لتوفير الفوائد."
                : "Direct surplus cash flow (annual bonuses, secondary income) toward early principal payments to cut total interest.");
            if (strategies.Count > 1)
            {
                adviceList.Add(isAr
                    ? $"استراتيجية السداد المسرع ({strategies[1].StrategyName}) يمكن أن توفر لك {strategies[1].InterestSavedComparedToStandard:N0} ج.م."
                    : $"Accelerated payoff strategy ({strategies[1].StrategyName}) can save you {strategies[1].InterestSavedComparedToStandard:N0} EGP.");
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

    private List<RepaymentStrategyOption> BuildStrategies(LoanParameters loan, AmortizationSchedule schedule, bool isAr)
    {
        var list = new List<RepaymentStrategyOption>
        {
            new RepaymentStrategyOption(
                StrategyName: isAr ? "الخطة القياسية (Standard Fixed)" : "Standard Fixed Plan",
                Description: isAr ? "سداد القسط الشهري الثابت في موعده بدون أي مبالغ إضافية حتى نهاية المدة." : "Pay regular fixed monthly installment with no extra prepayments until term ends.",
                MonthlyPayment: schedule.MonthlyInstallment,
                TotalInterestPaid: schedule.TotalInterest,
                InterestSavedComparedToStandard: 0m,
                TotalMonthsToPayoff: loan.TenorMonths
            )
        };

        if (loan.CalculationMethod == InterestCalculationMethod.Reducing && loan.TenorMonths > 12)
        {
            var extra = schedule.MonthlyInstallment * 0.15m;
            var acceleratedPayment = schedule.MonthlyInstallment + extra;

            var r = (loan.AnnualInterestRatePercentage / 100m) / 12m;
            decimal balance = loan.Principal;
            decimal totalInterest = 0m;
            int months = 0;

            while (balance > 0m && months < loan.TenorMonths)
            {
                months++;
                var interest = balance * r;
                var principalPaid = acceleratedPayment - interest;

                if (principalPaid >= balance)
                {
                    totalInterest += balance * r;
                    balance = 0m;
                    break;
                }

                totalInterest += interest;
                balance -= principalPaid;
            }

            var interestSaved = Math.Max(0m, schedule.TotalInterest - totalInterest);

            list.Add(new RepaymentStrategyOption(
                StrategyName: isAr ? "الخطة السريعة (+15% زيادة شهرية)" : "Accelerated Payoff Plan (+15% Monthly)",
                Description: isAr 
                    ? $"إضافة {Math.Round(extra, 2):N0} ج.م شهرياً مع القسط، لسداد القرض في وقت أسرع وتخفيض إجمالي الفوائد."
                    : $"Add {Math.Round(extra, 2):N0} EGP extra monthly to settle early and slash total interest paid.",
                MonthlyPayment: Math.Round(acceleratedPayment, 2),
                TotalInterestPaid: Math.Round(totalInterest, 2),
                InterestSavedComparedToStandard: Math.Round(interestSaved, 2),
                TotalMonthsToPayoff: months
            ));
        }

        return list;
    }

    private List<AlternativeComparison> EvaluateAlternatives(
        FinancialProfile profile, 
        LoanParameters targetLoan, 
        AmortizationSchedule targetSchedule,
        List<LoanProduct> catalog)
    {
        var comparisons = new List<AlternativeComparison>();

        foreach (var product in catalog)
        {
            if (targetLoan.Principal < product.MinPrincipal || targetLoan.Principal > product.MaxPrincipal)
                continue;
            if (profile.TotalNetIncome < product.MinMonthlyIncomeRequired)
                continue;
            if (profile.Age > product.MaxAgeAllowed)
                continue;

            int tenor = Math.Clamp(targetLoan.TenorMonths, product.MinTenorMonths, product.MaxTenorMonths);

            var altParams = new LoanParameters(
                Principal: targetLoan.Principal,
                TenorMonths: tenor,
                AnnualInterestRatePercentage: product.AnnualInterestRatePercentage,
                CalculationMethod: product.CalculationMethod,
                AdminFeePercentage: product.AdminFeePercentage
            );

            var altSchedule = _calculator.CalculateSchedule(altParams);
            var targetAdminFee = targetLoan.Principal * (targetLoan.AdminFeePercentage / 100m);
            var targetTotalCost = targetSchedule.TotalPayment + targetAdminFee;

            var altAdminFee = altParams.Principal * (altParams.AdminFeePercentage / 100m);
            var altTotalCost = altSchedule.TotalPayment + altAdminFee;
            var costDiff = targetTotalCost - altTotalCost;

            comparisons.Add(new AlternativeComparison(
                Product: product,
                MonthlyInstallment: altSchedule.MonthlyInstallment,
                TotalInterest: altSchedule.TotalInterest,
                TotalCost: Math.Round(altTotalCost, 2),
                EffectiveAprPercentage: altSchedule.EffectiveAnnualRatePercentage,
                CostDifferenceComparedToCurrent: Math.Round(Math.Abs(costDiff), 2),
                IsCheaperThanCurrent: costDiff > 0m
            ));
        }

        return comparisons.OrderBy(c => c.TotalCost).ToList();
    }
}
