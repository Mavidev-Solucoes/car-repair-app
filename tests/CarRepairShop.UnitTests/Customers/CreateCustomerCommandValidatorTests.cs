using CarRepairShop.Application.Customers.Commands;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.Customers;

public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator = new();

    private static CreateCustomerCommand ValidCommand() =>
        new("John Doe", "52998224725", "john@example.com", "11987654321");

    [Fact]
    public void Valid_Command_Passes()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
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

    [Fact]
    public void Name_WithOnlySpaces_Fails()
    {
        var cmd = ValidCommand() with { Name = "   " };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    // PersonalId rules
    [Fact]
    public void PersonalId_Empty_Fails()
    {
        var cmd = ValidCommand() with { PersonalId = "" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PersonalId).WithErrorMessage("PersonalId is required.");
    }

    [Fact]
    public void PersonalId_WrongDigitCount_Fails()
    {
        var cmd = ValidCommand() with { PersonalId = "1234567890" }; // 10 digits
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PersonalId)
            .WithErrorMessage("PersonalId must be a valid CPF (11 digits) or CNPJ (14 digits).");
    }

    [Fact]
    public void PersonalId_InvalidCpfChecksum_Fails()
    {
        var cmd = ValidCommand() with { PersonalId = "12345678901" }; // 11 digits, invalid checksum
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PersonalId)
            .WithErrorMessage("PersonalId is not a valid CPF or CNPJ.");
    }

    [Fact]
    public void PersonalId_ValidCpfWithFormatting_Passes()
    {
        var cmd = ValidCommand() with { PersonalId = "529.982.247-25" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalId);
    }

    [Fact]
    public void PersonalId_ValidCnpj_Passes()
    {
        var cmd = ValidCommand() with { PersonalId = "11222333000181" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.PersonalId);
    }

    [Fact]
    public void PersonalId_InvalidCnpjChecksum_Fails()
    {
        var cmd = ValidCommand() with { PersonalId = "11111111111111" }; // all same digits
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.PersonalId)
            .WithErrorMessage("PersonalId is not a valid CPF or CNPJ.");
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
        var cmd = ValidCommand() with { Telephone = "1198765432" }; // 10 digits
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Telephone);
    }

    [Fact]
    public void Telephone_WithFormatting_Passes()
    {
        var cmd = ValidCommand() with { Telephone = "(11) 9 8765-4321" }; // 11 digits with mask
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Telephone);
    }
}
