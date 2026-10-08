using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Application.Abstractions;

public interface IReservationRepository
{
    Task<List<Table>> GetTablesAsync(CancellationToken ct = default);
    Task<Table?> GetTableAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// ატომურად ამოწმებს გადაფარვას და ამატებს ჯავშანს.
    /// აბრუნებს false-ს, თუ მაგიდა ამ დროს დაკავებულია.
    /// </summary>
    Task<bool> TryAddAsync(Reservation reservation, CancellationToken ct = default);

    /// <summary>აბრუნებს User-ითა და Table-ით.</summary>
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Reservation reservation, CancellationToken ct = default);
    Task<List<Reservation>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<Reservation>> GetUpcomingAsync(DateTime fromUtc, CancellationToken ct = default);
}
