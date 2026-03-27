using ReconciliationEngine.Domain.Common;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Domain.Entities;

public class Transaction : Entity
{
    public string Source { get; private set; } = string.Empty;
    public string ExternalId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime TransactionDate { get; private set; }
    public string? Description { get; private set; }
    public string? Reference { get; private set; }
    public string? AccountId { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime IngestedAt { get; private set; }
    public string IngestedBy { get; private set; } = string.Empty;

    private Transaction() { }

    public static Transaction Create(
        string source,
        string externalId,
        decimal amount,
        string currency,
        DateTime transactionDate,
        string? description,
        string? reference,
        string? accountId,
        string ingestedBy)
    {
        return new Transaction
        {
            Source = source,
            ExternalId = externalId,
            Amount = amount,
            Currency = currency,
            TransactionDate = transactionDate,
            Description = description,
            Reference = reference,
            AccountId = accountId,
            Status = TransactionStatus.Pending,
            IngestedAt = DateTime.UtcNow,
            IngestedBy = ingestedBy
        };
    }

    public void MarkAsMatched()
    {
        Status = TransactionStatus.Matched;
    }

    public void MarkAsException()
    {
        Status = TransactionStatus.Exception;
    }
}
