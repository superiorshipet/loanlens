namespace LoanLens.Application.DTOs;

public record DebtConsolidationRequestDto(
    UserProfileDto Profile,
    List<ExistingDebtItemDto> ExistingDebts,
    ConsolidationTargetOfferDto TargetOffer,
    string? Language = null);
