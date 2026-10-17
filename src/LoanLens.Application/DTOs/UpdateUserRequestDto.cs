using System.ComponentModel.DataAnnotations;

namespace LoanLens.Application.DTOs;

public record UpdateUserRequestDto(
    [MinLength(2)] string? FullName,
    [EmailAddress] string? Email,
    string? Role,
    bool? IsActive,
    [MinLength(6)] string? NewPassword
);
