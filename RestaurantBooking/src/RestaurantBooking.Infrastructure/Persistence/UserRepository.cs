using Microsoft.EntityFrameworkCore;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Infrastructure.Persistence;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
