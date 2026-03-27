using System.Text.Json;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Application.Data;

namespace ReconciliationEngine.Infrastructure.Persistence;

public class AuditLogger : IAuditLogger
{
    private readonly ReconciliationDbContext _context;

    public AuditLogger(ReconciliationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string? previousState,
        string newState,
        string performedBy,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.Create(
            entityType,
            entityId,
            action,
            previousState,
            newState,
            performedBy,
            correlationId);

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
