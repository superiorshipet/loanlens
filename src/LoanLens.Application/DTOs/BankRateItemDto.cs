namespace LoanLens.Application.DTOs;

public record BankRateItemDto(
    string BankName,
    string BankNameAr,
    string Category,
    decimal RatePercentage,
    decimal? MinRatePercentage,
    decimal? MaxRatePercentage,
    string RateType,
    decimal AdminFeePercentage,
    int MaxTenorYears,
    string Notes,
    string NotesAr,
    DateTime LastUpdated,
    string Source);
