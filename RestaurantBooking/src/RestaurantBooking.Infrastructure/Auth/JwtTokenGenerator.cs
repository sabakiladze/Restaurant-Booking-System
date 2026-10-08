using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Infrastructure.Auth;

public class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
{
    public string Generate(User user)
    {
        var s = options.Value;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.FullName),
            new Claim("email", user.Email),
            new Claim("role", user.Role.ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: s.Issuer,
            audience: s.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(s.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
