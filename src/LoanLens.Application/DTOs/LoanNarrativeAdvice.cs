namespace LoanLens.Application.DTOs;

public record LoanNarrativeAdvice(
    string ExecutiveSummary,
    IReadOnlyList<string> KeyStrengths,
    IReadOnlyList<string> VulnerabilitiesAndWarnings,
    IReadOnlyList<string> ActionPlanSteps,
    string EgyptianMarketInsight,
    string Disclaimer,
    string ModelUsed,
    DateTime GeneratedAtUtc);
