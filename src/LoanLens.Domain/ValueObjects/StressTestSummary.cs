namespace LoanLens.Domain.ValueObjects;

public record StressTestSummary(
    IReadOnlyList<StressScenarioResult> Scenarios,
    int TotalScenariosTested,
    int ScenariosSurvived,
    bool IsResilient,
    string OverallAssessment);
