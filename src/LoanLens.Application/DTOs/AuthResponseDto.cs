namespace LoanLens.Application.DTOs;

public record AuthResponseDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string Token,
    DateTime ExpiresAt
);
