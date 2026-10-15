using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

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
