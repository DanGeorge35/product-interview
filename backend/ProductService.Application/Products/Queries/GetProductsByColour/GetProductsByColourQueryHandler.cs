using MediatR;
using ProductService.Application.Products.DTOs;
using ProductService.Domain.Common;
using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;

namespace ProductService.Application.Products.Queries.GetProductsByColour;

public sealed class GetProductsByColourQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductsByColourQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetProductsByColourQuery request, CancellationToken ct)
    {
        if (!Enum.TryParse<Colour>(request.Colour, true, out var colour) || !Enum.IsDefined(typeof(Colour), colour))
            return Result<IReadOnlyList<ProductDto>>.Failure(
                $"'{request.Colour}' is not a valid colour. Valid values: {string.Join(", ", Enum.GetNames<Colour>())}.");

        var products = await repository.GetByColourAsync(colour, ct);
        return Result<IReadOnlyList<ProductDto>>.Success(
            products.Select(ProductDto.FromEntity).ToList());
    }
}
