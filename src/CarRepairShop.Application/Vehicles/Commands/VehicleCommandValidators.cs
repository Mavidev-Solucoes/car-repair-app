using CarRepairShop.Application.Common.Validators;
using FluentValidation;

namespace CarRepairShop.Application.Vehicles.Commands;

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(100).WithMessage("Brand must not exceed 100 characters.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(200).WithMessage("Model must not exceed 200 characters.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year)
            .WithMessage($"Year must be between 1900 and {DateTime.UtcNow.Year}.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("License plate is required.")
            .Must(BrazilianLicensePlateValidator.IsValid)
            .WithMessage("License plate must follow the Brazilian Mercosul format (e.g. ABC1D23).");

        RuleFor(x => x.Color)
            .MaximumLength(50).WithMessage("Color must not exceed 50 characters.")
            .When(x => x.Color != null);
    }
}

public class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .MaximumLength(100).WithMessage("Brand must not exceed 100 characters.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(200).WithMessage("Model must not exceed 200 characters.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year)
            .WithMessage($"Year must be between 1900 and {DateTime.UtcNow.Year}.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("License plate is required.")
            .Must(BrazilianLicensePlateValidator.IsValid)
            .WithMessage("License plate must follow the Brazilian Mercosul format (e.g. ABC1D23).");

        RuleFor(x => x.Color)
            .MaximumLength(50).WithMessage("Color must not exceed 50 characters.")
            .When(x => x.Color != null);
    }
}
