using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để đọc thông tin phòng chiếu
/// </summary>
public class RoomReadDto
{
    public Guid Id { get; set; }
    public Guid CinemaId { get; set; }
    public string CinemaName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int TotalSeats { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO để hiển thị danh sách phòng chiếu
/// </summary>
public class RoomListDto
{
    public Guid Id { get; set; }
    public Guid CinemaId { get; set; }
    public string CinemaName { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public int TotalSeats { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO để tạo phòng chiếu mới
/// </summary>
public class CreateRoomDto
{
    [Required(ErrorMessage = "Tên phòng chiếu là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên phòng chiếu không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Mã phòng chiếu là bắt buộc")]
    [StringLength(20, ErrorMessage = "Mã phòng chiếu không được vượt quá 20 ký tự")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Tổng số ghế là bắt buộc")]
    [Range(1, 1000, ErrorMessage = "Tổng số ghế phải từ 1 đến 1000")]
    public int TotalSeats { get; set; }
}

/// <summary>
/// DTO để cập nhật thông tin phòng chiếu
/// </summary>
public class UpdateRoomDto
{
    [Required(ErrorMessage = "Tên phòng chiếu là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên phòng chiếu không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Mã phòng chiếu là bắt buộc")]
    [StringLength(20, ErrorMessage = "Mã phòng chiếu không được vượt quá 20 ký tự")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Tổng số ghế là bắt buộc")]
    [Range(1, 1000, ErrorMessage = "Tổng số ghế phải từ 1 đến 1000")]
    public int TotalSeats { get; set; }
}

/// <summary>
/// DTO để thay đổi trạng thái phòng chiếu
/// </summary>
public class ChangeRoomStatusDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public string Status { get; set; } = null!;
}

/// <summary>
/// DTO để tìm kiếm và lọc phòng chiếu
/// </summary>
public class RoomSearchDto
{
    [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự")]
    public string? Search { get; set; }

    public string? Status { get; set; }

    [Range(1, 100, ErrorMessage = "Số trang phải từ 1 đến 100")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Kích thước trang phải từ 1 đến 50")]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "CreatedAt";

    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// DTO để thống kê phòng chiếu
/// </summary>
public class RoomStatsDto
{
    public int TotalSeats { get; set; }
    public int ActiveShowtimes { get; set; }
    public int TotalShowtimes { get; set; }
}
