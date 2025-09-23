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
    public bool IsActive { get; set; } = true;
    public string? SpecialNotes { get; set; }             // Ghi chú đặc biệt
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }                 // User tạo ghế
    public Guid? UpdatedBy { get; set; }                 // User cập nhật ghế
}
