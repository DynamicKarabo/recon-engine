using MediatR;

namespace ReconciliationEngine.Application.Commands;

public class IngestTransactionCommand : IRequest<IngestTransactionResult>
{
    public string Source { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public string? Reference { get; set; }
    public string? AccountId { get; set; }
}

public class IngestTransactionResult
{
    public Guid TransactionId { get; set; }
    public bool IsDuplicate { get; set; }
}
