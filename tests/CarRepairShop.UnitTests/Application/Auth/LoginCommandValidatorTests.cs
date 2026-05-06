using CarRepairShop.Application.Auth.Commands;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.Application.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Theory]
    [InlineData("alice@example.com", "password")]
    [InlineData("user@domain.org", "secret123")]
    public void Validate_ValidCommand_HasNoErrors(string email, string password)
    {
        var result = _validator.TestValidate(new LoginCommand(email, password));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "password", "Email")]
    [InlineData("not-an-email", "password", "Email")]
    [InlineData("alice@example.com", "", "Password")]
    [InlineData("alice@example.com", "abc", "Password")]
    public void Validate_InvalidCommand_HasErrors(string email, string password, string expectedField)
    {
        var result = _validator.TestValidate(new LoginCommand(email, password));
        result.ShouldHaveValidationErrorFor(expectedField);
    }
}
