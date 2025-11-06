using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

public class PriceRuleCreateDto
{
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PriceRuleUpdateDto
{
    public Guid Id { get; set; }
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PriceRuleSearchDto
{
    public SeatType? SeatType { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PriceRuleResponseDto
{
    public Guid Id { get; set; }
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO để tính giá cho nhiều ghế - chỉ cần SeatIds
/// </summary>
public class PricingQuoteRequestDto
{
    public List<Guid> SeatIds { get; set; } = new();
}

/// <summary>
/// Response DTO cho một ghế
/// </summary>
public class PricingQuoteItemDto
{
    public Guid SeatId { get; set; }
    public SeatType SeatType { get; set; }
    public int PriceMinor { get; set; }
}

/// <summary>
/// Response DTO cho nhiều ghế
/// </summary>
public class PricingQuoteResponseDto
{
    public List<PricingQuoteItemDto> Quotes { get; set; } = new();
    public int TotalAmountMinor { get; set; }
    public string Currency { get; set; } = "VND";
}



