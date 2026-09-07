using CarRepairShop.Application.Auth.Commands;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.Application.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Theory]
    [InlineData("12345678909")]
    [InlineData("123.456.789-09")]
    public void Validate_ValidCommand_HasNoErrors(string cpf)
    {
        var result = _validator.TestValidate(new LoginCommand(cpf));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345678900")]
    [InlineData("11111111111")]
    [InlineData("12345")]
    public void Validate_InvalidCommand_HasErrors(string cpf)
    {
        var result = _validator.TestValidate(new LoginCommand(cpf));
        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }
}
