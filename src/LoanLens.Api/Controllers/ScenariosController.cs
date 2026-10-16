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
public class ScenariosController : ApiControllerBase
{
    private readonly ILoanAdvisorService _service;
    private readonly IScenarioRepository _repo;
    private readonly IPdfReportService _pdfService;
    private readonly IExcelExportService _excelService;
    private readonly ILoanNarrativeAdvisor _aiAdvisor;

    public ScenariosController(
        ILoanAdvisorService service,
        IScenarioRepository repo,
        IPdfReportService pdfService,
        IExcelExportService excelService,
        ILoanNarrativeAdvisor aiAdvisor)
    {
        _service = service;
        _repo = repo;
        _pdfService = pdfService;
        _excelService = excelService;
        _aiAdvisor = aiAdvisor;
    }

    /// <summary>
    /// Save Evaluated Scenario to Database
    /// </summary>
    [HttpPost("save-scenario")]
    [HttpPost("scenarios")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveScenario([FromBody] LoanAnalysisRequestDto request, CancellationToken ct)
    {
        var lang = ResolveLanguage(request?.Language, request?.CustomerName, request?.CustomQuestion);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات الإدخال غير صالحة" : "Invalid input parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var report = _service.GenerateFullReport(request!, lang);
        var id = await _repo.SaveScenarioAsync(request?.CustomerName, report, ct);

        return CreatedAtAction(nameof(GetById), new { id }, new
        {
            id,
            customerName = request?.CustomerName,
            message = LoanLensLanguage.IsArabic(lang) ? "تم حفظ السيناريو بنجاح في قاعدة البيانات" : "Scenario saved successfully to database",
            report
        });
    }

    /// <summary>
    /// List Recent Saved Scenarios
    /// </summary>
    [HttpGet("scenarios")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int count = 20, CancellationToken ct = default)
    {
        var limit = Math.Clamp(count, 1, 100);
        var list = await _repo.GetRecentScenariosAsync(limit, ct);
        return Ok(list);
    }

    /// <summary>
    /// Get Saved Scenario by ID
    /// </summary>
    [HttpGet("scenarios/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var lang = ResolveLanguage();
        var report = await _repo.GetScenarioByIdAsync(id, ct);
        if (report is null)
        {
            var message = LoanLensLanguage.IsArabic(lang)
                ? $"لم يتم العثور على السيناريو بالمعرف {id}"
                : $"Scenario not found with ID {id}";
            return NotFound(new { message });
        }
        return Ok(report);
    }

    /// <summary>
    /// Download Saved Scenario PDF by ID
    /// </summary>
    [HttpGet("scenarios/{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, [FromQuery] bool includeAi = false, CancellationToken ct = default)
    {
        var lang = ResolveLanguage();
        var report = await _repo.GetScenarioByIdAsync(id, ct);
        if (report is null)
        {
            var message = LoanLensLanguage.IsArabic(lang)
                ? $"لم يتم العثور على السيناريو بالمعرف {id}"
                : $"Scenario not found with ID {id}";
            return NotFound(new { message });
        }

        if (includeAi && report.AiNarrative == null)
        {
            var ai = await _aiAdvisor.GenerateAdviceAsync(report, null, null, lang, ct);
            report = report with { AiNarrative = ai };
        }

        var pdfBytes = _pdfService.GenerateReportPdf(report, null, lang);
        var fileName = $"LoanLens-Scenario-{id}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Download Saved Scenario Excel (.xlsx) by ID
    /// </summary>
    [HttpGet("scenarios/{id:guid}/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadExcel(Guid id, CancellationToken ct)
    {
        var lang = ResolveLanguage();
        var report = await _repo.GetScenarioByIdAsync(id, ct);
        if (report is null)
        {
            var message = LoanLensLanguage.IsArabic(lang)
                ? $"لم يتم العثور على السيناريو بالمعرف {id}"
                : $"Scenario not found with ID {id}";
            return NotFound(new { message });
        }

        var excelBytes = _excelService.ExportAmortizationScheduleToExcel(report.LoanDetails, report.AmortizationSchedule, null, lang);
        var fileName = $"LoanLens-Scenario-{id}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Compare Two Saved Scenarios by IDs
    /// </summary>
    [HttpGet("scenarios/compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareSaved([FromQuery] Guid id1, [FromQuery] Guid id2, CancellationToken ct)
    {
        var lang = ResolveLanguage();
        var reportA = await _repo.GetScenarioByIdAsync(id1, ct);
        if (reportA is null)
        {
            var msg = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو الأول بالمعرف {id1}" : $"Scenario 1 not found with ID {id1}";
            return NotFound(new { message = msg });
        }

        var reportB = await _repo.GetScenarioByIdAsync(id2, ct);
        if (reportB is null)
        {
            var msg = LoanLensLanguage.IsArabic(lang) ? $"لم يتم العثور على السيناريو الثاني بالمعرف {id2}" : $"Scenario 2 not found with ID {id2}";
            return NotFound(new { message = msg });
        }

        var nameA = LoanLensLanguage.IsArabic(lang) ? $"السيناريو الأول ({id1.ToString()[..8]})" : $"Scenario 1 ({id1.ToString()[..8]})";
        var nameB = LoanLensLanguage.IsArabic(lang) ? $"السيناريو الثاني ({id2.ToString()[..8]})" : $"Scenario 2 ({id2.ToString()[..8]})";

        var comparison = _service.CompareReports(reportA, reportB, nameA, nameB, lang);
        return Ok(comparison);
    }

    /// <summary>
    /// Generate AI Narrative Advice for Saved Scenario
    /// </summary>
    [HttpGet("scenarios/{id:guid}/ai-advice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScenarioAiAdvice(Guid id, [FromQuery] string? question, CancellationToken ct)
    {
        var lang = ResolveLanguage(null, question);
        var report = await _repo.GetScenarioByIdAsync(id, ct);
        if (report is null)
        {
            var message = LoanLensLanguage.IsArabic(lang)
                ? $"لم يتم العثور على السيناريو بالمعرف {id}"
                : $"Scenario not found with ID {id}";
            return NotFound(new { message });
        }

        var advice = await _aiAdvisor.GenerateAdviceAsync(report, null, question, lang, ct);
        return Ok(advice);
    }
}
