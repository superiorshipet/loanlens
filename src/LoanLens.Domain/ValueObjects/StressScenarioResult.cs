namespace LoanLens.Domain.ValueObjects;

public record StressScenarioResult(
    string ScenarioName,
    bool IsSurviving,
    int? MonthOfDefault,
    decimal EndingSavings,
    decimal MaxMonthlyDeficit,
    string ImpactDescription);
