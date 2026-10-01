using ProductService.Domain.Entities;

namespace ProductService.Domain.Events;

public sealed record ProductCreatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ProductId,
    string Name,
    Colour Colour,
    decimal Price
) : IDomainEvent;
