using System.ComponentModel.DataAnnotations;
using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để tạo booking mới
/// </summary>
public class CreateBookingDto
{
    [Required(ErrorMessage = "ShowtimeId là bắt buộc")]
    public Guid ShowtimeId { get; set; }

    [Required(ErrorMessage = "Danh sách ghế là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 ghế")]
    public List<Guid> SeatIds { get; set; } = new();

    public string? PromotionCode { get; set; }
}

/// <summary>
/// DTO để tìm kiếm bookings
/// </summary>
public class BookingSearchDto
{
    public Guid? UserId { get; set; }
    public BookingStatus? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";
}

/// <summary>
/// DTO để trả về booking
/// </summary>
public class BookingResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public Guid? UserId { get; set; }
    public UserInfoDto? User { get; set; }
    public BookingStatus Status { get; set; }
    public int TotalAmountMinor { get; set; }
    public string Currency { get; set; } = "VND";
    public string? BookingQr { get; set; }
    public CustomerInfoDto? CustomerInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BookingItemResponseDto> Items { get; set; } = new();
    public List<TicketResponseDto> Tickets { get; set; } = new();
}

/// <summary>
/// DTO để trả về booking item
/// </summary>
public class BookingItemResponseDto
{
    public Guid Id { get; set; }
    public Guid ShowtimeId { get; set; }
    public ShowtimeInfoDto Showtime { get; set; } = null!;
    public Guid SeatId { get; set; }
    public SeatInfoDto Seat { get; set; } = null!;
    public int SeatPriceMinor { get; set; }
    public string? PriceCategory { get; set; }
    public BookingItemStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO để xác nhận booking
/// </summary>
public class ConfirmBookingDto
{
    [Required(ErrorMessage = "PaymentId là bắt buộc")]
    public Guid PaymentId { get; set; }
}

/// <summary>
/// DTO để hủy booking
/// </summary>
public class CancelBookingDto
{
    public string? Reason { get; set; }
}

