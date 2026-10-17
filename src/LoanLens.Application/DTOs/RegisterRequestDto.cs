using System.ComponentModel.DataAnnotations;
using LoanLens.Domain.Enums;

namespace LoanLens.Application.DTOs;

public record RegisterRequestDto(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MinLength(2)] string FullName,
    int? Age = null,
    IncomeStability? Stability = null,
    decimal? MonthlyIncome = null,
    decimal? FixedMonthlyExpenses = null,
    decimal? CurrentMonthlyInstallments = null,
    decimal? Savings = null,
    int? DependentsCount = null,
    bool? HasHealthInsurance = null,
    decimal? SecondaryIncome = null
);
