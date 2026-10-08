using System.Net;
using Microsoft.Extensions.Logging;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Application.Common;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Application.Services;

public class ReservationService(
    IReservationRepository reservations,
    IUserRepository users,
    IEmailService email,
    IRestaurantClock clock,
    ILogger<ReservationService> logger)
{
    public static readonly TimeSpan Duration = TimeSpan.FromHours(2);

    public Task<List<Table>> GetTablesAsync(CancellationToken ct = default) => reservations.GetTablesAsync(ct);
    public Task<List<Reservation>> GetMineAsync(Guid userId, CancellationToken ct = default) => reservations.GetByUserAsync(userId, ct);
    public Task<List<Reservation>> GetUpcomingAsync(CancellationToken ct = default) => reservations.GetUpcomingAsync(clock.UtcNow, ct);

    public async Task<Result> CreateAsync(Guid userId, int tableId, DateTime startUtc, int guests, CancellationToken ct = default)
    {
        if (startUtc <= clock.UtcNow) return Result.Fail("selected time is in the past");

        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Result.Fail("user not found");

        var table = await reservations.GetTableAsync(tableId, ct);
        if (table is null) return Result.Fail("table not found");

        if (guests <= 0 || guests > table.Capacity)
            return Result.Fail($"this table can accommodate 1 to {table.Capacity} guests.");

        var reservation = new Reservation
        {
            UserId = userId,
            TableId = tableId,
            StartTime = startUtc,
            EndTime = startUtc + Duration,
            GuestCount = guests
        };

        if (!await reservations.TryAddAsync(reservation, ct))
            return Result.Fail("this table is occupied at the selected time.");

        await TrySendAsync(user.Email, "Reservation confirmed",
            $"<p>Hello, {WebUtility.HtmlEncode(user.FullName)}!</p>" +
            $"<p>Your reservation is confirmed: Table №{table.Number}, " +
            $"{clock.ToLocal(startUtc):yyyy-MM-dd HH:mm}, Guests: {guests}.</p>", ct);

        return Result.Ok();
    }

    public async Task<Result> CancelAsync(Guid reservationId, Guid requesterId, bool isAdmin, CancellationToken ct = default)
    {
        var reservation = await reservations.GetByIdAsync(reservationId, ct);

        if (reservation is null || (!isAdmin && reservation.UserId != requesterId))
            return Result.Fail("reservation not found.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return Result.Fail("reservation is already cancelled.");

        if (reservation.StartTime <= clock.UtcNow)
            return Result.Fail("cannot cancel a reservation that has already started or passed.");

        reservation.Cancel();
        await reservations.UpdateAsync(reservation, ct);

        await TrySendAsync(reservation.User.Email, "Reservation cancelled",
            $"<p>Your reservation (Table №{reservation.Table.Number}, " +
            $"{clock.ToLocal(reservation.StartTime):yyyy-MM-dd HH:mm}) has been cancelled.</p>", ct);

        return Result.Ok();
    }

 
    private async Task TrySendAsync(string to, string subject, string html, CancellationToken ct)
    {
        try
        {
            await email.SendAsync(to, subject, html, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to send email '{Subject}'", subject);
        }
    }
}
