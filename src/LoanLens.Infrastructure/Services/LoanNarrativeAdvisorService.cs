using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Localization;
using LoanLens.Domain.ValueObjects;
using LoanLens.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanLens.Infrastructure.Services;

public class LoanNarrativeAdvisorService : ILoanNarrativeAdvisor
{
    private readonly HttpClient _httpClient;
    private readonly AiAdvisorOptions _options;
    private readonly IBankRatesService _bankRatesService;
    private readonly ILogger<LoanNarrativeAdvisorService> _logger;

    public LoanNarrativeAdvisorService(
        HttpClient httpClient,
        IOptions<AiAdvisorOptions> options,
        IBankRatesService bankRatesService,
        ILogger<LoanNarrativeAdvisorService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? new AiAdvisorOptions();
        _bankRatesService = bankRatesService ?? throw new ArgumentNullException(nameof(bankRatesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoanNarrativeAdvice> GenerateAdviceAsync(
        ComprehensiveLoanReportDto report,
        string? customerName = null,
        string? customQuestion = null,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var isAr = LoanLensLanguage.IsArabic(language)
                || LoanLensLanguage.ContainsArabic(customQuestion)
                || LoanLensLanguage.ContainsArabic(customerName);

        BankRatesSummaryDto? marketRates = null;
        try
        {
            marketRates = await _bankRatesService.GetMarketRatesAsync(isAr ? "ar" : "en", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load live market benchmark for AI advisor. Proceeding with baseline.");
        }

        var geminiKey = ResolveGeminiKey();
        var groqKey = ResolveGroqKey();

        // 1. Try Primary Provider (Default: Gemini)
        if (!string.IsNullOrWhiteSpace(geminiKey))
        {
            try
            {
                var advice = await CallGeminiAsync(report, geminiKey, customerName, customQuestion, isAr, marketRates, cancellationToken);
                if (advice != null)
                {
                    _logger.LogInformation("Successfully generated narrative advice using Gemini ({Model}).", advice.ModelUsed);
                    return advice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API call failed. Attempting secondary fallback provider.");
            }
        }

        // 2. Try Secondary Provider (Groq)
        if (!string.IsNullOrWhiteSpace(groqKey))
        {
            try
            {
                var advice = await CallGroqAsync(report, groqKey, customerName, customQuestion, isAr, marketRates, cancellationToken);
                if (advice != null)
                {
                    _logger.LogInformation("Successfully generated narrative advice using Groq ({Model}).", advice.ModelUsed);
                    return advice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Groq API call failed. Falling back to deterministic rule-based engine.");
            }
        }

        // 3. Fallback to high-precision deterministic rule-based engine
        _logger.LogInformation("Generating narrative advice via high-precision rule-based engine with live market benchmark.");
        return GenerateRuleBasedAdvice(report, customerName, customQuestion, isAr, marketRates);
    }

    private string? ResolveGeminiKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.GeminiApiKey))
            return _options.GeminiApiKey.Trim();

        var env = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
               ?? Environment.GetEnvironmentVariable("LOANLENS_GEMINI_KEY");

        return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
    }

    private string? ResolveGroqKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.GroqApiKey))
            return _options.GroqApiKey.Trim();

        var env = Environment.GetEnvironmentVariable("GROQ_API_KEY")
               ?? Environment.GetEnvironmentVariable("LOANLENS_GROQ_KEY");

        return string.IsNullOrWhiteSpace(env) ? null : env.Trim();
    }

