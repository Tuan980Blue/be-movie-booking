namespace be_movie_booking.Models;

public enum EntityStatus
{
    Active = 1,
    Inactive = 2
}

public enum SeatType
{
    Standard = 1,
    Vip = 2,
    Couple = 3,
    Accessible = 4
}

public class Cinema
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

public class Room
{
    public Guid Id { get; set; }
    public Guid CinemaId { get; set; }
    public Cinema Cinema { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int TotalSeats { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}

public class Seat
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public SeatType SeatType { get; set; } = SeatType.Standard;
    public bool IsActive { get; set; } = true;
}
