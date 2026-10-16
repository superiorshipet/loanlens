namespace LoanLens.Domain.ValueObjects;

public record RepaymentStrategyOption(
    string StrategyName,
    string Description,
    decimal MonthlyPayment,
    decimal TotalInterestPaid,
    decimal InterestSavedComparedToStandard,
    int TotalMonthsToPayoff,
    decimal EarlySettlementFeePaid = 0m,
    decimal NetFinancialBenefit = 0m);
