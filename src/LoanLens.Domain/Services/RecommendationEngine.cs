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
        // 1. National Bank of Egypt (NBE)
        new LoanProduct(
            Id: "nbe-personal",
            ProviderName: isAr ? "البنك الأهلي المصري (NBE)" : "National Bank of Egypt (NBE)",
            ProductName: isAr ? "القرض الشخصي النقدي للموظفين وأصحاب الأعمال" : "Cash Personal Loan for Employees & Business Owners",
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
                ? "سعر فائدة تنافسي، بدون ضامن، فترات سداد حتى 7 سنوات، وثيقة تأمين مجانية على الحياة، غرامة سداد معجل 3% نقدي."
                : "Competitive reducing interest, no guarantor, tenor up to 7 years, free life insurance, 3% early settlement fee.",
            EarlySettlementFeePercentage: 3.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 2. Banque Misr
        new LoanProduct(
            Id: "banque-misr-easy",
            ProviderName: isAr ? "بنك مصر" : "Banque Misr",
            ProductName: isAr ? "قرض إكسبريس النقدي السريع" : "Express Cash Loan",
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
                ? "أبسط الإجراءات وأسرع وقت صرف في السوق المصري خلال 24 ساعة، غرامة سداد معجل 3%."
                : "Fastest disbursement in Egyptian market within 24 hours, minimal paperwork, 3% early settlement fee.",
            EarlySettlementFeePercentage: 3.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 3. Commercial International Bank (CIB)
        new LoanProduct(
            Id: "cib-personal",
            ProviderName: isAr ? "بنك CIB مصر" : "Commercial International Bank (CIB)",
            ProductName: isAr ? "قرض شخصي مرن بموافقة سريعة" : "Flexible Personal Loan",
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
                ? "مبالغ تمويل تصل لـ 5 ملايين، فترات سداد حتى 8 سنوات، تأمين شامل على الحياة، غرامة سداد معجل 5%."
                : "Financing up to 5M EGP, tenor up to 8 years, full life insurance, 5% early settlement fee.",
            EarlySettlementFeePercentage: 5.0m,
            LatePenaltyPercentage: 2.5m
        ),

        // 4. QNB Alahli
        new LoanProduct(
            Id: "qnb-personal",
            ProviderName: isAr ? "بنك QNB مصر" : "QNB Alahli",
            ProductName: isAr ? "القرض الشخصي بضمان تحويل المرتب" : "Salary Transfer Personal Loan",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 15_000m,
            MaxPrincipal: 4_000_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 21.75m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 1.5m,
            MinMonthlyIncomeRequired: 4_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: isAr 
                ? "أسعار عائد مميزة لعملاء تحويل الراتب، إمكانية دمج التسهيلات، غرامة سداد معجل 4%."
                : "Preferred rates for salary transfer clients, debt consolidation option, 4% early settlement fee.",
            EarlySettlementFeePercentage: 4.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 5. Arab African International Bank (AAIB)
        new LoanProduct(
            Id: "aaib-personal",
            ProviderName: isAr ? "البنك العربي الأفريقي الدولي (AAIB)" : "Arab African International Bank (AAIB)",
            ProductName: isAr ? "قرض برايم كاش الشخصي" : "Prime Personal Cash Loan",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 25_000m,
            MaxPrincipal: 3_500_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 21.9m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 1.75m,
            MinMonthlyIncomeRequired: 5_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: isAr 
                ? "مرونة عالية في الدفعات، برامج مخصصة للمهنيين وكبار العملاء، غرامة سداد معجل 4%."
                : "High payment flexibility, tailored wealth and professionals programs, 4% early settlement fee.",
            EarlySettlementFeePercentage: 4.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 6. Bank of Alexandria (AlexBank)
        new LoanProduct(
            Id: "alexbank-mobasher",
            ProviderName: isAr ? "بنك الإسكندرية (AlexBank)" : "Bank of Alexandria",
            ProductName: isAr ? "قرض المباشر النقدي" : "Al-Mobasher Cash Loan",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 10_000m,
            MaxPrincipal: 2_500_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 22.0m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 2.0m,
            MinMonthlyIncomeRequired: 3_500m,
            MaxAgeAllowed: 65,
            FeaturesSummary: isAr 
                ? "شروط ميسرة للموظفين وأصحاب المهن الحرة، وثيقة تأمين مجانية، غرامة سداد معجل 3%."
                : "Flexible conditions for employees and freelancers, free insurance, 3% early settlement fee.",
            EarlySettlementFeePercentage: 3.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 7. Banque du Caire
        new LoanProduct(
            Id: "bdc-personal",
            ProviderName: isAr ? "بنك القاهرة" : "Banque du Caire",
            ProductName: isAr ? "قرض موظفي القطاع الخاص والحكومي" : "Corporate & Public Sector Loan",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 10_000m,
            MaxPrincipal: 2_000_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 22.25m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 2.0m,
            MinMonthlyIncomeRequired: 3_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: isAr 
                ? "شبكة فروع واسعة، تمويل احتياجات الأسرة والتعليم، غرامة سداد معجل 3%."
                : "Extensive branch presence, family and educational financing, 3% early settlement fee.",
            EarlySettlementFeePercentage: 3.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 8. FABMISR
        new LoanProduct(
            Id: "fabmisr-personal",
            ProviderName: isAr ? "بنك أبوظبي الأول مصر (FABMISR)" : "First Abu Dhabi Bank Misr (FABMISR)",
            ProductName: isAr ? "برنامج التمويل الشخصي المتميز" : "Elite Personal Finance",
            Category: isAr ? "بنوك تجارية" : "Commercial Banks",
            MinPrincipal: 30_000m,
            MaxPrincipal: 4_500_000m,
            MinTenorMonths: 12,
            MaxTenorMonths: 84,
            AnnualInterestRatePercentage: 21.8m,
            CalculationMethod: InterestCalculationMethod.Reducing,
            AdminFeePercentage: 1.5m,
            MinMonthlyIncomeRequired: 6_000m,
            MaxAgeAllowed: 60,
            FeaturesSummary: isAr 
                ? "خدمات بنكية راقية، سرعة الاستجابة وصرف القرض، غرامة سداد معجل 4%."
                : "Premium banking service, rapid credit decision, 4% early settlement fee.",
            EarlySettlementFeePercentage: 4.0m,
            LatePenaltyPercentage: 2.0m
        ),

        // 9. valU Consumer Finance
        new LoanProduct(
            Id: "valu-consumer",
            ProviderName: isAr ? "ڤاليو (valU)" : "valU Consumer Finance",
            ProductName: isAr ? "تمويل استهلاكي ونقدي فوري (valU Cash)" : "Instant Consumer & Cash Financing",
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
                ? "صرف رقمي لحظي من الموبايل بدون تعقيدات بنكية، تقسيط مرن حتى 36 شهراً، غرامة سداد معجل 3%."
                : "Instant mobile disbursement without bank bureaucracy, flexible tenor up to 36 months, 3% early fee.",
            EarlySettlementFeePercentage: 3.0m,
            LatePenaltyPercentage: 3.0m
        ),

        // 10. Aman Consumer Finance
        new LoanProduct(
            Id: "aman-cash",
            ProviderName: isAr ? "أمان للتمويل الاستهلاكي" : "Aman Consumer Finance",
            ProductName: isAr ? "قرض كاش وتقسيط مباشر" : "Direct Cash & Consumer Loan",
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
                ? "أكثر من 15,000 نقطة بيع في محافظات مصر، استلام نقدية فوري بدون ضامن، غرامة سداد معجل 4%."
                : "Over 15,000 touchpoints across Egypt, instant cash pickup without guarantor, 4% early fee.",
            EarlySettlementFeePercentage: 4.0m,
            LatePenaltyPercentage: 3.0m
        ),

        // 11. Contact Financial Holding
        new LoanProduct(
            Id: "contact-personal",
            ProviderName: isAr ? "كونتكت للتمويل (Contact)" : "Contact Financial Holding",
            ProductName: isAr ? "تمويل الأفراد والسلع الاستهلاكية" : "Consumer & Personal Finance",
            Category: isAr ? "تمويل استهلاكي" : "Consumer Finance",
            MinPrincipal: 10_000m,
            MaxPrincipal: 300_000m,
            MinTenorMonths: 6,
            MaxTenorMonths: 48,
            AnnualInterestRatePercentage: 14.5m,
            CalculationMethod: InterestCalculationMethod.Flat,
            AdminFeePercentage: 3.0m,
            MinMonthlyIncomeRequired: 4_000m,
            MaxAgeAllowed: 65,
            FeaturesSummary: isAr 
                ? "خبرة رائدة في التقسيط والتمويل الشخصي، موافقة فورية بالبطاقة، غرامة سداد معجل 3.5%."
                : "Market pioneer in consumer and personal installment finance, instant ID verification, 3.5% early fee.",
            EarlySettlementFeePercentage: 3.5m,
            LatePenaltyPercentage: 2.5m
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
                ? "قرار حاسم: تجنب أخذ هذا القرض في الوقت الحالي"
                : "Decisive Verdict: Avoid taking this loan at this time";
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
                ? "توصية: عدّل شروط القرض لخفض القسط الشهري"
                : "Recommendation: Adjust loan terms to lower monthly installments";
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
                ? "قرار إيجابي: القرض مدروس ومناسب لوضعك المالي"
                : "Positive Verdict: Proceed with confidence, loan is sound and affordable";
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
                    ? $"استراتيجية السداد المسرع ({strategies[1].StrategyName}) توفر لك صافي {strategies[1].NetFinancialBenefit:N0} ج.م بعد خصم غرامة السداد المعجل."
                    : $"Accelerated payoff strategy ({strategies[1].StrategyName}) yields net savings of {strategies[1].NetFinancialBenefit:N0} EGP after early settlement fee.");
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
        var list = new List<RepaymentStrategyOption>();

        // 1. Standard Fixed Strategy
        list.Add(new RepaymentStrategyOption(
            StrategyName: isAr ? "الخطة القياسية (Standard Fixed)" : "Standard Fixed Plan",
            Description: isAr 
                ? "سداد القسط الشهري الثابت في موعده بدون أي مبالغ إضافية حتى نهاية المدة." 
                : "Pay regular fixed monthly installment with no extra prepayments until term ends.",
            MonthlyPayment: schedule.MonthlyInstallment,
            TotalInterestPaid: schedule.TotalInterest,
            InterestSavedComparedToStandard: 0m,
            TotalMonthsToPayoff: loan.TenorMonths,
            EarlySettlementFeePaid: 0m,
            NetFinancialBenefit: 0m
        ));

        // 2. Accelerated Monthly Payoff (+15% Monthly Installment)
        if (loan.CalculationMethod == InterestCalculationMethod.Reducing && loan.TenorMonths > 12)
        {
            var extra = schedule.MonthlyInstallment * 0.15m;
            var acceleratedPayment = schedule.MonthlyInstallment + extra;

            var r = (loan.AnnualInterestRatePercentage / 100m) / 12m;
            decimal balance = loan.Principal;
            decimal totalInterest = 0m;
            int months = 0;
            decimal cumulativeExtraPrincipal = 0m;

            while (balance > 0m && months < loan.TenorMonths)
            {
                months++;
                var interest = balance * r;
                var principalPaid = acceleratedPayment - interest;

                if (principalPaid >= balance)
                {
                    totalInterest += balance * r;
                    cumulativeExtraPrincipal += balance;
                    balance = 0m;
                    break;
                }

                totalInterest += interest;
                balance -= principalPaid;
                cumulativeExtraPrincipal += extra;
            }

            var grossInterestSaved = Math.Max(0m, schedule.TotalInterest - totalInterest);
            var earlySettlementFee = Math.Round(cumulativeExtraPrincipal * (loan.EarlyRepaymentFeePercentage / 100m), 2);
            var netBenefit = Math.Max(0m, grossInterestSaved - earlySettlementFee);

            list.Add(new RepaymentStrategyOption(
                StrategyName: isAr ? "الخطة السريعة (+15% زيادة شهرية)" : "Accelerated Payoff Plan (+15% Monthly)",
                Description: isAr 
                    ? $"إضافة {Math.Round(extra, 2):N0} ج.م شهرياً مع القسط؛ ينهي القرض في {months} شهراً بدلاً من {loan.TenorMonths}، ويوفر فوائد {grossInterestSaved:N0} ج.م (صافي الوفر {netBenefit:N0} ج.م بعد خصم غرامة السداد المعجل {loan.EarlyRepaymentFeePercentage}% البالغة {earlySettlementFee:N0} ج.م)."
                    : $"Add {Math.Round(extra, 2):N0} EGP monthly; finishes loan in {months} months vs {loan.TenorMonths}, saving {grossInterestSaved:N0} EGP interest (net benefit {netBenefit:N0} EGP after {loan.EarlyRepaymentFeePercentage}% early fee of {earlySettlementFee:N0} EGP).",
                MonthlyPayment: Math.Round(acceleratedPayment, 2),
                TotalInterestPaid: Math.Round(totalInterest, 2),
                InterestSavedComparedToStandard: Math.Round(grossInterestSaved, 2),
                TotalMonthsToPayoff: months,
                EarlySettlementFeePaid: earlySettlementFee,
                NetFinancialBenefit: Math.Round(netBenefit, 2)
            ));
        }

        // 3. Annual Bonus / Lump-Sum Prepayment Plan
        if (loan.CalculationMethod == InterestCalculationMethod.Reducing && loan.TenorMonths >= 24)
        {
            var annualBonusPrepayment = Math.Round(loan.Principal * 0.10m, 2); // 10% of original principal once a year
            var r = (loan.AnnualInterestRatePercentage / 100m) / 12m;
            decimal balance = loan.Principal;
            decimal totalInterest = 0m;
            int months = 0;
            decimal totalLumpSumsPaid = 0m;

            while (balance > 0m && months < loan.TenorMonths)
            {
                months++;
                var interest = balance * r;
                var principalPaid = schedule.MonthlyInstallment - interest;

                // Apply annual bonus prepayment every 12 months
                if (months % 12 == 0 && balance > principalPaid)
                {
                    var bonusApplied = Math.Min(balance - principalPaid, annualBonusPrepayment);
                    principalPaid += bonusApplied;
                    totalLumpSumsPaid += bonusApplied;
                }

                if (principalPaid >= balance)
                {
                    totalInterest += balance * r;
                    balance = 0m;
                    break;
                }

                totalInterest += interest;
                balance -= principalPaid;
            }

            var grossInterestSaved = Math.Max(0m, schedule.TotalInterest - totalInterest);
            var earlySettlementFee = Math.Round(totalLumpSumsPaid * (loan.EarlyRepaymentFeePercentage / 100m), 2);
            var netBenefit = Math.Max(0m, grossInterestSaved - earlySettlementFee);

            list.Add(new RepaymentStrategyOption(
                StrategyName: isAr ? "خطة السداد الجزئي السنوي (استغلال الأرباح السنوية)" : "Annual Bonus Lump-Sum Prepayment Plan",
                Description: isAr 
                    ? $"سداد دفعة سنوية قدرها {annualBonusPrepayment:N0} ج.م (10% من أصل القرض) عند صرف المكافأة؛ يختصر المدة إلى {months} شهراً بصافي وفر فوائد {netBenefit:N0} ج.م بعد غرامة السداد المعجل."
                    : $"Apply annual lump-sum of {annualBonusPrepayment:N0} EGP (10% of principal) at bonus months; reduces duration to {months} months with net interest savings of {netBenefit:N0} EGP after early settlement fee.",
                MonthlyPayment: schedule.MonthlyInstallment,
                TotalInterestPaid: Math.Round(totalInterest, 2),
                InterestSavedComparedToStandard: Math.Round(grossInterestSaved, 2),
                TotalMonthsToPayoff: months,
                EarlySettlementFeePaid: earlySettlementFee,
                NetFinancialBenefit: Math.Round(netBenefit, 2)
            ));
        }

        // 4. Payment Holiday / Deferral Simulation (Cost Warning)
        if (loan.CalculationMethod == InterestCalculationMethod.Reducing && loan.TenorMonths > 12)
        {
            // Simulates deferring 2 installments (e.g. at months 6 and 7). Interest is capitalized into balance.
            var r = (loan.AnnualInterestRatePercentage / 100m) / 12m;
            decimal balance = loan.Principal;
            decimal totalInterest = 0m;
            int months = 0;

            while (balance > 0m && months < loan.TenorMonths + 4)
            {
                months++;
                var interest = balance * r;
                totalInterest += interest;

                if (months == 6 || months == 7)
                {
                    // Payment holiday: zero payment, interest capitalized to principal
                    balance += interest;
                    continue;
                }

                var principalPaid = schedule.MonthlyInstallment - interest;
                if (principalPaid >= balance)
                {
                    balance = 0m;
                    break;
                }
                balance -= principalPaid;
            }

            var extraInterestIncurred = Math.Max(0m, totalInterest - schedule.TotalInterest);

            list.Add(new RepaymentStrategyOption(
                StrategyName: isAr ? "محاكاة تأجيل قسطين (فترة سماح طارئة)" : "Payment Holiday Simulation (2-Month Deferral)",
                Description: isAr 
                    ? $"تأجيل قسطين عند حدوث طارئ يمد أجل السداد إلى {months} شهراً ويرفع إجمالي الفوائد بمقدار {extraInterestIncurred:N0} ج.م بسبب رسملة الفوائد (يُوصى بعدم استخدامه إلا للضرورة القصوى)."
                    : $"Deferring 2 monthly installments extends loan duration to {months} months and increases total interest by {extraInterestIncurred:N0} EGP due to interest capitalization (use only as last resort).",
                MonthlyPayment: schedule.MonthlyInstallment,
                TotalInterestPaid: Math.Round(totalInterest, 2),
                InterestSavedComparedToStandard: -Math.Round(extraInterestIncurred, 2),
                TotalMonthsToPayoff: months,
                EarlySettlementFeePaid: 0m,
                NetFinancialBenefit: -Math.Round(extraInterestIncurred, 2)
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
                AdminFeePercentage: product.AdminFeePercentage,
                EarlyRepaymentFeePercentage: product.EarlySettlementFeePercentage,
                LatePaymentPenaltyPercentage: product.LatePenaltyPercentage
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
