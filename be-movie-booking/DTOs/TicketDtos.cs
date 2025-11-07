using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để trả về ticket
/// </summary>
public class TicketResponseDto
{
    public Guid Id { get; set; }
    public string TicketCode { get; set; } = null!;
    public string? TicketQr { get; set; }
    public Guid ShowtimeId { get; set; }
    public Guid SeatId { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInBy { get; set; }
}

/// <summary>
/// DTO để verify QR code và trả về thông tin booking (bao gồm tất cả tickets)
/// </summary>
public class BookingVerifyResponseDto
{
    public string BookingCode { get; set; } = null!;
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public bool IsFullyCheckedIn { get; set; } // Tất cả tickets đã được check-in
    
    // Thông tin booking
    public BookingInfoDto Booking { get; set; } = null!;
    
    // Danh sách tất cả tickets trong booking
    public List<TicketInfoDto> Tickets { get; set; } = new();
}

/// <summary>
/// Thông tin vé chi tiết
/// </summary>
public class TicketInfoDto
{
    public Guid Id { get; set; }
    public string TicketCode { get; set; } = null!;
    public Guid ShowtimeId { get; set; }
    public ShowtimeInfoDto Showtime { get; set; } = null!;
    public Guid SeatId { get; set; }
    public SeatInfoDto Seat { get; set; } = null!;
    public TicketStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? CheckedInAt { get; set; }
}

/// <summary>
/// Thông tin booking đơn giản (dùng cho ticket verify)
/// </summary>
public class BookingInfoDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public BookingStatus Status { get; set; }
    public CustomerInfoDto? CustomerInfo { get; set; }
    public DateTime CreatedAt { get; set; }
}

