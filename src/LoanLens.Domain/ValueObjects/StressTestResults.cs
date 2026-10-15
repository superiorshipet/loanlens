namespace LoanLens.Domain.ValueObjects;

public record StressScenarioResult(
    string ScenarioName,
    bool IsSurviving,
    int? MonthOfDefault,
    decimal EndingSavings,
    decimal MaxMonthlyDeficit,
    string ImpactDescription);

public record StressTestSummary(
    IReadOnlyList<StressScenarioResult> Scenarios,
    int TotalScenariosTested,
    int ScenariosSurvived,
    bool IsResilient,
    string OverallAssessment);
