namespace LoanLens.Application.DTOs;

public record AdminMarketStatsDto(
    decimal CbeLendingRate,
    decimal CbeDepositRate,
    decimal CbeMainOperationRate,
    decimal AveragePersonalLoanRate,
    decimal AverageAutoLoanRate,
    decimal AverageMortgageRate,
    string MarketStatus,
    string CacheStatus,
    DateTime RatesLastRefreshed
);
