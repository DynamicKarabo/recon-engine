namespace ReconciliationEngine.Domain.Events;

public class TransactionIngestedEvent : DomainEvent
{
    public Guid TransactionId { get; }
    public string Source { get; }
    public Guid CorrelationId { get; }

    public TransactionIngestedEvent(Guid transactionId, string source, Guid correlationId)
    {
        TransactionId = transactionId;
        Source = source;
        CorrelationId = correlationId;
    }
}
