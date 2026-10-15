using LoanLens.Application.Services;
using LoanLens.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LoanLens.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Domain Engines
        services.AddSingleton<IFinancialCalculator, FinancialCalculator>();
        services.AddSingleton<IAffordabilityAnalyzer, AffordabilityAnalyzer>();
        services.AddSingleton<IStressTestingEngine, StressTestingEngine>();
        services.AddSingleton<IMonteCarloEngine, MonteCarloEngine>();
        services.AddSingleton<IRiskScoringEngine, RiskScoringEngine>();
        services.AddSingleton<IRecommendationEngine, RecommendationEngine>();

        // Application Services
        services.AddScoped<ILoanAdvisorService, LoanAdvisorService>();

        return services;
    }
}
