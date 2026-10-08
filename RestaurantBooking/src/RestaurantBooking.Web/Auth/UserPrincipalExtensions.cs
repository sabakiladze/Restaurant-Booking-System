using System.Security.Claims;

namespace RestaurantBooking.Web.Auth;

public static class UserPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
}
