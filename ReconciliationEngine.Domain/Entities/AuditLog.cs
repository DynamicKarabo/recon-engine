using ReconciliationEngine.Domain.Common;

namespace ReconciliationEngine.Domain.Entities;

public class AuditLog : Entity
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? PreviousState { get; private set; }
    public string NewState { get; private set; } = string.Empty;
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime PerformedAt { get; private set; }
    public Guid CorrelationId { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string entityType,
        Guid entityId,
        string action,
        string? previousState,
        string newState,
        string performedBy,
        Guid correlationId)
    {
        return new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PreviousState = previousState,
            NewState = newState,
            PerformedBy = performedBy,
            PerformedAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };
    }
}
