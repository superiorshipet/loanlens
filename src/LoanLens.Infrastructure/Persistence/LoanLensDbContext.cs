using LoanLens.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanLens.Infrastructure.Persistence;

public class LoanLensDbContext : DbContext
{
    public LoanLensDbContext(DbContextOptions<LoanLensDbContext> options) : base(options)
    {
    }

    public DbSet<SavedScenarioEntity> SavedScenarios => Set<SavedScenarioEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Stability).HasConversion<string>().HasMaxLength(30);

            entity.Property(e => e.MonthlyIncome).HasPrecision(18, 2);
            entity.Property(e => e.FixedMonthlyExpenses).HasPrecision(18, 2);
            entity.Property(e => e.CurrentMonthlyInstallments).HasPrecision(18, 2);
            entity.Property(e => e.Savings).HasPrecision(18, 2);
            entity.Property(e => e.SecondaryIncome).HasPrecision(18, 2);

            entity.HasMany(u => u.Scenarios)
                  .WithOne(s => s.User)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<SavedScenarioEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.RiskCategory).HasMaxLength(50);
            entity.Property(e => e.Verdict).HasMaxLength(50);
            entity.Property(e => e.VerdictHeadline).HasMaxLength(250);
            entity.Property(e => e.ReportJson).HasColumnType("text");

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
        });
    }
}
