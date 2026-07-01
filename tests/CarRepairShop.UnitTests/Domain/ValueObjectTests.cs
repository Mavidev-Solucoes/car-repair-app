using CarRepairShop.Domain.ValueObjects;

namespace CarRepairShop.UnitTests.Domain;

public class PersonalIdTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData("000.000.000-00", "00000000000")]
    public void Constructor_StripsNonDigits(string raw, string expected)
    {
        var id = new PersonalId(raw);
        Assert.Equal(expected, id.Value);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var id = new PersonalId("529.982.247-25");
        string s = id;
        Assert.Equal("52998224725", s);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var id = new PersonalId("529.982.247-25");
        Assert.Equal("52998224725", id.ToString());
    }

    [Fact]
    public void EqualityByValue_SameDigits_AreEqual()
    {
        var a = new PersonalId("529.982.247-25");
        var b = new PersonalId("52998224725");
        Assert.Equal(a, b);
    }

    [Fact]
    public void EqualityByValue_DifferentDigits_AreNotEqual()
    {
        var a = new PersonalId("52998224725");
        var b = new PersonalId("11111111111");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Constructor_EmptyString_ReturnsEmptyValue()
    {
        var id = new PersonalId("");
        Assert.Equal(string.Empty, id.Value);
    }

    [Fact]
    public void Constructor_AllLetters_ReturnsEmptyValue()
    {
        var id = new PersonalId("ABC");
        Assert.Equal(string.Empty, id.Value);
    }
}

public class PhoneNumberTests
{
    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11987654321", "11987654321")]
    [InlineData("+55 (11) 98765-4321", "5511987654321")]
    public void Constructor_StripsNonDigits(string raw, string expected)
    {
        var phone = new PhoneNumber(raw);
        Assert.Equal(expected, phone.Value);
    }

    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        var phone = new PhoneNumber("(11) 98765-4321");
        string s = phone;
        Assert.Equal("11987654321", s);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var phone = new PhoneNumber("(11) 98765-4321");
        Assert.Equal("11987654321", phone.ToString());
    }

    [Fact]
    public void EqualityByValue_SameDigits_AreEqual()
    {
        var a = new PhoneNumber("(11) 98765-4321");
        var b = new PhoneNumber("11987654321");
        Assert.Equal(a, b);
    }

    [Fact]
    public void EqualityByValue_DifferentDigits_AreNotEqual()
    {
        var a = new PhoneNumber("11987654321");
        var b = new PhoneNumber("21987654321");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Constructor_EmptyString_ReturnsEmptyValue()
    {
        var phone = new PhoneNumber("");
        Assert.Equal(string.Empty, phone.Value);
    }
}
