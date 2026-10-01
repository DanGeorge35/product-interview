using ProductService.Domain.Entities;

namespace ProductService.Application.Products.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Colour,
    decimal Price,
    int StockQuantity,
    DateTimeOffset CreatedAt)
{
    public static ProductDto FromEntity(Product p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Colour.ToString(),
        p.Price,
        p.StockQuantity,
        p.CreatedAt);
}
