namespace be_movie_booking.DTOs;

public class BookingListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public int TotalAmountMinor { get; set; }
    public be_movie_booking.Models.BookingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // Lightweight preview fields
    public string? MovieTitle { get; set; }
    public DateTime? StartUtc { get; set; }
    public string? CinemaName { get; set; }
    public int SeatsCount { get; set; }
}

public class BookingListLightResultDto
{
    public List<BookingListItemDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
}


