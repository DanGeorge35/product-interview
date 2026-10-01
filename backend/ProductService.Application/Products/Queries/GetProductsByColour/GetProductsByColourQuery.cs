using MediatR;
using ProductService.Application.Products.DTOs;
using ProductService.Domain.Common;

namespace ProductService.Application.Products.Queries.GetProductsByColour;

public sealed record GetProductsByColourQuery(string Colour) : IRequest<Result<IReadOnlyList<ProductDto>>>;
