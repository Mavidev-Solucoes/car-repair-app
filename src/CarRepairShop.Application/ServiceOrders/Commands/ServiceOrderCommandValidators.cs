using FluentValidation;

namespace CarRepairShop.Application.ServiceOrders.Commands;

public class OpenServiceCommandValidator : AbstractValidator<OpenServiceCommand>
{
    public OpenServiceCommandValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");
    }
}

public class AddServiceItemCommandValidator : AbstractValidator<AddServiceItemCommand>
{
    public AddServiceItemCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description must not exceed 200 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}

public class RemoveServiceItemCommandValidator : AbstractValidator<RemoveServiceItemCommand>
{
    public RemoveServiceItemCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");

        RuleFor(x => x.ServiceItemId)
            .NotEmpty().WithMessage("Service Item ID is required.");
    }
}

public class AddServiceJobCommandValidator : AbstractValidator<AddServiceJobCommand>
{
    public AddServiceJobCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");

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

public class RequestApprovalCommandValidator : AbstractValidator<RequestApprovalCommand>
{
    public RequestApprovalCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");
    }
}

public class ApproveServiceCommandValidator : AbstractValidator<ApproveServiceCommand>
{
    public ApproveServiceCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");
    }
}

public class DeliverServiceCommandValidator : AbstractValidator<DeliverServiceCommand>
{
    public DeliverServiceCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");
    }
}

public class DisputeServiceCommandValidator : AbstractValidator<DisputeServiceCommand>
{
    public DisputeServiceCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");
    }
}
