using LoanLens.Application.DTOs;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;

namespace LoanLens.Application.Validation;

public static class LoanRequestValidator
{
    public static Dictionary<string, string[]> Validate(LoanInputDto? loan, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var isAr = LoanLensLanguage.IsArabic(language);

        if (loan is null)
        {
            errors["loan"] = [isAr ? "بيانات القرض مطلوبة." : "Loan details are required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        if (loan.Principal <= 0)
            AddError(errors, "loan.principal", isAr ? "مبلغ القرض يجب أن يكون أكبر من صفر." : "Loan principal must be greater than zero.");

        if (loan.TenorMonths <= 0)
            AddError(errors, "loan.tenorMonths", isAr ? "مدة القرض يجب أن تكون أكبر من صفر (بالشهور)." : "Loan tenor must be greater than zero (in months).");
        else if (loan.TenorMonths > 360)
            AddError(errors, "loan.tenorMonths", isAr ? "أقصى مدة للقرض هي 360 شهراً (30 سنة)." : "Maximum loan tenor is 360 months (30 years).");

        if (loan.AnnualInterestRatePercentage < 0)
            AddError(errors, "loan.annualInterestRatePercentage", isAr ? "نسبة الفائدة السنوية لا يمكن أن تكون سالبة." : "Annual interest rate cannot be negative.");
        else if (loan.AnnualInterestRatePercentage > 150)
            AddError(errors, "loan.annualInterestRatePercentage", isAr ? "نسبة الفائدة السنوية مرتفعة وغير واقعية (الحد الأقصى 150%)." : "Annual interest rate exceeds realistic threshold (maximum is 150%).");

        if (loan.AdminFeePercentage < 0 || loan.AdminFeePercentage > 25)
            AddError(errors, "loan.adminFeePercentage", isAr ? "نسبة الرسوم الإدارية يجب أن تكون بين 0% و 25%." : "Admin fee percentage must be between 0% and 25%.");

        if (!Enum.IsDefined(typeof(InterestCalculationMethod), loan.CalculationMethod))
            AddError(errors, "loan.calculationMethod", isAr ? "طريقة احتساب الفائدة غير صالحة (1 = Reducing, 2 = Flat)." : "Invalid interest calculation method (1 = Reducing, 2 = Flat).");

        if (!Enum.IsDefined(typeof(InterestRateType), loan.RateType))
            AddError(errors, "loan.rateType", isAr ? "نوع الفائدة غير صالح (1 = Fixed, 2 = Variable)." : "Invalid interest rate type (1 = Fixed, 2 = Variable).");

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(UserProfileDto? profile, LoanInputDto? loan, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var isAr = LoanLensLanguage.IsArabic(language);

        // Validate Loan
        var loanErrors = Validate(loan, language);
        foreach (var (key, msgs) in loanErrors)
        {
            errors[key] = msgs.ToList();
        }

        // Validate Profile
        if (profile is null)
        {
            AddError(errors, "profile", isAr ? "بيانات الملف المالي للمستخدم مطلوبة." : "User financial profile is required.");
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        if (profile.MonthlyIncome < 0)
            AddError(errors, "profile.monthlyIncome", isAr ? "الدخل الشهري لا يمكن أن يكون سالباً." : "Monthly income cannot be negative.");

        if (profile.SecondaryIncome < 0)
            AddError(errors, "profile.secondaryIncome", isAr ? "الدخل الثانوي لا يمكن أن يكون سالباً." : "Secondary income cannot be negative.");

        if (profile.MonthlyIncome + profile.SecondaryIncome <= 0)
            AddError(errors, "profile.monthlyIncome", isAr ? "إجمالي الدخل (الشهري + الثانوي) يجب أن يكون أكبر من صفر." : "Total income (monthly + secondary) must be greater than zero.");

        if (profile.FixedMonthlyExpenses < 0)
            AddError(errors, "profile.fixedMonthlyExpenses", isAr ? "المصاريف الشهرية الثابتة لا يمكن أن تكون سالبة." : "Fixed monthly expenses cannot be negative.");

        if (profile.CurrentMonthlyInstallments < 0)
            AddError(errors, "profile.currentMonthlyInstallments", isAr ? "الأقساط الحالية لا يمكن أن تكون سالبة." : "Current monthly installments cannot be negative.");

        if (profile.Savings < 0)
            AddError(errors, "profile.savings", isAr ? "المدخرات الحالية لا يمكن أن تكون سالبة." : "Current savings cannot be negative.");

        if (profile.DependentsCount < 0)
            AddError(errors, "profile.dependentsCount", isAr ? "عدد المعالين لا يمكن أن يكون سالباً." : "Dependents count cannot be negative.");

        if (profile.Age < 18 || profile.Age > 85)
            AddError(errors, "profile.age", isAr ? "العمر يجب أن يكون بين 18 و 85 سنة." : "Age must be between 18 and 85 years.");

        if (!Enum.IsDefined(typeof(IncomeStability), profile.Stability))
            AddError(errors, "profile.stability", isAr ? "نوع استقرار الدخل غير صالح (1 = StableEmployed, 2 = Freelance, 3 = BusinessOwner)." : "Invalid income stability type (1 = StableEmployed, 2 = Freelance, 3 = BusinessOwner).");

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(LoanAnalysisRequestDto? request, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var effectiveLang = !string.IsNullOrWhiteSpace(request?.Language) ? request.Language : language;
        var isAr = LoanLensLanguage.IsArabic(effectiveLang);

        if (request is null)
        {
            errors["request"] = [isAr ? "بيانات الطلب مطلوبة." : "Request payload is required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        var baseErrors = Validate(request.Profile, request.Loan, effectiveLang);
        foreach (var (key, msgs) in baseErrors)
        {
            errors[key] = msgs.ToList();
        }

        if (request.MonteCarloIterations <= 0 || request.MonteCarloIterations > 50000)
        {
            AddError(errors, "monteCarloIterations", isAr ? "عدد محاكاة مونت كارلو يجب أن يكون بين 100 و 50,000 محاكاة." : "Monte Carlo iterations must be between 100 and 50,000.");
        }

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    private static void AddError(Dictionary<string, List<string>> dict, string key, string message)
    {
        if (!dict.TryGetValue(key, out var list))
        {
            list = [];
            dict[key] = list;
        }
        list.Add(message);
    }
}
