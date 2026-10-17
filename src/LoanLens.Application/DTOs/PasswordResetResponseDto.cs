namespace LoanLens.Application.DTOs;

public record PasswordResetResponseDto(
    bool Success,
    string Message,
    string? Email = null
);
