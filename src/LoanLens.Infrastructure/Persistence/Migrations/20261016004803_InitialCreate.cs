using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "TEXT", nullable: false),
                    FixedExpenses = table.Column<decimal>(type: "TEXT", nullable: false),
                    CurrentInstallments = table.Column<decimal>(type: "TEXT", nullable: false),
                    Savings = table.Column<decimal>(type: "TEXT", nullable: false),
                    DependentsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Age = table.Column<int>(type: "INTEGER", nullable: false),
                    Principal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TenorMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    CalculationMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyInstallment = table.Column<decimal>(type: "TEXT", nullable: false),
                    DtiPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    RiskScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    RiskCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Verdict = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    VerdictHeadline = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    ReportJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedScenarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedScenarios_CreatedAt",
                table: "SavedScenarios",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedScenarios");
        }
    }
}
