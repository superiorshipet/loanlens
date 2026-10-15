using System.Text.Json;
using System.Text.Json.Serialization;
using LoanLens.Application;
using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Application.Services;
using LoanLens.Application.Validation;
using LoanLens.Domain.Localization;
using LoanLens.Infrastructure;
using LoanLens.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization: allow reading numbers from strings seamlessly
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
});

// Register Clean Architecture Application & Infrastructure Services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Enable CORS for local testing, frontend, and Postman
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure OpenAPI specification with clean, realistic default examples for Postman
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = [new() { Url = "http://localhost:5257" }];
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var path = context.Description.RelativePath?.TrimStart('/');
        if (operation.RequestBody?.Content != null && operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            var exampleJson = path switch
            {
                "api/loans/calculate" => """
                {
                  "principal": 100000,
                  "tenorMonths": 24,
                  "annualInterestRatePercentage": 22.0,
                  "calculationMethod": 1,
                  "rateType": 1,
                  "adminFeePercentage": 1.5
                }
                """,
                "api/loans/affordability" or "api/loans/stress-test" => """
                {
                  "profile": {
                    "monthlyIncome": 25000,
                    "age": 32,
                    "stability": 1,
                    "fixedMonthlyExpenses": 9000,
                    "currentMonthlyInstallments": 1500,
                    "savings": 40000,
                    "dependentsCount": 1,
                    "hasHealthInsurance": true
                  },
                  "loan": {
                    "principal": 100000,
                    "tenorMonths": 36,
                    "annualInterestRatePercentage": 22.0,
                    "calculationMethod": 1,
                    "rateType": 1,
                    "adminFeePercentage": 1.5
                  }
                }
                """,
                "api/loans/monte-carlo" => """
                {
                  "profile": {
                    "monthlyIncome": 25000,
                    "age": 32,
                    "stability": 1,
                    "fixedMonthlyExpenses": 9000,
                    "currentMonthlyInstallments": 1500,
                    "savings": 40000,
                    "dependentsCount": 1,
                    "hasHealthInsurance": true
                  },
                  "loan": {
                    "principal": 100000,
                    "tenorMonths": 36,
                    "annualInterestRatePercentage": 22.0,
                    "calculationMethod": 1,
                    "rateType": 1,
                    "adminFeePercentage": 1.5
                  },
                  "iterations": 5000
                }
                """,
                "api/loans/full-analysis" => """
                {
                  "customerName": "أحمد علي",
                  "profile": {
                    "monthlyIncome": 30000,
                    "age": 32,
                    "stability": 1,
                    "fixedMonthlyExpenses": 10000,
                    "currentMonthlyInstallments": 2000,
                    "savings": 50000,
                    "dependentsCount": 1,
                    "hasHealthInsurance": true
                  },
                  "loan": {
                    "principal": 150000,
                    "tenorMonths": 36,
                    "annualInterestRatePercentage": 22.0,
                    "calculationMethod": 1,
                    "rateType": 1,
                    "adminFeePercentage": 1.5
                  },
                  "includeAiNarrative": false
                }
                """,
                "api/loans/export-pdf" or "api/loans/save-scenario" => """
                {
                  "customerName": "أحمد علي",
                  "profile": {
                    "monthlyIncome": 30000,
                    "age": 32,
                    "stability": 1,
                    "fixedMonthlyExpenses": 10000,
                    "currentMonthlyInstallments": 2000,
                    "savings": 50000,
                    "dependentsCount": 1,
                    "hasHealthInsurance": true
                  },
                  "loan": {
                    "principal": 150000,
                    "tenorMonths": 36,
                    "annualInterestRatePercentage": 22.0,
                    "calculationMethod": 1,
                    "rateType": 1,
                    "adminFeePercentage": 1.5
                  }
                }
                """,
                "api/loans/compare" => """
                {
                  "scenarioAName": "البنك الأهلي المصري (36 شهر بفائدة 22%)",
                  "scenarioBName": "بنك مصر (24 شهر بفائدة 21%)",
                  "scenarioA": {
                    "profile": {
                      "monthlyIncome": 30000,
                      "age": 32,
                      "stability": 1,
                      "fixedMonthlyExpenses": 10000,
                      "currentMonthlyInstallments": 2000,
                      "savings": 50000,
                      "dependentsCount": 1,
                      "hasHealthInsurance": true
                    },
                    "loan": {
                      "principal": 120000,
                      "tenorMonths": 36,
                      "annualInterestRatePercentage": 22.0,
                      "calculationMethod": 1,
                      "rateType": 1,
                      "adminFeePercentage": 1.5
                    }
                  },
                  "scenarioB": {
                    "profile": {
                      "monthlyIncome": 30000,
                      "age": 32,
                      "stability": 1,
                      "fixedMonthlyExpenses": 10000,
                      "currentMonthlyInstallments": 2000,
                      "savings": 50000,
                      "dependentsCount": 1,
                      "hasHealthInsurance": true
                    },
                    "loan": {
                      "principal": 120000,
                      "tenorMonths": 24,
                      "annualInterestRatePercentage": 21.0,
                      "calculationMethod": 1,
                      "rateType": 1,
                      "adminFeePercentage": 1.5
                    }
                  }
                }
                """,
                "api/loans/borrowing-capacity" => """
                {
                  "targetInterestRatePercentage": 22.0,
                  "targetTenorMonths": 60,
                  "calculationMethod": 1,
                  "profile": {
                    "monthlyIncome": 35000,
                    "age": 34,
                    "stability": 1,
                    "fixedMonthlyExpenses": 12000,
                    "currentMonthlyInstallments": 3000,
                    "savings": 50000,
                    "dependentsCount": 1,
                    "hasHealthInsurance": true
                  }
                }
                """,
                "api/loans/debt-consolidation" => """
                {
                  "profile": {
                    "monthlyIncome": 35000,
                    "age": 35,
                    "stability": 1,
                    "fixedMonthlyExpenses": 12000,
                    "currentMonthlyInstallments": 11500,
                    "savings": 40000,
                    "dependentsCount": 1,
                    "hasHealthInsurance": true
                  },
                  "existingDebts": [
                    {
                      "debtName": "فيزا مشتريات CIB",
                      "remainingPrincipal": 50000,
                      "monthlyInstallment": 4500,
                      "remainingMonths": 24,
                      "earlySettlementFeePercentage": 0.0
                    },
                    {
                      "debtName": "قرض سيارة قديم",
                      "remainingPrincipal": 90000,
                      "monthlyInstallment": 7000,
                      "remainingMonths": 18,
                      "earlySettlementFeePercentage": 3.0
                    }
                  ],
                  "targetOffer": {
                    "providerName": "البنك الأهلي المصري (برنامج شراء المديونية)",
                    "annualInterestRatePercentage": 21.0,
                    "tenorMonths": 60,
                    "adminFeePercentage": 1.0,
                    "calculationMethod": 1
                  }
                }
                """,
                "api/loans/rate-sensitivity" => """
                {
                  "loan": {
                    "principal": 250000,
                    "tenorMonths": 48,
                    "annualInterestRatePercentage": 22.5,
                    "calculationMethod": 1,
                    "rateType": 2,
                    "adminFeePercentage": 1.5
                  }
                }
                """,
                "api/loans/export-excel" => """
                {
                  "customerName": "أحمد علي",
                  "loan": {
                    "principal": 150000,
                    "tenorMonths": 36,
                    "annualInterestRatePercentage": 22.0,
                    "calculationMethod": 1,
                    "rateType": 1,
                    "adminFeePercentage": 1.5
                  }
                }
                """,
                "api/loans/ai-advice" => """
                {
                  "analysisRequest": {
                    "customerName": "أحمد علي",
                    "profile": {
                      "monthlyIncome": 30000,
                      "age": 32,
                      "stability": 1,
                      "fixedMonthlyExpenses": 10000,
                      "currentMonthlyInstallments": 2000,
                      "savings": 50000,
                      "dependentsCount": 1,
                      "hasHealthInsurance": true
                    },
                    "loan": {
                      "principal": 150000,
                      "tenorMonths": 36,
                      "annualInterestRatePercentage": 22.0,
                      "calculationMethod": 1,
                      "rateType": 1,
                      "adminFeePercentage": 1.5
                    }
                  },
                  "customQuestion": "هل تنصحني بزيادة مدة السداد لـ 4 سنين لتقليل القسط الشهري، ولا الفايدة الزيادة مش مستاهلة؟"
                }
                """,
                _ => null
            };

            if (exampleJson != null)
            {
                mediaType.Example = System.Text.Json.Nodes.JsonNode.Parse(exampleJson);
            }
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Global Exception Handling middleware (RFC 7807 ProblemDetails)
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.ContentType = "application/problem+json";

        var isClientBadRequest = ex is BadHttpRequestException 
                               or JsonException 
                               or OverflowException 
                               or ArgumentException 
                               or FormatException
                               || ex.InnerException is JsonException 
                               or OverflowException 
                               or FormatException;

        var statusCode = isClientBadRequest ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        if (ex is BadHttpRequestException badHttp)
        {
            statusCode = badHttp.StatusCode;
        }

        context.Response.StatusCode = statusCode;

        var isAr = ResolveLanguage(context) == LoanLensLanguage.Arabic;
        string title;
        string detail;

        if (statusCode == StatusCodes.Status400BadRequest)
        {
            if (ex is BadHttpRequestException or JsonException || ex.InnerException is JsonException or OverflowException)
            {
                title = isAr ? "صيغة البيانات أو كود الـ JSON غير صالح" : "Malformed JSON or invalid request payload";
                detail = isAr 
                    ? "تعذر قراءة جسم الطلب (JSON). يرجى التأكد من صحة بناء الـ JSON ونوع الحقول (أرقام وليس نصوص أو قيم تفيض عن سعة المتغير)."
                    : $"Failed to parse request body as JSON: {ex.Message}";
            }
            else
            {
                title = isAr ? "بيانات المدخلات غير صحيحة" : "Invalid request parameters";
                detail = ex.Message;
            }
        }
        else
        {
            title = isAr ? "خطأ غير متوقع في الخادم" : "An unexpected server error occurred";
            detail = ex.Message;
        }

        var problem = new
        {
            type = statusCode == 400 
                ? "https://tools.ietf.org/html/rfc7231#section-6.5.1" 
                : "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title,
            status = statusCode,
            detail,
            instance = context.Request.Path.Value
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
});

