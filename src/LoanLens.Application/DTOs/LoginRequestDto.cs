using System.ComponentModel.DataAnnotations;

namespace LoanLens.Application.DTOs;

public record LoginRequestDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);
