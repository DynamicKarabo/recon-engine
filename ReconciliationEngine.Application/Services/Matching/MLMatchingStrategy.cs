using Microsoft.Extensions.Logging;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Application.Services.Matching;

public class MLMatchingStrategy : IMatchingStrategy
{
    private readonly IMLServiceClient _mlClient;
    private readonly ILogger<MLMatchingStrategy> _logger;

    public MLMatchingStrategy(
        IMLServiceClient mlClient,
        ILogger<MLMatchingStrategy>? logger = null)
    {
        _mlClient = mlClient;
        _logger = logger ?? new Microsoft.Extensions.Logging.Abstractions.NullLogger<MLMatchingStrategy>();
    }

    public MatchResult? TryMatch(Transaction transaction, IEnumerable<Transaction> candidates)
    {
        var candidateList = candidates
            .Where(c => c.Id != transaction.Id)
            .Where(c => string.Equals(c.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            .Where(c => Math.Abs((c.Amount - transaction.Amount) / transaction.Amount) <= 20m / 100m)
            .ToList();

        if (candidateList.Count == 0)
        {
            _logger.LogDebug("No candidates within ±20% amount tolerance and same currency for transaction {Id}", transaction.Id);
            return null;
        }

        var scores = _mlClient.GetBatchMatchScoresAsync(transaction, candidateList)
            .GetAwaiter().GetResult();

        if (scores.Count == 0)
        {
            _logger.LogDebug("ML service returned no scores for transaction {Id}", transaction.Id);
            return null;
        }

        var bestScore = scores
            .Where(s => s.Value >= 0.85m)
            .OrderByDescending(s => s.Value)
            .FirstOrDefault();

        if (bestScore.Key == Guid.Empty)
        {
            _logger.LogDebug("No candidate above confidence threshold for transaction {Id}", transaction.Id);
            return null;
        }

        var bestCandidate = candidateList.First(c => c.Id == bestScore.Key);
        var confidenceDecimal = bestScore.Value;

        _logger.LogInformation(
            "ML match found for transaction {Id} with candidate {CandidateId}, confidence: {Confidence}",
            transaction.Id, bestCandidate.Id, confidenceDecimal);

        var reconciliationRecord = ReconciliationRecord.Create(
            MatchMethod.ML,
            confidenceDecimal,
            null);

        var allTransactions = new List<Transaction> { transaction, bestCandidate };
        foreach (var t in allTransactions)
        {
            reconciliationRecord.AddTransaction(t.Id);
        }

        return new MatchResult
        {
            ReconciliationRecord = reconciliationRecord,
            MatchedTransactions = allTransactions,
            ConfidenceScore = confidenceDecimal
        };
    }
}