// URL path normalization: safely handle double slashes from Postman baseUrl concatenation (e.g. //api/...)
app.Use((context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && path.Contains("//"))
    {
        while (path.Contains("//"))
        {
            path = path.Replace("//", "/");
        }
        context.Request.Path = path;
    }
    return next();
});

app.UseCors();

// OpenAPI spec JSON endpoint (Postman direct import URL: http://localhost:5257/openapi/v1.json)
app.MapOpenApi();

// Scalar interactive documentation UI (http://localhost:5257/scalar/v1)
app.MapScalarApiReference();

// Quick root redirect to API documentation
app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

var loanGroup = app.MapGroup("/api/loans")
                   .WithTags("Loans Financial Advisor Engine");

// 1. Calculate Amortization Schedule
loanGroup.MapPost("/calculate", (LoanInputDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات القرض غير صالحة" : "Invalid loan parameters");

    var schedule = service.CalculateSchedule(request);
    return Results.Ok(schedule);
})
.WithName("CalculateAmortization")
.WithSummary("Calculate Amortization Schedule and APR conversion")
.WithDescription("Computes monthly installment, principal/interest breakdown, and converts Flat rate to effective APR/EAR.");

// 2. Analyze Affordability (DTI & Emergency Runway)
loanGroup.MapPost("/affordability", (AffordabilityRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context);
    var errors = LoanRequestValidator.Validate(request?.Profile, request?.Loan, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var metrics = service.AnalyzeAffordability(request!.Profile, request.Loan);
    return Results.Ok(metrics);
})
.WithName("AnalyzeAffordability")
.WithSummary("Analyze DTI and Emergency Runway")
.WithDescription("Checks Debt-To-Income against CBE 50% limit and assesses disposable income and emergency runway months.");

