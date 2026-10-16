using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    MonthlyIncome = table.Column<decimal>(type: "numeric", nullable: false),
                    FixedExpenses = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrentInstallments = table.Column<decimal>(type: "numeric", nullable: false),
                    Savings = table.Column<decimal>(type: "numeric", nullable: false),
                    DependentsCount = table.Column<int>(type: "integer", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Principal = table.Column<decimal>(type: "numeric", nullable: false),
                    TenorMonths = table.Column<int>(type: "integer", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CalculationMethod = table.Column<int>(type: "integer", nullable: false),
                    MonthlyInstallment = table.Column<decimal>(type: "numeric", nullable: false),
                    DtiPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskScore = table.Column<decimal>(type: "numeric", nullable: false),
                    RiskCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Verdict = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VerdictHeadline = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ReportJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedScenarios_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedScenarios_CreatedAt",
                table: "SavedScenarios",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SavedScenarios_UserId",
                table: "SavedScenarios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedScenarios");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
