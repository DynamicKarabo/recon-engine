using FluentAssertions;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;
using Xunit;

namespace ReconciliationEngine.Tests.Domain;

public class EntityTimestampTests
{
    [Fact]
    public void Transaction_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var transaction = Transaction.Create(
            "BankFeedA",
            "EXT-001",
            100.50m,
            "USD",
            DateTime.Today,
            "Test description",
            "REF-001",
            "ACC-001",
            "test-user");
        var after = DateTime.UtcNow;

        transaction.CreatedAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        transaction.IngestedAt.Kind.Should().Be(DateTimeKind.Utc);
        transaction.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ReconciliationRecord_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var record = ReconciliationRecord.Create(
            MatchMethod.Exact,
            1.0m,
            null);
        var after = DateTime.UtcNow;

        record.CreatedAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        record.MatchedAt.Kind.Should().Be(DateTimeKind.Utc);
        record.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ExceptionRecord_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var exceptionRecord = ExceptionRecord.Create(
            Guid.NewGuid(),
            ExceptionCategory.Unmatched);
        var after = DateTime.UtcNow;

        exceptionRecord.CreatedAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        exceptionRecord.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void AuditLog_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var auditLog = AuditLog.Create(
            "Transaction",
            Guid.NewGuid(),
            "Ingested",
            null,
            "{}",
            "test-user",
            Guid.NewGuid());
        var after = DateTime.UtcNow;

        auditLog.CreatedAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        auditLog.PerformedAt.Kind.Should().Be(DateTimeKind.Utc);
        auditLog.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void MatchingRule_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var rule = MatchingRule.Create(
            "00000000-0000-0000-0000-000000000001",
            "Test rule",
            1,
            "{}");
        var after = DateTime.UtcNow;

        rule.CreatedAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        rule.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }
}