// 3. Run Deterministic Stress Testing Shocks
loanGroup.MapPost("/stress-test", (AffordabilityRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context);
    var errors = LoanRequestValidator.Validate(request?.Profile, request?.Loan, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var summary = service.RunStressTest(request!.Profile, request.Loan, lang);
    return Results.Ok(summary);
})
.WithName("RunStressTest")
.WithSummary("Simulate Financial Stress Shocks")
.WithDescription("Tests resilience against medical emergencies, job loss (1/3/6 months), income reduction (25%), inflation, and combined crisis.");

// 4. Run Stochastic Monte Carlo Simulation
loanGroup.MapPost("/monte-carlo", (MonteCarloRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context);
    var errors = LoanRequestValidator.Validate(request?.Profile, request?.Loan, request?.Options, request?.Iterations ?? 5000, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var result = service.RunMonteCarlo(request!.Profile, request.Loan, request.Iterations, request.Options);
    return Results.Ok(result);
})
.WithName("RunMonteCarlo")
.WithSummary("Run Monte Carlo Default Simulation")
.WithDescription("Simulates 5,000+ stochastic life paths, returning default probability %, peak risk month, and 5th/50th/95th percentile savings.");

// 5. Generate 360° Comprehensive Advisor Report
loanGroup.MapPost("/full-analysis", async (LoanAnalysisRequestDto request, HttpContext context, ILoanAdvisorService service, ILoanNarrativeAdvisor aiAdvisor, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context, request?.Language, request?.CustomerName, request?.CustomQuestion);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var report = service.GenerateFullReport(request!, lang);
    if (request!.IncludeAiNarrative)
    {
        var ai = await aiAdvisor.GenerateAdviceAsync(report, request.CustomerName, request.CustomQuestion, lang, ct);
        report = report with { AiNarrative = ai };
    }
    return Results.Ok(report);
})
.WithName("GenerateFullAnalysis")
.WithSummary("Comprehensive 360° Loan Analysis Report")
.WithDescription("Executes complete financial advisory pipeline: Amortization, Affordability, Stress Testing, Monte Carlo, 0-100 Risk Score, Strategy Options, Catalog Alternatives, and Final Decision. Optionally includes AI narrative advice.");

