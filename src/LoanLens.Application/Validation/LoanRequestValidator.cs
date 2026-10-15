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

        if (loan.EarlyRepaymentFeePercentage < 0 || loan.EarlyRepaymentFeePercentage > 15)
            AddError(errors, "loan.earlyRepaymentFeePercentage", isAr ? "نسبة غرامة السداد المعجل يجب أن تكون بين 0% و 15%." : "Early repayment fee percentage must be between 0% and 15%.");

        if (loan.LatePaymentPenaltyPercentage < 0 || loan.LatePaymentPenaltyPercentage > 25)
            AddError(errors, "loan.latePaymentPenaltyPercentage", isAr ? "نسبة غرامة التأخير يجب أن تكون بين 0% و 25%." : "Late payment penalty percentage must be between 0% and 25%.");

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

    public static Dictionary<string, string[]> Validate(UserProfileDto? profile, LoanInputDto? loan, MonteCarloOptionsDto? options, int iterations = 5000, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var isAr = LoanLensLanguage.IsArabic(language);

        var baseErrors = Validate(profile, loan, language);
        foreach (var (key, msgs) in baseErrors)
        {
            errors[key] = msgs.ToList();
        }

        if (iterations <= 0 || iterations > 50000)
        {
            AddError(errors, "iterations", isAr ? "عدد محاكاة مونت كارلو يجب أن يكون بين 100 و 50,000 محاكاة." : "Monte Carlo iterations must be between 100 and 50,000.");
        }

        if (options != null)
        {
            ValidateMonteCarloOptions(options, errors, isAr);
        }

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

        if (request.MonteCarloOptions != null)
        {
            ValidateMonteCarloOptions(request.MonteCarloOptions, errors, isAr);
        }

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(ScenarioComparisonRequestDto? request, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var effectiveLang = !string.IsNullOrWhiteSpace(request?.Language) ? request.Language : language;
        var isAr = LoanLensLanguage.IsArabic(effectiveLang);

        if (request is null)
        {
            errors["request"] = [isAr ? "بيانات طلب المقارنة مطلوبة." : "Comparison request payload is required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        var errorsA = Validate(request.ScenarioA, effectiveLang);
        foreach (var (key, msgs) in errorsA)
        {
            errors[$"scenarioA.{key}"] = msgs.ToList();
        }

        var errorsB = Validate(request.ScenarioB, effectiveLang);
        foreach (var (key, msgs) in errorsB)
        {
            errors[$"scenarioB.{key}"] = msgs.ToList();
        }

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(BorrowingCapacityRequestDto? request, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var effectiveLang = !string.IsNullOrWhiteSpace(request?.Language) ? request.Language : language;
        var isAr = LoanLensLanguage.IsArabic(effectiveLang);

        if (request is null)
        {
            errors["request"] = [isAr ? "بيانات الطلب مطلوبة." : "Request payload is required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        var profileErrors = Validate(request.Profile, new LoanInputDto(1000m, 12, 10m), effectiveLang);
        foreach (var (key, msgs) in profileErrors.Where(p => p.Key.StartsWith("profile", StringComparison.OrdinalIgnoreCase)))
        {
            errors[key] = msgs.ToList();
        }

        if (request.TargetInterestRatePercentage < 0 || request.TargetInterestRatePercentage > 150)
            AddError(errors, "targetInterestRatePercentage", isAr ? "نسبة الفائدة المستهدفة يجب أن تكون بين 0% و 150%." : "Target interest rate percentage must be between 0% and 150%.");

        if (request.TargetTenorMonths <= 0 || request.TargetTenorMonths > 360)
            AddError(errors, "targetTenorMonths", isAr ? "مدة القرض المستهدفة يجب أن تكون بين 1 و 360 شهراً." : "Target tenor must be between 1 and 360 months.");

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(DebtConsolidationRequestDto? request, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var effectiveLang = !string.IsNullOrWhiteSpace(request?.Language) ? request.Language : language;
        var isAr = LoanLensLanguage.IsArabic(effectiveLang);

        if (request is null)
        {
            errors["request"] = [isAr ? "بيانات طلب دمج القروض مطلوبة." : "Debt consolidation request payload is required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        var profileErrors = Validate(request.Profile, new LoanInputDto(1000m, 12, 10m), effectiveLang);
        foreach (var (key, msgs) in profileErrors.Where(p => p.Key.StartsWith("profile", StringComparison.OrdinalIgnoreCase)))
        {
            errors[key] = msgs.ToList();
        }

        if (request.ExistingDebts is null || request.ExistingDebts.Count == 0)
        {
            AddError(errors, "existingDebts", isAr ? "يجب إدخال قرض أو مديونية حالية واحدة على الأقل." : "At least one existing debt must be provided.");
        }
        else
        {
            for (int i = 0; i < request.ExistingDebts.Count; i++)
            {
                var debt = request.ExistingDebts[i];
                if (debt.RemainingPrincipal <= 0)
                    AddError(errors, $"existingDebts[{i}].remainingPrincipal", isAr ? "أصل المديونية المتبقي يجب أن يكون أكبر من صفر." : "Remaining principal must be greater than zero.");
                if (debt.MonthlyInstallment <= 0)
                    AddError(errors, $"existingDebts[{i}].monthlyInstallment", isAr ? "قيمة القسط الحالي يجب أن تكون أكبر من صفر." : "Monthly installment must be greater than zero.");
                if (debt.RemainingMonths <= 0 || debt.RemainingMonths > 360)
                    AddError(errors, $"existingDebts[{i}].remainingMonths", isAr ? "الشهور المتبقية يجب أن تكون بين 1 و 360 شهراً." : "Remaining months must be between 1 and 360.");
                if (debt.EarlySettlementFeePercentage < 0 || debt.EarlySettlementFeePercentage > 25)
                    AddError(errors, $"existingDebts[{i}].earlySettlementFeePercentage", isAr ? "نسبة غرامة السداد المعجل يجب أن تكون بين 0% و 25%." : "Early settlement fee must be between 0% and 25%.");
            }
        }

        if (request.TargetOffer is null)
        {
            AddError(errors, "targetOffer", isAr ? "بيانات عرض القرض الجديد المستهدف مطلوبة." : "Target consolidation loan offer details are required.");
        }
        else
        {
            if (request.TargetOffer.TenorMonths <= 0 || request.TargetOffer.TenorMonths > 360)
                AddError(errors, "targetOffer.tenorMonths", isAr ? "مدة القرض الجديد يجب أن تكون بين 1 و 360 شهراً." : "Target tenor must be between 1 and 360 months.");
            if (request.TargetOffer.AnnualInterestRatePercentage < 0 || request.TargetOffer.AnnualInterestRatePercentage > 150)
                AddError(errors, "targetOffer.annualInterestRatePercentage", isAr ? "نسبة الفائدة السنوية المستهدفة يجب أن تكون بين 0% و 150%." : "Annual interest rate must be between 0% and 150%.");
            if (request.TargetOffer.AdminFeePercentage < 0 || request.TargetOffer.AdminFeePercentage > 25)
                AddError(errors, "targetOffer.adminFeePercentage", isAr ? "نسبة المصاريف الإدارية يجب أن تكون بين 0% و 25%." : "Admin fee must be between 0% and 25%.");
        }

        return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public static Dictionary<string, string[]> Validate(RateSensitivityRequestDto? request, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var effectiveLang = !string.IsNullOrWhiteSpace(request?.Language) ? request.Language : language;
        var isAr = LoanLensLanguage.IsArabic(effectiveLang);

        if (request is null)
        {
            errors["request"] = [isAr ? "بيانات الطلب مطلوبة." : "Request payload is required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        return Validate(request.Loan, effectiveLang);
    }

    public static Dictionary<string, string[]> Validate(ExportExcelRequestDto? request, string language = "en")
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var effectiveLang = !string.IsNullOrWhiteSpace(request?.Language) ? request.Language : language;
        var isAr = LoanLensLanguage.IsArabic(effectiveLang);

        if (request is null)
        {
            errors["request"] = [isAr ? "بيانات الطلب مطلوبة." : "Request payload is required."];
            return errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
        }

        return Validate(request.Loan, effectiveLang);
    }

    public static void ValidateMonteCarloOptions(MonteCarloOptionsDto options, Dictionary<string, List<string>> errors, bool isAr)
    {
        if (options.Iterations <= 0 || options.Iterations > 50000)
        {
            AddError(errors, "monteCarloOptions.iterations", isAr ? "عدد محاكاة مونت كارلو يجب أن يكون بين 100 و 50,000 محاكاة." : "Monte Carlo iterations must be between 100 and 50,000.");
        }

        if (options.CustomIncomeVolatilityPercentage.HasValue && (options.CustomIncomeVolatilityPercentage.Value < 0 || options.CustomIncomeVolatilityPercentage.Value > 100))
        {
            AddError(errors, "monteCarloOptions.customIncomeVolatilityPercentage", isAr ? "نسبة تذبذب الدخل المخصصة يجب أن تكون بين 0% و 100%." : "Custom income volatility percentage must be between 0% and 100%.");
        }

        if (options.CustomEmergencyAnnualProbability.HasValue && (options.CustomEmergencyAnnualProbability.Value < 0 || options.CustomEmergencyAnnualProbability.Value > 100))
        {
            AddError(errors, "monteCarloOptions.customEmergencyAnnualProbability", isAr ? "نسبة احتمالية حدوث طارئ سنوي يجب أن تكون بين 0% و 100%." : "Custom emergency annual probability must be between 0% and 100%.");
        }

        if (options.CustomEmergencyCost.HasValue && options.CustomEmergencyCost.Value < 0)
        {
            AddError(errors, "monteCarloOptions.customEmergencyCost", isAr ? "تكلفة الطوارئ المخصصة لا يمكن أن تكون سالبة." : "Custom emergency cost cannot be negative.");
        }
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
