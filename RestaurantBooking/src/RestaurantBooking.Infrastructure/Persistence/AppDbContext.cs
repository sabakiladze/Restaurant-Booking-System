using Microsoft.EntityFrameworkCore;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Table> Tables => Set<Table>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(u => u.Email).IsUnique();
        });

        b.Entity<Table>(e =>
        {
            e.HasIndex(t => t.Number).IsUnique();
            e.HasData(
                new Table { Id = 1, Number = 1, Capacity = 2 },
                new Table { Id = 2, Number = 2, Capacity = 2 },
                new Table { Id = 3, Number = 3, Capacity = 4 },
                new Table { Id = 4, Number = 4, Capacity = 4 },
                new Table { Id = 5, Number = 5, Capacity = 6 });
        });

        b.Entity<Reservation>(e =>
        {
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Table).WithMany().HasForeignKey(r => r.TableId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.TableId, r.StartTime, r.EndTime });
        });
    }
}
