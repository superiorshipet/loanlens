using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface IBankRatesScraperService
{
    Task<ScrapeResultDto> ScrapeLatestRatesAsync(CancellationToken ct = default);
}
