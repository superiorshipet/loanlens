namespace LoanLens.Application.DTOs;

public record ScenarioComparisonRequestDto(
    LoanAnalysisRequestDto ScenarioA,
    LoanAnalysisRequestDto ScenarioB,
    string? ScenarioAName = "Scenario A",
    string? ScenarioBName = "Scenario B",
    string? Language = null);
