using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}