// 6. Get Loan Products Catalog
loanGroup.MapGet("/catalog", (HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context);
    var catalog = service.GetLoanCatalog(lang);
    return Results.Ok(catalog);
})
.WithName("GetLoanCatalog")
.WithSummary("Get Egyptian Market Loan Catalog")
.WithDescription("Returns pre-seeded catalog of commercial bank loans and consumer finance products for comparison.");

// 7. Export Comprehensive PDF Report
loanGroup.MapPost("/export-pdf", async (LoanAnalysisRequestDto request, HttpContext context, ILoanAdvisorService service, IPdfReportService pdfService, ILoanNarrativeAdvisor aiAdvisor, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context, request?.Language, request?.CustomerName, request?.CustomQuestion);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var report = service.GenerateFullReport(request!, lang);
    if (request!.IncludeAiNarrative)
    {
        var ai = await aiAdvisor.GenerateAdviceAsync(report, request.CustomerName, request.CustomQuestion, lang, ct);
        report = report with { AiNarrative = ai };
    }
    var pdfBytes = pdfService.GenerateReportPdf(report, request?.CustomerName, lang);
    var fileName = $"LoanLens-Report-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
    return Results.File(pdfBytes, "application/pdf", fileName);
})
.WithName("ExportPdfReport")
.WithSummary("Export Comprehensive Financial Report as PDF")
.WithDescription("Generates and downloads a branded PDF report with risk score, stress testing, and product comparison.");

