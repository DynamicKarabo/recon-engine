using FluentAssertions;
using ReconciliationEngine.Domain.Events;
using Xunit;

namespace ReconciliationEngine.Tests.Domain;

public class DomainEventTests
{
    [Fact]
    public void TransactionIngestedEvent_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var @event = new TransactionIngestedEvent(
            Guid.NewGuid(),
            "BankFeedA",
            Guid.NewGuid());
        var after = DateTime.UtcNow;

        @event.OccurredAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        @event.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void TransactionIngestedEvent_ShouldHaveValidProperties()
    {
        var transactionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var @event = new TransactionIngestedEvent(transactionId, "BankFeedA", correlationId);

        @event.EventId.Should().NotBeEmpty();
        @event.TransactionId.Should().Be(transactionId);
        @event.Source.Should().Be("BankFeedA");
        @event.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void TransactionMatchedEvent_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var @event = new TransactionMatchedEvent(
            Guid.NewGuid(),
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            "Exact",
            1.0m,
            Guid.NewGuid());
        var after = DateTime.UtcNow;

        @event.OccurredAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        @event.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void TransactionMatchedEvent_ShouldHaveValidProperties()
    {
        var reconciliationRecordId = Guid.NewGuid();
        var transactionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var correlationId = Guid.NewGuid();
        var @event = new TransactionMatchedEvent(
            reconciliationRecordId,
            transactionIds,
            "Fuzzy",
            0.95m,
            correlationId);

        @event.EventId.Should().NotBeEmpty();
        @event.ReconciliationRecordId.Should().Be(reconciliationRecordId);
        @event.TransactionIds.Should().BeEquivalentTo(transactionIds);
        @event.MatchMethod.Should().Be("Fuzzy");
        @event.ConfidenceScore.Should().Be(0.95m);
        @event.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void ExceptionRaisedEvent_ShouldInitializeTimestampInUtc()
    {
        var before = DateTime.UtcNow;
        var @event = new ExceptionRaisedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Unmatched",
            Guid.NewGuid());
        var after = DateTime.UtcNow;

        @event.OccurredAt.Should().BeAfter(before.AddSeconds(-1)).And.BeBefore(after.AddSeconds(1));
        @event.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ExceptionRaisedEvent_ShouldHaveValidProperties()
    {
        var exceptionRecordId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var @event = new ExceptionRaisedEvent(
            exceptionRecordId,
            transactionId,
            "Mismatch",
            correlationId);

        @event.EventId.Should().NotBeEmpty();
        @event.ExceptionRecordId.Should().Be(exceptionRecordId);
        @event.TransactionId.Should().Be(transactionId);
        @event.Category.Should().Be("Mismatch");
        @event.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void DomainEvent_ShouldHaveUniqueEventId()
    {
        var event1 = new TransactionIngestedEvent(Guid.NewGuid(), "Source", Guid.NewGuid());
        var event2 = new TransactionIngestedEvent(Guid.NewGuid(), "Source", Guid.NewGuid());

        event1.EventId.Should().NotBe(event2.EventId);
    }
}
