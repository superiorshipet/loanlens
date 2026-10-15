using System.Text;
using LoanLens.Application.DTOs;
using LoanLens.Application.Validation;
using LoanLens.Domain.Enums;
using LoanLens.Domain.Services;
using LoanLens.Domain.ValueObjects;
using LoanLens.Infrastructure.Persistence;
using LoanLens.Infrastructure.Repositories;
using LoanLens.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LoanLens.Domain.Tests;

public class InfrastructureAndExportTests
{
    private static ComprehensiveLoanReportDto CreateSampleReport()
    {
        var profile = new FinancialProfile(
            MonthlyIncome: 30000m,
            Stability: IncomeStability.StableEmployed,
            FixedMonthlyExpenses: 12000m,
            CurrentMonthlyInstallments: 2000m,
            Savings: 60000m,
            DependentsCount: 1,
            Age: 32,
            HasHealthInsurance: true
        );

        var loan = new LoanParameters(
            Principal: 100000m,
            TenorMonths: 24,
            AnnualInterestRatePercentage: 18m,
            CalculationMethod: InterestCalculationMethod.Reducing
        );

        var calc = new FinancialCalculator();
        var schedule = calc.CalculateSchedule(loan);
        var affordability = new AffordabilityAnalyzer().Analyze(profile, schedule.MonthlyInstallment);
        var stressTest = new StressTestingEngine().RunAllScenarios(profile, loan, schedule.MonthlyInstallment);
        var mc = new MonteCarloEngine().RunSimulation(profile, loan, schedule.MonthlyInstallment, 100);
        var risk = new RiskScoringEngine().Evaluate(profile, affordability, stressTest, mc);
        var rec = new RecommendationEngine(calc).GenerateDecision(profile, loan, schedule, affordability, risk);

        return new ComprehensiveLoanReportDto(
            LoanDetails: loan,
            UserProfile: profile,
            AmortizationSchedule: schedule,
            Affordability: affordability,
            StressTest: stressTest,
            MonteCarlo: mc,
            RiskEvaluation: risk,
            Recommendation: rec,
            GeneratedAtUtc: DateTime.UtcNow
        );
    }

    [Fact]
    public void PdfReportService_GeneratesValidPdfBytes_WithMagicHeader()
    {
        // Arrange
        var service = new LoanPdfReportService();
        var report = CreateSampleReport();

        // Act
        var pdfBytes = service.GenerateReportPdf(report, "أحمد محمد");

        // Assert
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 2000, "PDF byte stream should contain full content pages");

        // PDF file header check "%PDF"
        var header = Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 4));
        Assert.Equal("%PDF", header);
    }

    [Fact]
    public async Task ScenarioRepository_SavesAndRetrievesScenario_Accurately()
    {
        // Arrange: In-memory SQLite connection must be opened and kept open
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoanLensDbContext>()
            .UseSqlite(connection)
            .Options;

        using var db = new LoanLensDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var repo = new ScenarioRepository(db);
        var report = CreateSampleReport();

        // Act - Save
        var scenarioId = await repo.SaveScenarioAsync("سارة إبراهيم", report);

        // Assert - Retrieve by ID
        var retrieved = await repo.GetScenarioByIdAsync(scenarioId);
        Assert.NotNull(retrieved);
        Assert.Equal(report.LoanDetails.Principal, retrieved.LoanDetails.Principal);
        Assert.Equal(report.AmortizationSchedule.MonthlyInstallment, retrieved.AmortizationSchedule.MonthlyInstallment);
        Assert.Equal(report.RiskEvaluation.TotalRiskScore, retrieved.RiskEvaluation.TotalRiskScore);

        // Assert - Retrieve recent list
        var recentList = await repo.GetRecentScenariosAsync(10);
        Assert.Single(recentList);
        Assert.Equal(scenarioId, recentList[0].Id);
        Assert.Equal("سارة إبراهيم", recentList[0].CustomerName);
    }

    [Fact]
    public void LoanRequestValidator_ValidatesLoanAnalysisRequestDtoStrictly()
    {
        // Arrange - Negative values & out of range iterations
        var invalidRequest = new LoanAnalysisRequestDto(
            Profile: new UserProfileDto(
                MonthlyIncome: -5000m,
                Stability: IncomeStability.StableEmployed,
                FixedMonthlyExpenses: -100m,
                CurrentMonthlyInstallments: 0m,
                Savings: -20m,
                DependentsCount: -1,
                Age: 15
            ),
            Loan: new LoanInputDto(
                Principal: -100000m,
                TenorMonths: -12,
                AnnualInterestRatePercentage: -5m
            ),
            MonteCarloIterations: -500
        );

        // Act
        var errors = LoanRequestValidator.Validate(invalidRequest);

        // Assert
        Assert.NotEmpty(errors);
        Assert.True(errors.ContainsKey("loan.principal"));
        Assert.True(errors.ContainsKey("loan.tenorMonths"));
        Assert.True(errors.ContainsKey("loan.annualInterestRatePercentage"));
        Assert.True(errors.ContainsKey("profile.monthlyIncome"));
        Assert.True(errors.ContainsKey("profile.fixedMonthlyExpenses"));
        Assert.True(errors.ContainsKey("profile.savings"));
        Assert.True(errors.ContainsKey("profile.dependentsCount"));
        Assert.True(errors.ContainsKey("profile.age"));
        Assert.True(errors.ContainsKey("monteCarloIterations"));
    }
}
