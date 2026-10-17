namespace LoanLens.Application.DTOs;

public record AdminDashboardDto(
    AdminUserStatsDto UserStats,
    AdminScenarioStatsDto ScenarioStats,
    AdminMarketStatsDto MarketStats,
    IReadOnlyList<UserSummaryDto> RecentUsers,
    IReadOnlyList<SavedScenarioSummaryDto> RecentScenarios,
    DateTime GeneratedAtUtc
);
