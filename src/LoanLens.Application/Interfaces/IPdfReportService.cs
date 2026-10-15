using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface IPdfReportService
{
    byte[] GenerateReportPdf(ComprehensiveLoanReportDto report, string? customerName = null, string language = "en");
}
