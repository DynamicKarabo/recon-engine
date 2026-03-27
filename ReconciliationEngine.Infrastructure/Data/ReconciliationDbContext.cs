using Microsoft.EntityFrameworkCore;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Infrastructure.Data.Configurations;

namespace ReconciliationEngine.Infrastructure.Data;

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

        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationRecordConfiguration());
        modelBuilder.ApplyConfiguration(new ReconciliationRecordTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ExceptionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new MatchingRuleConfiguration());
    }
}
