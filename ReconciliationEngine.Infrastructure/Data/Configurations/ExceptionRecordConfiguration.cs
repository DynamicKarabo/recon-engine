using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Infrastructure.Data.Configurations;

public class ExceptionRecordConfiguration : IEntityTypeConfiguration<ExceptionRecord>
{
    public void Configure(EntityTypeBuilder<ExceptionRecord> builder)
    {
        builder.ToTable("ExceptionRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.AssignedTo)
            .HasMaxLength(255);

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(e => e.Status);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
