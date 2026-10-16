namespace LoanLens.Application.DTOs;

public record UserProfileResponseDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime CreatedAt,
    int TotalSavedScenarios
);
