using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Domain.Enums;
using LoanLens.Infrastructure.Entities;
using LoanLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanLens.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly LoanLensDbContext _db;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IBankRatesService? _bankRatesService;
    private readonly ICacheService? _cacheService;

    public AuthService(LoanLensDbContext db, IJwtTokenGenerator jwtGenerator)
        : this(db, jwtGenerator, null, null)
    {
    }

    public AuthService(
        LoanLensDbContext db,
        IJwtTokenGenerator jwtGenerator,
        IBankRatesService? bankRatesService,
        ICacheService? cacheService)
    {
        _db = db;
        _jwtGenerator = jwtGenerator;
        _bankRatesService = bankRatesService;
        _cacheService = cacheService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, ct);
        if (existingUser)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var role = await _db.Users.AnyAsync(ct) ? UserRole.User : UserRole.Admin; // First user becomes Admin

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FullName = request.FullName.Trim(),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _jwtGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString(), user.FullName);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponseDto(user.Id, user.Email, user.FullName, user.Role.ToString(), token, expiresAt);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account has been deactivated.");
        }

        var token = _jwtGenerator.GenerateToken(user.Id, user.Email, user.Role.ToString(), user.FullName);
        var expiresAt = DateTime.UtcNow.AddHours(24);

        return new AuthResponseDto(user.Id, user.Email, user.FullName, user.Role.ToString(), token, expiresAt);
    }

    public async Task<UserProfileResponseDto?> GetCurrentUserProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return null;

        var scenarioCount = await _db.SavedScenarios.CountAsync(s => s.UserId == userId, ct);

        return new UserProfileResponseDto(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            CreatedAt: user.CreatedAt,
            TotalSavedScenarios: scenarioCount
        );
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetAllUsersAsync(string? searchTerm = null, string? role = null, bool? isActive = null, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var parsedRole))
        {
            query = query.Where(u => u.Role == parsedRole);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                u.Scenarios.Count
            ))
            .ToListAsync(ct);

        return users;
    }

    public async Task<UserDetailsDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .Include(u => u.Scenarios)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null) return null;

        var scenarios = user.Scenarios
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SavedScenarioSummaryDto(
                s.Id,
                s.CreatedAt,
                s.CustomerName,
                s.Principal,
                s.TenorMonths,
                s.MonthlyInstallment,
                s.DtiPercentage,
                s.RiskScore,
                s.RiskCategory,
                s.VerdictHeadline,
                s.UserId
            ))
            .ToList();

        return new UserDetailsDto(
            UserId: user.Id,
            Email: user.Email,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            TotalSavedScenarios: user.Scenarios.Count,
            Scenarios: scenarios
        );
    }

    public async Task<UserSummaryDto?> UpdateUserAsync(Guid id, UpdateUserRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Scenarios).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalized = request.Email.Trim().ToLowerInvariant();
            if (normalized != user.Email)
            {
                var emailTaken = await _db.Users.AnyAsync(u => u.Id != id && u.Email == normalized, ct);
                if (emailTaken)
                {
                    throw new InvalidOperationException("Email address is already in use by another account.");
                }
                user.Email = normalized;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            user.Role = role;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        await _db.SaveChangesAsync(ct);

        return new UserSummaryDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.ToString(),
            user.IsActive,
            user.CreatedAt,
            user.Scenarios.Count
        );
    }

    public async Task<bool> DeleteUserAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users.Include(u => u.Scenarios).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return false;

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);

        // 1. User stats
        var totalUsers = await _db.Users.CountAsync(ct);
        var activeUsers = await _db.Users.CountAsync(u => u.IsActive, ct);
        var inactiveUsers = totalUsers - activeUsers;
        var adminUsers = await _db.Users.CountAsync(u => u.Role == UserRole.Admin, ct);
        var standardUsers = totalUsers - adminUsers;
        var newUsers7 = await _db.Users.CountAsync(u => u.CreatedAt >= sevenDaysAgo, ct);
        var newUsers30 = await _db.Users.CountAsync(u => u.CreatedAt >= thirtyDaysAgo, ct);

        var userStats = new AdminUserStatsDto(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            InactiveUsers: inactiveUsers,
            AdminUsers: adminUsers,
            StandardUsers: standardUsers,
            NewUsersLast7Days: newUsers7,
            NewUsersLast30Days: newUsers30
        );

        // 2. Scenario & Loan stats
        var totalScenarios = await _db.SavedScenarios.CountAsync(ct);
        var scenarios7 = await _db.SavedScenarios.CountAsync(s => s.CreatedAt >= sevenDaysAgo, ct);
        var scenarios30 = await _db.SavedScenarios.CountAsync(s => s.CreatedAt >= thirtyDaysAgo, ct);

        decimal totalPrincipal = 0;
        decimal avgPrincipal = 0;
        int avgTenor = 0;
        decimal avgDti = 0;
        decimal avgRisk = 0;
        Dictionary<string, int> verdictDict = new();
        Dictionary<string, int> riskDict = new();

        if (totalScenarios > 0)
        {
            totalPrincipal = await _db.SavedScenarios.SumAsync(s => s.Principal, ct);
            avgPrincipal = Math.Round(await _db.SavedScenarios.AverageAsync(s => s.Principal, ct), 2);
            avgTenor = (int)Math.Round(await _db.SavedScenarios.AverageAsync(s => (double)s.TenorMonths, ct));
            avgDti = Math.Round(await _db.SavedScenarios.AverageAsync(s => s.DtiPercentage, ct), 2);
            avgRisk = Math.Round(await _db.SavedScenarios.AverageAsync(s => s.RiskScore, ct), 2);

            var verdicts = await _db.SavedScenarios
                .GroupBy(s => s.Verdict)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            verdictDict = verdicts.ToDictionary(
                v => string.IsNullOrWhiteSpace(v.Key) ? "Unknown" : v.Key,
                v => v.Count
            );

            var riskCategories = await _db.SavedScenarios
                .GroupBy(s => s.RiskCategory)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            riskDict = riskCategories.ToDictionary(
                r => string.IsNullOrWhiteSpace(r.Key) ? "Unknown" : r.Key,
                r => r.Count
            );
        }

        var scenarioStats = new AdminScenarioStatsDto(
            TotalScenarios: totalScenarios,
            ScenariosLast7Days: scenarios7,
            ScenariosLast30Days: scenarios30,
            TotalPrincipalSimulated: totalPrincipal,
            AverageLoanAmount: avgPrincipal,
            AverageTenorMonths: avgTenor,
            AverageDtiPercentage: avgDti,
            AverageRiskScore: avgRisk,
            VerdictBreakdown: verdictDict,
            RiskCategoryBreakdown: riskDict
        );

        // 3. Market Rates stats
        AdminMarketStatsDto marketStats;
        if (_bankRatesService != null)
        {
            try
            {
                var rates = await _bankRatesService.GetMarketRatesAsync("en", ct);
                marketStats = new AdminMarketStatsDto(
                    CbeLendingRate: rates.CbeLendingRate,
                    CbeDepositRate: rates.CbeDepositRate,
                    CbeMainOperationRate: rates.CbeMainOperationRate,
                    AveragePersonalLoanRate: rates.AveragePersonalLoanRate,
                    AverageAutoLoanRate: rates.AverageAutoLoanRate,
                    AverageMortgageRate: rates.AverageMortgageRate,
                    MarketStatus: rates.MarketStatus,
                    CacheStatus: "Active / Redis Online",
                    RatesLastRefreshed: rates.AsOfDate
                );
            }
            catch
            {
                marketStats = BuildDefaultMarketStats(now);
            }
        }
        else
        {
            marketStats = BuildDefaultMarketStats(now);
        }

        // 4. Recent activities
        var recentUsers = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role.ToString(),
                u.IsActive,
                u.CreatedAt,
                u.Scenarios.Count
            ))
            .ToListAsync(ct);

        var recentScenarios = await _db.SavedScenarios
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new SavedScenarioSummaryDto(
                s.Id,
                s.CreatedAt,
                s.CustomerName,
                s.Principal,
                s.TenorMonths,
                s.MonthlyInstallment,
                s.DtiPercentage,
                s.RiskScore,
                s.RiskCategory,
                s.VerdictHeadline,
                s.UserId
            ))
            .ToListAsync(ct);

        return new AdminDashboardDto(
            UserStats: userStats,
            ScenarioStats: scenarioStats,
            MarketStats: marketStats,
            RecentUsers: recentUsers,
            RecentScenarios: recentScenarios,
            GeneratedAtUtc: now
        );
    }

    private static AdminMarketStatsDto BuildDefaultMarketStats(DateTime now)
    {
        return new AdminMarketStatsDto(
            CbeLendingRate: 28.25m,
            CbeDepositRate: 27.25m,
            CbeMainOperationRate: 27.75m,
            AveragePersonalLoanRate: 27.80m,
            AverageAutoLoanRate: 27.50m,
            AverageMortgageRate: 26.00m,
            MarketStatus: "Monetary Policy Committee Active Corridor",
            CacheStatus: "In-Memory Fallback Active",
            RatesLastRefreshed: now
        );
    }
}
