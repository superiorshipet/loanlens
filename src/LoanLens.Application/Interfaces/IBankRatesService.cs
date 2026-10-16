using LoanLens.Application.DTOs;

namespace LoanLens.Application.Interfaces;

public interface IBankRatesService
{
    Task<BankRatesSummaryDto> GetMarketRatesAsync(string language = "en", CancellationToken ct = default);
    Task<BankRatesSummaryDto> RefreshMarketRatesAsync(CancellationToken ct = default);
}
