using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanLens.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowerProfileToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentMonthlyInstallments",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependentsCount",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedMonthlyExpenses",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasHealthInsurance",
                table: "Users",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyIncome",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Savings",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SecondaryIncome",
                table: "Users",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stability",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentMonthlyInstallments",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DependentsCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FixedMonthlyExpenses",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HasHealthInsurance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MonthlyIncome",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Savings",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecondaryIncome",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Stability",
                table: "Users");
        }
    }
}
