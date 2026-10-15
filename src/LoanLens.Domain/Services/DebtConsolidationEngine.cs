using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class DebtConsolidationEngine : IDebtConsolidationEngine
{
    private readonly IFinancialCalculator _calculator;

    public DebtConsolidationEngine(IFinancialCalculator calculator)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
    }

    public DebtConsolidationResult AnalyzeConsolidation(
        FinancialProfile profile,
        IReadOnlyList<ExistingDebtItem> existingDebts,
        ConsolidationTargetOffer targetOffer,
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(existingDebts);
        ArgumentNullException.ThrowIfNull(targetOffer);

        var isAr = LoanLensLanguage.IsArabic(language);

        decimal totalOldPrincipal = 0m;
        decimal totalEarlySettlementPenalty = 0m;
        decimal totalCurrentMonthlyInstallments = 0m;
        decimal totalRemainingInterestOldLoans = 0m;

        foreach (var debt in existingDebts)
        {
            totalOldPrincipal += debt.RemainingPrincipal;
            var penalty = debt.RemainingPrincipal * (debt.EarlySettlementFeePercentage / 100m);
            totalEarlySettlementPenalty += penalty;
            totalCurrentMonthlyInstallments += debt.MonthlyInstallment;

            var remainingObligation = debt.MonthlyInstallment * debt.RemainingMonths;
            var remainingInterest = Math.Max(0m, remainingObligation - debt.RemainingPrincipal);
            totalRemainingInterestOldLoans += remainingInterest;
        }

        // New loan must cover both remaining debt principal and early settlement penalties to existing banks
        decimal requiredNewPrincipal = totalOldPrincipal + totalEarlySettlementPenalty;

        var newLoanParams = new LoanParameters(
            Principal: requiredNewPrincipal,
            TenorMonths: targetOffer.TenorMonths,
            AnnualInterestRatePercentage: targetOffer.AnnualInterestRatePercentage,
            CalculationMethod: targetOffer.CalculationMethod,
            AdminFeePercentage: targetOffer.AdminFeePercentage
        );

        var newSchedule = _calculator.CalculateSchedule(newLoanParams);
        decimal newMonthlyInstallment = newSchedule.MonthlyInstallment;
        decimal monthlyCashFlowFreedUp = totalCurrentMonthlyInstallments - newMonthlyInstallment;

        decimal totalIncome = profile.TotalNetIncome;
        decimal currentDti = totalIncome > 0 ? Math.Round((profile.CurrentMonthlyInstallments / totalIncome) * 100m, 1) : 0m;

        // In new DTI, old debt installments are replaced by the new consolidated installment
        decimal otherInstallments = Math.Max(0m, profile.CurrentMonthlyInstallments - totalCurrentMonthlyInstallments);
        decimal newDti = totalIncome > 0 ? Math.Round(((otherInstallments + newMonthlyInstallment) / totalIncome) * 100m, 1) : 0m;

        decimal newLoanAdminFee = requiredNewPrincipal * (targetOffer.AdminFeePercentage / 100m);
        decimal netLifetimeSavings = totalRemainingInterestOldLoans - (newSchedule.TotalInterest + totalEarlySettlementPenalty + newLoanAdminFee);

        bool isDtiCbeCompliant = newDti <= 50.0m;

        DebtConsolidationVerdict verdict;
        string headline;
        string reasoning;

        if (monthlyCashFlowFreedUp > 0 && netLifetimeSavings > 0 && isDtiCbeCompliant)
        {
            verdict = DebtConsolidationVerdict.HighlyRecommended;
            headline = isAr
                ? "شراء المديونية ودمج القروض خيار ممتاز وموصى به بشدة"
                : "Debt Consolidation & Buyout is Highly Recommended";
            reasoning = isAr
                ? $"دمج الديون في قرض واحد لدى {targetOffer.ProviderName} سيوفر لك {monthlyCashFlowFreedUp:N0} ج.م شهرياً من السيولة، ويحقق وفراً كلياً في الفوائد قدره {netLifetimeSavings:N0} ج.م بعد سداد غرامات السداد المعجل والرسوم الإدارية، كما يخفض عبء الدين (DTI) من {currentDti:0.0}% إلى {newDti:0.0}%."
                : $"Consolidating into a single facility with {targetOffer.ProviderName} frees up {monthlyCashFlowFreedUp:N0} EGP monthly cash flow and yields a net lifetime interest savings of {netLifetimeSavings:N0} EGP after settling early repayment fees. It also lowers your DTI from {currentDti:0.0}% to {newDti:0.0}%.";
        }
        else if (monthlyCashFlowFreedUp > 0 && isDtiCbeCompliant)
        {
            verdict = DebtConsolidationVerdict.Recommended;
            headline = isAr
                ? "شراء المديونية مجدٍ لتخفيف الضغط الشهري (تحسين السيولة)"
                : "Debt Consolidation is Recommended for Cash Flow Relief";
            reasoning = isAr
                ? $"سيوفر الدمج راحة فورية في السيولة الشهرية بمقدار {monthlyCashFlowFreedUp:N0} ج.م شهرياً ويخفض الـ DTI إلى {newDti:0.0}%، مع العلم أن إجمالي الفائدة الكلية قد يزيد بشكل طفيف بسبب تمديد فترة السداد."
                : $"Consolidation provides immediate cash flow relief saving {monthlyCashFlowFreedUp:N0} EGP monthly and improves DTI to {newDti:0.0}%, though total lifetime interest is slightly higher due to tenor extension.";
        }
        else
        {
            verdict = DebtConsolidationVerdict.NotRecommended;
            headline = isAr
                ? "شراء المديونية غير موصى به في هذا التوقيت"
                : "Debt Consolidation is Not Recommended Under Current Terms";
            reasoning = isAr
                ? $"تكلفة غرامات السداد المعجل ({totalEarlySettlementPenalty:N0} ج.م) أو الفائدة الجديدة تجعل الدمج غير مجدٍ مالياً، أو أن القسط الجديد لا يزال يتجاوز الحدود الآمنة للبنك المركزي."
                : $"Early settlement penalties ({totalEarlySettlementPenalty:N0} EGP) or interest rates under the target offer make buyout financially disadvantageous, or the new DTI remains above safe CBE thresholds.";
        }

        return new DebtConsolidationResult(
            Verdict: verdict,
            VerdictHeadline: headline,
            DetailedReasoning: reasoning,
            TotalCurrentDebtPrincipal: Math.Round(totalOldPrincipal, 2),
            TotalEarlySettlementPenalty: Math.Round(totalEarlySettlementPenalty, 2),
            RequiredNewLoanPrincipal: Math.Round(requiredNewPrincipal, 2),
            CurrentTotalMonthlyInstallments: Math.Round(totalCurrentMonthlyInstallments, 2),
            NewConsolidatedMonthlyInstallment: Math.Round(newMonthlyInstallment, 2),
            MonthlyCashFlowFreedUp: Math.Round(monthlyCashFlowFreedUp, 2),
            CurrentDtiPercentage: currentDti,
            NewDtiPercentage: newDti,
            TotalRemainingInterestOldLoans: Math.Round(totalRemainingInterestOldLoans, 2),
            TotalInterestNewConsolidatedLoan: Math.Round(newSchedule.TotalInterest, 2),
            NetFinancialLifetimeSavings: Math.Round(netLifetimeSavings, 2),
            IsDtiCbeCompliant: isDtiCbeCompliant
        );
    }
}
