using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.PreviousState)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.NewState)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.PerformedBy)
            .HasMaxLength(255);

        builder.HasIndex(a => new { a.EntityId, a.EntityType });

        builder.HasIndex(a => a.CorrelationId);
    }
}
