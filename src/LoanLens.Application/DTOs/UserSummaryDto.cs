namespace LoanLens.Application.DTOs;

public record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    int SavedScenariosCount
);
