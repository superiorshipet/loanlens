using LoanLens.Domain.ValueObjects;

namespace LoanLens.Application.DTOs;

public record ComprehensiveLoanReportDto(
    LoanParameters LoanDetails,
    FinancialProfile UserProfile,
    AmortizationSchedule AmortizationSchedule,
    AffordabilityMetrics Affordability,
    StressTestSummary StressTest,
    MonteCarloSimulationResult MonteCarlo,
    RiskEvaluation RiskEvaluation,
    FinalAdvisorDecision Recommendation,
    DateTime GeneratedAtUtc);
