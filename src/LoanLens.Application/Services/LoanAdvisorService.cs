using LoanLens.Application.DTOs;
using LoanLens.Domain.Localization;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.Services;

public class LoanAdvisorService : ILoanAdvisorService
{
    private readonly IFinancialCalculator _calculator;
    private readonly IAffordabilityAnalyzer _affordabilityAnalyzer;
    private readonly IStressTestingEngine _stressTestingEngine;
    private readonly IMonteCarloEngine _monteCarloEngine;
    private readonly IRiskScoringEngine _riskScoringEngine;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IBorrowingCapacityEngine _borrowingCapacityEngine;
    private readonly IDebtConsolidationEngine _debtConsolidationEngine;
    private readonly IRateSensitivityEngine _rateSensitivityEngine;

    public LoanAdvisorService(
        IFinancialCalculator calculator,
        IAffordabilityAnalyzer affordabilityAnalyzer,
        IStressTestingEngine stressTestingEngine,
        IMonteCarloEngine monteCarloEngine,
        IRiskScoringEngine riskScoringEngine,
        IRecommendationEngine recommendationEngine,
        IBorrowingCapacityEngine borrowingCapacityEngine,
        IDebtConsolidationEngine debtConsolidationEngine,
        IRateSensitivityEngine rateSensitivityEngine)
    {
        _calculator = calculator;
        _affordabilityAnalyzer = affordabilityAnalyzer;
        _stressTestingEngine = stressTestingEngine;
        _monteCarloEngine = monteCarloEngine;
        _riskScoringEngine = riskScoringEngine;
        _recommendationEngine = recommendationEngine;
        _borrowingCapacityEngine = borrowingCapacityEngine;
        _debtConsolidationEngine = debtConsolidationEngine;
        _rateSensitivityEngine = rateSensitivityEngine;
    }

    public AmortizationSchedule CalculateSchedule(LoanInputDto loanDto)
    {
        ArgumentNullException.ThrowIfNull(loanDto);
        return _calculator.CalculateSchedule(loanDto.ToDomain());
    }

    public AffordabilityMetrics AnalyzeAffordability(UserProfileDto profileDto, LoanInputDto loanDto)
    {
        ArgumentNullException.ThrowIfNull(profileDto);
        ArgumentNullException.ThrowIfNull(loanDto);

        var installment = _calculator.CalculateMonthlyInstallment(loanDto.ToDomain());
        return _affordabilityAnalyzer.Analyze(profileDto.ToDomain(), installment);
    }

    public StressTestSummary RunStressTest(UserProfileDto profileDto, LoanInputDto loanDto, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(profileDto);
        ArgumentNullException.ThrowIfNull(loanDto);

        var loan = loanDto.ToDomain();
        var installment = _calculator.CalculateMonthlyInstallment(loan);
        return _stressTestingEngine.RunAllScenarios(profileDto.ToDomain(), loan, installment, language);
    }

    public MonteCarloSimulationResult RunMonteCarlo(UserProfileDto profileDto, LoanInputDto loanDto, int iterations = 5000, MonteCarloOptionsDto? options = null)
    {
        ArgumentNullException.ThrowIfNull(profileDto);
        ArgumentNullException.ThrowIfNull(loanDto);

        var loan = loanDto.ToDomain();
        var installment = _calculator.CalculateMonthlyInstallment(loan);
        return _monteCarloEngine.RunSimulation(profileDto.ToDomain(), loan, installment, iterations, options?.ToDomain());
    }

