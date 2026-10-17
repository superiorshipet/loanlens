namespace LoanLens.Application.DTOs;

public record AdminUserStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    int AdminUsers,
    int StandardUsers,
    int NewUsersLast7Days,
    int NewUsersLast30Days
);
