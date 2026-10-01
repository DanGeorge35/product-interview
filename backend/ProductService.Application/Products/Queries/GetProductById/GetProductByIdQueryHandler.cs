using MediatR;
using ProductService.Application.Products.DTOs;
using ProductService.Domain.Repositories;

namespace ProductService.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(request.Id, ct);
        return product is null ? null : ProductDto.FromEntity(product);
    }
}
