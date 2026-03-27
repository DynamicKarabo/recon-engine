using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetBySourceAndExternalIdAsync(string source, string externalId, CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string source, string externalId, CancellationToken cancellationToken = default);
}