// 8. Save Evaluated Scenario
loanGroup.MapPost("/save-scenario", async (LoanAnalysisRequestDto request, HttpContext context, ILoanAdvisorService service, IScenarioRepository repo, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context, request?.Language, request?.CustomerName, request?.CustomQuestion);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var report = service.GenerateFullReport(request!, lang);
    var id = await repo.SaveScenarioAsync(request?.CustomerName, report, ct);
    return Results.Created($"/api/loans/scenarios/{id}", new
    {
        id,
        customerName = request?.CustomerName,
        message = LoanLensLanguage.IsArabic(lang) ? "تم حفظ السيناريو بنجاح في قاعدة البيانات" : "Scenario saved successfully to database",
        report
    });
})
.WithName("SaveScenario")
.WithSummary("Save Evaluated Scenario to Database")
.WithDescription("Persists full analysis report and parameters into SQLite database for historical tracking and reload.");

// 9. Get Recent Saved Scenarios
loanGroup.MapGet("/scenarios", async (IScenarioRepository repo, int count = 20, CancellationToken ct = default) =>
{
    var limit = Math.Clamp(count, 1, 100);
    var list = await repo.GetRecentScenariosAsync(limit, ct);
    return Results.Ok(list);
})
.WithName("GetRecentScenarios")
.WithSummary("List Saved Scenarios")
.WithDescription("Retrieves recent evaluated scenario summaries ordered by creation date descending.");

// 10. Get Saved Scenario by ID
loanGroup.MapGet("/scenarios/{id:guid}", async (Guid id, HttpContext context, IScenarioRepository repo, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context);
    var report = await repo.GetScenarioByIdAsync(id, ct);
    if (report is null)
    {
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو بالمعرف {id}" : $"Scenario not found with ID {id}" });
    }
    return Results.Ok(report);
})
.WithName("GetScenarioById")
.WithSummary("Get Saved Scenario by ID")
.WithDescription("Loads complete saved loan analysis report from database by unique ID.");

// 11. Download Saved Scenario PDF by ID
loanGroup.MapGet("/scenarios/{id:guid}/pdf", async (Guid id, bool includeAi, HttpContext context, IScenarioRepository repo, IPdfReportService pdfService, ILoanNarrativeAdvisor aiAdvisor, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context);
    var report = await repo.GetScenarioByIdAsync(id, ct);
    if (report is null)
    {
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو بالمعرف {id}" : $"Scenario not found with ID {id}" });
    }

    if (includeAi && report.AiNarrative == null)
    {
        var ai = await aiAdvisor.GenerateAdviceAsync(report, null, null, lang, ct);
        report = report with { AiNarrative = ai };
    }

    var pdfBytes = pdfService.GenerateReportPdf(report, null, lang);
    var fileName = $"LoanLens-Scenario-{id}.pdf";
    return Results.File(pdfBytes, "application/pdf", fileName);
})
.WithName("DownloadScenarioPdf")
.WithSummary("Download Saved Scenario PDF")
.WithDescription("Generates and downloads PDF report directly from a previously saved scenario ID. Optionally includes AI narrative briefing with ?includeAi=true.");

// 12. Compare Two Loan Scenarios Side-by-Side (Arbitrary Request Payloads)
loanGroup.MapPost("/compare", (ScenarioComparisonRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context, request?.Language, request?.ScenarioAName, request?.ScenarioBName);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات المقارنة غير صالحة" : "Invalid comparison input parameters");

    var comparison = service.CompareScenarios(request!, lang);
    return Results.Ok(comparison);
})
.WithName("CompareScenarios")
.WithSummary("Side-by-Side Loan Scenarios Comparison")
.WithDescription("Compares two arbitrary loan scenarios across Monthly Installment, Total Interest, APR, DTI, Risk Score, and Monte Carlo Default Probability.");

