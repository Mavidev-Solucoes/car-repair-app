using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CarRepairShop.API.Services;

public static class UiDisplayService
{
    public static Guid? GetCurrentUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string FormatUserLabel(Guid? userId, ClaimsPrincipal user)
    {
        if (!userId.HasValue)
            return "Unassigned";

        var currentUserId = GetCurrentUserId(user);
        if (currentUserId == userId)
            return user.Identity?.Name ?? userId.Value.ToString("N")[..8];

        return userId.Value.ToString("N")[..8];
    }
}
