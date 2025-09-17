namespace be_movie_booking.Models;

public enum MovieFormat
{
    TwoD = 1,
    ThreeD = 2,
    Imax = 3
}

public class Showtime
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public string Language { get; set; } = "vi";
    public bool Subtitle { get; set; } = true;
    public MovieFormat Format { get; set; } = MovieFormat.TwoD;

    public int BasePriceMinor { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SeatTypePrice> SeatTypePrices { get; set; } = new List<SeatTypePrice>();
}

public class SeatTypePrice
{
    public Guid Id { get; set; }
    public Guid ShowtimeId { get; set; }
    public Showtime Showtime { get; set; } = null!;
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
}
