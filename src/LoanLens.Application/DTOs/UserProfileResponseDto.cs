using LoanLens.Domain.Enums;

namespace LoanLens.Application.DTOs;

public record UserProfileResponseDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime CreatedAt,
    int TotalSavedScenarios,
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
