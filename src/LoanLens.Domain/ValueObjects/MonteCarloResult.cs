namespace LoanLens.Domain.ValueObjects;

public record MonteCarloSimulationResult(
    int TotalIterations,
    int DefaultCount,
    decimal DefaultProbabilityPercentage,
    int? PeakRiskMonth,
    double AverageDefaultMonth,
    decimal MedianEndingSavings,
    decimal WorstCase5thPercentileSavings,
    decimal BestCase95thPercentileSavings,
    IReadOnlyDictionary<int, int> DefaultsByMonthDistribution,
    long ElapsedMilliseconds);
