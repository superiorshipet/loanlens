namespace LoanLens.Application.DTOs;

public record ScenarioComparisonResultDto(
    string ScenarioAName,
    string ScenarioBName,
    ComprehensiveLoanReportDto ReportA,
    ComprehensiveLoanReportDto ReportB,
    decimal MonthlyInstallmentDiff,
    decimal TotalInterestDiff,
    decimal EffectiveAprDiff,
    decimal DtiDiff,
    decimal DefaultProbabilityDiff,
    decimal RiskScoreDiff,
    IReadOnlyList<ScenarioComparisonMetricDiff> KeyDifferences,
    string VerdictComparisonSummary,
    string WinnerScenarioName,
    string DetailedAdvisorRationale);
