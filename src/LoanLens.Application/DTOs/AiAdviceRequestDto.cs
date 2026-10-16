namespace LoanLens.Application.DTOs;

public record AiAdviceRequestDto(
    LoanAnalysisRequestDto AnalysisRequest,
    string? CustomQuestion = null);
