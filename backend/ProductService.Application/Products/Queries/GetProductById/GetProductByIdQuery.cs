using MediatR;
using ProductService.Application.Products.DTOs;

namespace ProductService.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