    private async Task<LoanNarrativeAdvice?> CallGeminiAsync(
        ComprehensiveLoanReportDto report,
        string apiKey,
        string? customerName,
        string? customQuestion,
        bool isAr,
        BankRatesSummaryDto? marketRates,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(_options.GeminiModelId) ? "gemini-2.5-flash" : _options.GeminiModelId;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var (systemPrompt, userPrompt) = BuildPrompts(report, customerName, customQuestion, isAr, marketRates);

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = $"{systemPrompt}\n\n{userPrompt}" }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = _options.Temperature
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var response = await _httpClient.SendAsync(request, timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            _logger.LogWarning("Gemini returned HTTP {StatusCode}: {Error}", response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var cElem) &&
                cElem.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0)
            {
                var text = parts[0].GetProperty("text").GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return ParseAdvisorJson(text, $"Gemini:{model}", isAr);
                }
            }
        }

        return null;
    }

    private async Task<LoanNarrativeAdvice?> CallGroqAsync(
        ComprehensiveLoanReportDto report,
        string apiKey,
        string? customerName,
        string? customQuestion,
        bool isAr,
        BankRatesSummaryDto? marketRates,
        CancellationToken ct)
    {
        var model = string.IsNullOrWhiteSpace(_options.GroqModelId) ? "openai/gpt-oss-120b" : _options.GroqModelId;
        var url = "https://api.groq.com/openai/v1/chat/completions";

        var (systemPrompt, userPrompt) = BuildPrompts(report, customerName, customQuestion, isAr, marketRates);

        var payload = new
        {
            model = model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" },
            temperature = _options.Temperature
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        var response = await _httpClient.SendAsync(request, timeoutCts.Token);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(timeoutCts.Token);
            _logger.LogWarning("Groq returned HTTP {StatusCode}: {Error}", response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var msg = choices[0].GetProperty("message");
            var text = msg.GetProperty("content").GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return ParseAdvisorJson(text, $"Groq:{model}", isAr);
            }
        }

        return null;
    }

    private (string SystemPrompt, string UserPrompt) BuildPrompts(
        ComprehensiveLoanReportDto report,
        string? customerName,
        string? customQuestion,
        bool isAr,
        BankRatesSummaryDto? marketRates)
    {
        string systemPrompt;
        var clientLabel = !string.IsNullOrWhiteSpace(customerName) ? customerName : (isAr ? "العميل" : "Valued Client");
        var sb = new StringBuilder();

        if (isAr)
        {
            systemPrompt =
                "أنت خبير ومستشار مالي مصرفي معتمد رفيع المستوى متخصص في السوق المصرفي المصري وضوابط البنك المركزي المصري (CBE).\n" +
                "مهمتك صياغة استشارة مالية إنسانية، واقعية، شديدة الشفافية والعمق بناءً على التقرير المالي الرقمي المقدم.\n" +
                "قواعد صارمة وإلزامية:\n" +
                "1. اللغة: يجب أن تكون الإجابة بجميع حقولها بنسبة 100% باللغة العربية الفصحى السليمة، وممنوع منعاً باتاً استخدام أو خلط أي كلمات أو مصطلحات أو حروف باللغة الإنجليزية في أي حقل.\n" +
                "2. إذا طرح العميل سؤالاً محدداً، يجب الإجابة عليه بوضوح وتفصيل ودقة مصرفية داخل الملخص وخطة العمل.\n" +
                "3. لا تقم أبداً بحساب أو تغيير أي أرقام؛ الأرقام المقدمة محسوبة بدقة برمجية قطعية، دورك هو شرح دلالتها ومخاطرها.\n" +
                "4. راعي معايير البنك المركزي المصري (حد الـ 50% DTI)، وأثر القروض متناقصة الفائدة مقارنة بالفائدة الثابتة، وغرامات السداد المعجل.\n" +
                "5. أجب دائماً بهيئة JSON صالح تماماً يحتوي حصراً على الحقول التالية:\n" +
                "{\n" +
                "  \"executiveSummary\": \"ملخص تحليلي وافي وشامل باللغة العربية من فقرتين إلى 3 فقرات يوضح القرار المالي النهائي ويجيب على تساؤل العميل بالتحديد ولماذا يناسبه أو يهدده القرض\",\n" +
                "  \"keyStrengths\": [\"نقاط القوة والأمان المالي لدى العميل باللغة العربية\"],\n" +
                "  \"vulnerabilitiesAndWarnings\": [\"نقاط الضعف والمخاطر الحرجة التي تهدد بتعثره باللغة العربية\"],\n" +
                "  \"actionPlanSteps\": [\"خطوات عملية مرتبة زمنياً ينبغي للعميل اتخاذها باللغة العربية\"],\n" +
                "  \"egyptianMarketInsight\": \"نصيحة مصرفية ذكية خاصة بالسوق المصري باللغة العربية\",\n" +
                "  \"disclaimer\": \"إخلاء مسؤولية استرشادي باللغة العربية\"\n" +
                "}";

            sb.AppendLine($"اسم العميل: {clientLabel}");
            sb.AppendLine("اللغة المطلوبة: العربية الفصحى (100% Arabic Only - No English Words)");
            if (!string.IsNullOrWhiteSpace(customQuestion))
            {
                sb.AppendLine($"سؤال العميل المحدد: {customQuestion}");
            }

            sb.AppendLine("\n--- الملف المالي للعميل ---");
            sb.AppendLine($"صافي الدخل الشهري: {report.UserProfile.MonthlyIncome:N0} جنيه مصري");
            sb.AppendLine($"الدخل الثانوي: {report.UserProfile.SecondaryIncome:N0} جنيه مصري");
            sb.AppendLine($"إجمالي الدخل الشهري: {report.UserProfile.TotalNetIncome:N0} جنيه مصري");
            sb.AppendLine($"طبيعة الاستقرار الوظيفي: {(report.UserProfile.Stability == Domain.Enums.IncomeStability.StableEmployed ? "موظف بعقد ثابت" : report.UserProfile.Stability == Domain.Enums.IncomeStability.Freelance ? "عمل حر / مهن حرة" : "صاحب عمل / نشاط تجاري")}");
            sb.AppendLine($"المصاريف الشهرية الإلزامية: {report.UserProfile.FixedMonthlyExpenses:N0} جنيه مصري");
            sb.AppendLine($"الأقساط الشهرية القائمة: {report.UserProfile.CurrentMonthlyInstallments:N0} جنيه مصري");
            sb.AppendLine($"المدخرات النقدية (وسادة الأمان): {report.UserProfile.Savings:N0} جنيه مصري");
            sb.AppendLine($"عدد المعالين: {report.UserProfile.DependentsCount}");
            sb.AppendLine($"العمر: {report.UserProfile.Age} سنة");
            sb.AppendLine($"تأمين صحي: {(report.UserProfile.HasHealthInsurance ? "مؤمن عليه طبياً" : "غير مؤمن عليه")}");

            sb.AppendLine("\n--- تفاصيل القرض المطلوب ---");
            sb.AppendLine($"أصل التمويل المطلوب: {report.LoanDetails.Principal:N0} جنيه مصري");
            sb.AppendLine($"مدة السداد: {report.LoanDetails.TenorMonths} شهراً");
            sb.AppendLine($"سعر الفائدة الاسمي: {report.LoanDetails.AnnualInterestRatePercentage}% ({(report.LoanDetails.CalculationMethod == Domain.Enums.InterestCalculationMethod.Reducing ? "متناقصة" : "ثابتة")})");
            sb.AppendLine($"معدل الفائدة الفعلي السنوي (APR): {report.AmortizationSchedule.EffectiveAnnualRatePercentage:F1}%");
            sb.AppendLine($"القسط الشهري للقرض: {report.AmortizationSchedule.MonthlyInstallment:N0} جنيه مصري");
            sb.AppendLine($"إجمالي الفوائد المحسوبة: {report.AmortizationSchedule.TotalInterest:N0} جنيه مصري");
            sb.AppendLine($"إجمالي المبلغ المسدد: {report.AmortizationSchedule.TotalPayment:N0} جنيه مصري");

            sb.AppendLine("\n--- القدرة المالية وعبء الدين ---");
            sb.AppendLine($"نسبة عبء الدين (DTI): {report.Affordability.DtiPercentage:F1}% (الحد الأقصى للبنك المركزي المصري هو 50.0%)");
            sb.AppendLine($"التوافق مع تعليمات البنك المركزي: {(report.Affordability.DtiPercentage <= 50.0m ? "متوافق ومسموح" : "مخالف ومتجاوز للحد الأقصى")}");
            sb.AppendLine($"الفائض المالي الشهري بعد سداد القسط: {report.Affordability.DisposableIncomeAfterInstallment:N0} جنيه مصري");
            sb.AppendLine($"فترة الصمود المالي (صندوق الطوارئ): {report.Affordability.EmergencyRunwayMonths:F1} شهراً");
            var safeMaxInst = Math.Max(0m, (report.Affordability.NetMonthlyIncome * 0.35m) - report.Affordability.ExistingInstallments);
            sb.AppendLine($"أقصى قسط آمن موصى به (حد الأمان 35%): {safeMaxInst:N0} جنيه مصري");

            sb.AppendLine("\n--- نتائج اختبارات الضغط والأزمات المالية ---");
            sb.AppendLine($"اجتياز {report.StressTest.ScenariosSurvived} من أصل {report.StressTest.TotalScenariosTested} سيناريوهات أزمة.");
            foreach (var sc in report.StressTest.Scenarios)
            {
                sb.AppendLine($"- {sc.ScenarioName}: ينجو={(sc.IsSurviving ? "نعم" : "تعثر")}, العجز الشهري الأقصى={sc.MaxMonthlyDeficit:N0} جنيه, التفاصيل={sc.ImpactDescription}");
            }

            sb.AppendLine("\n--- محاكاة مونت كارلو الاحتمالية (5,000 سيناريو) ---");
            sb.AppendLine($"احتمالية التعثر: {report.MonteCarlo.DefaultProbabilityPercentage:F1}%");
            sb.AppendLine($"شهر ذروة الخطر: {(report.MonteCarlo.PeakRiskMonth.HasValue ? $"الشهر #{report.MonteCarlo.PeakRiskMonth.Value}" : "لا يوجد تعثر")}");
            sb.AppendLine($"متوسط المدخرات بعد انتهاء القرض: {report.MonteCarlo.MedianEndingSavings:N0} جنيه مصري");
            sb.AppendLine($"المدخرات في أسوأ 5% من الحالات: {report.MonteCarlo.WorstCase5thPercentileSavings:N0} جنيه مصري");

            sb.AppendLine("\n--- مؤشر المخاطر والقرار الحسابي ---");
            sb.AppendLine($"درجة الخطر: {report.RiskEvaluation.TotalRiskScore}/100 ({report.RiskEvaluation.RiskLabel})");
            sb.AppendLine($"قرار النظام الحسابي: {report.Recommendation.VerdictHeadline}");
            sb.AppendLine($"التعليل المالي الأساسي: {report.Recommendation.DetailedReasoning}");

            if (marketRates != null)
            {
                sb.AppendLine("\n--- مؤشرات البنك المركزي المصري وأسعار السوق المصرفي المباشرة ---");
                sb.AppendLine($"سعر الإقراض لليلة واحدة بالمركزي المصري (سقف الكوريدور): {marketRates.CbeLendingRate}%");
                sb.AppendLine($"سعر الإيداع لليلة واحدة (قاع الكوريدور): {marketRates.CbeDepositRate}%");
                sb.AppendLine($"سعر العملية الرئيسية للبنك المركزي: {marketRates.CbeMainOperationRate}%");
                sb.AppendLine($"متوسط فائدة القروض الشخصية بالسوق المصري: {marketRates.AveragePersonalLoanRate}%");
                sb.AppendLine($"متوسط فائدة قروض السيارات: {marketRates.AverageAutoLoanRate}%");
                sb.AppendLine($"متوسط فائدة التمويل العقاري: {marketRates.AverageMortgageRate}%");
            }
        }
        else
        {
            systemPrompt =
                "You are an elite, certified retail banking financial advisor specializing in the Egyptian banking sector and Central Bank of Egypt (CBE) regulations.\n" +
                "Your objective is to craft an empathetic, deeply insightful, transparent, and actionable narrative advisory report based on the deterministic financial figures provided.\n" +
                "Strict Rules:\n" +
                "1. Language: All fields in the response must be strictly 100% in professional English without mixing any Arabic or other language words.\n" +
                "2. If the client asked a specific question, answer it directly and comprehensively within the executiveSummary and actionPlanSteps.\n" +
                "3. NEVER recalculate or alter numbers. All numbers are computed deterministically; explain their meaning, risks, and trade-offs.\n" +
                "4. Reference CBE 50% DTI guidelines, reducing vs flat rate implications, early settlement fees, and emergency runway buffers.\n" +
                "5. Respond strictly with valid JSON conforming to this schema:\n" +
                "{\n" +
                "  \"executiveSummary\": \"Comprehensive 2-3 paragraph executive summary explaining the verdict and answering the client's questions\",\n" +
                "  \"keyStrengths\": [\"Financial safety factors protecting the borrower in English\"],\n" +
                "  \"vulnerabilitiesAndWarnings\": [\"Critical risks that could trigger loan default in English\"],\n" +
                "  \"actionPlanSteps\": [\"Sequential concrete action steps the borrower should take in English\"],\n" +
                "  \"egyptianMarketInsight\": \"Specific insight regarding Egyptian banking market nuances or alternatives in English\",\n" +
                "  \"disclaimer\": \"Regulatory educational disclaimer in English\"\n" +
                "}";

            sb.AppendLine($"Client: {clientLabel}");
            sb.AppendLine("Language Requested: English (100% English Only)");
            if (!string.IsNullOrWhiteSpace(customQuestion))
            {
                sb.AppendLine($"Client Specific Question: {customQuestion}");
            }

            sb.AppendLine("\n--- USER FINANCIAL PROFILE ---");
            sb.AppendLine($"Monthly Net Income: {report.UserProfile.MonthlyIncome:N0} EGP");
            sb.AppendLine($"Secondary Income: {report.UserProfile.SecondaryIncome:N0} EGP");
            sb.AppendLine($"Total Net Income: {report.UserProfile.TotalNetIncome:N0} EGP");
            sb.AppendLine($"Employment Stability: {report.UserProfile.Stability}");
            sb.AppendLine($"Fixed Expenses: {report.UserProfile.FixedMonthlyExpenses:N0} EGP");
            sb.AppendLine($"Existing Monthly Installments: {report.UserProfile.CurrentMonthlyInstallments:N0} EGP");
            sb.AppendLine($"Current Savings (Buffer): {report.UserProfile.Savings:N0} EGP");
            sb.AppendLine($"Dependents: {report.UserProfile.DependentsCount}");
            sb.AppendLine($"Age: {report.UserProfile.Age} years");
            sb.AppendLine($"Has Health Insurance: {report.UserProfile.HasHealthInsurance}");

            sb.AppendLine("\n--- REQUESTED LOAN DETAILS ---");
            sb.AppendLine($"Principal: {report.LoanDetails.Principal:N0} EGP");
            sb.AppendLine($"Tenor: {report.LoanDetails.TenorMonths} Months");
            sb.AppendLine($"Annual Interest Rate: {report.LoanDetails.AnnualInterestRatePercentage}% ({report.LoanDetails.CalculationMethod}, {report.LoanDetails.RateType})");
            sb.AppendLine($"Effective APR: {report.AmortizationSchedule.EffectiveAnnualRatePercentage:F1}%");
            sb.AppendLine($"Monthly Installment: {report.AmortizationSchedule.MonthlyInstallment:N0} EGP");
            sb.AppendLine($"Total Interest: {report.AmortizationSchedule.TotalInterest:N0} EGP");
            sb.AppendLine($"Total Repayment: {report.AmortizationSchedule.TotalPayment:N0} EGP");

            sb.AppendLine("\n--- AFFORDABILITY & DTI ---");
            sb.AppendLine($"DTI Ratio: {report.Affordability.DtiPercentage:F1}% (CBE Regulatory Limit is 50.0%)");
            sb.AppendLine($"Is CBE Compliant: {report.Affordability.DtiPercentage <= 50.0m}");
            sb.AppendLine($"Remaining Disposable Income: {report.Affordability.DisposableIncomeAfterInstallment:N0} EGP");
            sb.AppendLine($"Emergency Runway: {report.Affordability.EmergencyRunwayMonths:F1} Months");
            var safeMaxInst = Math.Max(0m, (report.Affordability.NetMonthlyIncome * 0.35m) - report.Affordability.ExistingInstallments);
            sb.AppendLine($"Max Safe Installment (35% DTI): {safeMaxInst:N0} EGP");

            sb.AppendLine("\n--- STRESS TESTING (DETERMINISTIC CRISES) ---");
            sb.AppendLine($"Resilient in {report.StressTest.ScenariosSurvived} of {report.StressTest.TotalScenariosTested} crisis scenarios.");
            foreach (var sc in report.StressTest.Scenarios)
            {
                sb.AppendLine($"- {sc.ScenarioName}: Survives={sc.IsSurviving}, Max Monthly Deficit={sc.MaxMonthlyDeficit:N0} EGP, Description={sc.ImpactDescription}");
            }

            sb.AppendLine("\n--- MONTE CARLO STOCHASTIC SIMULATION (5,000 PATHS) ---");
            sb.AppendLine($"Default Probability: {report.MonteCarlo.DefaultProbabilityPercentage:F1}%");
            sb.AppendLine($"Peak Risk Month: Month #{report.MonteCarlo.PeakRiskMonth ?? 1}");
            sb.AppendLine($"Median Ending Savings: {report.MonteCarlo.MedianEndingSavings:N0} EGP");
            sb.AppendLine($"Worst 5th Percentile Savings: {report.MonteCarlo.WorstCase5thPercentileSavings:N0} EGP");

            sb.AppendLine("\n--- 0-100 RISK SCORE & DECISION ---");
            sb.AppendLine($"Total Risk Score: {report.RiskEvaluation.TotalRiskScore}/100 ({report.RiskEvaluation.RiskLabel})");
            sb.AppendLine($"Deterministic Verdict: {report.Recommendation.Verdict} - {report.Recommendation.VerdictHeadline}");
            sb.AppendLine($"Primary Rationale: {report.Recommendation.DetailedReasoning}");

            var bestStrat = report.Recommendation.Strategies.FirstOrDefault(s => s.NetFinancialBenefit > 0)
                         ?? report.Recommendation.Strategies.FirstOrDefault();
            if (bestStrat != null)
            {
                sb.AppendLine($"Suggested Repayment Strategy: {bestStrat.StrategyName} (Saves {bestStrat.InterestSavedComparedToStandard:N0} EGP interest, payoff in {bestStrat.TotalMonthsToPayoff} months)");
            }

            var bestAlt = report.Recommendation.TopAlternatives.FirstOrDefault(a => a.IsCheaperThanCurrent)
                       ?? report.Recommendation.TopAlternatives.FirstOrDefault();
            if (bestAlt != null)
            {
                sb.AppendLine($"Market Alternative Offer: {bestAlt.Product.ProviderName} ({bestAlt.Product.ProductName}) at {bestAlt.Product.AnnualInterestRatePercentage}% - Saves {bestAlt.CostDifferenceComparedToCurrent:N0} EGP total cost");
            }

            if (marketRates != null)
            {
                sb.AppendLine("\n--- LIVE CENTRAL BANK OF EGYPT (CBE) & MARKET BENCHMARKS ---");
                sb.AppendLine($"CBE Overnight Lending Corridor: {marketRates.CbeLendingRate}%");
                sb.AppendLine($"CBE Overnight Deposit Corridor: {marketRates.CbeDepositRate}%");
                sb.AppendLine($"CBE Main Operation Rate: {marketRates.CbeMainOperationRate}%");
                sb.AppendLine($"Market Average Personal Loan Rate: {marketRates.AveragePersonalLoanRate}%");
                sb.AppendLine($"Market Average Auto Loan Rate: {marketRates.AverageAutoLoanRate}%");
                sb.AppendLine($"Market Average Mortgage Rate: {marketRates.AverageMortgageRate}%");
            }
        }

        return (systemPrompt, sb.ToString());
    }

    private LoanNarrativeAdvice? ParseAdvisorJson(string content, string modelUsed, bool isAr)
    {
        try
        {
            var cleanJson = content.Trim();
            if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson[7..];
            }
            else if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson[3..];
            }

            if (cleanJson.EndsWith("```"))
            {
                cleanJson = cleanJson[..^3];
            }

            cleanJson = cleanJson.Trim();

            try
            {
                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    root = root[0];
                }

                if (root.ValueKind == JsonValueKind.Object)
                {
                    var summary = root.TryGetProperty("executiveSummary", out var s) ? s.GetString() ?? "" : "";
                    var insight = root.TryGetProperty("egyptianMarketInsight", out var emi) ? emi.GetString() ?? "" : "";
                    var disclaimer = root.TryGetProperty("disclaimer", out var d) ? d.GetString() ?? "" : "";

                    var strengths = ReadStringArray(root, "keyStrengths");
                    var warnings = ReadStringArray(root, "vulnerabilitiesAndWarnings");
                    var actions = ReadStringArray(root, "actionPlanSteps");

                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        return new LoanNarrativeAdvice(
                            ExecutiveSummary: summary,
                            KeyStrengths: strengths,
                            VulnerabilitiesAndWarnings: warnings,
                            ActionPlanSteps: actions,
                            EgyptianMarketInsight: insight,
                            Disclaimer: disclaimer,
                            ModelUsed: modelUsed,
                            GeneratedAtUtc: DateTime.UtcNow
                        );
                    }
                }
            }
            catch
            {
                // Resilient regex fallback below
            }

            return ExtractAdviceWithRegex(cleanJson, modelUsed, isAr);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM JSON completion: {RawContent}", content);
        }

        return null;
    }

    private static LoanNarrativeAdvice? ExtractAdviceWithRegex(string text, string modelUsed, bool isAr)
    {
        var summaryMatch = System.Text.RegularExpressions.Regex.Match(text, "\"executiveSummary\"\\s*:\\s*\"([^\"]+)\"");
        var summary = summaryMatch.Success ? summaryMatch.Groups[1].Value : "";

        if (string.IsNullOrWhiteSpace(summary))
            return null;

        var insightMatch = System.Text.RegularExpressions.Regex.Match(text, "\"egyptianMarketInsight\"\\s*:\\s*\"([^\"]+)\"");
        var insight = insightMatch.Success ? insightMatch.Groups[1].Value : "";

        var disclaimerMatch = System.Text.RegularExpressions.Regex.Match(text, "\"disclaimer\"\\s*:\\s*\"([^\"]+)\"");
        var disclaimer = disclaimerMatch.Success ? disclaimerMatch.Groups[1].Value : "";

        var strengths = ExtractArrayWithRegex(text, "keyStrengths");
        var warnings = ExtractArrayWithRegex(text, "vulnerabilitiesAndWarnings");
        var actions = ExtractArrayWithRegex(text, "actionPlanSteps");

        return new LoanNarrativeAdvice(
            ExecutiveSummary: summary.Replace("\\n", "\n").Replace("\\\"", "\""),
            KeyStrengths: strengths,
            VulnerabilitiesAndWarnings: warnings,
            ActionPlanSteps: actions,
            EgyptianMarketInsight: insight.Replace("\\n", "\n").Replace("\\\"", "\""),
            Disclaimer: disclaimer.Replace("\\n", "\n").Replace("\\\"", "\""),
            ModelUsed: modelUsed,
            GeneratedAtUtc: DateTime.UtcNow
        );
    }

    private static List<string> ExtractArrayWithRegex(string text, string propertyName)
    {
        var list = new List<string>();
        var match = System.Text.RegularExpressions.Regex.Match(text, $"\"{propertyName}\"\\s*:\\s*\\[([^\\]]*)\\]");
        if (match.Success)
        {
            var rawItems = match.Groups[1].Value;
            var itemMatches = System.Text.RegularExpressions.Regex.Matches(rawItems, "\"([^\"]+)\"");
            foreach (System.Text.RegularExpressions.Match im in itemMatches)
            {
                var val = im.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(val))
                    list.Add(val.Replace("\\n", " ").Replace("\\\"", "\""));
            }
        }
        return list;
    }

    private static List<string> ReadStringArray(JsonElement root, string propertyName)
    {
        var list = new List<string>();
        if (root.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var str = item.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                {
                    list.Add(str.Trim());
                }
            }
        }
        return list;
    }

    public LoanNarrativeAdvice GenerateRuleBasedAdvice(
        ComprehensiveLoanReportDto report,
        string? customerName,
        string? customQuestion,
        bool isAr,
        BankRatesSummaryDto? marketRates = null)
    {
        var clientName = !string.IsNullOrWhiteSpace(customerName) ? customerName : (isAr ? "العميل" : "Valued Client");
        var dti = report.Affordability.DtiPercentage;
        var installment = report.AmortizationSchedule.MonthlyInstallment;
        var income = report.UserProfile.TotalNetIncome;
        var savings = report.UserProfile.Savings;
        var runway = report.Affordability.EmergencyRunwayMonths;
        var defaultProb = report.MonteCarlo.DefaultProbabilityPercentage;
        var riskScore = report.RiskEvaluation.TotalRiskScore;
        var verdict = report.Recommendation.Verdict;
        var safeMaxInstallment = Math.Max(0m, (report.Affordability.NetMonthlyIncome * 0.35m) - report.Affordability.ExistingInstallments);

        string summary;
        var strengths = new List<string>();
        var warnings = new List<string>();
        var actions = new List<string>();
        string marketInsight;
        string disclaimer;

        if (isAr)
        {
            // Arabic Narrative Synthesis
            if (verdict == RecommendationVerdict.TakeLoan)
            {
                summary = $"بناءً على فحص مركزك المالي الشامل، يُعد هذا التمويل مناسباً وآمناً لظروفك المالية الحالية يا {clientName}. يبلغ القسط الشهري {installment:N0} ج.م وهو ما يمثل {dti:0.0}% من دخلك الصافي البالغ {income:N0} ج.م، وهي نسبة تقع بأمان ضمن النطاق الموصى به وأقل بكثير من الحد الأقصى للبنك المركزي (50%). علاوة على ذلك، أظهرت محاكاة مونت كارلو عبر 5,000 مسار عشوائي استقراراً ملحوظاً مع احتمال تعثر منخفض للغاية ({defaultProb:0.0}%).";
            }
            else if (verdict == RecommendationVerdict.AdjustTerms)
            {
                summary = $"نوصي بتعديل شروط القرض قبل اتخاذ القرار النهائي يا {clientName}. القرض في صيغته الحالية يضغط على سيولتك بنسبة عبء دين تبلغ {dti:0.0}%، ما يترك هامشاً نقدياً محدوداً للمصاريف الطارئة. تمديد مدة السداد أو تقليص مبلغ القرض سيخفض القسط الشهري إلى النطاق الآمن ({safeMaxInstallment:N0} ج.م) ويجنبك مخاطر الصدمات المالية.";
            }
            else if (verdict == RecommendationVerdict.TakeAlternative)
            {
                var alt = report.Recommendation.TopAlternatives.FirstOrDefault(a => a.IsCheaperThanCurrent);
                var provider = alt?.Product.ProviderName ?? "البنوك المنافسة";
                var saving = alt != null ? Math.Abs(alt.CostDifferenceComparedToCurrent) : 10000m;
                summary = $"توجد فرصة تمويلية أفضل بكثير في السوق المصري تحقق لك وفراً مالياً مباشراً يا {clientName}. القرض الحالي يكبدك فوائد مرتفعة، بينما يتيح لك عرض ({provider}) فائدة أقل توفر لك ما يقارب {saving:N0} ج.م من إجمالي التكلفة لنفس المبلغ والمدة تقريباً.";
            }
            else
            {
                summary = $"ننصحك بشدة بعدم المضي قدماً في أخذ هذا القرض في الوقت الراهن يا {clientName}. مؤشرات المخاطر المالية تسجل درجة حرجة ({riskScore}/100)، مع احتمال تعثر يقدر بنحو {defaultProb:0.0}% وفق محاكاة مونت كارلو. أي هزة مالية أو ظرف طبي مفاجئ سيستنزف رصيد مدخراتك ({savings:N0} ج.م) في أقل من {runway:0.1} شهر.";
            }

            // Key Strengths (Arabic)
            if (savings >= report.UserProfile.FixedMonthlyExpenses * 3)
                strengths.Add($"وجود صندوق مدخرات طوارئ حالي ({savings:N0} ج.م) يغطي نفقات المعيشة لنحو {runway:0.1} شهر.");
            if (report.UserProfile.Stability == IncomeStability.StableEmployed)
                strengths.Add("استقرار وظيفي وتدفق دخل شهري ثابت يسهل جدولة الالتزامات البنكية بانتظام.");
            if (report.UserProfile.HasHealthInsurance)
                strengths.Add("التغطية بالتأمين الصحي تحميك من صدمات مصاريف العلاج المفاجئة التي تستنزف السيولة.");
            if (dti <= 35.0m)
                strengths.Add($"نسبة عبء الدين ({dti:0.0}%) متزنة ومثالية، وتترك فائضاً نقدياً شهرياً ({report.Affordability.DisposableIncomeAfterInstallment:N0} ج.م) للاستثمار والادخار.");
            if (strengths.Count == 0)
                strengths.Add("الرغبة في تحسين التخطيط المالي والمقارنة الاستباقية قبل التعاقد البنكي.");

            // Warnings (Arabic)
            if (dti > 50.0m)
                warnings.Add($"تجاوز الحد الأقصى الإلزامي للبنك المركزي المصري (50% DTI)، حيث يلتهم القسط والأقساط السابقة {dti:0.0}% من راتبك، وهو ما يمنع البنوك من الموافقة قانوناً.");
            else if (dti > 40.0m)
                warnings.Add($"ارتفاع نسبة عبء الدين ({dti:0.0}%) لتلامس المنطقة الخطرة وتقلل من سيولتك النقدية الحرة.");

            if (runway < 2.0m)
                warnings.Add($"هشاشة صندوق الطوارئ: رصيد الأمان الحالي يكفي فقط {runway:0.1} شهر في حال انقطاع الدخل أو حدوث أزمة مفاجئة.");

            if (defaultProb > 15.0m)
                warnings.Add($"احتمال التعثر المستقبلي مرتفع نسبياً ({defaultProb:0.0}%) مع تركيز ذروة المخاطر في الشهر رقم #{report.MonteCarlo.PeakRiskMonth ?? 1}.");

            if (report.LoanDetails.RateType == InterestRateType.Variable)
                warnings.Add("القرض ذو فائدة متغيرة مرتبطة بسعر الكوريدور؛ أي رفع مستقبلي للفائدة من البنك المركزي سيرفع القسط الشهري تلقائياً.");

            // Action Plan (Arabic)
            if (savings < report.UserProfile.FixedMonthlyExpenses * 3)
                actions.Add($"الخطوة 1: ادخار وتخصيص مبلغ إضافي قدره {Math.Max(0m, (report.UserProfile.FixedMonthlyExpenses * 3) - savings):N0} ج.م كصندوق طوارئ عازل قبل سحب التمويل.");
            var strat = report.Recommendation.Strategies.FirstOrDefault(s => s.NetFinancialBenefit > 0);
            if (strat != null)
                actions.Add($"الخطوة 2: تطبيق استراتيجية ({strat.StrategyName}) لتوفير ما يقارب {strat.InterestSavedComparedToStandard:N0} ج.م من إجمالي الفوائد وتقليص مدة القرض.");
            actions.Add("الخطوة 3: التفاوض مع البنك على إلغاء أو تخفيض المصاريف الإدارية وضمان عدم احتساب فائدة تأخير مركبة.");
            actions.Add("الخطوة 4: الحفاظ التام على سجل إيجابي في شركة الاستعلام الائتماني (I-Score) بسداد الأقساط قبل موعد استحقاقها بـ 48 ساعة.");

            // Market Insight (Arabic)
            if (report.LoanDetails.CalculationMethod == InterestCalculationMethod.Flat)
            {
                marketInsight = $"تنبيه هام بشأن الفائدة الثابتة (Flat Rate): يتم الترويج للقرض بفائدة {report.LoanDetails.AnnualInterestRatePercentage}% ثابتة، لكن الفائدة الفعلية الحقيقية (Effective APR) تصل إلى {report.AmortizationSchedule.EffectiveAnnualRatePercentage:0.0}% متناقصة. يُفضل دائماً طلب عروض التمويل بفايدة متناقصة من البنوك الحكومية أو التجارية الكبرى.";
            }
            else
            {
                marketInsight = "نصيحة مصرفية: يتيح البنك المركزي المصري سداد القروض معجلاً برسوم سداد معجل لا تتجاوز عادة 3-5% من الرصيد المتبقي. استغلال المكافآت السنوية أو أي فائض دخل في السداد الجزئي المعجل يوفر عليك أضعاف غرامة السداد كفوائد مهدرة.";
            }

            if (marketRates != null)
            {
                marketInsight += $" مع العلم أن سعر الإقراض لليلة واحدة بالبنك المركزي حالياً هو {marketRates.CbeLendingRate}% ومتوسط فائدة القروض الشخصية بالسوق المصري {marketRates.AveragePersonalLoanRate}%.";
            }

            disclaimer = "إخلاء مسؤولية: هذا التقرير هو استشارة استرشادية مبنية على نماذج محاكاة مالية وافتراضات إحصائية لدعم اتخاذ القرار، ولا يُعد وثيقة موافقة بنكية ملزمة أو استشارة استثمارية تخضع لأحكام القانون المالي.";
        }
        else
        {
            // English Narrative Synthesis
            if (verdict == RecommendationVerdict.TakeLoan)
            {
                summary = $"Based on a comprehensive diagnostic of your financial position, this financing structure is solid and well-aligned with your debt capacity, {clientName}. Your monthly installment of {installment:N0} EGP constitutes {dti:0.0}% of your net disposable income ({income:N0} EGP), well below the Central Bank of Egypt's mandatory 50% DTI ceiling. Furthermore, our 5,000-path Monte Carlo stress simulation demonstrates high resilience with an exceptionally low default risk ({defaultProb:0.0}%).";
            }
            else if (verdict == RecommendationVerdict.AdjustTerms)
            {
                summary = $"We recommend renegotiating loan terms prior to execution, {clientName}. At a {dti:0.0}% DTI, this installment restricts your discretionary liquidity, leaving insufficient headroom for unexpected expenses. Extending the tenor or lowering the borrowing amount will adjust the installment into your safe zone ({safeMaxInstallment:N0} EGP) and mitigate severe stress scenarios.";
            }
            else if (verdict == RecommendationVerdict.TakeAlternative)
            {
                var alt = report.Recommendation.TopAlternatives.FirstOrDefault(a => a.IsCheaperThanCurrent);
                var provider = alt?.Product.ProviderName ?? "competing tier-1 banks";
                var saving = alt != null ? Math.Abs(alt.CostDifferenceComparedToCurrent) : 10000m;
                summary = $"A substantially superior financing opportunity exists in the Egyptian retail market, {clientName}. The selected product carries elevated interest charges, whereas ({provider}) offers competitive reducing rates that save approximately {saving:N0} EGP in cumulative cost over the same term.";
            }
            else
            {
                summary = $"We strongly advise against proceeding with this loan under current conditions, {clientName}. Key financial stress indicators place your profile at critical risk (Score {riskScore}/100), with a {defaultProb:0.0}% estimated default probability across life shocks. Any income interruption or medical emergency will exhaust your liquid savings ({savings:N0} EGP) in less than {runway:0.1} months.";
            }

            // Key Strengths (English)
            if (savings >= report.UserProfile.FixedMonthlyExpenses * 3)
                strengths.Add($"Liquid emergency savings buffer of {savings:N0} EGP provides {runway:0.1} months of basic expense runway.");
            if (report.UserProfile.Stability == IncomeStability.StableEmployed)
                strengths.Add("Predictable and stable salaried employment simplifies scheduled installment budgeting.");
            if (report.UserProfile.HasHealthInsurance)
                strengths.Add("Active health insurance coverage shields against catastrophic medical emergency costs.");
            if (dti <= 35.0m)
                strengths.Add($"Conservative DTI ratio of {dti:0.0}% leaves a healthy disposable monthly surplus ({report.Affordability.DisposableIncomeAfterInstallment:N0} EGP).");
            if (strengths.Count == 0)
                strengths.Add("Proactive willingness to conduct objective financial analysis before contractual commitment.");

            // Warnings (English)
            if (dti > 50.0m)
                warnings.Add($"Breaches the mandatory Central Bank of Egypt (CBE) 50% DTI ceiling ({dti:0.0}% total debt obligation), disqualifying bank approval by regulation.");
            else if (dti > 40.0m)
                warnings.Add($"Elevated DTI ({dti:0.0}%) approaches the regulatory threshold, tightening free monthly cash flow.");

            if (runway < 2.0m)
                warnings.Add($"Fragile emergency buffer: Existing liquid savings sustain only {runway:0.1} months under income cessation.");

            if (defaultProb > 15.0m)
                warnings.Add($"Elevated stochastic default probability ({defaultProb:0.0}%) with peak vulnerability concentrated at Month #{report.MonteCarlo.PeakRiskMonth ?? 1}.");

            if (report.LoanDetails.RateType == InterestRateType.Variable)
                warnings.Add("Variable interest rate linked to CBE Corridor benchmark; future rate hikes will automatically increase installment obligations.");

            // Action Plan (English)
            if (savings < report.UserProfile.FixedMonthlyExpenses * 3)
                actions.Add($"Step 1: Accumulate an additional {Math.Max(0m, (report.UserProfile.FixedMonthlyExpenses * 3) - savings):N0} EGP into an emergency reserve fund prior to loan disbursement.");
            var strat = report.Recommendation.Strategies.FirstOrDefault(s => s.NetFinancialBenefit > 0);
            if (strat != null)
                actions.Add($"Step 2: Implement the '{strat.StrategyName}' strategy to save approximately {strat.InterestSavedComparedToStandard:N0} EGP in interest and cut {strat.TotalMonthsToPayoff} months.");
            actions.Add("Step 3: Negotiate upfront administrative fees and confirm that late penalties are simple rather than compounding.");
            actions.Add("Step 4: Maintain flawless I-Score bureau credit history by setting up automated auto-debit payments 48 hours before due dates.");

            // Market Insight (English)
            if (report.LoanDetails.CalculationMethod == InterestCalculationMethod.Flat)
            {
                marketInsight = $"Flat Rate Warning: Marketed at a seemingly low {report.LoanDetails.AnnualInterestRatePercentage}% Flat rate, the actual Effective APR reaches {report.AmortizationSchedule.EffectiveAnnualRatePercentage:0.0}% Reducing. Commercial bank reducing rate offerings generally prove significantly cheaper over medium terms.";
            }
            else
            {
                marketInsight = "Egyptian Market Tip: CBE regulations cap early loan settlement fees at 3-5% of remaining principal. Directing annual corporate bonuses or surplus cash toward early principal repayments generates substantial net interest savings.";
            }

            if (marketRates != null)
            {
                marketInsight += $" Note that the current CBE Overnight Lending Corridor is {marketRates.CbeLendingRate}% and average market personal loan rate is {marketRates.AveragePersonalLoanRate}%.";
            }

            disclaimer = "Disclaimer: This advisory report is based on deterministic financial simulation models for educational and decision-support purposes. It does not constitute a legally binding banking approval or formal credit commitment.";
        }

        return new LoanNarrativeAdvice(
            ExecutiveSummary: summary,
            KeyStrengths: strengths,
            VulnerabilitiesAndWarnings: warnings,
            ActionPlanSteps: actions,
            EgyptianMarketInsight: marketInsight,
            Disclaimer: disclaimer,
            ModelUsed: "LoanLens-RuleBased-ExpertEngine",
            GeneratedAtUtc: DateTime.UtcNow
        );
    }
}
