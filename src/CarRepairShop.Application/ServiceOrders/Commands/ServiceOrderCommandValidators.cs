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

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ServiceItemId)
                    .NotEmpty().WithMessage("Service item ID is required.");
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("Item quantity must be greater than zero.");
            });

        RuleForEach(x => x.Jobs)
            .ChildRules(job =>
            {
                job.RuleFor(j => j.ServiceJobId)
                    .NotEmpty().WithMessage("Service job ID is required.");
            });
    }
}

public class AddServiceItemCommandValidator : AbstractValidator<AddServiceItemCommand>
{
    public AddServiceItemCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");

        RuleFor(x => x.ServiceItemId)
            .NotEmpty().WithMessage("Service item ID is required.");

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

        RuleFor(x => x.ServiceJobId)
            .NotEmpty().WithMessage("Service job ID is required.");
    }
}

public class RemoveServiceJobCommandValidator : AbstractValidator<RemoveServiceJobCommand>
{
    public RemoveServiceJobCommandValidator()
    {
        RuleFor(x => x.ServiceOrderId)
            .NotEmpty().WithMessage("Service Order ID is required.");

        RuleFor(x => x.ServiceJobId)
            .NotEmpty().WithMessage("Service job ID is required.");
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

public class RejectServiceCommandValidator : AbstractValidator<RejectServiceCommand>
{
    public RejectServiceCommandValidator()
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
