using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Infrastructure.Auth;
using RestaurantBooking.Infrastructure.Email;
using RestaurantBooking.Infrastructure.Persistence;

namespace RestaurantBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o.UseSqlServer(config.GetConnectionString("Default")));

        services.Configure<SmtpSettings>(config.GetSection("Smtp"));
        services.Configure<JwtSettings>(config.GetSection("Jwt"));
        services.Configure<RestaurantOptions>(config.GetSection("Restaurant"));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<DbSeeder>();

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<Application.Abstractions.IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IRestaurantClock, RestaurantClock>();

        return services;
    }
}
