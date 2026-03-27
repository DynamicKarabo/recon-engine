using ReconciliationEngine.Domain.Common;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.Domain.Entities;

public class ExceptionRecord : Entity
{
    public Guid TransactionId { get; private set; }
    public ExceptionCategory Category { get; private set; }
    public ExceptionStatus Status { get; private set; }
    public string? AssignedTo { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private ExceptionRecord() { }

    public static ExceptionRecord Create(
        Guid transactionId,
        ExceptionCategory category)
    {
        return new ExceptionRecord
        {
            TransactionId = transactionId,
            Category = category,
            Status = ExceptionStatus.PendingReview
        };
    }

    public void AssignTo(string userId)
    {
        AssignedTo = userId;
        Status = ExceptionStatus.UnderReview;
    }

    public void Resolve(string? notes)
    {
        Notes = notes;
        Status = ExceptionStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Dismiss(string? notes)
    {
        Notes = notes;
        Status = ExceptionStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
    }
}
