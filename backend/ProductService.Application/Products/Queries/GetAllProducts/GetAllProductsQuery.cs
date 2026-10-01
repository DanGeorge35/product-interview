using MediatR;
using ProductService.Application.Products.DTOs;

namespace ProductService.Application.Products.Queries.GetAllProducts;

public sealed record GetAllProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
