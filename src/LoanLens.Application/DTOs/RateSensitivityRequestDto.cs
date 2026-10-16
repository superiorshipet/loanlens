namespace LoanLens.Application.DTOs;

public record RateSensitivityRequestDto(
    LoanInputDto Loan,
    string? Language = null);
