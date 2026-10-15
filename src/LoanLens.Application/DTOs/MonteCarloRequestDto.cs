namespace LoanLens.Application.DTOs;

public record MonteCarloRequestDto(
    UserProfileDto Profile,
    LoanInputDto Loan,
    int Iterations = 5000,
    MonteCarloOptionsDto? Options = null);
