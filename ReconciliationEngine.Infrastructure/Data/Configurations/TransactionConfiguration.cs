using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.ExternalId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 4);

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Reference)
            .HasMaxLength(255);

        builder.Property(t => t.AccountId)
            .HasMaxLength(255);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.IngestedBy)
            .HasMaxLength(100);

        builder.HasIndex(t => t.Status);

        builder.HasIndex(t => t.TransactionDate);

        builder.HasIndex(t => new { t.Source, t.ExternalId })
            .IsUnique();
    }
}
