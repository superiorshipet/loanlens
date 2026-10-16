using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Application.Services;
using LoanLens.Application.Validation;
using LoanLens.Domain.Localization;
using Microsoft.AspNetCore.Mvc;

namespace LoanLens.Api.Controllers;

[ApiController]
[Route("api/v1/loans")]
[Route("api/loans")]
public class LoansController : ApiControllerBase
{
    private readonly ILoanAdvisorService _service;
    private readonly ILoanNarrativeAdvisor _aiAdvisor;

    public LoansController(ILoanAdvisorService service, ILoanNarrativeAdvisor aiAdvisor)
    {
        _service = service;
        _aiAdvisor = aiAdvisor;
    }

    /// <summary>
    /// Calculate Amortization Schedule and APR conversion
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Calculate([FromBody] LoanInputDto request)
    {
        var lang = ResolveLanguage();
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات القرض غير صالحة" : "Invalid loan parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var schedule = _service.CalculateSchedule(request);
        return Ok(schedule);
    }

    /// <summary>
    /// Analyze DTI and Emergency Runway against Central Bank limits
    /// </summary>
    [HttpPost("affordability")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Affordability([FromBody] AffordabilityRequestDto request)
    {
        var lang = ResolveLanguage();
        var errors = LoanRequestValidator.Validate(request?.Profile, request?.Loan, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var metrics = _service.AnalyzeAffordability(request!.Profile, request.Loan);
        return Ok(metrics);
    }

    /// <summary>
    /// Simulate Financial Stress Shocks
    /// </summary>
    [HttpPost("stress-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult StressTest([FromBody] AffordabilityRequestDto request)
    {
        var lang = ResolveLanguage();
        var errors = LoanRequestValidator.Validate(request?.Profile, request?.Loan, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var summary = _service.RunStressTest(request!.Profile, request.Loan, lang);
        return Ok(summary);
    }

    /// <summary>
    /// Run Monte Carlo Default Simulation
    /// </summary>
    [HttpPost("monte-carlo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult MonteCarlo([FromBody] MonteCarloRequestDto request)
    {
        var lang = ResolveLanguage();
        var errors = LoanRequestValidator.Validate(request?.Profile, request?.Loan, request?.Options, request?.Iterations ?? 5000, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var result = _service.RunMonteCarlo(request!.Profile, request.Loan, request.Iterations, request.Options);
        return Ok(result);
    }

    /// <summary>
    /// Comprehensive 360° Loan Analysis Report
    /// </summary>
    [HttpPost("full-analysis")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FullAnalysis([FromBody] LoanAnalysisRequestDto request, CancellationToken ct)
    {
        var lang = ResolveLanguage(request?.Language, request?.CustomerName, request?.CustomQuestion);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var report = _service.GenerateFullReport(request!, lang);
        if (request!.IncludeAiNarrative)
        {
            var ai = await _aiAdvisor.GenerateAdviceAsync(report, request.CustomerName, request.CustomQuestion, lang, ct);
            report = report with { AiNarrative = ai };
        }
        return Ok(report);
    }

    /// <summary>
    /// Get Egyptian Market Loan Products Catalog
    /// </summary>
    [HttpGet("catalog")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCatalog()
    {
        var lang = ResolveLanguage();
        var catalog = _service.GetLoanCatalog(lang);
        return Ok(catalog);
    }

    /// <summary>
    /// Side-by-Side Comparison of Two Arbitrary Scenarios
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Compare([FromBody] ScenarioComparisonRequestDto request)
    {
        var lang = ResolveLanguage(request?.Language, request?.ScenarioAName, request?.ScenarioBName);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات المقارنة غير صالحة" : "Invalid comparison input parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var comparison = _service.CompareScenarios(request!, lang);
        return Ok(comparison);
    }

    /// <summary>
    /// Calculate Maximum Borrowing Capacity
    /// </summary>
    [HttpPost("borrowing-capacity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult BorrowingCapacity([FromBody] BorrowingCapacityRequestDto request)
    {
        var lang = ResolveLanguage(request?.Language);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات القدرة الاقتراضية غير صالحة" : "Invalid borrowing capacity parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var capacity = _service.CalculateBorrowingCapacity(request!, lang);
        return Ok(capacity);
    }

    /// <summary>
    /// Simulate Debt Buyout and Multi-Loan Consolidation
    /// </summary>
    [HttpPost("debt-consolidation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DebtConsolidation([FromBody] DebtConsolidationRequestDto request)
    {
        var lang = ResolveLanguage(request?.Language);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات دمج المديونيات غير صالحة" : "Invalid debt consolidation parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var result = _service.AnalyzeDebtConsolidation(request!, lang);
        return Ok(result);
    }

    /// <summary>
    /// Variable Rate and CBE Corridor Sensitivity Analysis
    /// </summary>
    [HttpPost("rate-sensitivity")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RateSensitivity([FromBody] RateSensitivityRequestDto request)
    {
        var lang = ResolveLanguage(request?.Language);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات حساسية الفائدة غير صالحة" : "Invalid rate sensitivity parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var result = _service.AnalyzeRateSensitivity(request!, lang);
        return Ok(result);
    }
}
