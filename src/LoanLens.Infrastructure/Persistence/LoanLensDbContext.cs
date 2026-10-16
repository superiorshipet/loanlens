using LoanLens.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoanLens.Infrastructure.Persistence;

public class LoanLensDbContext : DbContext
{
    public LoanLensDbContext(DbContextOptions<LoanLensDbContext> options) : base(options)
    {
    }

    public DbSet<SavedScenarioEntity> SavedScenarios => Set<SavedScenarioEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SavedScenarioEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.RiskCategory).HasMaxLength(50);
            entity.Property(e => e.Verdict).HasMaxLength(50);
            entity.Property(e => e.VerdictHeadline).HasMaxLength(250);
            entity.Property(e => e.ReportJson).HasColumnType("TEXT");

            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
