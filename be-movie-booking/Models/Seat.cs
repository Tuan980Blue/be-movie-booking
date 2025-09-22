namespace be_movie_booking.Models;

/// <summary>
/// Loại ghế ngồi
/// </summary>
public enum SeatType
{
    Standard = 1,    // Ghế thường
    Vip = 2,         // Ghế VIP
    Couple = 3,      // Ghế đôi
    Accessible = 4   // Ghế khuyết tật
}

/// <summary>
/// Trạng thái ghế ngồi
/// </summary>
public enum SeatStatus
{
    Available = 1,     // Có thể đặt
    Occupied = 2,       // Đã được đặt
    Maintenance = 3,    // Đang bảo trì
    Disabled = 4        // Tạm ngưng sử dụng
}

/// <summary>
/// Model ghế ngồi trong phòng chiếu
/// </summary>
public class Seat
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    
    // Vị trí ghế
    public string RowLabel { get; set; } = null!;        // A, B, C...
    public int SeatNumber { get; set; }                  // 1, 2, 3...
    
    // Layout information (cho frontend render)
    public int? PositionX { get; set; }                  // Tọa độ X trong layout
    public int? PositionY { get; set; }                  // Tọa độ Y trong layout
    
    // Seat properties
    public SeatType SeatType { get; set; } = SeatType.Standard;
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public bool IsActive { get; set; } = true;
    
    // Accessibility & Special features
    public bool IsWheelchairAccessible { get; set; } = false;
    public bool HasExtraLegroom { get; set; } = false;
    public bool IsReclining { get; set; } = false;
    public string? SpecialNotes { get; set; }             // Ghi chú đặc biệt
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }                 // User tạo ghế
    public Guid? UpdatedBy { get; set; }                 // User cập nhật ghế
}
