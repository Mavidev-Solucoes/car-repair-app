using CarRepairShop.Application.Common.Validators;

namespace CarRepairShop.UnitTests.Validators;

public class BrazilianDocumentValidatorTests
{
    // --- Valid CPF ---

    [Theory]
    [InlineData("52998224725")]  // plain digits
    [InlineData("529.982.247-25")]  // formatted
    public void IsValidCpfOrCnpj_ValidCpf_ReturnsTrue(string value)
    {
        Assert.True(BrazilianDocumentValidator.IsValidCpfOrCnpj(value));
    }

    // --- Invalid CPF ---

    [Theory]
    [InlineData("00000000000")]  // all same digits
    [InlineData("11111111111")]
    [InlineData("12345678900")]  // wrong check digits
    [InlineData("52998224726")]  // last digit wrong
    public void IsValidCpfOrCnpj_InvalidCpf_ReturnsFalse(string value)
    {
        Assert.False(BrazilianDocumentValidator.IsValidCpfOrCnpj(value));
    }

    // --- Valid CNPJ ---

    [Theory]
    [InlineData("11222333000181")]   // plain digits
    [InlineData("11.222.333/0001-81")]  // formatted
    public void IsValidCpfOrCnpj_ValidCnpj_ReturnsTrue(string value)
    {
        Assert.True(BrazilianDocumentValidator.IsValidCpfOrCnpj(value));
    }

    // --- Invalid CNPJ ---

    [Theory]
    [InlineData("00000000000000")]   // all same digits
    [InlineData("11111111111111")]
    [InlineData("11222333000182")]   // last digit wrong
    [InlineData("11222333000189")]   // first check digit wrong
    public void IsValidCpfOrCnpj_InvalidCnpj_ReturnsFalse(string value)
    {
        Assert.False(BrazilianDocumentValidator.IsValidCpfOrCnpj(value));
    }

    // --- Wrong length ---

    [Theory]
    [InlineData("1234567890")]   // 10 digits
    [InlineData("123456789012345")]  // 15 digits
    [InlineData("")]
    public void IsValidCpfOrCnpj_WrongLength_ReturnsFalse(string value)
    {
        Assert.False(BrazilianDocumentValidator.IsValidCpfOrCnpj(value));
    }
}