// 13. Compare Two Saved Scenarios by IDs
loanGroup.MapGet("/scenarios/compare", async (Guid id1, Guid id2, HttpContext context, IScenarioRepository repo, ILoanAdvisorService service, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context);
    var reportA = await repo.GetScenarioByIdAsync(id1, ct);
    if (reportA is null)
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو الأول بالمعرف {id1}" : $"Scenario 1 not found with ID {id1}" });

    var reportB = await repo.GetScenarioByIdAsync(id2, ct);
    if (reportB is null)
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو الثاني بالمعرف {id2}" : $"Scenario 2 not found with ID {id2}" });

    var nameA = LoanLensLanguage.IsArabic(lang) ? $"السيناريو الأول ({id1.ToString()[..8]})" : $"Scenario 1 ({id1.ToString()[..8]})";
    var nameB = LoanLensLanguage.IsArabic(lang) ? $"السيناريو الثاني ({id2.ToString()[..8]})" : $"Scenario 2 ({id2.ToString()[..8]})";

    var comparison = service.CompareReports(reportA, reportB, nameA, nameB, lang);
    return Results.Ok(comparison);
})
.WithName("CompareSavedScenarios")
.WithSummary("Compare Two Saved Scenarios by IDs")
.WithDescription("Loads two saved scenarios from SQLite database and performs a full side-by-side comparative analysis.");

// 14. Calculate Borrowing Capacity & Affordability Ceiling
loanGroup.MapPost("/borrowing-capacity", (BorrowingCapacityRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context, request?.Language);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات القدرة الاقتراضية غير صالحة" : "Invalid borrowing capacity parameters");

    var capacity = service.CalculateBorrowingCapacity(request!, lang);
    return Results.Ok(capacity);
})
.WithName("CalculateBorrowingCapacity")
.WithSummary("Calculate Maximum Borrowing Capacity")
.WithDescription("Calculates maximum loan amount and installment under Egyptian Central Bank (CBE) 50% legal limit vs 35% safe recommended ceiling.");

// 15. Simulate Debt Buyout & Multi-Loan Consolidation
loanGroup.MapPost("/debt-consolidation", (DebtConsolidationRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context, request?.Language);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات دمج المديونيات غير صالحة" : "Invalid debt consolidation parameters");

    var result = service.AnalyzeDebtConsolidation(request!, lang);
    return Results.Ok(result);
})
.WithName("AnalyzeDebtConsolidation")
.WithSummary("Analyze Debt Buyout and Consolidation")
.WithDescription("Evaluates consolidation of multiple existing loans/credit cards into a single new facility, accounting for bank early settlement fees and monthly cash flow freed up.");

// 16. Variable Rate & CBE Corridor Sensitivity Simulation
loanGroup.MapPost("/rate-sensitivity", (RateSensitivityRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context, request?.Language);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات حساسية الفائدة غير صالحة" : "Invalid rate sensitivity parameters");

    var result = service.AnalyzeRateSensitivity(request!, lang);
    return Results.Ok(result);
})
.WithName("AnalyzeRateSensitivity")
.WithSummary("Variable Rate and CBE Corridor Sensitivity Analysis")
.WithDescription("Models the impact of benchmark interest rate shifts (+100 bps, +200 bps, +300 bps, -100 bps, -200 bps) on monthly installments and total interest paid.");

// 17. Export Amortization Schedule to Excel (.xlsx)
loanGroup.MapPost("/export-excel", (ExportExcelRequestDto request, HttpContext context, ILoanAdvisorService service, IExcelExportService excelService) =>
{
    var lang = ResolveLanguage(context, request?.Language, request?.CustomerName);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات تصدير الإكسيل غير صالحة" : "Invalid excel export parameters");

    var schedule = service.CalculateSchedule(request!.Loan);
    var excelBytes = excelService.ExportAmortizationScheduleToExcel(request.Loan.ToDomain(), schedule, request.CustomerName, lang);
    var fileName = $"LoanLens-Schedule-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
    return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
})
.WithName("ExportExcelSchedule")
.WithSummary("Export Amortization Schedule to Excel (.xlsx)")
.WithDescription("Generates an interactive, styled Excel spreadsheet with metadata summary, formatted currency numbers, and complete monthly amortization schedule.");