    public ComprehensiveLoanReportDto GenerateFullReport(LoanAnalysisRequestDto request, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveLang = !string.IsNullOrWhiteSpace(request.Language) ? request.Language : language;
        var profile = request.Profile.ToDomain();
        var loan = request.Loan.ToDomain();

        // 1. Calculate Amortization
        var schedule = _calculator.CalculateSchedule(loan);

        // 2. Analyze Affordability
        var affordability = _affordabilityAnalyzer.Analyze(profile, schedule.MonthlyInstallment);

        // 3. Stress Test
        var stressTest = _stressTestingEngine.RunAllScenarios(profile, loan, schedule.MonthlyInstallment, effectiveLang);

        // 4. Monte Carlo Simulation
        var monteCarlo = _monteCarloEngine.RunSimulation(
            profile, 
            loan, 
            schedule.MonthlyInstallment, 
            request.MonteCarloIterations,
            request.MonteCarloOptions?.ToDomain());

        // 5. Evaluate Composite Risk
        var risk = _riskScoringEngine.Evaluate(profile, affordability, stressTest, monteCarlo, effectiveLang);

        // 6. Generate Recommendations & Alternative Comparisons
        var recommendation = _recommendationEngine.GenerateDecision(profile, loan, schedule, affordability, risk, effectiveLang);

        return new ComprehensiveLoanReportDto(
            LoanDetails: loan,
            UserProfile: profile,
            AmortizationSchedule: schedule,
            Affordability: affordability,
            StressTest: stressTest,
            MonteCarlo: monteCarlo,
            RiskEvaluation: risk,
            Recommendation: recommendation,
            GeneratedAtUtc: DateTime.UtcNow
        );
    }

    public IReadOnlyList<LoanProduct> GetLoanCatalog(string language = "en")
    {
        return _recommendationEngine.GetCatalog(language);
    }

    public ScenarioComparisonResultDto CompareScenarios(ScenarioComparisonRequestDto request, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(request);

        var effectiveLang = !string.IsNullOrWhiteSpace(request.Language) ? request.Language : language;
        var nameA = !string.IsNullOrWhiteSpace(request.ScenarioAName) ? request.ScenarioAName : (LoanLensLanguage.IsArabic(effectiveLang) ? "السيناريو الأول (أ)" : "Scenario A");
        var nameB = !string.IsNullOrWhiteSpace(request.ScenarioBName) ? request.ScenarioBName : (LoanLensLanguage.IsArabic(effectiveLang) ? "السيناريو الثاني (ب)" : "Scenario B");

        var reportA = GenerateFullReport(request.ScenarioA, effectiveLang);
        var reportB = GenerateFullReport(request.ScenarioB, effectiveLang);

        return CompareReports(reportA, reportB, nameA, nameB, effectiveLang);
    }

