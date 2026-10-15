using LoanLens.Application;
using LoanLens.Application.DTOs;
using LoanLens.Application.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Register Clean Architecture Application & Domain Services
builder.Services.AddApplication();

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
loanGroup.MapPost("/calculate", (LoanInputDto request, ILoanAdvisorService service) =>
{
    var schedule = service.CalculateSchedule(request);
    return Results.Ok(schedule);
})
.WithName("CalculateAmortization")
.WithSummary("Calculate Amortization Schedule and APR conversion")
.WithDescription("Computes monthly installment, principal/interest breakdown, and converts Flat rate to effective APR/EAR.");

// 2. Analyze Affordability (DTI & Emergency Runway)
loanGroup.MapPost("/affordability", (AffordabilityRequestDto request, ILoanAdvisorService service) =>
{
    var metrics = service.AnalyzeAffordability(request.Profile, request.Loan);
    return Results.Ok(metrics);
})
.WithName("AnalyzeAffordability")
.WithSummary("Analyze DTI and Emergency Runway")
.WithDescription("Checks Debt-To-Income against CBE 50% limit and assesses disposable income and emergency runway months.");

// 3. Run Deterministic Stress Testing Shocks
loanGroup.MapPost("/stress-test", (AffordabilityRequestDto request, ILoanAdvisorService service) =>
{
    var summary = service.RunStressTest(request.Profile, request.Loan);
    return Results.Ok(summary);
})
.WithName("RunStressTest")
.WithSummary("Simulate Financial Stress Shocks")
.WithDescription("Tests resilience against medical emergencies, job loss (1/3/6 months), income reduction (25%), inflation, and combined crisis.");

// 4. Run Stochastic Monte Carlo Simulation
loanGroup.MapPost("/monte-carlo", (MonteCarloRequestDto request, ILoanAdvisorService service) =>
{
    var result = service.RunMonteCarlo(request.Profile, request.Loan, request.Iterations);
    return Results.Ok(result);
})
.WithName("RunMonteCarlo")
.WithSummary("Run Monte Carlo Default Simulation")
.WithDescription("Simulates 5,000+ stochastic life paths, returning default probability %, peak risk month, and 5th/50th/95th percentile savings.");

// 5. Generate 360° Comprehensive Advisor Report
loanGroup.MapPost("/full-analysis", (LoanAnalysisRequestDto request, ILoanAdvisorService service) =>
{
    var report = service.GenerateFullReport(request);
    return Results.Ok(report);
})
.WithName("GenerateFullAnalysis")
.WithSummary("Comprehensive 360° Loan Analysis Report")
.WithDescription("Executes complete financial advisory pipeline: Amortization, Affordability, Stress Testing, Monte Carlo, 0-100 Risk Score, Strategy Options, Catalog Alternatives, and Final Decision.");

// 6. Get Loan Products Catalog
loanGroup.MapGet("/catalog", (ILoanAdvisorService service) =>
{
    var catalog = service.GetLoanCatalog();
    return Results.Ok(catalog);
})
.WithName("GetLoanCatalog")
.WithSummary("Get Egyptian Market Loan Catalog")
.WithDescription("Returns pre-seeded catalog of commercial bank loans and consumer finance products for comparison.");

app.Run();

// DTO helpers for API bindings
public record AffordabilityRequestDto(UserProfileDto Profile, LoanInputDto Loan);
public record MonteCarloRequestDto(UserProfileDto Profile, LoanInputDto Loan, int Iterations = 5000);
