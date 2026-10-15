namespace LoanLens.Application.DTOs;

public record ScenarioComparisonMetricDiff(
    string MetricName,
    string ScenarioAValue,
    string ScenarioBValue,
    decimal NumericalDifference,
    string AdvantageDescription,
    string PreferredScenario);
