using FluentAssertions;
using FluentValidation.TestHelper;
using ProductService.Application.Products.Commands.CreateProduct;

namespace ProductService.UnitTests.Application.Validators;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new CreateProductCommand("Shirt", "Desc", "Red", 29.99m, 100);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyName_FailsWithCorrectMessage(string? name)
    {
        var command = new CreateProductCommand(name!, null, "Red", 10m, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Validate_NameExceeds200Chars_FailsValidation()
    {
        var command = new CreateProductCommand(new string('x', 201), null, "Red", 10m, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Validate_NonPositivePrice_FailsWithCorrectMessage(decimal price)
    {
        var command = new CreateProductCommand("Shirt", null, "Red", price, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be positive.");
    }

    [Fact]
    public void Validate_NegativeStockQuantity_FailsWithCorrectMessage()
    {
        var command = new CreateProductCommand("Shirt", null, "Red", 10m, -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StockQuantity)
            .WithErrorMessage("Stock quantity cannot be negative.");
    }

    [Theory]
    [InlineData("Chartreuse")]
    [InlineData("Rainbow")]
    [InlineData("")]
    [InlineData("123")]
    public void Validate_InvalidColour_FailsWithCorrectMessage(string colour)
    {
        var command = new CreateProductCommand("Shirt", null, colour, 10m, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Colour);
    }

    [Theory]
    [InlineData("Red")]
    [InlineData("red")]
    [InlineData("RED")]
    [InlineData("Blue")]
    [InlineData("Green")]
    public void Validate_ValidColourCaseInsensitive_PassesValidation(string colour)
    {
        var command = new CreateProductCommand("Shirt", null, colour, 10m, 5);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Colour);
    }

    [Fact]
    public void Validate_DescriptionExceeds1000Chars_FailsValidation()
    {
        var command = new CreateProductCommand("Shirt", new string('x', 1001), "Red", 10m, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NullDescription_PassesValidation()
    {
        var command = new CreateProductCommand("Shirt", null, "Red", 10m, 5);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }
}
