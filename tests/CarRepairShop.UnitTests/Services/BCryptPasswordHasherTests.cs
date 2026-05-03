using CarRepairShop.Services.Implementations;

namespace CarRepairShop.UnitTests.Services;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsNonEmptyHash()
    {
        var hash = _hasher.Hash("password123");

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void Hash_ReturnsBCryptFormattedString()
    {
        var hash = _hasher.Hash("test");

        Assert.StartsWith("$2", hash);
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword()
    {
        var hash1 = _hasher.Hash("password123");
        var hash2 = _hasher.Hash("password123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ReturnsTrueForCorrectPassword()
    {
        const string password = "mySecret!";
        var hash = _hasher.Hash(password);

        Assert.True(_hasher.Verify(password, hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        var hash = _hasher.Hash("correctPassword");

        Assert.False(_hasher.Verify("wrongPassword", hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForEmptyPassword()
    {
        var hash = _hasher.Hash("somePassword");

        Assert.False(_hasher.Verify("", hash));
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        var hash = _hasher.Hash("Password");

        Assert.False(_hasher.Verify("password", hash));
    }
}
