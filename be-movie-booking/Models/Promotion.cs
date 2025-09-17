namespace be_movie_booking.Models;

public enum PromotionType
{
    Percent = 1,
    Fixed = 2
}

public class Promotion
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PromotionType Type { get; set; }
    public int Value { get; set; }
    public int? MaxDiscountMinor { get; set; }
    public int? MinOrderMinor { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int? UsageLimit { get; set; }
    public int? PerUserLimit { get; set; }
    public string? ConditionsJson { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PromotionUsage
{
    public Guid Id { get; set; }
    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public int DiscountMinor { get; set; }
}
