using System.Text.Json;
using LoanLens.Application.DTOs;
using LoanLens.Application.Interfaces;
using LoanLens.Infrastructure.Entities;
using LoanLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoanLens.Infrastructure.Repositories;

public class ScenarioRepository : IScenarioRepository
{
    private readonly LoanLensDbContext _db;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ScenarioRepository(LoanLensDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> SaveScenarioAsync(string? customerName, ComprehensiveLoanReportDto report, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var entity = new SavedScenarioEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            CustomerName = customerName?.Trim(),
            MonthlyIncome = report.UserProfile.MonthlyIncome,
            FixedExpenses = report.UserProfile.FixedMonthlyExpenses,
            CurrentInstallments = report.UserProfile.CurrentMonthlyInstallments,
            Savings = report.UserProfile.Savings,
            DependentsCount = report.UserProfile.DependentsCount,
            Age = report.UserProfile.Age,
            Principal = report.LoanDetails.Principal,
            TenorMonths = report.LoanDetails.TenorMonths,
            InterestRate = report.LoanDetails.AnnualInterestRatePercentage,
            CalculationMethod = (int)report.LoanDetails.CalculationMethod,
            MonthlyInstallment = report.AmortizationSchedule.MonthlyInstallment,
            DtiPercentage = report.Affordability.DtiPercentage,
            RiskScore = report.RiskEvaluation.TotalRiskScore,
            RiskCategory = report.RiskEvaluation.RiskLabel,
            Verdict = report.Recommendation.Verdict.ToString(),
            VerdictHeadline = report.Recommendation.VerdictHeadline,
            ReportJson = JsonSerializer.Serialize(report, JsonOptions)
        };

        _db.SavedScenarios.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }

    public async Task<ComprehensiveLoanReportDto?> GetScenarioByIdAsync(Guid id, Guid? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var query = _db.SavedScenarios.AsNoTracking().Where(e => e.Id == id);

        if (!isAdmin && userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId || e.UserId == null);
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);

        if (entity is null || string.IsNullOrWhiteSpace(entity.ReportJson))
            return null;

        return JsonSerializer.Deserialize<ComprehensiveLoanReportDto>(entity.ReportJson, JsonOptions);
    }

    public async Task<IReadOnlyList<SavedScenarioSummaryDto>> GetRecentScenariosAsync(int count = 20, Guid? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var query = _db.SavedScenarios.AsNoTracking();

        if (!isAdmin && userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .Select(e => new SavedScenarioSummaryDto(
                e.Id,
                e.CreatedAt,
                e.CustomerName,
                e.Principal,
                e.TenorMonths,
                e.MonthlyInstallment,
                e.DtiPercentage,
                e.RiskScore,
                e.RiskCategory,
                e.VerdictHeadline,
                e.UserId
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteScenarioAsync(Guid id, Guid? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var query = _db.SavedScenarios.Where(e => e.Id == id);
        if (!isAdmin && userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId);
        }

        var entity = await query.FirstOrDefaultAsync(cancellationToken);
        if (entity is null) return false;

        _db.SavedScenarios.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CountUserScenariosAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.SavedScenarios.CountAsync(s => s.UserId == userId, cancellationToken);
    }
}
