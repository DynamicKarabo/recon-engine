namespace ReconciliationEngine.Application.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string? previousState,
        string newState,
        string performedBy,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}
