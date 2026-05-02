using CarRepairShop.Application.ServiceItems.Commands;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.ServiceItems;

public class ServiceItemCommandValidatorTests
{
    private readonly CreateServiceItemCommandValidator _createValidator = new();
    private readonly UpdateServiceItemCommandValidator _updateValidator = new();

    // --- CreateServiceItemCommandValidator ---

    [Fact]
    public void CreateValidator_WithValidCommand_PassesValidation()
    {
        var command = new CreateServiceItemCommand("Oil Change", "Full synthetic oil change", 5000);

        var result = _createValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_WithEmptyName_FailsValidation()
    {
        var command = new CreateServiceItemCommand("", "Valid description", 1000);

        var result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void CreateValidator_WithNameExceeding100Characters_FailsValidation()
    {
        var longName = new string('A', 101);
        var command = new CreateServiceItemCommand(longName, "Valid description", 1000);

        var result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public void CreateValidator_WithNameExactly100Characters_PassesValidation()
    {
        var exactName = new string('A', 100);
        var command = new CreateServiceItemCommand(exactName, "Valid description", 1000);

        var result = _createValidator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateValidator_WithEmptyDescription_FailsValidation()
    {
        var command = new CreateServiceItemCommand("Valid Name", "", 1000);

        var result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void CreateValidator_WithDescriptionExceeding400Characters_FailsValidation()
    {
        var longDescription = new string('A', 401);
        var command = new CreateServiceItemCommand("Valid Name", longDescription, 1000);

        var result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description must not exceed 400 characters.");
    }

    [Fact]
    public void CreateValidator_WithDescriptionExactly400Characters_PassesValidation()
    {
        var exactDescription = new string('A', 400);
        var command = new CreateServiceItemCommand("Valid Name", exactDescription, 1000);

        var result = _createValidator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateValidator_WithZeroPrice_FailsValidation()
    {
        var command = new CreateServiceItemCommand("Valid Name", "Valid description", 0);

        var result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public void CreateValidator_WithNegativePrice_FailsValidation()
    {
        var command = new CreateServiceItemCommand("Valid Name", "Valid description", -1);

        var result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public void CreateValidator_WithPositivePrice_PassesValidation()
    {
        var command = new CreateServiceItemCommand("Valid Name", "Valid description", 1);

        var result = _createValidator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    // --- UpdateServiceItemCommandValidator ---

    [Fact]
    public void UpdateValidator_WithValidCommand_PassesValidation()
    {
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "Oil Change", "Full synthetic oil change", 5000);

        var result = _updateValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateValidator_WithEmptyId_FailsValidation()
    {
        var command = new UpdateServiceItemCommand(Guid.Empty, "Oil Change", "Full synthetic oil change", 5000);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("ServiceItem ID is required.");
    }

    [Fact]
    public void UpdateValidator_WithEmptyName_FailsValidation()
    {
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "", "Valid description", 1000);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void UpdateValidator_WithNameExceeding100Characters_FailsValidation()
    {
        var longName = new string('A', 101);
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), longName, "Valid description", 1000);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public void UpdateValidator_WithEmptyDescription_FailsValidation()
    {
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "Valid Name", "", 1000);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void UpdateValidator_WithDescriptionExceeding400Characters_FailsValidation()
    {
        var longDescription = new string('A', 401);
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "Valid Name", longDescription, 1000);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description)
              .WithErrorMessage("Description must not exceed 400 characters.");
    }

    [Fact]
    public void UpdateValidator_WithZeroPrice_FailsValidation()
    {
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "Valid Name", "Valid description", 0);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be greater than zero.");
    }

    [Fact]
    public void UpdateValidator_WithNegativePrice_FailsValidation()
    {
        var command = new UpdateServiceItemCommand(Guid.NewGuid(), "Valid Name", "Valid description", -100);

        var result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Price)
              .WithErrorMessage("Price must be greater than zero.");
    }
}
