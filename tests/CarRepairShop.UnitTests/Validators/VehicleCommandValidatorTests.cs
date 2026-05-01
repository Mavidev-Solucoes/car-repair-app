using CarRepairShop.Application.Vehicles.Commands;

namespace CarRepairShop.UnitTests.Validators;

public class CreateVehicleCommandValidatorTests
{
    private readonly CreateVehicleCommandValidator _validator = new();

    private static CreateVehicleCommand ValidCommand() =>
        new(Guid.NewGuid(), "Toyota", "Corolla", 2020, "ABC1D23", null);

    private bool IsValid(CreateVehicleCommand cmd) => _validator.Validate(cmd).IsValid;

    private IEnumerable<string> ErrorsFor(CreateVehicleCommand cmd, string propertyName) =>
        _validator.Validate(cmd).Errors
            .Where(e => e.PropertyName == propertyName)
            .Select(e => e.ErrorMessage);

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        Assert.True(IsValid(ValidCommand()));
    }

    [Fact]
    public void EmptyCustomerId_FailsValidation()
    {
        var cmd = ValidCommand() with { CustomerId = Guid.Empty };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.CustomerId)));
    }

    [Fact]
    public void EmptyBrand_FailsValidation()
    {
        var cmd = ValidCommand() with { Brand = "" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Brand)));
    }

    [Fact]
    public void BrandTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Brand = new string('X', 101) };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Brand)));
    }

    [Fact]
    public void BrandAtMaxLength_PassesValidation()
    {
        var cmd = ValidCommand() with { Brand = new string('X', 100) };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void EmptyModel_FailsValidation()
    {
        var cmd = ValidCommand() with { Model = "" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Model)));
    }

    [Fact]
    public void ModelTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Model = new string('X', 201) };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Model)));
    }

    [Fact]
    public void ModelAtMaxLength_PassesValidation()
    {
        var cmd = ValidCommand() with { Model = new string('X', 200) };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void YearBelowMinimum_FailsValidation()
    {
        var cmd = ValidCommand() with { Year = 1899 };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Year)));
    }

    [Fact]
    public void YearAboveCurrentYear_FailsValidation()
    {
        var cmd = ValidCommand() with { Year = DateTime.UtcNow.Year + 1 };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Year)));
    }

    [Fact]
    public void Year1900_PassesValidation()
    {
        var cmd = ValidCommand() with { Year = 1900 };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void YearCurrentYear_PassesValidation()
    {
        var cmd = ValidCommand() with { Year = DateTime.UtcNow.Year };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void EmptyLicensePlate_FailsValidation()
    {
        var cmd = ValidCommand() with { LicensePlate = "" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.LicensePlate)));
    }

    [Fact]
    public void InvalidLicensePlate_FailsValidation()
    {
        var cmd = ValidCommand() with { LicensePlate = "ABC1234" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.LicensePlate)));
    }

    [Fact]
    public void NullColor_PassesValidation()
    {
        var cmd = ValidCommand() with { Color = null };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void ColorTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Color = new string('X', 51) };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Color)));
    }

    [Fact]
    public void ColorAtMaxLength_PassesValidation()
    {
        var cmd = ValidCommand() with { Color = new string('X', 50) };
        Assert.True(IsValid(cmd));
    }
}

public class UpdateVehicleCommandValidatorTests
{
    private readonly UpdateVehicleCommandValidator _validator = new();

    private static UpdateVehicleCommand ValidCommand() =>
        new(Guid.NewGuid(), "Toyota", "Corolla", 2020, "ABC1D23", null);

    private bool IsValid(UpdateVehicleCommand cmd) => _validator.Validate(cmd).IsValid;

    private IEnumerable<string> ErrorsFor(UpdateVehicleCommand cmd, string propertyName) =>
        _validator.Validate(cmd).Errors
            .Where(e => e.PropertyName == propertyName)
            .Select(e => e.ErrorMessage);

    [Fact]
    public void ValidCommand_PassesValidation()
    {
        Assert.True(IsValid(ValidCommand()));
    }

    [Fact]
    public void EmptyId_FailsValidation()
    {
        var cmd = ValidCommand() with { Id = Guid.Empty };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Id)));
    }

    [Fact]
    public void EmptyBrand_FailsValidation()
    {
        var cmd = ValidCommand() with { Brand = "" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Brand)));
    }

    [Fact]
    public void BrandTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Brand = new string('X', 101) };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Brand)));
    }

    [Fact]
    public void EmptyModel_FailsValidation()
    {
        var cmd = ValidCommand() with { Model = "" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Model)));
    }

    [Fact]
    public void ModelTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Model = new string('X', 201) };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Model)));
    }

    [Fact]
    public void YearBelowMinimum_FailsValidation()
    {
        var cmd = ValidCommand() with { Year = 1899 };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Year)));
    }

    [Fact]
    public void YearAboveCurrentYear_FailsValidation()
    {
        var cmd = ValidCommand() with { Year = DateTime.UtcNow.Year + 1 };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Year)));
    }

    [Fact]
    public void Year1900_PassesValidation()
    {
        var cmd = ValidCommand() with { Year = 1900 };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void InvalidLicensePlate_FailsValidation()
    {
        var cmd = ValidCommand() with { LicensePlate = "ABC1234" };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.LicensePlate)));
    }

    [Fact]
    public void ColorTooLong_FailsValidation()
    {
        var cmd = ValidCommand() with { Color = new string('X', 51) };
        Assert.False(IsValid(cmd));
        Assert.NotEmpty(ErrorsFor(cmd, nameof(cmd.Color)));
    }

    [Fact]
    public void NullColor_PassesValidation()
    {
        var cmd = ValidCommand() with { Color = null };
        Assert.True(IsValid(cmd));
    }

    [Fact]
    public void ValidLicensePlateWithDash_PassesValidation()
    {
        var cmd = ValidCommand() with { LicensePlate = "ABC-1D23" };
        Assert.True(IsValid(cmd));
    }
}
