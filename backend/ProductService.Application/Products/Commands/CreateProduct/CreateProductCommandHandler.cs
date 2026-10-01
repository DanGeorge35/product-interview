using MediatR;
using Microsoft.Extensions.Logging;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.Products.DTOs;
using ProductService.Domain.Common;
using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    ILogger<CreateProductCommandHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var colour = Enum.Parse<Colour>(request.Colour, true);

        var product = Product.Create(
            request.Name,
            request.Description,
            colour,
            request.Price,
            request.StockQuantity);

        await repository.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);

        foreach (var domainEvent in product.DomainEvents)
            await eventPublisher.PublishAsync(domainEvent, ct);

        product.ClearDomainEvents();

        logger.LogInformation("Product created: {ProductId} ({ProductName})", product.Id, product.Name);

        return Result<ProductDto>.Success(ProductDto.FromEntity(product));
    }
}
