using Microsoft.Extensions.Logging;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Events;

namespace ProductService.Infrastructure.Messaging;

/// <summary>
/// No-op event publisher — logs event details. In production, replace with
/// MassTransit/Azure Service Bus/Kafka adapter.
/// </summary>
public sealed class NoOpEventPublisher(ILogger<NoOpEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EventBus] Publishing {EventType} (EventId: {EventId}, OccurredAt: {OccurredAt})",
            domainEvent.GetType().Name,
            domainEvent.EventId,
            domainEvent.OccurredAt);

        return Task.CompletedTask;
    }
}
