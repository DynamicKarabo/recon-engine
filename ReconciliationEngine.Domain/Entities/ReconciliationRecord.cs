using ReconciliationEngine.Domain.Common;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Domain.Entities;

public class ReconciliationRecord : Entity
{
    public DateTime MatchedAt { get; private set; }
    public MatchMethod MatchMethod { get; private set; }
    public decimal? ConfidenceScore { get; private set; }
    public string? RuleId { get; private set; }

    private readonly List<ReconciliationRecordTransaction> _transactions = new();
    public IReadOnlyCollection<ReconciliationRecordTransaction> Transactions => _transactions.AsReadOnly();

    private ReconciliationRecord() { }

    public static ReconciliationRecord Create(
        MatchMethod matchMethod,
        decimal? confidenceScore,
        string? ruleId)
    {
        return new ReconciliationRecord
        {
            MatchedAt = DateTime.UtcNow,
            MatchMethod = matchMethod,
            ConfidenceScore = confidenceScore,
            RuleId = ruleId
        };
    }

    public void AddTransaction(Guid transactionId)
    {
        _transactions.Add(ReconciliationRecordTransaction.Create(Id, transactionId));
    }
}
