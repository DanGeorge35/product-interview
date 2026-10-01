using MassTransit;
using Microsoft.Extensions.Logging;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Events;

namespace ProductService.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to Azure Service Bus via MassTransit.
/// Each domain event type is published to its own topic (MassTransit convention).
/// </summary>
public sealed class AzureServiceBusEventPublisher(
    IPublishEndpoint publishEndpoint,
    ILogger<AzureServiceBusEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        logger.LogInformation(
            "Publishing {EventType} (EventId: {EventId}) to Azure Service Bus",
            domainEvent.GetType().Name,
            domainEvent.EventId);

        await publishEndpoint.Publish((dynamic)domainEvent, ct);

        logger.LogInformation(
            "Published {EventType} (EventId: {EventId}) successfully",
            domainEvent.GetType().Name,
            domainEvent.EventId);
    }
}
