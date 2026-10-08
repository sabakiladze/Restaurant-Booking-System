namespace RestaurantBooking.Web.Auth;

public static class AuthCookie
{
    public const string Name = "access_token";

    public static void Set(HttpResponse response, string token, int minutes) =>
        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,                
            Secure = true,                 
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(minutes)
        });

    public static void Clear(HttpResponse response) => response.Cookies.Delete(Name);
}
