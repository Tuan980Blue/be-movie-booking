namespace be_movie_booking.Models;

public enum PaymentProvider
{
    VnPay = 1,
    MoMo = 2,
    Stripe = 3
}

public enum PaymentStatus
{
    Initiated = 1,
    Pending = 2,
    Succeeded = 3,
    Failed = 4,
    Canceled = 5,
    Refunded = 6,
    PartiallyRefunded = 7
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public PaymentProvider Provider { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

    public string? ProviderTxnId { get; set; }
    public string? ReturnUrl { get; set; }
    public string? NotifyUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
}

public class PaymentEvent
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public string EventType { get; set; } = null!;
    public string RawPayloadJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
