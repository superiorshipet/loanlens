using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface IScenarioRepository
{
    Task<Guid> SaveScenarioAsync(string? customerName, ComprehensiveLoanReportDto report, CancellationToken cancellationToken = default);
    Task<ComprehensiveLoanReportDto?> GetScenarioByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedScenarioSummaryDto>> GetRecentScenariosAsync(int count = 20, CancellationToken cancellationToken = default);
}
