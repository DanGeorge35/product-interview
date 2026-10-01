using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.Products.Commands.CreateProduct;
using ProductService.Domain.Entities;
using ProductService.Domain.Repositories;

namespace ProductService.UnitTests.Application.Commands;

public sealed class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly ILogger<CreateProductCommandHandler> _logger =
        Substitute.For<ILogger<CreateProductCommandHandler>>();

    private CreateProductCommandHandler CreateHandler() =>
        new(_repository, _unitOfWork, _publisher, _logger);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithProductDto()
    {
        var command = new CreateProductCommand("Test Shirt", "Desc", "Red", 29.99m, 100);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Shirt");
        result.Value.Colour.Should().Be("Red");
        result.Value.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryAddAsync()
    {
        var command = new CreateProductCommand("Shirt", null, "Blue", 10m, 5);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsUnitOfWorkSaveChanges()
    {
        var command = new CreateProductCommand("Shirt", null, "Blue", 10m, 5);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesDomainEvent()
    {
        var command = new CreateProductCommand("Shirt", null, "Green", 15m, 20);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _publisher.Received(1).PublishAsync(
            Arg.Any<ProductService.Domain.Events.IDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CaseInsensitiveColour_ParsesCorrectly()
    {
        var command = new CreateProductCommand("Shirt", null, "rEd", 10m, 5);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Colour.Should().Be("Red");
    }
}
