namespace ReconciliationEngine.Domain.Events;

public class TransactionMatchedEvent : DomainEvent
{
    public Guid ReconciliationRecordId { get; }
    public IReadOnlyCollection<Guid> TransactionIds { get; }
    public string MatchMethod { get; }
    public decimal? ConfidenceScore { get; }
    public Guid CorrelationId { get; }

    public TransactionMatchedEvent(
        Guid reconciliationRecordId,
        IReadOnlyCollection<Guid> transactionIds,
        string matchMethod,
        decimal? confidenceScore,
        Guid correlationId)
    {
        ReconciliationRecordId = reconciliationRecordId;
        TransactionIds = transactionIds;
        MatchMethod = matchMethod;
        ConfidenceScore = confidenceScore;
        CorrelationId = correlationId;
    }
}
