using ProductService.Domain.Events;

namespace ProductService.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
