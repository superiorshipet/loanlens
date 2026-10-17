using LoanLens.Domain.Enums;

namespace LoanLens.Application.DTOs;

public record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    int SavedScenariosCount,
    int? Age = null,
    IncomeStability? Stability = null,
    decimal? MonthlyIncome = null
);
