using FluentValidation;
using ProductService.Domain.Entities;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Colour)
            .NotEmpty().WithMessage("Colour is required.")
            .Must(c => Enum.TryParse<Colour>(c, true, out var parsed) && Enum.IsDefined(typeof(Colour), parsed))
            .WithMessage($"Colour must be one of: {string.Join(", ", Enum.GetNames<Colour>())}.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be positive.")
            .PrecisionScale(18, 2, false).WithMessage("Price cannot have more than 2 decimal places.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");
    }
}
