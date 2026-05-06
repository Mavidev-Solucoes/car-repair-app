using CarRepairShop.Application.ServiceOrders.Commands;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.Application.ServiceOrders;

public class OpenServiceCommandValidatorTests
{
    private readonly OpenServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new OpenServiceCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyVehicleId_HasError()
    {
        var result = _validator.TestValidate(new OpenServiceCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.VehicleId);
    }

    [Fact]
    public void Validate_EmptyCustomerId_HasError()
    {
        var result = _validator.TestValidate(new OpenServiceCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }
}

public class AddServiceItemCommandValidatorTests
{
    private readonly AddServiceItemCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new AddServiceItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroQuantity_HasError()
    {
        var result = _validator.TestValidate(new AddServiceItemCommand(Guid.NewGuid(), Guid.NewGuid(), 0));
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_NegativeQuantity_HasError()
    {
        var result = _validator.TestValidate(new AddServiceItemCommand(Guid.NewGuid(), Guid.NewGuid(), -1));
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}

public class AddServiceJobCommandValidatorTests
{
    private readonly AddServiceJobCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new AddServiceJobCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new AddServiceJobCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }

    [Fact]
    public void Validate_EmptyServiceJobId_HasError()
    {
        var result = _validator.TestValidate(new AddServiceJobCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceJobId);
    }
}

public class RequestApprovalCommandValidatorTests
{
    private readonly RequestApprovalCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new RequestApprovalCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new RequestApprovalCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }
}

public class ApproveServiceCommandValidatorTests
{
    private readonly ApproveServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new ApproveServiceCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new ApproveServiceCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }
}

public class DeliverServiceCommandValidatorTests
{
    private readonly DeliverServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new DeliverServiceCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new DeliverServiceCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }
}

public class RemoveServiceItemCommandValidatorTests
{
    private readonly RemoveServiceItemCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new RemoveServiceItemCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new RemoveServiceItemCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }

    [Fact]
    public void Validate_EmptyServiceItemId_HasError()
    {
        var result = _validator.TestValidate(new RemoveServiceItemCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceItemId);
    }
}

public class RemoveServiceJobCommandValidatorTests
{
    private readonly RemoveServiceJobCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new RemoveServiceJobCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new RemoveServiceJobCommand(Guid.Empty, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }

    [Fact]
    public void Validate_EmptyServiceJobId_HasError()
    {
        var result = _validator.TestValidate(new RemoveServiceJobCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceJobId);
    }
}

public class DisputeServiceCommandValidatorTests
{
    private readonly DisputeServiceCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new DisputeServiceCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyServiceOrderId_HasError()
    {
        var result = _validator.TestValidate(new DisputeServiceCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ServiceOrderId);
    }
}
