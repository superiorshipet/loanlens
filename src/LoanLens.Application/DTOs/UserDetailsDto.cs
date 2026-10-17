namespace LoanLens.Application.DTOs;

public record UserDetailsDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    int TotalSavedScenarios,
    IReadOnlyList<SavedScenarioSummaryDto> Scenarios
);
