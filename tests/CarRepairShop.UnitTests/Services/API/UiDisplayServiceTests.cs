using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CarRepairShop.API.Services;

namespace CarRepairShop.UnitTests.Services.API;

public class UiDisplayServiceTests
{
    private static ClaimsPrincipal CreateUser(Guid? userId = null, string? name = null)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString()));
        if (name is not null)
            claims.Add(new Claim(ClaimTypes.Name, name));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void GetCurrentUserId_ValidSubClaim_ReturnsGuid()
    {
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);

        var result = UiDisplayService.GetCurrentUserId(user);

        Assert.Equal(userId, result);
    }

    [Fact]
    public void GetCurrentUserId_NoSubClaim_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        var result = UiDisplayService.GetCurrentUserId(user);

        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentUserId_InvalidSubClaim_ReturnsNull()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid")
        }, "Test"));

        var result = UiDisplayService.GetCurrentUserId(user);

        Assert.Null(result);
    }

    [Fact]
    public void FormatUserLabel_NullUserId_ReturnsUnassigned()
    {
        var user = CreateUser();

        var result = UiDisplayService.FormatUserLabel(null, user);

        Assert.Equal("Unassigned", result);
    }

    [Fact]
    public void FormatUserLabel_CurrentUser_WithName_ReturnsName()
    {
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, "Alice");

        var result = UiDisplayService.FormatUserLabel(userId, user);

        Assert.Equal("Alice", result);
    }

    [Fact]
    public void FormatUserLabel_CurrentUser_NoName_ReturnsShortId()
    {
        var userId = Guid.NewGuid();
        var user = CreateUser(userId); // no name

        var result = UiDisplayService.FormatUserLabel(userId, user);

        Assert.Equal(userId.ToString("N")[..8], result);
    }

    [Fact]
    public void FormatUserLabel_OtherUser_ReturnsShortId()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var user = CreateUser(currentUserId, "Alice");

        var result = UiDisplayService.FormatUserLabel(otherUserId, user);

        Assert.Equal(otherUserId.ToString("N")[..8], result);
    }
}