    public ScenarioComparisonResultDto CompareReports(
        ComprehensiveLoanReportDto reportA, 
        ComprehensiveLoanReportDto reportB, 
        string scenarioAName = "Scenario A", 
        string scenarioBName = "Scenario B", 
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(reportA);
        ArgumentNullException.ThrowIfNull(reportB);

        var isAr = LoanLensLanguage.IsArabic(language);
        var nameA = scenarioAName;
        var nameB = scenarioBName;

        var monthlyDiff = reportB.AmortizationSchedule.MonthlyInstallment - reportA.AmortizationSchedule.MonthlyInstallment;
        var totalInterestDiff = reportB.AmortizationSchedule.TotalInterest - reportA.AmortizationSchedule.TotalInterest;
        var aprDiff = reportB.AmortizationSchedule.EffectiveAnnualRatePercentage - reportA.AmortizationSchedule.EffectiveAnnualRatePercentage;
        var dtiDiff = reportB.Affordability.DtiPercentage - reportA.Affordability.DtiPercentage;
        var defaultProbDiff = reportB.MonteCarlo.DefaultProbabilityPercentage - reportA.MonteCarlo.DefaultProbabilityPercentage;
        var riskScoreDiff = reportB.RiskEvaluation.TotalRiskScore - reportA.RiskEvaluation.TotalRiskScore;

        var diffList = new List<ScenarioComparisonMetricDiff>();

        // 1. Monthly Installment
        string instAdvantage = monthlyDiff < 0
            ? (isAr ? $"{nameB} أوفر في القسط الشهري بـ {Math.Abs(monthlyDiff):N0} ج.م" : $"{nameB} saves {Math.Abs(monthlyDiff):N0} EGP monthly")
            : (monthlyDiff > 0 
                ? (isAr ? $"{nameA} أوفر في القسط الشهري بـ {Math.Abs(monthlyDiff):N0} ج.م" : $"{nameA} saves {Math.Abs(monthlyDiff):N0} EGP monthly")
                : (isAr ? "القسط متطابق في السيناريوهين" : "Identical monthly installment"));
        string instWinner = monthlyDiff < 0 ? nameB : (monthlyDiff > 0 ? nameA : (isAr ? "تعادل" : "Tie"));
        diffList.Add(new ScenarioComparisonMetricDiff(
            MetricName: isAr ? "القسط الشهري" : "Monthly Installment",
            ScenarioAValue: $"{reportA.AmortizationSchedule.MonthlyInstallment:N0} EGP",
            ScenarioBValue: $"{reportB.AmortizationSchedule.MonthlyInstallment:N0} EGP",
            NumericalDifference: monthlyDiff,
            AdvantageDescription: instAdvantage,
            PreferredScenario: instWinner
        ));

        // 2. Total Interest Paid
        string interestAdvantage = totalInterestDiff < 0
            ? (isAr ? $"{nameB} يوفر {Math.Abs(totalInterestDiff):N0} ج.م من إجمالي الفوائد" : $"{nameB} saves {Math.Abs(totalInterestDiff):N0} EGP in total interest")
            : (totalInterestDiff > 0
                ? (isAr ? $"{nameA} يوفر {Math.Abs(totalInterestDiff):N0} ج.م من إجمالي الفوائد" : $"{nameA} saves {Math.Abs(totalInterestDiff):N0} EGP in total interest")
                : (isAr ? "إجمالي الفائدة متطابق" : "Identical total interest"));
        string interestWinner = totalInterestDiff < 0 ? nameB : (totalInterestDiff > 0 ? nameA : (isAr ? "تعادل" : "Tie"));
        diffList.Add(new ScenarioComparisonMetricDiff(
            MetricName: isAr ? "إجمالي الفائدة المدفوعة" : "Total Interest Paid",
            ScenarioAValue: $"{reportA.AmortizationSchedule.TotalInterest:N0} EGP",
            ScenarioBValue: $"{reportB.AmortizationSchedule.TotalInterest:N0} EGP",
            NumericalDifference: totalInterestDiff,
            AdvantageDescription: interestAdvantage,
            PreferredScenario: interestWinner
        ));

        // 3. Effective APR
        string aprAdvantage = aprDiff < 0
            ? (isAr ? $"{nameB} بمعدل فائدة فعلية أقل بـ {Math.Abs(aprDiff):0.0}%" : $"{nameB} has lower effective APR by {Math.Abs(aprDiff):0.0}%")
            : (aprDiff > 0
                ? (isAr ? $"{nameA} بمعدل فائدة فعلية أقل بـ {Math.Abs(aprDiff):0.0}%" : $"{nameA} has lower effective APR by {Math.Abs(aprDiff):0.0}%")
                : (isAr ? "معدل APR متطابق" : "Identical APR"));
        string aprWinner = aprDiff < 0 ? nameB : (aprDiff > 0 ? nameA : (isAr ? "تعادل" : "Tie"));
        diffList.Add(new ScenarioComparisonMetricDiff(
            MetricName: isAr ? "الفائدة الفعلية السنوية (APR)" : "Effective Annual Rate (APR)",
            ScenarioAValue: $"{reportA.AmortizationSchedule.EffectiveAnnualRatePercentage:0.0}%",
            ScenarioBValue: $"{reportB.AmortizationSchedule.EffectiveAnnualRatePercentage:0.0}%",
            NumericalDifference: aprDiff,
            AdvantageDescription: aprAdvantage,
            PreferredScenario: aprWinner
        ));

        // 4. DTI Percentage
        string dtiAdvantage = dtiDiff < 0
            ? (isAr ? $"{nameB} يترك عبء دين أقل بـ {Math.Abs(dtiDiff):0.0}%" : $"{nameB} leaves lower debt burden by {Math.Abs(dtiDiff):0.0}%")
            : (dtiDiff > 0
                ? (isAr ? $"{nameA} يترك عبء دين أقل بـ {Math.Abs(dtiDiff):0.0}%" : $"{nameA} leaves lower debt burden by {Math.Abs(dtiDiff):0.0}%")
                : (isAr ? "نسبة DTI متطابقة" : "Identical DTI"));
        string dtiWinner = dtiDiff < 0 ? nameB : (dtiDiff > 0 ? nameA : (isAr ? "تعادل" : "Tie"));
        diffList.Add(new ScenarioComparisonMetricDiff(
            MetricName: isAr ? "نسبة عبء الدين (DTI)" : "Debt-to-Income (DTI)",
            ScenarioAValue: $"{reportA.Affordability.DtiPercentage:0.0}%",
            ScenarioBValue: $"{reportB.Affordability.DtiPercentage:0.0}%",
            NumericalDifference: dtiDiff,
            AdvantageDescription: dtiAdvantage,
            PreferredScenario: dtiWinner
        ));

        // 5. Monte Carlo Default Probability
        string probAdvantage = defaultProbDiff < 0
            ? (isAr ? $"{nameB} احتمالية تعثر أقل بـ {Math.Abs(defaultProbDiff):0.0}%" : $"{nameB} lower default risk by {Math.Abs(defaultProbDiff):0.0}%")
            : (defaultProbDiff > 0
                ? (isAr ? $"{nameA} احتمالية تعثر أقل بـ {Math.Abs(defaultProbDiff):0.0}%" : $"{nameA} lower default risk by {Math.Abs(defaultProbDiff):0.0}%")
                : (isAr ? "احتمالية التعثر متطابقة" : "Identical default probability"));
        string probWinner = defaultProbDiff < 0 ? nameB : (defaultProbDiff > 0 ? nameA : (isAr ? "تعادل" : "Tie"));
        diffList.Add(new ScenarioComparisonMetricDiff(
            MetricName: isAr ? "احتمالية التعثر (Monte Carlo)" : "Default Probability (Monte Carlo)",
            ScenarioAValue: $"{reportA.MonteCarlo.DefaultProbabilityPercentage:0.0}%",
            ScenarioBValue: $"{reportB.MonteCarlo.DefaultProbabilityPercentage:0.0}%",
            NumericalDifference: defaultProbDiff,
            AdvantageDescription: probAdvantage,
            PreferredScenario: probWinner
        ));

        // Determine Overall Winner & Rationale
        string winnerName;
        string verdictSummary;
        string rationale;

        int scoreA = 0;
        int scoreB = 0;

        if (reportA.Recommendation.Verdict < reportB.Recommendation.Verdict) scoreA += 3;
        else if (reportB.Recommendation.Verdict < reportA.Recommendation.Verdict) scoreB += 3;

        if (reportA.RiskEvaluation.TotalRiskScore < reportB.RiskEvaluation.TotalRiskScore) scoreA += 2;
        else if (reportB.RiskEvaluation.TotalRiskScore < reportA.RiskEvaluation.TotalRiskScore) scoreB += 2;

        if (reportA.AmortizationSchedule.TotalInterest < reportB.AmortizationSchedule.TotalInterest) scoreA += 2;
        else if (reportB.AmortizationSchedule.TotalInterest < reportA.AmortizationSchedule.TotalInterest) scoreB += 2;

        if (reportA.Affordability.DtiPercentage < reportB.Affordability.DtiPercentage) scoreA += 1;
        else if (reportB.Affordability.DtiPercentage < reportA.Affordability.DtiPercentage) scoreB += 1;

        if (scoreB > scoreA)
        {
            winnerName = nameB;
            verdictSummary = isAr 
                ? $"الخيار المفضل هو ({nameB}) لتفوقه من حيث الأمان المالي والتكلفة الإجمالية."
                : $"The preferred choice is ({nameB}) due to superior financial safety and total cost.";
            rationale = isAr
                ? $"يوفر ({nameB}) استقراراً مالياً أفضل مع فارق فائدة يبلغ {Math.Abs(totalInterestDiff):N0} ج.م ومخاطر ائتمانية أقل بـ {Math.Abs(riskScoreDiff):0.0} نقطة."
                : $"({nameB}) offers better financial resilience with an interest difference of {Math.Abs(totalInterestDiff):N0} EGP and {Math.Abs(riskScoreDiff):0.0} points lower risk.";
        }
        else if (scoreA > scoreB)
        {
            winnerName = nameA;
            verdictSummary = isAr 
                ? $"الخيار المفضل هو ({nameA}) لتفوقه من حيث الأمان المالي والتكلفة الإجمالية."
                : $"The preferred choice is ({nameA}) due to superior financial safety and total cost.";
            rationale = isAr
                ? $"يوفر ({nameA}) استقراراً مالياً أفضل مع فارق فائدة يبلغ {Math.Abs(totalInterestDiff):N0} ج.م ومخاطر ائتمانية أقل بـ {Math.Abs(riskScoreDiff):0.0} نقطة."
                : $"({nameA}) offers better financial resilience with an interest difference of {Math.Abs(totalInterestDiff):N0} EGP and {Math.Abs(riskScoreDiff):0.0} points lower risk.";
        }
        else
        {
            winnerName = isAr ? "كلا الخيارين متقاربان" : "Both Scenarios Are Comparable";
            verdictSummary = isAr
                ? "السيناريوهان متقاربان جداً في درجة الأمان والتكلفة؛ اختر الأنسب لسيولتك الشهرية."
                : "Both scenarios are very close in safety and cost; pick whichever fits your monthly cash flow preference.";
            rationale = isAr
                ? "الفروق المالية طفيفة جداً، وينبغي تفضيل القسط الأقل إذا كانت الأولوية لسيولة الطوارئ."
                : "The financial differences are minimal; favor the lower monthly installment if emergency liquidity is your priority.";
        }

        return new ScenarioComparisonResultDto(
            ScenarioAName: nameA,
            ScenarioBName: nameB,
            ReportA: reportA,
            ReportB: reportB,
            MonthlyInstallmentDiff: Math.Round(monthlyDiff, 2),
            TotalInterestDiff: Math.Round(totalInterestDiff, 2),
            EffectiveAprDiff: Math.Round(aprDiff, 2),
            DtiDiff: Math.Round(dtiDiff, 2),
            DefaultProbabilityDiff: Math.Round(defaultProbDiff, 2),
            RiskScoreDiff: Math.Round(riskScoreDiff, 2),
            KeyDifferences: diffList,
            VerdictComparisonSummary: verdictSummary,
            WinnerScenarioName: winnerName,
            DetailedAdvisorRationale: rationale
        );
    }

