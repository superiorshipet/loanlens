namespace LoanLens.Application.DTOs;

public record BankRatesSummaryDto(
    decimal CbeLendingRate,
    decimal CbeDepositRate,
    decimal CbeMainOperationRate,
    decimal AveragePersonalLoanRate,
    decimal AverageAutoLoanRate,
    decimal AverageMortgageRate,
    IReadOnlyList<BankRateItemDto> Rates,
    DateTime AsOfDate,
    string MarketStatus,
    string MarketStatusAr);
