using LoanLens.Application.DTOs;
using LoanLens.Application.Services;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;
using LoanLens.Infrastructure.Configuration;
using LoanLens.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LoanLens.Domain.Tests;

public class AiNarrativeAdvisorTests
{
    private readonly FinancialCalculator _calculator;
    private readonly AffordabilityAnalyzer _affordability;
    private readonly StressTestingEngine _stress;
    private readonly MonteCarloEngine _monteCarlo;
    private readonly RiskScoringEngine _risk;
    private readonly RecommendationEngine _recommendation;
    private readonly BorrowingCapacityEngine _borrowing;
    private readonly DebtConsolidationEngine _consolidation;
    private readonly RateSensitivityEngine _sensitivity;
    private readonly LoanAdvisorService _advisorService;
    private readonly LoanNarrativeAdvisorService _aiNarrativeService;
    private readonly LoanPdfReportService _pdfService;

    public AiNarrativeAdvisorTests()
    {
        _calculator = new FinancialCalculator();
        _affordability = new AffordabilityAnalyzer();
        _stress = new StressTestingEngine();
        _monteCarlo = new MonteCarloEngine();
        _risk = new RiskScoringEngine();
        _recommendation = new RecommendationEngine(_calculator);
        _borrowing = new BorrowingCapacityEngine();
        _consolidation = new DebtConsolidationEngine(_calculator);
        _sensitivity = new RateSensitivityEngine(_calculator);

        _advisorService = new LoanAdvisorService(
            _calculator,
            _affordability,
            _stress,
            _monteCarlo,
            _risk,
            _recommendation,
            _borrowing,
            _consolidation,
            _sensitivity
        );

        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var cacheService = new RedisCacheService(memoryCache, NullLogger<RedisCacheService>.Instance, null);
        var scraperService = new BankRatesScraperService(new HttpClient(), cacheService, Options.Create(new ScraperOptions()), NullLogger<BankRatesScraperService>.Instance);
        var bankRatesService = new BankRatesService(cacheService, scraperService, NullLogger<BankRatesService>.Instance);

        var options = Options.Create(new AiAdvisorOptions());
        _aiNarrativeService = new LoanNarrativeAdvisorService(
            new HttpClient(),
            options,
            bankRatesService,
            NullLogger<LoanNarrativeAdvisorService>.Instance
        );

        _pdfService = new LoanPdfReportService();
    }

    [Fact]
    public async Task AiAdvisor_GeneratesInsightfulNarrative_InEnglish()
    {
        var request = new LoanAnalysisRequestDto(
            Profile: new UserProfileDto(
                MonthlyIncome: 35_000m,
                Stability: IncomeStability.StableEmployed,
                FixedMonthlyExpenses: 12_000m,
                CurrentMonthlyInstallments: 3_000m,
                Savings: 60_000m,
                DependentsCount: 1,
                Age: 33,
                HasHealthInsurance: true
            ),
            Loan: new LoanInputDto(
                Principal: 150_000m,
                TenorMonths: 36,
                AnnualInterestRatePercentage: 22.0m,
                CalculationMethod: InterestCalculationMethod.Reducing
            ),
            CustomerName: "Kareem Tarek"
        );

        var report = _advisorService.GenerateFullReport(request, "en");
        var advice = await _aiNarrativeService.GenerateAdviceAsync(report, "Kareem Tarek", null, "en");

        Assert.NotNull(advice);
        Assert.False(string.IsNullOrWhiteSpace(advice.ExecutiveSummary));
        Assert.NotEmpty(advice.KeyStrengths);
        Assert.NotEmpty(advice.ActionPlanSteps);
        Assert.False(string.IsNullOrWhiteSpace(advice.EgyptianMarketInsight));
        Assert.False(string.IsNullOrWhiteSpace(advice.Disclaimer));
        Assert.Contains("Kareem Tarek", advice.ExecutiveSummary);
    }

    [Fact]
    public async Task AiAdvisor_GeneratesInsightfulNarrative_InArabic()
    {
        var request = new LoanAnalysisRequestDto(
            Profile: new UserProfileDto(
                MonthlyIncome: 25_000m,
                Stability: IncomeStability.Freelance,
                FixedMonthlyExpenses: 10_000m,
                CurrentMonthlyInstallments: 5_000m,
                Savings: 15_000m,
                DependentsCount: 2,
                Age: 38,
                HasHealthInsurance: false
            ),
            Loan: new LoanInputDto(
                Principal: 120_000m,
                TenorMonths: 24,
                AnnualInterestRatePercentage: 24.0m,
                CalculationMethod: InterestCalculationMethod.Reducing
            ),
            CustomerName: "محمد سامي"
        );

        var report = _advisorService.GenerateFullReport(request, "ar");
        var advice = await _aiNarrativeService.GenerateAdviceAsync(report, "محمد سامي", null, "ar");

        Assert.NotNull(advice);
        Assert.False(string.IsNullOrWhiteSpace(advice.ExecutiveSummary));
        Assert.NotEmpty(advice.VulnerabilitiesAndWarnings);
        Assert.NotEmpty(advice.ActionPlanSteps);
        Assert.Contains("محمد سامي", advice.ExecutiveSummary);
        Assert.False(string.IsNullOrWhiteSpace(advice.EgyptianMarketInsight));
        Assert.False(string.IsNullOrWhiteSpace(advice.Disclaimer));
    }

    [Fact]
    public async Task PdfReport_RendersSeamlessly_WhenAiNarrativeIsIncluded()
    {
        var request = new LoanAnalysisRequestDto(
            Profile: new UserProfileDto(
                MonthlyIncome: 30_000m,
                Stability: IncomeStability.StableEmployed,
                FixedMonthlyExpenses: 11_000m,
                CurrentMonthlyInstallments: 2_000m,
                Savings: 45_000m,
                DependentsCount: 1,
                Age: 32,
                HasHealthInsurance: true
            ),
            Loan: new LoanInputDto(
                Principal: 100_000m,
                TenorMonths: 24,
                AnnualInterestRatePercentage: 21.0m,
                CalculationMethod: InterestCalculationMethod.Reducing
            ),
            CustomerName: "Hossam Zaki"
        );

        var report = _advisorService.GenerateFullReport(request, "en");
        var advice = await _aiNarrativeService.GenerateAdviceAsync(report, "Hossam Zaki", null, "en");
        var reportWithAi = report with { AiNarrative = advice };

        var pdfBytes = _pdfService.GenerateReportPdf(reportWithAi, "Hossam Zaki", "en");

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 5000);
        // Valid PDF signature %PDF
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }
}
