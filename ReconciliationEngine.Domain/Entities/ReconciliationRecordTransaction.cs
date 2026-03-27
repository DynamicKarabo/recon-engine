namespace ReconciliationEngine.Domain.Entities;

public class ReconciliationRecordTransaction
{
    public Guid ReconciliationRecordId { get; private set; }
    public Guid TransactionId { get; private set; }

    private ReconciliationRecordTransaction() { }

    public static ReconciliationRecordTransaction Create(Guid reconciliationRecordId, Guid transactionId)
    {
        return new ReconciliationRecordTransaction
        {
            ReconciliationRecordId = reconciliationRecordId,
            TransactionId = transactionId
        };
    }
}
