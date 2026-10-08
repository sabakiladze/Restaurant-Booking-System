using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Application.Services;
using RestaurantBooking.Domain.Entities;

namespace RestaurantBooking.Tests;

public class ReservationServiceTests
{
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Future = Now.AddDays(1);

    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IRestaurantClock> _clock = new();

    private readonly User _user = new() { FullName = "Nika", Email = "nika@x.ge" };
    private readonly Table _table = new() { Id = 1, Number = 5, Capacity = 4 };

    public ReservationServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _clock.Setup(c => c.ToLocal(It.IsAny<DateTime>())).Returns((DateTime d) => d);
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _reservations.Setup(r => r.GetTableAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(_table);
        _reservations.Setup(r => r.TryAddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private ReservationService Sut() => new(
        _reservations.Object, _users.Object, _email.Object, _clock.Object,
        NullLogger<ReservationService>.Instance);

    // ---------- Create ----------

    [Fact]
    public async Task Create_InThePast_Fails_WithoutTouchingRepository()
    {
        var r = await Sut().CreateAsync(_user.Id, 1, Now.AddMinutes(-1), 2);

        Assert.False(r.Success);
        _reservations.Verify(x => x.TryAddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_UnknownUser_Fails()
    {
        var r = await Sut().CreateAsync(Guid.NewGuid(), 1, Future, 2);
        Assert.False(r.Success);
    }

    [Fact]
    public async Task Create_UnknownTable_Fails()
    {
        var r = await Sut().CreateAsync(_user.Id, 999, Future, 2);
        Assert.False(r.Success);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)] // capacity = 4
    public async Task Create_InvalidGuestCount_Fails(int guests)
    {
        var r = await Sut().CreateAsync(_user.Id, 1, Future, guests);

        Assert.False(r.Success);
        _reservations.Verify(x => x.TryAddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_TableTaken_Fails_AndSendsNoEmail()
    {
        _reservations.Setup(x => x.TryAddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var r = await Sut().CreateAsync(_user.Id, 1, Future, 2);

        Assert.False(r.Success);
        _email.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_Success_SavesTwoHourReservation_AndEmailsUser()
    {
        Reservation? saved = null;
        _reservations.Setup(x => x.TryAddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
                     .Callback<Reservation, CancellationToken>((res, _) => saved = res)
                     .ReturnsAsync(true);

        var r = await Sut().CreateAsync(_user.Id, 1, Future, 3);

        Assert.True(r.Success);
        Assert.NotNull(saved);
        Assert.Equal(_user.Id, saved!.UserId);
        Assert.Equal(Future, saved.StartTime);
        Assert.Equal(Future.AddHours(2), saved.EndTime);
        Assert.Equal(3, saved.GuestCount);
        _email.Verify(e => e.SendAsync("nika@x.ge", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_EmailFailure_DoesNotFailTheReservation()
    {
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("smtp down"));

        var r = await Sut().CreateAsync(_user.Id, 1, Future, 2);

        Assert.True(r.Success);
    }

    // ---------- Cancel ----------

    private Reservation ExistingReservation(Guid ownerId, DateTime start) => new()
    {
        UserId = ownerId, User = _user, TableId = 1, Table = _table,
        StartTime = start, EndTime = start.AddHours(2), GuestCount = 2
    };

    [Fact]
    public async Task Cancel_ByOwner_Succeeds()
    {
        var res = ExistingReservation(_user.Id, Future);
        _reservations.Setup(x => x.GetByIdAsync(res.Id, It.IsAny<CancellationToken>())).ReturnsAsync(res);

        var r = await Sut().CancelAsync(res.Id, _user.Id, isAdmin: false);

        Assert.True(r.Success);
        Assert.Equal(ReservationStatus.Cancelled, res.Status);
        _reservations.Verify(x => x.UpdateAsync(res, It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(e => e.SendAsync("nika@x.ge", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_ByAnotherUser_Fails_AndDoesNotChangeStatus()
    {
        var res = ExistingReservation(_user.Id, Future);
        _reservations.Setup(x => x.GetByIdAsync(res.Id, It.IsAny<CancellationToken>())).ReturnsAsync(res);

        var r = await Sut().CancelAsync(res.Id, Guid.NewGuid(), isAdmin: false);

        Assert.False(r.Success);
        Assert.Equal(ReservationStatus.Confirmed, res.Status);
        _reservations.Verify(x => x.UpdateAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ByAdmin_OtherUsersReservation_Succeeds()
    {
        var res = ExistingReservation(_user.Id, Future);
        _reservations.Setup(x => x.GetByIdAsync(res.Id, It.IsAny<CancellationToken>())).ReturnsAsync(res);

        var r = await Sut().CancelAsync(res.Id, Guid.NewGuid(), isAdmin: true);

        Assert.True(r.Success);
        Assert.Equal(ReservationStatus.Cancelled, res.Status);
    }

    [Fact]
    public async Task Cancel_AlreadyStarted_Fails()
    {
        var res = ExistingReservation(_user.Id, Now.AddMinutes(-30));
        _reservations.Setup(x => x.GetByIdAsync(res.Id, It.IsAny<CancellationToken>())).ReturnsAsync(res);

        var r = await Sut().CancelAsync(res.Id, _user.Id, isAdmin: false);

        Assert.False(r.Success);
        Assert.Equal(ReservationStatus.Confirmed, res.Status);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_Fails()
    {
        var res = ExistingReservation(_user.Id, Future);
        res.Cancel();
        _reservations.Setup(x => x.GetByIdAsync(res.Id, It.IsAny<CancellationToken>())).ReturnsAsync(res);

        var r = await Sut().CancelAsync(res.Id, _user.Id, isAdmin: false);

        Assert.False(r.Success);
    }
}
