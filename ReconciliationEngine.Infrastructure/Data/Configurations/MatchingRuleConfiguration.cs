using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Infrastructure.Data.Configurations;

public class MatchingRuleConfiguration : IEntityTypeConfiguration<MatchingRule>
{
    public void Configure(EntityTypeBuilder<MatchingRule> builder)
    {
        builder.ToTable("MatchingRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.IsActive);

        builder.Property(r => r.ConfigJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(r => r.Priority);

        builder.HasIndex(r => r.IsActive);
    }
}
