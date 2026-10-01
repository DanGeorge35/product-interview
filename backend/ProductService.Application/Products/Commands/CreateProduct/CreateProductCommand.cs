using MediatR;
using ProductService.Application.Products.DTOs;
using ProductService.Domain.Common;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    string Colour,
    decimal Price,
    int StockQuantity
) : IRequest<Result<ProductDto>>;
