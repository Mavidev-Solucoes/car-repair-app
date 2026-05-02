using CarRepairShop.Application.ServiceJobs.Commands;
using FluentValidation;
using FluentValidation.Results;

namespace CarRepairShop.UnitTests.Application.ServiceJobs;

public class ServiceJobValidatorTests
{
    private readonly UpdateServiceJobCommandValidator _validator = new();

    // ── Valid cases ──────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Brake Repair", "Fix the brakes", 1500);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNameAtMaxLength_IsValid()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), new string('A', 100), "Description", 500);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDescriptionAtMaxLength_IsValid()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", new string('D', 400), 500);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    // ── Id validation ────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithEmptyId_ReturnsIdRequiredError()
    {
        var command = new UpdateServiceJobCommand(Guid.Empty, "Name", "Description", 500);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Id" && e.ErrorMessage.Contains("required"));
    }

    // ── Name validation ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithEmptyName_ReturnsNameRequiredError()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "", "Description", 500);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithNameExceedingMaxLength_ReturnsMaxLengthError()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), new string('A', 101), "Description", 500);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.ErrorMessage.Contains("100"));
    }

    // ── Description validation ───────────────────────────────────────────────

    [Fact]
    public void Validate_WithEmptyDescription_ReturnsDescriptionRequiredError()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "", 500);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_WithDescriptionExceedingMaxLength_ReturnsMaxLengthError()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", new string('D', 401), 500);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description" && e.ErrorMessage.Contains("400"));
    }

    // ── Price validation ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithZeroPrice_ReturnsPriceError()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Description", 0);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price" && e.ErrorMessage.Contains("greater than zero"));
    }

    [Fact]
    public void Validate_WithNegativePrice_ReturnsPriceError()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Description", -1);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price" && e.ErrorMessage.Contains("greater than zero"));
    }

    [Fact]
    public void Validate_WithPriceOfOne_IsValid()
    {
        var command = new UpdateServiceJobCommand(Guid.NewGuid(), "Name", "Description", 1);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    // ── Multiple errors ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var command = new UpdateServiceJobCommand(Guid.Empty, "", "", 0);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4);
    }
}
