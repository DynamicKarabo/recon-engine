using Microsoft.EntityFrameworkCore;
using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Application.Data;

public class ReconciliationDbContext : DbContext
{
    public ReconciliationDbContext(DbContextOptions<ReconciliationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ReconciliationRecord> ReconciliationRecords => Set<ReconciliationRecord>();
    public DbSet<ReconciliationRecordTransaction> ReconciliationRecordTransactions => Set<ReconciliationRecordTransaction>();
    public DbSet<ExceptionRecord> ExceptionRecords => Set<ExceptionRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MatchingRule> MatchingRules => Set<MatchingRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ReconciliationRecordTransaction>(entity =>
        {
            entity.HasKey(rt => new { rt.ReconciliationRecordId, rt.TransactionId });
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReconciliationDbContext).Assembly);
    }
}
