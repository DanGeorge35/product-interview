using MediatR;
using ProductService.Application.Products.DTOs;
using ProductService.Domain.Repositories;

namespace ProductService.Application.Products.Queries.GetAllProducts;

public sealed class GetAllProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken ct)
    {
        var products = await repository.GetAllAsync(ct);
        return products.Select(ProductDto.FromEntity).ToList();
    }
}
