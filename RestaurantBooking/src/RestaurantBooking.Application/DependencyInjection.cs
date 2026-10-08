using Microsoft.Extensions.DependencyInjection;
using RestaurantBooking.Application.Services;

namespace RestaurantBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ReservationService>();
        return services;
    }
}
