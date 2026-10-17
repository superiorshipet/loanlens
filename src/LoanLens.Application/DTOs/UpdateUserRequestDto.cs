using System.ComponentModel.DataAnnotations;
using LoanLens.Domain.Enums;

namespace LoanLens.Application.DTOs;

public record UpdateUserRequestDto(
    [MinLength(2)] string? FullName = null,
    [EmailAddress] string? Email = null,
    string? Role = null,
    bool? IsActive = null,
    [MinLength(6)] string? NewPassword = null,
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
