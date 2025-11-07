using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO tối thiểu để lưu draft booking vào Redis (chỉ thông tin cần thiết để confirm)
/// </summary>
public class BookingDraftDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid ShowtimeId { get; set; }
    
    /// <summary>
    /// Danh sách SeatId đã chọn
    /// </summary>
    public List<Guid> SeatIds { get; set; } = new();
    
    /// <summary>
    /// Giá của từng ghế (theo thứ tự SeatIds), đơn vị: minor (VND * 100)
    /// </summary>
    public List<int> SeatPricesMinor { get; set; } = new();
    
    /// <summary>
    /// Tổng tiền, đơn vị: minor (VND * 100)
    /// </summary>
    public int TotalAmountMinor { get; set; }
    
    /// <summary>
    /// Thông tin khách hàng (JSON serialized CustomerInfoDto)
    /// </summary>
    public string? CustomerContactJson { get; set; }
}

/// <summary>
/// DTO đơn giản để trả về booking draft (chưa thanh toán)
/// </summary>
public class BookingDraftResponseDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public BookingStatus Status { get; set; }
    public int TotalAmountMinor { get; set; }
    public string Currency { get; set; } = "VND";
    
    /// <summary>
    /// Thông tin khách hàng (nếu có userId thì lấy từ user, không thì null)
    /// </summary>
    public CustomerInfoDto? CustomerInfo { get; set; }
    
    /// <summary>
    /// Thông tin suất chiếu đơn giản
    /// </summary>
    public SimpleShowtimeInfoDto Showtime { get; set; } = null!;
    
    /// <summary>
    /// Danh sách ghế đã chọn (đơn giản)
    /// </summary>
    public List<BookingDraftItemDto> Seats { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // Thời gian hết hạn draft (3 phút)
}

/// <summary>
/// Thông tin suất chiếu đơn giản cho draft
/// </summary>
public class SimpleShowtimeInfoDto
{
    public Guid Id { get; set; }
    public string MovieTitle { get; set; } = null!;
    public string CinemaName { get; set; } = null!;
    public string RoomName { get; set; } = null!;
    public DateTime StartUtc { get; set; }
    public string Format { get; set; } = null!;
}

/// <summary>
/// Item đơn giản cho draft booking
/// </summary>
public class BookingDraftItemDto
{
    public Guid SeatId { get; set; }
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public int SeatPriceMinor { get; set; }
}

