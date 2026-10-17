using LoanLens.Application.DTOs;
using LoanLens.Domain.Enums;

namespace LoanLens.Infrastructure.Entities;

public class UserEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? Age { get; set; }
    public IncomeStability? Stability { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public decimal? FixedMonthlyExpenses { get; set; }
    public decimal? CurrentMonthlyInstallments { get; set; }
    public decimal? Savings { get; set; }
    public int? DependentsCount { get; set; }
    public bool? HasHealthInsurance { get; set; }
    public decimal? SecondaryIncome { get; set; }

    public ICollection<SavedScenarioEntity> Scenarios { get; set; } = new List<SavedScenarioEntity>();

    public UserProfileDto? ToUserProfileDto()
    {
        if (!MonthlyIncome.HasValue || !Stability.HasValue || !Age.HasValue)
            return null;

        return new UserProfileDto(
            MonthlyIncome: MonthlyIncome.Value,
            Stability: Stability.Value,
            FixedMonthlyExpenses: FixedMonthlyExpenses ?? 0m,
            CurrentMonthlyInstallments: CurrentMonthlyInstallments ?? 0m,
            Savings: Savings ?? 0m,
            DependentsCount: DependentsCount ?? 0,
            Age: Age.Value,
            HasHealthInsurance: HasHealthInsurance ?? false,
            SecondaryIncome: SecondaryIncome ?? 0m
        );
    }
}
