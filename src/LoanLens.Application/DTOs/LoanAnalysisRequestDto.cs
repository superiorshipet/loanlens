namespace LoanLens.Application.DTOs;

public record LoanAnalysisRequestDto(
    UserProfileDto Profile,
    LoanInputDto Loan,
    int MonteCarloIterations = 5000,
    string? CustomerName = null,
    string? Language = null,
    MonteCarloOptionsDto? MonteCarloOptions = null,
    bool IncludeAiNarrative = false,
    string? CustomQuestion = null);
