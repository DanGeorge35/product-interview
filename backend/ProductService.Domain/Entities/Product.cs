using ProductService.Domain.Common;
using ProductService.Domain.Events;

namespace ProductService.Domain.Entities;

public sealed class Product : AggregateRoot
{
    private Product() { } // EF Core constructor

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Colour Colour { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; private set; }

    public static Product Create(
        string name,
        string? description,
        Colour colour,
        decimal price,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        if (name.Length > 200)
            throw new DomainException("Name must not exceed 200 characters.");

        if (description?.Length > 1000)
            throw new DomainException("Description must not exceed 1000 characters.");

        if (price <= 0)
            throw new DomainException("Price must be positive.");

        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Colour = colour,
            Price = price,
            StockQuantity = stockQuantity,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            product.Id,
            product.Name,
            product.Colour,
            product.Price));

        return product;
    }

    public void UpdateStock(int quantity)
    {
        if (quantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        StockQuantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
