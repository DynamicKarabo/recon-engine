using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Application.Interfaces;

public interface IMLServiceClient
{
    /// <summary>
    /// Get match probability for a pair of transactions.
    /// Returns a confidence score between 0 and 1.
    /// Returns null if service is unavailable.
    /// </summary>
    Task<decimal?> GetMatchScoreAsync(
        Transaction tx1,
        Transaction tx2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get match probabilities for multiple candidate pairs against a source transaction.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetBatchMatchScoresAsync(
        Transaction source,
        IEnumerable<Transaction> candidates,
        CancellationToken cancellationToken = default);
}
