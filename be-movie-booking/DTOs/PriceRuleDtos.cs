using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

public class PriceRuleCreateDto
{
    public Guid? CinemaId { get; set; }
    public DayType DayType { get; set; }
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PriceRuleUpdateDto
{
    public Guid Id { get; set; }
    public Guid? CinemaId { get; set; }
    public DayType DayType { get; set; }
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PriceRuleSearchDto
{
    public Guid? CinemaId { get; set; }
    public DayType? DayType { get; set; }
    public SeatType? SeatType { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PriceRuleResponseDto
{
    public Guid Id { get; set; }
    public Guid? CinemaId { get; set; }
    public DayType DayType { get; set; }
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PricingQuoteRequestDto
{
    public Guid ShowtimeId { get; set; }
    public Guid SeatId { get; set; }
}

public class PricingQuoteResponseDto
{
    public Guid ShowtimeId { get; set; }
    public Guid SeatId { get; set; }
    public int PriceMinor { get; set; }
    public string Currency { get; set; } = "VND";
    public DayType DayType { get; set; }
    public SeatType SeatType { get; set; }
    public Guid CinemaId { get; set; }
    public bool UsedGlobalRule { get; set; }
}


