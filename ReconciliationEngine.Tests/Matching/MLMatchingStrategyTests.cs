using FluentAssertions;
using Moq;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Application.Services.Matching;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;
using Xunit;

namespace ReconciliationEngine.Tests.Matching;

public class MLMatchingStrategyTests
{
    private readonly Mock<IMLServiceClient> _mlClientMock;
    private readonly MLMatchingStrategy _strategy;

    public MLMatchingStrategyTests()
    {
        _mlClientMock = new Mock<IMLServiceClient>();
        _strategy = new MLMatchingStrategy(_mlClientMock.Object);
    }

    [Fact]
    public void TryMatch_WhenCandidatesExistAndScoreAboveThreshold_ShouldReturnMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Payment for invoice", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 110.00m, "USD", DateTime.UtcNow.Date,
            "Payment for services", "REF-002", "ACC-002", "user");

        _mlClientMock
            .Setup(m => m.GetBatchMatchScoresAsync(transaction, It.IsAny<IEnumerable<Transaction>>(), default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { candidate.Id, 0.92m }
            });

        var result = _strategy.TryMatch(transaction, new[] { candidate });

        result.Should().NotBeNull();
        result!.MatchedTransactions.Should().HaveCount(2);
        result.ConfidenceScore.Should().Be(0.92m);
        result.ReconciliationRecord.MatchMethod.Should().Be(MatchMethod.ML);
    }

    [Fact]
    public void TryMatch_WhenAllScoresBelowThreshold_ShouldReturnNull()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 110.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        _mlClientMock
            .Setup(m => m.GetBatchMatchScoresAsync(transaction, It.IsAny<IEnumerable<Transaction>>(), default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { candidate.Id, 0.45m }
            });

        var result = _strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_WhenMLServiceReturnsNoScores_ShouldReturnNull()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 110.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        _mlClientMock
            .Setup(m => m.GetBatchMatchScoresAsync(transaction, It.IsAny<IEnumerable<Transaction>>(), default))
            .ReturnsAsync(new Dictionary<Guid, decimal>());

        var result = _strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_WhenNoCandidatesWithinAmountTolerance_ShouldReturnNull()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 200.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        var result = _strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
        _mlClientMock.Verify(
            m => m.GetBatchMatchScoresAsync(It.IsAny<Transaction>(), It.IsAny<IEnumerable<Transaction>>(), default),
            Times.Never);
    }

    [Fact]
    public void TryMatch_WhenDifferentCurrency_ShouldReturnNull()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 105.00m, "EUR", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        var result = _strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void TryMatch_WhenPickBestScore_ShouldReturnHighestScoreMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate1 = Transaction.Create(
            "SourceB", "EXT-002", 105.00m, "USD", DateTime.UtcNow.Date,
            "Desc1", "REF-002", "ACC-002", "user");

        var candidate2 = Transaction.Create(
            "SourceC", "EXT-003", 110.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-003", "ACC-003", "user");

        _mlClientMock
            .Setup(m => m.GetBatchMatchScoresAsync(transaction, It.IsAny<IEnumerable<Transaction>>(), default))
            .ReturnsAsync(new Dictionary<Guid, decimal>
            {
                { candidate1.Id, 0.88m },
                { candidate2.Id, 0.95m }
            });

        var result = _strategy.TryMatch(transaction, new[] { candidate1, candidate2 });

        result.Should().NotBeNull();
        result!.MatchedTransactions.Should().Contain(t => t.Id == candidate2.Id);
        result.ConfidenceScore.Should().Be(0.95m);
    }
}
