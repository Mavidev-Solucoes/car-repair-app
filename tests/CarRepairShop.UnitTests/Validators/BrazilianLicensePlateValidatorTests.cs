using CarRepairShop.Application.Common.Validators;

namespace CarRepairShop.UnitTests.Validators;

public class BrazilianLicensePlateValidatorTests
{
    // --- Valid plates ---

    [Theory]
    [InlineData("ABC1D23")]
    [InlineData("abc1d23")]
    [InlineData("XYZ9W87")]
    [InlineData("AAA0A00")]
    public void IsValid_ReturnsTrueForValidMercosulPlate(string plate)
    {
        Assert.True(BrazilianLicensePlateValidator.IsValid(plate));
    }

    [Theory]
    [InlineData("ABC-1D23")]
    [InlineData("abc-1d23")]
    public void IsValid_ReturnsTrueForValidPlateWithDash(string plate)
    {
        Assert.True(BrazilianLicensePlateValidator.IsValid(plate));
    }

    // --- Invalid plates ---

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValid_ReturnsFalseForEmptyOrWhitespace(string plate)
    {
        Assert.False(BrazilianLicensePlateValidator.IsValid(plate));
    }

    [Theory]
    [InlineData("ABC1234")]   // old Brazilian format (LLLNNNN)
    [InlineData("AB1D23")]    // too short
    [InlineData("ABCD1D23")]  // too long
    [InlineData("1BC1D23")]   // starts with digit
    [InlineData("ABC1123")]   // second letter position is digit
    [InlineData("ABC D23")]   // contains space
    public void IsValid_ReturnsFalseForInvalidPlate(string plate)
    {
        Assert.False(BrazilianLicensePlateValidator.IsValid(plate));
    }
}
