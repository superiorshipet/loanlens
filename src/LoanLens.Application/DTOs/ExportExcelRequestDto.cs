namespace LoanLens.Application.DTOs;

public record ExportExcelRequestDto(
    LoanInputDto Loan,
    string? CustomerName = null,
    string? Language = null);
