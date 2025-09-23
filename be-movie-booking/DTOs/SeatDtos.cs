using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để đọc thông tin ghế ngồi
/// </summary>
public class SeatReadDto
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public string SeatType { get; set; } = null!;
    public bool IsActive { get; set; }
    
    // Layout information
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public string? SpecialNotes { get; set; }
    
    // Audit information
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO để hiển thị danh sách ghế ngồi
/// </summary>
public class SeatListDto
{
    public Guid Id { get; set; }
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public string SeatType { get; set; } = null!;
    public bool IsActive { get; set; }
    
    public int? PositionX { get; set; }
    
    public int? PositionY { get; set; }
    
    public string? SpecialNotes { get; set; }
}

/// <summary>
/// DTO để tạo ghế ngồi mới
/// </summary>
public class CreateSeatDto
{
    [Required(ErrorMessage = "Nhãn hàng là bắt buộc")]
    [StringLength(10, ErrorMessage = "Nhãn hàng không được vượt quá 10 ký tự")]
    public string RowLabel { get; set; } = null!;

    [Required(ErrorMessage = "Số ghế là bắt buộc")]
    [Range(1, 100, ErrorMessage = "Số ghế phải từ 1 đến 100")]
    public int SeatNumber { get; set; }

    [Required(ErrorMessage = "Loại ghế là bắt buộc")]
    public string SeatType { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    // Layout properties
    [Range(0, 1000, ErrorMessage = "Tọa độ X phải từ 0 đến 1000")]
    public int? PositionX { get; set; }

    [Range(0, 1000, ErrorMessage = "Tọa độ Y phải từ 0 đến 1000")]
    public int? PositionY { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú đặc biệt không được vượt quá 500 ký tự")]
    public string? SpecialNotes { get; set; }
}

/// <summary>
/// DTO để cập nhật thông tin ghế ngồi
/// </summary>
public class UpdateSeatDto
{
    [Required(ErrorMessage = "Nhãn hàng là bắt buộc")]
    [StringLength(10, ErrorMessage = "Nhãn hàng không được vượt quá 10 ký tự")]
    public string RowLabel { get; set; } = null!;

    [Required(ErrorMessage = "Số ghế là bắt buộc")]
    [Range(1, 100, ErrorMessage = "Số ghế phải từ 1 đến 100")]
    public int SeatNumber { get; set; }

    [Required(ErrorMessage = "Loại ghế là bắt buộc")]
    public string SeatType { get; set; } = null!;

    public bool IsActive { get; set; }

    // Layout properties
    [Range(0, 1000, ErrorMessage = "Tọa độ X phải từ 0 đến 1000")]
    public int? PositionX { get; set; }

    [Range(0, 1000, ErrorMessage = "Tọa độ Y phải từ 0 đến 1000")]
    public int? PositionY { get; set; }
    
    [StringLength(500, ErrorMessage = "Ghi chú đặc biệt không được vượt quá 500 ký tự")]
    public string? SpecialNotes { get; set; }
}

/// <summary>
/// DTO để tìm kiếm và lọc ghế ngồi
/// </summary>
public class SeatSearchDto
{
    [StringLength(10, ErrorMessage = "Nhãn hàng không được vượt quá 10 ký tự")]
    public string? RowLabel { get; set; }

    [Range(1, 100, ErrorMessage = "Số ghế phải từ 1 đến 100")]
    public int? SeatNumber { get; set; }

    public string? SeatType { get; set; }
    public bool? IsActive { get; set; }

    [Range(1, 100, ErrorMessage = "Số trang phải từ 1 đến 100")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Kích thước trang phải từ 1 đến 50")]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "RowLabel";
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// DTO để tạo layout ghế hàng loạt
/// </summary>
public class CreateSeatLayoutDto
{
    [Required(ErrorMessage = "Số hàng là bắt buộc")]
    [Range(1, 50, ErrorMessage = "Số hàng phải từ 1 đến 50")]
    public int Rows { get; set; }

    [Required(ErrorMessage = "Số ghế mỗi hàng là bắt buộc")]
    [Range(1, 50, ErrorMessage = "Số ghế mỗi hàng phải từ 1 đến 50")]
    public int SeatsPerRow { get; set; }

    [StringLength(10, ErrorMessage = "Nhãn hàng bắt đầu không được vượt quá 10 ký tự")]
    public string RowStartLabel { get; set; } = "A";

    [Required(ErrorMessage = "Loại ghế mặc định là bắt buộc")]
    public string DefaultSeatType { get; set; } = "Standard";

    // Middle aisle removed

    // Layout properties
    public int? StartPositionX { get; set; } = 0;
    public int? StartPositionY { get; set; } = 0;
    public int? SeatSpacingX { get; set; } = 50;
    public int? SeatSpacingY { get; set; } = 50;

    // Rows that should be generated entirely as Couple seats (pairs)
    public List<string>? CoupleRows { get; set; } = new();

    // Vertical spacing between two seats in a couple (pixels)
    public int? CoupleSpacingY { get; set; } = 100; // legacy, unused in horizontal layout

    // Horizontal spacing between two seats in a couple (pixels)
    public int? CoupleSpacingX { get; set; } = 100;
}

/// <summary>
/// DTO để hiển thị layout ghế
/// </summary>
public class SeatLayoutDto
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public List<SeatRowDto> Rows { get; set; } = new();
    public List<SeatTypeInfoDto> SeatTypes { get; set; } = new();
    public string ScreenPosition { get; set; } = "front";
}

/// <summary>
/// DTO cho một hàng ghế
/// </summary>
public class SeatRowDto
{
    public string RowLabel { get; set; } = null!;
    public List<SeatDto> Seats { get; set; } = new();
}

/// <summary>
/// DTO cho một ghế trong layout
/// </summary>
public class SeatDto
{
    public Guid Id { get; set; }
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public string SeatType { get; set; } = null!;
    public bool IsActive { get; set; }
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public string? SpecialNotes { get; set; }
}

/// <summary>
/// DTO cho thông tin loại ghế
/// </summary>
public class SeatTypeInfoDto
{
    public string Type { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Color { get; set; } = null!;
    public string Description { get; set; } = null!;
}

/// <summary>
/// DTO để thống kê ghế ngồi
/// </summary>
public class SeatStatsDto
{
    public int TotalSeats { get; set; }
    public int ActiveSeats { get; set; }
    public Dictionary<string, int> SeatsByType { get; set; } = new();
}
