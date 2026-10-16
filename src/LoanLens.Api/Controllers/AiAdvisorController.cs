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
public class AiAdvisorController : ApiControllerBase
{
    private readonly ILoanAdvisorService _service;
    private readonly ILoanNarrativeAdvisor _aiAdvisor;

    public AiAdvisorController(ILoanAdvisorService service, ILoanNarrativeAdvisor aiAdvisor)
    {
        _service = service;
        _aiAdvisor = aiAdvisor;
    }

    /// <summary>
    /// Generate AI Narrative Loan Advisor Briefing
    /// </summary>
    [HttpPost("ai-advice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAiAdvice([FromBody] AiAdviceRequestDto request, CancellationToken ct)
    {
        var lang = ResolveLanguage(request?.AnalysisRequest?.Language, request?.CustomQuestion, request?.AnalysisRequest?.CustomerName);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات الاستشارة غير صالحة" : "Invalid advice request parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var report = _service.GenerateFullReport(request!.AnalysisRequest, lang);
        var advice = await _aiAdvisor.GenerateAdviceAsync(report, request.AnalysisRequest.CustomerName, request.CustomQuestion, lang, ct);
        return Ok(advice);
    }
}
