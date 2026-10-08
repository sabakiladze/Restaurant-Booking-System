using Moq;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Application.Services;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();

    private AuthService Sut() => new(_users.Object, _hasher.Object, _jwt.Object);

    [Fact]
    public async Task Register_ShortPassword_Fails()
    {
        var r = await Sut().RegisterAsync("Nika", "n@x.ge", "short");

        Assert.False(r.Success);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Fails()
    {
        _users.Setup(u => u.ExistsByEmailAsync("n@x.ge", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var r = await Sut().RegisterAsync("Nika", "N@X.ge", "password123");

        Assert.False(r.Success);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_Success_NormalizesEmail_AndStoresHashNotPassword()
    {
        _hasher.Setup(h => h.Hash("password123")).Returns("HASH");
        User? saved = null;
        _users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Callback<User, CancellationToken>((u, _) => saved = u)
              .Returns(Task.CompletedTask);

        var r = await Sut().RegisterAsync("  Nika  ", "  N@X.ge ", "password123");

        Assert.True(r.Success);
        Assert.NotNull(saved);
        Assert.Equal("n@x.ge", saved!.Email);
        Assert.Equal("Nika", saved.FullName);
        Assert.Equal("HASH", saved.PasswordHash);
        Assert.Equal(UserRole.Customer, saved.Role);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_GiveSameError()
    {
        var known = new User { Email = "a@x.ge", PasswordHash = "H" };
        _users.Setup(u => u.GetByEmailAsync("a@x.ge", It.IsAny<CancellationToken>())).ReturnsAsync(known);
        _users.Setup(u => u.GetByEmailAsync("nobody@x.ge", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _hasher.Setup(h => h.Verify("H", "wrong")).Returns(false);

        var unknown = await Sut().LoginAsync("nobody@x.ge", "whatever");
        var wrongPwd = await Sut().LoginAsync("a@x.ge", "wrong");

        Assert.False(unknown.Success);
        Assert.False(wrongPwd.Success);
        Assert.Equal(unknown.Error, wrongPwd.Error);
        _jwt.Verify(j => j.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_Success_ReturnsToken()
    {
        var user = new User { Email = "a@x.ge", PasswordHash = "H" };
        _users.Setup(u => u.GetByEmailAsync("a@x.ge", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("H", "pw")).Returns(true);
        _jwt.Setup(j => j.Generate(user)).Returns("TOKEN");

        var r = await Sut().LoginAsync(" A@x.ge ", "pw");

        Assert.True(r.Success);
        Assert.Equal("TOKEN", r.Value);
    }
}
