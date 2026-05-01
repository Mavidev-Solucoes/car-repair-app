using FluentValidation;

namespace CarRepairShop.Application.ServiceItems.Commands;

public class CreateServiceItemCommandValidator : AbstractValidator<CreateServiceItemCommand>
{
    public CreateServiceItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters.");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("UnitCost must be greater than zero.");
    }
}

public class UpdateServiceItemCommandValidator : AbstractValidator<UpdateServiceItemCommand>
{
    public UpdateServiceItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ServiceItem ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(400).WithMessage("Description must not exceed 400 characters.");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("UnitCost must be greater than zero.");
    }
}
