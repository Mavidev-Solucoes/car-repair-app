using CarRepairShop.Application.Users.Commands;
using CarRepairShop.Domain.Enums;
using FluentValidation.TestHelper;

namespace CarRepairShop.UnitTests.Application.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(
            new CreateUserCommand("Alice", "alice@example.com", "Pass@word1", UserRole.Admin));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var result = _validator.TestValidate(
            new CreateUserCommand("", "alice@example.com", "Pass@word1", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_InvalidEmail_HasError()
    {
        var result = _validator.TestValidate(
            new CreateUserCommand("Alice", "not-an-email", "Pass@word1", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_PasswordTooShort_HasError()
    {
        var result = _validator.TestValidate(
            new CreateUserCommand("Alice", "alice@example.com", "abc", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordNoUppercase_HasError()
    {
        var result = _validator.TestValidate(
            new CreateUserCommand("Alice", "alice@example.com", "password1", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordNoDigit_HasError()
    {
        var result = _validator.TestValidate(
            new CreateUserCommand("Alice", "alice@example.com", "Password", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(
            new UpdateUserCommand(Guid.NewGuid(), "Alice", "alice@example.com", UserRole.Admin));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_HasError()
    {
        var result = _validator.TestValidate(
            new UpdateUserCommand(Guid.Empty, "Alice", "alice@example.com", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_InvalidEmail_HasError()
    {
        var result = _validator.TestValidate(
            new UpdateUserCommand(Guid.NewGuid(), "Alice", "bad-email", UserRole.Admin));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(
            new ChangePasswordCommand(Guid.NewGuid(), "OldPass@1", "NewPass@1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCurrentPassword_HasError()
    {
        var result = _validator.TestValidate(
            new ChangePasswordCommand(Guid.NewGuid(), "", "NewPass@1"));
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void Validate_WeakNewPassword_HasError()
    {
        var result = _validator.TestValidate(
            new ChangePasswordCommand(Guid.NewGuid(), "OldPass@1", "short"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
