using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

public class SeatLockRequestDto
{
    [Required]
    public Guid ShowtimeId { get; set; }

    [Required]
    [MinLength(1)]
    public IEnumerable<Guid> SeatIds { get; set; } = Array.Empty<Guid>();

    public Guid? UserId { get; set; }
}

public class SeatLockInfo
{
    public Guid UserId { get; set; }
    public DateTime LockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
public class SeatUnlockRequestDto
{
    [Required]
    public Guid ShowtimeId { get; set; }

    [Required]
    [MinLength(1)]
    public IEnumerable<Guid> SeatIds { get; set; } = Array.Empty<Guid>();

    public Guid? UserId { get; set; }
}

public class LockedSeatsResponseDto
{
    public Guid ShowtimeId { get; set; }
    public List<Guid> LockedSeatIds { get; set; } = new();
}

public class SeatLockResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid ShowtimeId { get; set; }
    public List<Guid> LockedSeatIds { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}


