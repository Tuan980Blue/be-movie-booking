namespace be_movie_booking.Models;

public enum BookingStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 3,
    Expired = 4,
    Refunding = 5,
    Refunded = 6
}

public enum BookingItemStatus
{
    Pending = 1,
    Confirmed = 2,
    Canceled = 3,
    Expired = 4
}

public class Booking
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public int TotalAmountMinor { get; set; }
    public string Currency { get; set; } = "VND";

    public string? CustomerContactJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class BookingItem
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid ShowtimeId { get; set; }
    public Showtime Showtime { get; set; } = null!;

    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public int SeatPriceMinor { get; set; }
    public string? PriceCategory { get; set; }

    public BookingItemStatus Status { get; set; } = BookingItemStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum TicketStatus
{
    Issued = 1,
    Void = 2
}

public class Ticket
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid ShowtimeId { get; set; }
    public Showtime Showtime { get; set; } = null!;

    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public string TicketCode { get; set; } = null!;
    public string? TicketQr { get; set; }

    public TicketStatus Status { get; set; } = TicketStatus.Issued;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
