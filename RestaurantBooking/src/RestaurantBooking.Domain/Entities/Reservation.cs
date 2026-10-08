namespace RestaurantBooking.Domain.Entities;

public enum ReservationStatus { Confirmed, Cancelled }

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int TableId { get; set; }
    public Table Table { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public int GuestCount { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

    public void Cancel() => Status = ReservationStatus.Cancelled;
}
