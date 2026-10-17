namespace LoanLens.Application.DTOs;

public record ScrapedBankRateDto(
    string BankName,
    string BankNameAr,
    string Category,
    decimal RatePercentage,
    decimal MinRatePercentage,
    decimal MaxRatePercentage,
    string RateType,
    decimal AdminFeePercentage,
    int MaxTenorYears,
    string Notes,
    string NotesAr,
    string SourceUrl,
    DateTime ExtractedAtUtc,
    bool IsLiveScraped
);
