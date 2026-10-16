using LoanLens.Domain.ValueObjects;

namespace LoanLens.Domain.Services;

public interface IDebtConsolidationEngine
{
    DebtConsolidationResult AnalyzeConsolidation(
        FinancialProfile profile,
        IReadOnlyList<ExistingDebtItem> existingDebts,
        ConsolidationTargetOffer targetOffer,
        string language = "en");
}
