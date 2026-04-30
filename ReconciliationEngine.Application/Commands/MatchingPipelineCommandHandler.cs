using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReconciliationEngine.Application.Data;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Application.Services.Matching;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;
using ReconciliationEngine.Domain.Events;

namespace ReconciliationEngine.Application.Commands;

public class MatchingPipelineCommandHandler : IRequestHandler<MatchingPipelineCommand, MatchingPipelineResult>
{
    private readonly ReconciliationDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMatchingRuleCache _ruleCache;
    private readonly IMLServiceClient _mlClient;

    public MatchingPipelineCommandHandler(
        ReconciliationDbContext context,
        IEventPublisher eventPublisher,
        IMatchingRuleCache ruleCache,
        IMLServiceClient mlClient)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _ruleCache = ruleCache;
        _mlClient = mlClient;
    }

    public async Task<MatchingPipelineResult> Handle(MatchingPipelineCommand request, CancellationToken cancellationToken)
    {
        var transaction = await _context.Transactions.FindAsync(new object[] { request.TransactionId }, cancellationToken);
        
        if (transaction == null)
            throw new InvalidOperationException($"Transaction {request.TransactionId} not found");

        if (transaction.Status == TransactionStatus.Matched || transaction.Status == TransactionStatus.Exception)
        {
            return new MatchingPipelineResult
            {
                IsMatched = transaction.Status == TransactionStatus.Matched,
                ReconciliationRecordId = transaction.Status == TransactionStatus.Matched ? transaction.Id : null
            };
        }

        var candidates = await _context.Transactions
            .Where(t => t.Status == TransactionStatus.Pending)
            .Where(t => t.Id != transaction.Id)
            .ToListAsync(cancellationToken);

        var strategies = new List<IMatchingStrategy>
        {
            new ExactMatchingStrategy(),
            new FuzzyMatchingStrategy(),
            new MLMatchingStrategy(_mlClient),
            new RuleBasedMatchingStrategy(_ruleCache)
        };

        foreach (var strategy in strategies)
        {
            var result = strategy.TryMatch(transaction, candidates);
            
            if (result != null)
            {
                if (result.MatchedTransactions.Count > 1)
                {
                    var matchedTxIds = result.MatchedTransactions.Select(t => t.Id).ToList();
                    var matchedTransactions = await _context.Transactions
                        .Where(t => matchedTxIds.Contains(t.Id))
                        .ToListAsync(cancellationToken);

                    foreach (var matched in matchedTransactions)
                    {
                        matched.MarkAsMatched();
                    }

                    _context.ReconciliationRecords.Add(result.ReconciliationRecord);
                    await _context.SaveChangesAsync(cancellationToken);

                    var domainEvent = new TransactionMatchedEvent(
                        result.ReconciliationRecord.Id,
                        matchedTxIds,
                        result.ReconciliationRecord.MatchMethod.ToString(),
                        result.ConfidenceScore,
                        request.CorrelationId);

                    await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

                    return new MatchingPipelineResult
                    {
                        IsMatched = true,
                        ReconciliationRecordId = result.ReconciliationRecord.Id
                    };
                }
            }
        }

        var exceptionRecord = ExceptionRecord.Create(transaction.Id, ExceptionCategory.Unmatched);
        _context.ExceptionRecords.Add(exceptionRecord);
        
        transaction.MarkAsException();
        
        var transactionState = JsonSerializer.Serialize(new
        {
            transaction.Id,
            transaction.Status
        });

        var exceptionState = JsonSerializer.Serialize(new
        {
            exceptionRecord.Id,
            exceptionRecord.Category,
            exceptionRecord.Status
        });

        var auditLog = Domain.Entities.AuditLog.Create(
            "Transaction",
            transaction.Id,
            "ExceptionRaised",
            transactionState,
            exceptionState,
            "MatchingPipeline",
            request.CorrelationId);

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);

        var exceptionEvent = new ExceptionRaisedEvent(
            exceptionRecord.Id,
            transaction.Id,
            ExceptionCategory.Unmatched.ToString(),
            request.CorrelationId);

        await _eventPublisher.PublishAsync(exceptionEvent, cancellationToken);

        return new MatchingPipelineResult
        {
            IsMatched = false,
            ExceptionRecordId = exceptionRecord.Id
        };
    }
}
