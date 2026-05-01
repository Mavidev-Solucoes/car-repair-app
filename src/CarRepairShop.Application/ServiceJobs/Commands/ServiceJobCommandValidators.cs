using FluentValidation;

namespace CarRepairShop.Application.ServiceJobs.Commands;

public class CreateServiceJobCommandValidator : AbstractValidator<CreateServiceJobCommand>
{
    public CreateServiceJobCommandValidator()
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

public class UpdateServiceJobCommandValidator : AbstractValidator<UpdateServiceJobCommand>
{
    public UpdateServiceJobCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ServiceJob ID is required.");

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
