using LoanLens.Domain.Enums;

namespace LoanLens.Application.DTOs;

public record BorrowingCapacityRequestDto(
    UserProfileDto Profile,
    decimal TargetInterestRatePercentage,
    int TargetTenorMonths,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing,
    string? Language = null);
