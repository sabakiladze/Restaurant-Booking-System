namespace RestaurantBooking.Application.Abstractions;

/// <summary>დროის წყარო + რესტორნის დროის სარტყელი. ტესტებში ჩანაცვლებადია.</summary>
public interface IRestaurantClock
{
    DateTime UtcNow { get; }
    DateTime ToLocal(DateTime utc);

    /// <summary>false, თუ ლოკალური დრო არ არსებობს (მაგ. DST-ის "გამოტოვებული" საათი).</summary>
    bool TryToUtc(DateTime local, out DateTime utc);
}
