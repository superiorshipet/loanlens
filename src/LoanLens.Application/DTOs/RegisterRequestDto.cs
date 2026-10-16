using System.ComponentModel.DataAnnotations;

namespace LoanLens.Application.DTOs;

public record RegisterRequestDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MinLength(2)] string FullName
);
