using System.Text.Json;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Application.Services.Matching;

public class RuleBasedMatchingStrategy : IMatchingStrategy
{
    private readonly IMatchingRuleCache _ruleCache;

    public RuleBasedMatchingStrategy(IMatchingRuleCache ruleCache)
    {
        _ruleCache = ruleCache;
    }

    public MatchResult? TryMatch(Transaction transaction, IEnumerable<Transaction> candidates)
    {
        var rules = _ruleCache.GetRules();
        
        foreach (var rule in rules)
        {
            var config = ParseConfig(rule.ConfigJson);
            var matchedCandidate = EvaluateRule(transaction, candidates, rule.Id.ToString(), config);
            
            if (matchedCandidate != null)
            {
                var reconciliationRecord = ReconciliationRecord.Create(
                    MatchMethod.RuleBased,
                    1.0m,
                    rule.Id.ToString());

                var allTransactions = new List<Transaction> { transaction, matchedCandidate };
                
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

        return null;
    }

    private static Transaction? EvaluateRule(Transaction transaction, IEnumerable<Transaction> candidates, string ruleId, Dictionary<string, object>? config)
    {
        var candidateList = candidates.ToList();
        
        return ruleId.ToLowerInvariant() switch
        {
            "rule-amount-tolerance" => EvaluateAmountToleranceRule(transaction, candidateList, config),
            "rule-reference-prefix" => EvaluateReferencePrefixRule(transaction, candidateList, config),
            "rule-date-range" => EvaluateDateRangeRule(transaction, candidateList, config),
            _ => null
        };
    }

    private static Transaction? EvaluateAmountToleranceRule(Transaction transaction, List<Transaction> candidates, Dictionary<string, object>? config)
    {
        var tolerance = config?.GetValueOrDefault("tolerance") is JsonElement { ValueKind: JsonValueKind.Number } toleranceElement 
            ? toleranceElement.GetDecimal() 
            : 0.01m;

        return candidates
            .FirstOrDefault(c => 
                c.Id != transaction.Id &&
                Math.Abs(c.Amount - transaction.Amount) <= tolerance &&
                string.Equals(c.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase));
    }

    private static Transaction? EvaluateReferencePrefixRule(Transaction transaction, List<Transaction> candidates, Dictionary<string, object>? config)
    {
        var prefix = config?.GetValueOrDefault("prefix") is JsonElement { ValueKind: JsonValueKind.String } prefixElement 
            ? prefixElement.GetString() 
            : null;

        if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(transaction.Reference))
            return null;

        return candidates
            .FirstOrDefault(c => 
                c.Id != transaction.Id &&
                c.Amount == transaction.Amount &&
                string.Equals(c.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase) &&
                c.Reference != null &&
                c.Reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static Transaction? EvaluateDateRangeRule(Transaction transaction, List<Transaction> candidates, Dictionary<string, object>? config)
    {
        var daysBefore = config?.GetValueOrDefault("daysBefore") is JsonElement { ValueKind: JsonValueKind.Number } daysBeforeElement 
            ? daysBeforeElement.GetInt32() 
            : 3;
        var daysAfter = config?.GetValueOrDefault("daysAfter") is JsonElement { ValueKind: JsonValueKind.Number } daysAfterElement 
            ? daysAfterElement.GetInt32() 
            : 3;

        var startDate = transaction.TransactionDate.AddDays(-daysBefore);
        var endDate = transaction.TransactionDate.AddDays(daysAfter);

        return candidates
            .FirstOrDefault(c => 
                c.Id != transaction.Id &&
                c.Amount == transaction.Amount &&
                string.Equals(c.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase) &&
                c.TransactionDate >= startDate &&
                c.TransactionDate <= endDate);
    }

    private static Dictionary<string, object>? ParseConfig(string? configJson)
    {
        if (string.IsNullOrEmpty(configJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
        }
        catch
        {
            return null;
        }
    }
}
