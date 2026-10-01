using FluentAssertions;
using ProductService.Domain.Common;
using ProductService.Domain.Entities;
using ProductService.Domain.Events;

namespace ProductService.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsProductWithCorrectProperties()
    {
        var product = Product.Create("Red Shirt", "A nice shirt", Colour.Red, 29.99m, 100);

        product.Id.Should().NotBeEmpty();
        product.Name.Should().Be("Red Shirt");
        product.Description.Should().Be("A nice shirt");
        product.Colour.Should().Be(Colour.Red);
        product.Price.Should().Be(29.99m);
        product.StockQuantity.Should().Be(100);
        product.IsDeleted.Should().BeFalse();
        product.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ValidProduct_RaisesProductCreatedDomainEvent()
    {
        var product = Product.Create("Blue Jeans", null, Colour.Blue, 49.99m, 50);

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedDomainEvent>();

        var evt = (ProductCreatedDomainEvent)product.DomainEvents[0];
        evt.ProductId.Should().Be(product.Id);
        evt.Name.Should().Be("Blue Jeans");
        evt.Colour.Should().Be(Colour.Blue);
        evt.Price.Should().Be(49.99m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNonPositivePrice_ThrowsDomainException(decimal price)
    {
        var act = () => Product.Create("Shirt", null, Colour.Red, price, 10);

        act.Should().Throw<DomainException>()
            .WithMessage("Price must be positive.");
    }

    [Fact]
    public void Create_WithNegativeStockQuantity_ThrowsDomainException()
    {
        var act = () => Product.Create("Shirt", null, Colour.Red, 10m, -1);

        act.Should().Throw<DomainException>()
            .WithMessage("Stock quantity cannot be negative.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNullName_ThrowsDomainException(string? name)
    {
        var act = () => Product.Create(name!, null, Colour.Red, 10m, 5);

        act.Should().Throw<DomainException>()
            .WithMessage("Name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var longName = new string('x', 201);

        var act = () => Product.Create(longName, null, Colour.Red, 10m, 5);

        act.Should().Throw<DomainException>()
            .WithMessage("Name must not exceed 200 characters.");
    }

    [Fact]
    public void Create_WithDescriptionExceeding1000Chars_ThrowsDomainException()
    {
        var longDesc = new string('x', 1001);

        var act = () => Product.Create("Shirt", longDesc, Colour.Red, 10m, 5);

        act.Should().Throw<DomainException>()
            .WithMessage("Description must not exceed 1000 characters.");
    }

    [Fact]
    public void Create_WithNullDescription_DoesNotThrow()
    {
        var act = () => Product.Create("Shirt", null, Colour.Red, 10m, 5);

        act.Should().NotThrow();
    }

    [Fact]
    public void Delete_SetsIsDeletedToTrue()
    {
        var product = Product.Create("Shirt", null, Colour.Red, 10m, 5);

        product.Delete();

        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void UpdateStock_WithValidQuantity_UpdatesStockQuantity()
    {
        var product = Product.Create("Shirt", null, Colour.Red, 10m, 5);

        product.UpdateStock(50);

        product.StockQuantity.Should().Be(50);
    }

    [Fact]
    public void UpdateStock_WithNegativeQuantity_ThrowsDomainException()
    {
        var product = Product.Create("Shirt", null, Colour.Red, 10m, 5);

        var act = () => product.UpdateStock(-1);

        act.Should().Throw<DomainException>()
            .WithMessage("Stock quantity cannot be negative.");
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var product = Product.Create("Shirt", null, Colour.Red, 10m, 5);
        product.DomainEvents.Should().HaveCount(1);

        product.ClearDomainEvents();

        product.DomainEvents.Should().BeEmpty();
    }
}
