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

// Configure OpenAPI specification
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Servers = [new() { Url = "http://localhost:5257" }];
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
        var statusCode = ex is ArgumentException or FormatException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = statusCode;

        var isAr = ResolveLanguage(context) == LoanLensLanguage.Arabic;
        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            title = statusCode == 400 
                ? (isAr ? "بيانات المدخلات غير صحيحة" : "Invalid request parameters") 
                : (isAr ? "خطأ غير متوقع في الخادم" : "An unexpected server error occurred"),
            status = statusCode,
            detail = ex.Message,
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
loanGroup.MapPost("/full-analysis", (LoanAnalysisRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context, request?.Language);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var report = service.GenerateFullReport(request!, lang);
    return Results.Ok(report);
})
.WithName("GenerateFullAnalysis")
.WithSummary("Comprehensive 360° Loan Analysis Report")
.WithDescription("Executes complete financial advisory pipeline: Amortization, Affordability, Stress Testing, Monte Carlo, 0-100 Risk Score, Strategy Options, Catalog Alternatives, and Final Decision.");

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
loanGroup.MapPost("/export-pdf", (LoanAnalysisRequestDto request, HttpContext context, ILoanAdvisorService service, IPdfReportService pdfService) =>
{
    var lang = ResolveLanguage(context, request?.Language);
    var errors = LoanRequestValidator.Validate(request, lang);
    if (errors.Count > 0)
        return Results.ValidationProblem(errors, title: LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters");

    var report = service.GenerateFullReport(request!, lang);
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
    var lang = ResolveLanguage(context, request?.Language);
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
loanGroup.MapGet("/scenarios/{id:guid}/pdf", async (Guid id, HttpContext context, IScenarioRepository repo, IPdfReportService pdfService, CancellationToken ct) =>
{
    var lang = ResolveLanguage(context);
    var report = await repo.GetScenarioByIdAsync(id, ct);
    if (report is null)
    {
        return Results.NotFound(new { message = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو بالمعرف {id}" : $"Scenario not found with ID {id}" });
    }

    var pdfBytes = pdfService.GenerateReportPdf(report, null, lang);
    var fileName = $"LoanLens-Scenario-{id}.pdf";
    return Results.File(pdfBytes, "application/pdf", fileName);
})
.WithName("DownloadScenarioPdf")
.WithSummary("Download Saved Scenario PDF")
.WithDescription("Generates and downloads PDF report directly from a previously saved scenario ID.");

// 12. Compare Two Loan Scenarios Side-by-Side (Arbitrary Request Payloads)
loanGroup.MapPost("/compare", (ScenarioComparisonRequestDto request, HttpContext context, ILoanAdvisorService service) =>
{
    var lang = ResolveLanguage(context, request?.Language);
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

// Initialize SQLite database schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoanLensDbContext>();
    db.Database.EnsureCreated();
}
    
app.Run();

// Language resolution helper: supports explicit param, ?lang= query string, Accept-Language header, default English
static string ResolveLanguage(HttpContext context, string? explicitLang = null)
{
    if (!string.IsNullOrWhiteSpace(explicitLang))
        return LoanLensLanguage.IsArabic(explicitLang) ? LoanLensLanguage.Arabic : LoanLensLanguage.English;

    if (context.Request.Query.TryGetValue("lang", out var queryLang) && !string.IsNullOrWhiteSpace(queryLang))
        return LoanLensLanguage.IsArabic(queryLang.ToString()) ? LoanLensLanguage.Arabic : LoanLensLanguage.English;

    var acceptLang = context.Request.Headers.AcceptLanguage.ToString();
    if (!string.IsNullOrWhiteSpace(acceptLang) && LoanLensLanguage.IsArabic(acceptLang))
        return LoanLensLanguage.Arabic;

    return LoanLensLanguage.English;
}

// DTO helpers for API bindings
public record AffordabilityRequestDto(UserProfileDto Profile, LoanInputDto Loan);
public record MonteCarloRequestDto(UserProfileDto Profile, LoanInputDto Loan, int Iterations = 5000, MonteCarloOptionsDto? Options = null);
