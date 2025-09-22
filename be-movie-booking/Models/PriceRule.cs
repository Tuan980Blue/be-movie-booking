namespace be_movie_booking.Models;

public enum DayType
{
    Weekday = 1,
    Weekend = 2
}

public class PriceRule
{
    public Guid Id { get; set; }

    public Guid? CinemaId { get; set; }
    public Cinema? Cinema { get; set; }

    public DayType DayType { get; set; }
    public SeatType SeatType { get; set; }

    public int PriceMinor { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}


