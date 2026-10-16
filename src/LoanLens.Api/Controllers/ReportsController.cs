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
public class ReportsController : ApiControllerBase
{
    private readonly ILoanAdvisorService _service;
    private readonly IPdfReportService _pdfService;
    private readonly IExcelExportService _excelService;
    private readonly ILoanNarrativeAdvisor _aiAdvisor;

    public ReportsController(
        ILoanAdvisorService service,
        IPdfReportService pdfService,
        IExcelExportService excelService,
        ILoanNarrativeAdvisor aiAdvisor)
    {
        _service = service;
        _pdfService = pdfService;
        _excelService = excelService;
        _aiAdvisor = aiAdvisor;
    }

    [HttpPost("export-pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportPdf([FromBody] LoanAnalysisRequestDto request, CancellationToken ct)
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

        var pdfBytes = _pdfService.GenerateReportPdf(report, request?.CustomerName, lang);
        var fileName = $"LoanLens-Report-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpPost("export-excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ExportExcel([FromBody] ExportExcelRequestDto request)
    {
        var lang = ResolveLanguage(request?.Language, request?.CustomerName);
        var errors = LoanRequestValidator.Validate(request, lang);
        if (errors.Count > 0)
        {
            var title = LoanLensLanguage.IsArabic(lang) ? "بيانات تصدير الإكسيل غير صالحة" : "Invalid excel export parameters";
            return ValidationProblem(new ValidationProblemDetails(errors) { Title = title });
        }

        var schedule = _service.CalculateSchedule(request!.Loan);
        var excelBytes = _excelService.ExportAmortizationScheduleToExcel(request.Loan.ToDomain(), schedule, request.CustomerName, lang);
        var fileName = $"LoanLens-Schedule-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
