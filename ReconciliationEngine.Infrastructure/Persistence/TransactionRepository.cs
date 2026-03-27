using Microsoft.EntityFrameworkCore;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Application.Data;

namespace ReconciliationEngine.Infrastructure.Persistence;

public class TransactionRepository : ITransactionRepository
{
    private readonly ReconciliationDbContext _context;

    public TransactionRepository(ReconciliationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetBySourceAndExternalIdAsync(string source, string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .FirstOrDefaultAsync(t => t.Source == source && t.ExternalId == externalId, cancellationToken);
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        return transaction;
    }

    public async Task<bool> ExistsAsync(string source, string externalId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AnyAsync(t => t.Source == source && t.ExternalId == externalId, cancellationToken);
    }
}
