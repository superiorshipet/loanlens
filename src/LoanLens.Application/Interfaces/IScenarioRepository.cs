using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface IScenarioRepository
{
    Task<Guid> SaveScenarioAsync(string? customerName, ComprehensiveLoanReportDto report, Guid? userId = null, CancellationToken cancellationToken = default);
    Task<ComprehensiveLoanReportDto?> GetScenarioByIdAsync(Guid id, Guid? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedScenarioSummaryDto>> GetRecentScenariosAsync(int count = 20, Guid? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<bool> DeleteScenarioAsync(Guid id, Guid? userId = null, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task<int> CountUserScenariosAsync(Guid userId, CancellationToken cancellationToken = default);
}
