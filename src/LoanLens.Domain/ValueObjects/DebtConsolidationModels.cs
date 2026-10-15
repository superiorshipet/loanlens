using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public enum DebtConsolidationVerdict
{
    HighlyRecommended = 1,
    Recommended = 2,
    NotRecommended = 3
}

public record ExistingDebtItem(
    string DebtName,
    decimal RemainingPrincipal,
    decimal MonthlyInstallment,
    int RemainingMonths,
    decimal EarlySettlementFeePercentage = 3.0m);

public record ConsolidationTargetOffer(
    string ProviderName,
    decimal AnnualInterestRatePercentage,
    int TenorMonths,
    decimal AdminFeePercentage = 1.5m,
    InterestCalculationMethod CalculationMethod = InterestCalculationMethod.Reducing);

public record DebtConsolidationResult(
    DebtConsolidationVerdict Verdict,
    string VerdictHeadline,
    string DetailedReasoning,
    decimal TotalCurrentDebtPrincipal,
    decimal TotalEarlySettlementPenalty,
    decimal RequiredNewLoanPrincipal,
    decimal CurrentTotalMonthlyInstallments,
    decimal NewConsolidatedMonthlyInstallment,
    decimal MonthlyCashFlowFreedUp,
    decimal CurrentDtiPercentage,
    decimal NewDtiPercentage,
    decimal TotalRemainingInterestOldLoans,
    decimal TotalInterestNewConsolidatedLoan,
    decimal NetFinancialLifetimeSavings,
    bool IsDtiCbeCompliant);
