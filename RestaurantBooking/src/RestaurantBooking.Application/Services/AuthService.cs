using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Application.Common;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator jwt)
{
    public const int MinPasswordLength = 8;

    public async Task<Result> RegisterAsync(string fullName, string email, string password, CancellationToken ct = default)
    {
        email = Normalize(email);

        if (string.IsNullOrWhiteSpace(fullName)) return Result.Fail("name is necessary.");
        if (string.IsNullOrWhiteSpace(email)) return Result.Fail("email is necessary.");
        if (password is null || password.Length < MinPasswordLength)
            return Result.Fail($"password minimum {MinPasswordLength} characters.");

        if (await users.ExistsByEmailAsync(email, ct))
            return Result.Fail("this email is already registered.");

        await users.AddAsync(new User
        {
            FullName = fullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(password)
        }, ct);

        return Result.Ok();
    }

    public async Task<Result<string>> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        const string invalid = "invalid email or password";

        var user = await users.GetByEmailAsync(Normalize(email), ct);
        if (user is null || !hasher.Verify(user.PasswordHash, password ?? ""))
            return Result<string>.Fail(invalid);

        return Result<string>.Ok(jwt.Generate(user));
    }

    private static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();
}
