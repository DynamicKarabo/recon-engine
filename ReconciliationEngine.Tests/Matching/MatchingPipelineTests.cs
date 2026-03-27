using FluentAssertions;
using Moq;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Application.Services.Matching;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;
using ReconciliationEngine.Infrastructure.Cache;
using Xunit;

namespace ReconciliationEngine.Tests.Matching;

public class MatchingPipelineTests
{
    [Fact]
    public void ExactMatch_WhenAmountCurrencyDateAndReferenceMatch_ShouldReturnMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-001", "ACC-002", "user");

        var strategy = new ExactMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().NotBeNull();
        result!.MatchedTransactions.Should().HaveCount(2);
        result.ConfidenceScore.Should().Be(1.0m);
    }

    [Fact]
    public void ExactMatch_WhenReferenceCaseDiffers_ShouldMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "ref-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-001", "ACC-002", "user");

        var strategy = new ExactMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().NotBeNull();
    }

    [Fact]
    public void ExactMatch_WhenReferenceHasDifferentWhitespace_ShouldMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "  REF-001  ", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-001", "ACC-002", "user");

        var strategy = new ExactMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().NotBeNull();
    }

    [Fact]
    public void ExactMatch_WhenNoMatch_ShouldReturnNull()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 200.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        var strategy = new ExactMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void FuzzyMatch_WhenReferenceSimilarityAboveThreshold_ShouldMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Payment for invoice", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date,
            "Payment for invoice", "REF-002", "ACC-002", "user");

        var strategy = new FuzzyMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().NotBeNull();
        result!.MatchedTransactions.Should().HaveCount(2);
    }

    [Fact]
    public void FuzzyMatch_WhenDateWithinOneDay_ShouldMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Payment for services", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date.AddDays(1),
            "Payment for services", "REF-002", "ACC-002", "user");

        var strategy = new FuzzyMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().NotBeNull();
    }

    [Fact]
    public void FuzzyMatch_WhenDateOutsideTolerance_ShouldNotMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Payment for services", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date.AddDays(5),
            "Payment for services", "REF-002", "ACC-002", "user");

        var strategy = new FuzzyMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void FuzzyMatch_WhenTwoCandidatesHaveSameScore_ShouldReturnNull()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Invoice payment 12345", "REF-001", "ACC-001", "user");

        var candidate1 = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date,
            "Invoice payment 12345", "REF-002", "ACC-002", "user");

        var candidate2 = Transaction.Create(
            "SourceC", "EXT-003", 100.00m, "USD", DateTime.UtcNow.Date,
            "Invoice payment 12345", "REF-003", "ACC-003", "user");

        var strategy = new FuzzyMatchingStrategy();
        var result = strategy.TryMatch(transaction, new[] { candidate1, candidate2 });

        result.Should().BeNull();
    }

    [Fact]
    public void RuleBasedMatch_WhenRulesExist_ShouldEvaluateInPriorityOrder()
    {
        var rule = MatchingRule.Create("00000000-0000-0000-0000-000000000001", "Amount tolerance rule", 1, "{\"tolerance\": 100}");

        var cache = new Mock<IMatchingRuleCache>();
        cache.Setup(c => c.GetRules()).Returns((IReadOnlyList<MatchingRule>)[rule]);

        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 50.00m, "USD", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        var strategy = new RuleBasedMatchingStrategy(cache.Object);
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void RuleBasedMatch_WhenNoRuleMatches_ShouldReturnNull()
    {
        var rule = MatchingRule.Create("00000000-0000-0000-0000-000000000001", "Amount tolerance rule", 1, "{\"tolerance\": 0.01}");

        var cache = new Mock<IMatchingRuleCache>();
        cache.Setup(c => c.GetRules()).Returns((IReadOnlyList<MatchingRule>)[rule]);

        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-001", "user");

        var candidate = Transaction.Create(
            "SourceB", "EXT-002", 200.00m, "EUR", DateTime.UtcNow.Date,
            "Desc2", "REF-002", "ACC-002", "user");

        var strategy = new RuleBasedMatchingStrategy(cache.Object);
        var result = strategy.TryMatch(transaction, new[] { candidate });

        result.Should().BeNull();
    }

    [Fact]
    public void Pipeline_ShouldShortCircuit_OnFirstMatch()
    {
        var transaction = Transaction.Create(
            "SourceA", "EXT-001", 100.00m, "USD", DateTime.UtcNow.Date,
            "Exact match ref", "REF-001", "ACC-001", "user");

        var exactMatchCandidate = Transaction.Create(
            "SourceB", "EXT-002", 100.00m, "USD", DateTime.UtcNow.Date,
            "Desc", "REF-001", "ACC-002", "user");

        var fuzzyCandidate = Transaction.Create(
            "SourceC", "EXT-003", 100.00m, "USD", DateTime.UtcNow.Date,
            "Similar ref", "REF-002", "ACC-003", "user");

        var strategies = new List<IMatchingStrategy>
        {
            new ExactMatchingStrategy(),
            new FuzzyMatchingStrategy(),
            new RuleBasedMatchingStrategy(new Infrastructure.Cache.MatchingRuleCache())
        };

        IMatchingStrategy? matchedStrategy = null;
        MatchResult? result = null;

        foreach (var strategy in strategies)
        {
            result = strategy.TryMatch(transaction, new[] { exactMatchCandidate, fuzzyCandidate });
            if (result != null)
            {
                matchedStrategy = strategy;
                break;
            }
        }

        matchedStrategy.Should().BeOfType<ExactMatchingStrategy>();
        result.Should().NotBeNull();
        result!.ConfidenceScore.Should().Be(1.0m);
    }
}
