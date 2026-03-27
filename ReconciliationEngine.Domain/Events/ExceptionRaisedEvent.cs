namespace ReconciliationEngine.Domain.Events;

public class ExceptionRaisedEvent : DomainEvent
{
    public Guid ExceptionRecordId { get; }
    public Guid TransactionId { get; }
    public string Category { get; }
    public Guid CorrelationId { get; }

    public ExceptionRaisedEvent(
        Guid exceptionRecordId,
        Guid transactionId,
        string category,
        Guid correlationId)
    {
        ExceptionRecordId = exceptionRecordId;
        TransactionId = transactionId;
        Category = category;
        CorrelationId = correlationId;
    }
}
