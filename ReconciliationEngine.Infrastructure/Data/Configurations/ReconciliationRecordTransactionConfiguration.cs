using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Infrastructure.Data.Configurations;

public class ReconciliationRecordTransactionConfiguration : IEntityTypeConfiguration<ReconciliationRecordTransaction>
{
    public void Configure(EntityTypeBuilder<ReconciliationRecordTransaction> builder)
    {
        builder.ToTable("ReconciliationRecordTransactions");

        builder.HasKey(rt => new { rt.ReconciliationRecordId, rt.TransactionId });
    }
}