// 18. Export Saved Scenario Amortization Schedule to Excel (.xlsx)
loanGroup.MapGet("/scenarios/{id:guid}/excel", async (Guid id, HttpContext context, IScenarioRepository repo, IExcelExportService excelService, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context);
    var report = await repo.GetScenarioByIdAsync(id, ct);
    if (report is null)
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو بالمعرف {id}" : $"Scenario not found with ID {id}" });

    var excelBytes = excelService.ExportAmortizationScheduleToExcel(report.LoanDetails, report.AmortizationSchedule, null, lang);
    var fileName = $"LoanLens-Scenario-{id}.xlsx";
    return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
})
.WithName("DownloadScenarioExcel")
.WithSummary("Download Saved Scenario Excel (.xlsx)")
.WithDescription("Exports amortization schedule of a saved loan scenario directly to an Excel spreadsheet.");

// 19. Generate Personalized AI Narrative Financial Advice
loanGroup.MapPost("/ai-advice", async (AiAdviceRequestDto request, HttpContext context, ILoanAdvisorService service, ILoanNarrativeAdvisor aiAdvisor, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context, request?.AnalysisRequest?.Language, request?.CustomQuestion, request?.AnalysisRequest?.CustomerName);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الاستشارة غير صالحة" : "Invalid advice request parameters");

    var report = service.GenerateFullReport(request!.AnalysisRequest, lang);
    var advice = await aiAdvisor.GenerateAdviceAsync(report, request.AnalysisRequest.CustomerName, request.CustomQuestion, lang, ct);
    return Results.Ok(advice);
})
.WithName("GenerateAiAdvice")
.WithSummary("Generate AI Narrative Loan Advisor Briefing")
.WithDescription("Synthesizes personalized human-level financial advisory narrative with key strengths, vulnerabilities, step-by-step action plan, and Egyptian market nuances.");

// 20. Generate AI Narrative Advice for Saved Scenario
loanGroup.MapGet("/scenarios/{id:guid}/ai-advice", async (Guid id, string? question, HttpContext context, IScenarioRepository repo, ILoanNarrativeAdvisor aiAdvisor, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context, null, question);
    var report = await repo.GetScenarioByIdAsync(id, ct);
    if (report is null)
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو بالمعرف {id}" : $"Scenario not found with ID {id}" });

    var advice = await aiAdvisor.GenerateAdviceAsync(report, null, question, lang, ct);
    return Results.Ok(advice);
})
.WithName("GetScenarioAiAdvice")
.WithSummary("Get AI Narrative Advice for Saved Scenario")
.WithDescription("Generates personalized AI narrative advice and answers questions for a previously saved loan scenario.");

// Initialize SQLite database schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanLensDbContext>();
    db.Database.EnsureCreated();
}
    
app.Run();

// Language resolution helper: supports explicit param, ?lang= query string, Accept-Language header, intelligent Arabic text detection
static string ResolveLanguage(HttpContext context, string? explicitLang = null, params string?[] textHints)
{
    // 1. Explicit query parameter ?lang=
    if (context.Request.Query.TryGetValue("lang", out var queryLang) && !string.IsNullOrWhiteSpace(queryLang))
        return LoanLensLanguage.IsArabic(queryLang.ToString()) ? LoanLensLanguage.Arabic : LoanLensLanguage.English;

    // 2. Explicit language parameter in payload
    if (!string.IsNullOrWhiteSpace(explicitLang))
        return LoanLensLanguage.IsArabic(explicitLang) ? LoanLensLanguage.Arabic : LoanLensLanguage.English;

    // 3. Intelligent auto-detection from text hints (Arabic question, Arabic customer name, etc.)
    if (textHints != null)
    {
        foreach (var hint in textHints)
        {
            if (LoanLensLanguage.ContainsArabic(hint))
                return LoanLensLanguage.Arabic;
        }
    }

    // 4. Accept-Language header
    var acceptLang = context.Request.Headers.AcceptLanguage.ToString();
    if (!string.IsNullOrWhiteSpace(acceptLang) && LoanLensLanguage.IsArabic(acceptLang))
        return LoanLensLanguage.Arabic;

    return LoanLensLanguage.English;
}
