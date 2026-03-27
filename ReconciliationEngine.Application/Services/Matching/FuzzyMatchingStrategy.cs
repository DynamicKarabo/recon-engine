using FuzzySharp;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Application.Services.Matching;

public class FuzzyMatchingStrategy : IMatchingStrategy
{
    private const decimal DefaultThreshold = 0.92m;
    private const int DateToleranceDays = 1;

    public MatchResult? TryMatch(Transaction transaction, IEnumerable<Transaction> candidates)
    {
        var candidateList = candidates.ToList();
        
        var potentialMatches = candidateList
            .Where(c => c.Id != transaction.Id)
            .Where(c => c.Amount == transaction.Amount)
            .Where(c => string.Equals(c.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            .Where(c => Math.Abs((c.TransactionDate - transaction.TransactionDate).TotalDays) <= DateToleranceDays)
            .ToList();

        if (!potentialMatches.Any())
            return null;

        var scoredMatches = potentialMatches
            .Select(c => new
            {
                Transaction = c,
                Score = CalculateSimilarityScore(transaction, c)
            })
            .Where(x => x.Score >= DefaultThreshold)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scoredMatches.Count == 0)
            return null;

        if (scoredMatches.Count > 1)
        {
            var topScore = scoredMatches[0].Score;
            if (scoredMatches.Any(x => x.Score == topScore && x.Transaction.Id != scoredMatches[0].Transaction.Id))
            {
                return null;
            }
        }

        var bestMatch = scoredMatches[0];

        var reconciliationRecord = ReconciliationRecord.Create(
            MatchMethod.Fuzzy,
            bestMatch.Score,
            null);

        var allTransactions = new List<Transaction> { transaction, bestMatch.Transaction };
        
        foreach (var t in allTransactions)
        {
            reconciliationRecord.AddTransaction(t.Id);
        }

        return new MatchResult
        {
            ReconciliationRecord = reconciliationRecord,
            MatchedTransactions = allTransactions,
            ConfidenceScore = bestMatch.Score
        };
    }

    private static decimal CalculateSimilarityScore(Transaction t1, Transaction t2)
    {
        var referenceScore = CalculateFieldSimilarity(t1.Reference, t2.Reference);
        var descriptionScore = CalculateFieldSimilarity(t1.Description, t2.Description);

        return (referenceScore + descriptionScore) / 2m;
    }

    private static decimal CalculateFieldSimilarity(string? field1, string? field2)
    {
        if (string.IsNullOrWhiteSpace(field1) && string.IsNullOrWhiteSpace(field2))
            return 1.0m;

        if (string.IsNullOrWhiteSpace(field1) || string.IsNullOrWhiteSpace(field2))
            return 0.0m;

        var similarity = Fuzz.WeightedRatio(field1.Trim(), field2.Trim());
        return similarity / 100.0m;
    }
}
