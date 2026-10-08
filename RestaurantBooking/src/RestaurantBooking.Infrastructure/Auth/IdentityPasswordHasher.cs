using Microsoft.AspNetCore.Identity;
using RestaurantBooking.Application.Abstractions;

namespace RestaurantBooking.Infrastructure.Auth;

public class IdentityPasswordHasher : Application.Abstractions.IPasswordHasher
{
    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string hash, string password) =>
        _inner.VerifyHashedPassword(null!, hash, password) != PasswordVerificationResult.Failed;
}
