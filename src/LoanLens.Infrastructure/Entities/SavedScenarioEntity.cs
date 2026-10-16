namespace LoanLens.Infrastructure.Entities;

public class SavedScenarioEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CustomerName { get; set; }

    // Profile snapshot
    public decimal MonthlyIncome { get; set; }
    public decimal FixedExpenses { get; set; }
    public decimal CurrentInstallments { get; set; }
    public decimal Savings { get; set; }
    public int DependentsCount { get; set; }
    public int Age { get; set; }

    // Loan snapshot
    public decimal Principal { get; set; }
    public int TenorMonths { get; set; }
    public decimal InterestRate { get; set; }
    public int CalculationMethod { get; set; }

    // Results snapshot
    public decimal MonthlyInstallment { get; set; }
    public decimal DtiPercentage { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public string Verdict { get; set; } = string.Empty;
    public string VerdictHeadline { get; set; } = string.Empty;

    // Full JSON snapshot for complete report reload
    public string ReportJson { get; set; } = string.Empty;
}
