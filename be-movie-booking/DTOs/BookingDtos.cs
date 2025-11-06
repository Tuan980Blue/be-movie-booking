using System.ComponentModel.DataAnnotations;
using be_movie_booking.Models;
using System.Text.Json;

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

    [Required(ErrorMessage = "Thông tin khách hàng là bắt buộc")]
    public CustomerInfoDto CustomerInfo { get; set; } = null!;

    public string? PromotionCode { get; set; }
}

public class CustomerInfoDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = null!;
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
    public CustomerInfoDto? CustomerInfo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BookingItemResponseDto> Items { get; set; } = new();
    public List<TicketResponseDto> Tickets { get; set; } = new();
}

public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
}

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

public class ShowtimeInfoDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = null!;
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public Guid CinemaId { get; set; }
    public string CinemaName { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Language { get; set; } = null!;
    public string Format { get; set; } = null!;
}

public class SeatInfoDto
{
    public Guid Id { get; set; }
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public SeatType SeatType { get; set; }
}

public class TicketResponseDto
{
    public Guid Id { get; set; }
    public string TicketCode { get; set; } = null!;
    public string? TicketQr { get; set; }
    public Guid ShowtimeId { get; set; }
    public Guid SeatId { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
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

