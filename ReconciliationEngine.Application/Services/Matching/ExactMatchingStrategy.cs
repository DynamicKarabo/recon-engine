using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Application.Services.Matching;

public class ExactMatchingStrategy : IMatchingStrategy
{
    public MatchResult? TryMatch(Transaction transaction, IEnumerable<Transaction> candidates)
    {
        var candidateList = candidates.ToList();
        
        var exactMatches = candidateList
            .Where(c => c.Id != transaction.Id)
            .Where(c => c.Amount == transaction.Amount)
            .Where(c => string.Equals(c.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            .Where(c => c.TransactionDate == transaction.TransactionDate)
            .Where(c => string.Equals(
                c.Reference?.Trim(), 
                transaction.Reference?.Trim(), 
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exactMatches.Count == 0)
            return null;

        if (exactMatches.Count > 1)
            return null;

        var matchedTransaction = exactMatches[0];
        
        var reconciliationRecord = ReconciliationRecord.Create(
            MatchMethod.Exact,
            1.0m,
            null);

        var allTransactions = new List<Transaction> { transaction, matchedTransaction };
        
        foreach (var t in allTransactions)
        {
            reconciliationRecord.AddTransaction(t.Id);
        }

        return new MatchResult
        {
            ReconciliationRecord = reconciliationRecord,
            MatchedTransactions = allTransactions,
            ConfidenceScore = 1.0m
        };
    }
}
