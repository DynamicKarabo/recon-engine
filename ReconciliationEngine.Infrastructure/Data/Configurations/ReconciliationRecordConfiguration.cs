using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Infrastructure.Data.Configurations;

public class ReconciliationRecordConfiguration : IEntityTypeConfiguration<ReconciliationRecord>
{
    public void Configure(EntityTypeBuilder<ReconciliationRecord> builder)
    {
        builder.ToTable("ReconciliationRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.MatchMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(r => r.ConfidenceScore)
            .HasPrecision(5, 4);

        builder.Property(r => r.RuleId)
            .HasMaxLength(100);

        builder.HasMany(r => r.Transactions)
            .WithOne()
            .HasForeignKey(rt => rt.ReconciliationRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
