using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface ILoanNarrativeAdvisor
{
    Task<LoanNarrativeAdvice> GenerateAdviceAsync(
        ComprehensiveLoanReportDto report,
        string? customerName = null,
        string? customQuestion = null,
        string language = "en",
        CancellationToken cancellationToken = default);
}
