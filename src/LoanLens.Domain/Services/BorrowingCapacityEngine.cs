using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public class BorrowingCapacityEngine : IBorrowingCapacityEngine
{
    public BorrowingCapacityResult CalculateCapacity(
        FinancialProfile profile,
        decimal targetInterestRate,
        int targetTenorMonths,
        InterestCalculationMethod calculationMethod = InterestCalculationMethod.Reducing,
        string language = "en")
    {
        ArgumentNullException.ThrowIfNull(profile);

        var isAr = LoanLensLanguage.IsArabic(language);
        var totalIncome = profile.TotalNetIncome;

        // Cap tenor by retirement age (60 for employees, 65 for business owners)
        int maxRetirementAge = profile.Stability == IncomeStability.BusinessOwner ? 65 : 60;
        int maxMonthsUntilRetirement = Math.Max(12, (maxRetirementAge - profile.Age) * 12);
        int effectiveTenor = Math.Min(targetTenorMonths, maxMonthsUntilRetirement);

        // CBE 50% legal ceiling vs safe recommended ceiling (35% if insured, 30% if uninsured to reserve emergency medical buffer)
        decimal safeDtiThreshold = profile.HasHealthInsurance ? 0.35m : 0.30m;
        decimal legalMaxInstallmentAllowed = Math.Max(0m, (totalIncome * 0.50m) - profile.CurrentMonthlyInstallments);
        decimal safeInstallmentAllowed = Math.Max(0m, (totalIncome * safeDtiThreshold) - profile.CurrentMonthlyInstallments);

        decimal legalMaxPrincipal = CalculatePrincipalFromInstallment(legalMaxInstallmentAllowed, targetInterestRate, effectiveTenor, calculationMethod);
        decimal safePrincipal = CalculatePrincipalFromInstallment(safeInstallmentAllowed, targetInterestRate, effectiveTenor, calculationMethod);

        decimal currentDti = totalIncome > 0 ? Math.Round((profile.CurrentMonthlyInstallments / totalIncome) * 100m, 1) : 0m;
        decimal legalMaxDti = totalIncome > 0 ? Math.Round(((profile.CurrentMonthlyInstallments + legalMaxInstallmentAllowed) / totalIncome) * 100m, 1) : 0m;
        decimal safeDti = totalIncome > 0 ? Math.Round(((profile.CurrentMonthlyInstallments + safeInstallmentAllowed) / totalIncome) * 100m, 1) : 0m;

        decimal remainingAtMax = totalIncome - profile.FixedMonthlyExpenses - profile.CurrentMonthlyInstallments - legalMaxInstallmentAllowed;
        decimal remainingAtSafe = totalIncome - profile.FixedMonthlyExpenses - profile.CurrentMonthlyInstallments - safeInstallmentAllowed;

        string advice = isAr
            ? $"الحد الأقصى القانوني للاقتراض وفق ضوابط البنك المركزي (50% DTI) هو {legalMaxPrincipal:N0} ج.م بقسط شهري {legalMaxInstallmentAllowed:N0} ج.م. بينما الحد الآمن الموصى به مالياً للحفاظ على سيولة الطوارئ ({(int)(safeDtiThreshold * 100)}% DTI{(profile.HasHealthInsurance ? "" : " - خُفّض إلى 30% لغياب التأمين الصحي كعازل طوارئ")}) هو {safePrincipal:N0} ج.م بقسط شهري {safeInstallmentAllowed:N0} ج.م."
            : $"According to CBE regulations (50% DTI), your legal borrowing ceiling is {legalMaxPrincipal:N0} EGP (monthly installment {legalMaxInstallmentAllowed:N0} EGP). Your recommended safe borrowing ceiling ({(int)(safeDtiThreshold * 100)}% DTI{(profile.HasHealthInsurance ? "" : " - adjusted down to 30% due to lack of health insurance")}) to preserve emergency buffer is {safePrincipal:N0} EGP (monthly installment {safeInstallmentAllowed:N0} EGP).";

        if (effectiveTenor < targetTenorMonths)
        {
            var retirementNote = isAr
                ? $" تم تقليص مدة التمويل إلى {effectiveTenor} شهراً بدلاً من {targetTenorMonths} شهراً لمراعاة سن التقاعد القانوني ({maxRetirementAge} سنة) وفقاً لضوابط البنوك المصرية."
                : $" Tenor was capped to {effectiveTenor} months instead of {targetTenorMonths} months to align with Egyptian banking retirement rules ({maxRetirementAge} years).";
            advice += " " + retirementNote;
        }

        return new BorrowingCapacityResult(
            LegalMaxPrincipal: Math.Round(legalMaxPrincipal, 2),
            SafeRecommendedPrincipal: Math.Round(safePrincipal, 2),
            LegalMaxMonthlyInstallment: Math.Round(legalMaxInstallmentAllowed, 2),
            SafeMonthlyInstallment: Math.Round(safeInstallmentAllowed, 2),
            TenorMonthsUsed: effectiveTenor,
            AnnualInterestRatePercentage: targetInterestRate,
            CurrentDtiPercentage: currentDti,
            LegalMaxDtiPercentage: legalMaxDti,
            SafeDtiPercentage: safeDti,
            RemainingDisposableIncomeAtMax: Math.Round(remainingAtMax, 2),
            RemainingDisposableIncomeAtSafe: Math.Round(remainingAtSafe, 2),
            AdviceExplanation: advice
        );
    }

    private static decimal CalculatePrincipalFromInstallment(
        decimal installment, 
        decimal annualRatePercentage, 
        int tenorMonths, 
        InterestCalculationMethod method)
    {
        if (installment <= 0 || tenorMonths <= 0) return 0m;

        if (annualRatePercentage <= 0)
        {
            return installment * tenorMonths;
        }

        if (method == InterestCalculationMethod.Flat)
        {
            var factor = (1m / tenorMonths) + ((annualRatePercentage / 100m) / 12m);
            return factor > 0 ? installment / factor : 0m;
        }

        // Reducing rate formula: P = PMT * (1 - (1 + r)^(-n)) / r
        double r = (double)(annualRatePercentage / 100m / 12m);
        double n = tenorMonths;
        double discountFactor = (1.0 - Math.Pow(1.0 + r, -n)) / r;

        return installment * (decimal)discountFactor;
    }
}
