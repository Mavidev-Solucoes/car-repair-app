using CarRepairShop.Application.Customers.Commands;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.Customers;

public class UpdateCustomerCommandValidatorTests
{
    private readonly UpdateCustomerCommandValidator _validator = new();

    private static UpdateCustomerCommand ValidCommand() =>
        new(Guid.NewGuid(), "John Doe", "john@example.com", "11987654321");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    // Id rules
    [Fact]
    public void Id_Empty_Fails()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Id).WithErrorMessage("Customer ID is required.");
    }

    // Name rules
    [Fact]
    public void Name_Empty_Fails()
    {
        var cmd = ValidCommand() with { Name = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Name_ExceedsMaxLength_Fails()
    {
        var cmd = ValidCommand() with { Name = new string('A', 50) + " " + new string('B', 51) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Name must not exceed 100 characters.");
    }

    [Fact]
    public void Name_WithoutSpace_Fails()
    {
        var cmd = ValidCommand() with { Name = "JohnDoe" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must include both a first name and a last name.");
    }

    // Email rules
    [Fact]
    public void Email_Empty_Fails()
    {
        var cmd = ValidCommand() with { Email = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Email_Invalid_Fails()
    {
        var cmd = ValidCommand() with { Email = "not-an-email" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be a valid email address.");
    }

    [Fact]
    public void Email_ExceedsMaxLength_Fails()
    {
        var longLocal = new string('a', 90);
        var cmd = ValidCommand() with { Email = $"{longLocal}@example.com" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 100 characters.");
    }

    // Telephone rules
    [Fact]
    public void Telephone_Empty_Fails()
    {
        var cmd = ValidCommand() with { Telephone = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Telephone).WithErrorMessage("Telephone is required.");
    }

    [Fact]
    public void Telephone_WrongDigitCount_Fails()
    {
        var cmd = ValidCommand() with { Telephone = "123456789" }; // 9 digits
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Telephone)
            .WithErrorMessage("Telephone must have 10 or 11 digits (Brazilian standard).");
    }

    [Fact]
    public void Telephone_TenDigits_Passes()
    {
        var cmd = ValidCommand() with { Telephone = "1198765432" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Telephone);
    }
}
