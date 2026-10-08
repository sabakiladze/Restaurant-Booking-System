using Microsoft.Extensions.Options;
using RestaurantBooking.Application.Abstractions;

namespace RestaurantBooking.Infrastructure;

public class RestaurantOptions
{
    public string TimeZone { get; set; } = "Asia/Tbilisi";
}

public class RestaurantClock : IRestaurantClock
{
    private readonly TimeZoneInfo _tz;

    public RestaurantClock(IOptions<RestaurantOptions> options)
    {
        try { _tz = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone); }
        catch (TimeZoneNotFoundException) { _tz = TimeZoneInfo.FindSystemTimeZoneById("Georgian Standard Time"); }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _tz);

    public bool TryToUtc(DateTime local, out DateTime utc)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (_tz.IsInvalidTime(local)) { utc = default; return false; }
        utc = TimeZoneInfo.ConvertTimeToUtc(local, _tz);
        return true;
    }
}
