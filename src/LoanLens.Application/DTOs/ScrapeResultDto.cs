namespace LoanLens.Application.DTOs;

public record ScrapeResultDto(
    bool Success,
    int RatesUpdatedCount,
    string Message,
    DateTime ScrapedAtUtc,
    decimal CbeLendingRate,
    decimal CbeDepositRate,
    decimal CbeMainOperationRate,
    decimal AveragePersonalLoanRate,
    decimal AverageAutoLoanRate,
    decimal AverageMortgageRate,
    IReadOnlyList<ScrapedBankRateDto> ExtractedRates,
    IReadOnlyList<string> SourcesChecked,
    string ScraperEngine
);
