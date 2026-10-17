namespace LoanLens.Application.DTOs;

public record ResetPasswordRequestDto(
    string Email,
    string Token,
    string NewPassword
);
