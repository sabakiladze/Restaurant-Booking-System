using System.Data;
using Microsoft.EntityFrameworkCore;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Infrastructure.Persistence;

public class ReservationRepository(AppDbContext db) : IReservationRepository
{
    public Task<List<Table>> GetTablesAsync(CancellationToken ct = default) =>
        db.Tables.AsNoTracking().OrderBy(t => t.Number).ToListAsync(ct);

    public Task<Table?> GetTableAsync(int id, CancellationToken ct = default) =>
        db.Tables.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<bool> TryAddAsync(Reservation r, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var overlap = await db.Reservations.AnyAsync(x =>
            x.TableId == r.TableId
            && x.Status == ReservationStatus.Confirmed
            && x.StartTime < r.EndTime
            && r.StartTime < x.EndTime, ct);

        if (overlap) return false;

        db.Reservations.Add(r);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Reservations.Include(r => r.User).Include(r => r.Table).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task UpdateAsync(Reservation r, CancellationToken ct = default)
    {
        db.Reservations.Update(r);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<Reservation>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        db.Reservations.AsNoTracking().Include(r => r.Table)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartTime)
            .ToListAsync(ct);

    public Task<List<Reservation>> GetUpcomingAsync(DateTime fromUtc, CancellationToken ct = default) =>
        db.Reservations.AsNoTracking().Include(r => r.Table).Include(r => r.User)
            .Where(r => r.StartTime >= fromUtc && r.Status == ReservationStatus.Confirmed)
            .OrderBy(r => r.StartTime)
            .ToListAsync(ct);
}
