using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Infrastructure.Persistence;

public class DbSeeder(AppDbContext db, IPasswordHasher hasher, IConfiguration config)
{
    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        var email = config["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        db.Users.Add(new User
        {
            FullName = "Admin",
            Email = email,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin
        });
        await db.SaveChangesAsync();
    }
}
