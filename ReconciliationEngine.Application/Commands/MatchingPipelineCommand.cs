using MediatR;

namespace ReconciliationEngine.Application.Commands;

public class MatchingPipelineCommand : IRequest<MatchingPipelineResult>
{
    public Guid TransactionId { get; set; }
    public Guid CorrelationId { get; set; }
}

public class MatchingPipelineResult
{
    public bool IsMatched { get; set; }
    public Guid? ReconciliationRecordId { get; set; }
    public Guid? ExceptionRecordId { get; set; }
}
