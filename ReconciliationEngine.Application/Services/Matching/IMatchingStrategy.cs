using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Application.Services.Matching;

public interface IMatchingStrategy
{
    MatchResult? TryMatch(Transaction transaction, IEnumerable<Transaction> candidates);
}

public class MatchResult
{
    public required ReconciliationRecord ReconciliationRecord { get; init; }
    public required List<Transaction> MatchedTransactions { get; init; }
    public decimal? ConfidenceScore { get; init; }
}
