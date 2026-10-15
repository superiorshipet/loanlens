using LoanLens.Domain.Enums;

namespace LoanLens.Domain.ValueObjects;

public record FinalAdvisorDecision(
    RecommendationVerdict Verdict,
    string VerdictHeadline,
    string DetailedReasoning,
    IReadOnlyList<string> KeyActionableAdvice,
    IReadOnlyList<RepaymentStrategyOption> Strategies,
    IReadOnlyList<AlternativeComparison> TopAlternatives);
