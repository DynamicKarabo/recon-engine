using ReconciliationEngine.Domain.Events;

namespace ReconciliationEngine.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(TransactionIngestedEvent @event, CancellationToken cancellationToken = default);
    Task PublishAsync(TransactionMatchedEvent @event, CancellationToken cancellationToken = default);
    Task PublishAsync(ExceptionRaisedEvent @event, CancellationToken cancellationToken = default);
}
