using LoanLens.Application.DTOs;
using LoanLens.Domain.Enums;

namespace LoanLens.Application.Validation;

public static class LoanRequestValidator
{
    public static Dictionary<string, string[]> Validate(LoanInputDto? loan)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (loan is null)
        {
            errors["loan"] = ["بيانات القرض مطلوبة."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        if (loan.Principal <= 0)
            AddError(errors, "loan.principal", "مبلغ القرض يجب أن يكون أكبر من صفر.");

        if (loan.TenorMonths <= 0)
            AddError(errors, "loan.tenorMonths", "مدة القرض يجب أن تكون أكبر من صفر (بالشهور).");
        else if (loan.TenorMonths > 360)
            AddError(errors, "loan.tenorMonths", "أقصى مدة للقرض هي 360 شهراً (30 سنة).");

        if (loan.AnnualInterestRatePercentage < 0)
            AddError(errors, "loan.annualInterestRatePercentage", "نسبة الفائدة السنوية لا يمكن أن تكون سالبة.");
        else if (loan.AnnualInterestRatePercentage > 150)
            AddError(errors, "loan.annualInterestRatePercentage", "نسبة الفائدة السنوية مرتفعة وغير واقعية (الحد الأقصى 150%).");

        if (loan.AdminFeePercentage < 0 || loan.AdminFeePercentage > 25)
            AddError(errors, "loan.adminFeePercentage", "نسبة الرسوم الإدارية يجب أن تكون بين 0% و 25%.");

        if (!Enum.IsDefined(typeof(InterestCalculationMethod), loan.CalculationMethod))
            AddError(errors, "loan.calculationMethod", "طريقة احتساب الفائدة غير صالحة (1 = Reducing, 2 = Flat).");

        if (!Enum.IsDefined(typeof(InterestRateType), loan.RateType))
            AddError(errors, "loan.rateType", "نوع الفائدة غير صالح (1 = Fixed, 2 = Variable).");

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(UserProfileDto? profile, LoanInputDto? loan)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        // Validate Loan
        var loanErrors = Validate(loan);
        foreach (var (key, msgs) in loanErrors)
        {
            errors[key] = msgs.ToList();
        }

        // Validate Profile
        if (profile is null)
        {
            AddError(errors, "profile", "بيانات الملف المالي للمستخدم مطلوبة.");
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        if (profile.MonthlyIncome < 0)
            AddError(errors, "profile.monthlyIncome", "الدخل الشهري لا يمكن أن يكون سالباً.");

        if (profile.SecondaryIncome < 0)
            AddError(errors, "profile.secondaryIncome", "الدخل الثانوي لا يمكن أن يكون سالباً.");

        if (profile.MonthlyIncome + profile.SecondaryIncome <= 0)
            AddError(errors, "profile.monthlyIncome", "إجمالي الدخل (الشهري + الثانوي) يجب أن يكون أكبر من صفر.");

        if (profile.FixedMonthlyExpenses < 0)
            AddError(errors, "profile.fixedMonthlyExpenses", "المصاريف الشهرية الثابتة لا يمكن أن تكون سالبة.");

        if (profile.CurrentMonthlyInstallments < 0)
            AddError(errors, "profile.currentMonthlyInstallments", "الأقساط الحالية لا يمكن أن تكون سالبة.");

        if (profile.Savings < 0)
            AddError(errors, "profile.savings", "المدخرات الحالية لا يمكن أن تكون سالبة.");

        if (profile.DependentsCount < 0)
            AddError(errors, "profile.dependentsCount", "عدد المعالين لا يمكن أن يكون سالباً.");

        if (profile.Age < 18 || profile.Age > 85)
            AddError(errors, "profile.age", "العمر يجب أن يكون بين 18 و 85 سنة.");

        if (!Enum.IsDefined(typeof(IncomeStability), profile.Stability))
            AddError(errors, "profile.stability", "نوع استقرار الدخل غير صالح (1 = StableEmployed, 2 = Freelance, 3 = BusinessOwner).");

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(LoanAnalysisRequestDto? request)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (request is null)
        {
            errors["request"] = ["بيانات الطلب مطلوبة."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        var baseErrors = Validate(request.Profile, request.Loan);
        foreach (var (key, msgs) in baseErrors)
        {
            errors[key] = msgs.ToList();
        }

        if (request.MonteCarloIterations <= 0 || request.MonteCarloIterations > 50000)
        {
            AddError(errors, "monteCarloIterations", "عدد محاكاة مونت كارلو يجب أن يكون بين 100 و 50,000 محاكاة.");
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