    public BorrowingCapacityResult CalculateBorrowingCapacity(BorrowingCapacityRequestDto request, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(request);
        var effectiveLang = !string.IsNullOrWhiteSpace(request.Language) ? request.Language : language;
        return _borrowingCapacityEngine.CalculateCapacity(
            request.Profile.ToDomain(),
            request.TargetInterestRatePercentage,
            request.TargetTenorMonths,
            request.CalculationMethod,
            effectiveLang
        );
    }

    public DebtConsolidationResult AnalyzeDebtConsolidation(DebtConsolidationRequestDto request, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(request);
        var effectiveLang = !string.IsNullOrWhiteSpace(request.Language) ? request.Language : language;
        var debts = request.ExistingDebts.Select(d => d.ToDomain()).ToList();
        return _debtConsolidationEngine.AnalyzeConsolidation(
            request.Profile.ToDomain(),
            debts,
            request.TargetOffer.ToDomain(),
            effectiveLang
        );
    }

    public RateSensitivitySummary AnalyzeRateSensitivity(RateSensitivityRequestDto request, string language = "en")
    {
        ArgumentNullException.ThrowIfNull(request);
        var effectiveLang = !string.IsNullOrWhiteSpace(request.Language) ? request.Language : language;
        var loan = request.Loan.ToDomain();
        var installment = _calculator.CalculateMonthlyInstallment(loan);
        return _rateSensitivityEngine.AnalyzeSensitivity(loan, installment, effectiveLang);
    }
}
