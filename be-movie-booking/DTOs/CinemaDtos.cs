using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để đọc thông tin rạp chiếu phim
/// </summary>
public class CinemaReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
}

/// <summary>
/// DTO để hiển thị danh sách rạp chiếu phim
/// </summary>
public class CinemaListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public int TotalRooms { get; set; }
}

/// <summary>
/// DTO để tạo rạp chiếu phim mới
/// </summary>
public class CreateCinemaDto
{
    [Required(ErrorMessage = "Tên rạp chiếu phim là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên rạp chiếu phim không được vượt quá 200 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Thành phố là bắt buộc")]
    [StringLength(100, ErrorMessage = "Thành phố không được vượt quá 100 ký tự")]
    public string City { get; set; } = null!;

    [Range(-90, 90, ErrorMessage = "Vĩ độ phải từ -90 đến 90")]
    public double? Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ phải từ -180 đến 180")]
    public double? Lng { get; set; }
}

/// <summary>
/// DTO để cập nhật thông tin rạp chiếu phim
/// </summary>
public class UpdateCinemaDto
{
    [Required(ErrorMessage = "Tên rạp chiếu phim là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên rạp chiếu phim không được vượt quá 200 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Địa chỉ là bắt buộc")]
    [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
    public string Address { get; set; } = null!;

    [Required(ErrorMessage = "Thành phố là bắt buộc")]
    [StringLength(100, ErrorMessage = "Thành phố không được vượt quá 100 ký tự")]
    public string City { get; set; } = null!;

    [Range(-90, 90, ErrorMessage = "Vĩ độ phải từ -90 đến 90")]
    public double? Lat { get; set; }

    [Range(-180, 180, ErrorMessage = "Kinh độ phải từ -180 đến 180")]
    public double? Lng { get; set; }
}

/// <summary>
/// DTO để thay đổi trạng thái rạp chiếu phim
/// </summary>
public class ChangeCinemaStatusDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public string Status { get; set; } = null!;
}

/// <summary>
/// DTO để tìm kiếm và lọc rạp chiếu phim
/// </summary>
public class CinemaSearchDto
{
    [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự")]
    public string? Search { get; set; }

    public string? City { get; set; }

    public string? Status { get; set; }

    [Range(1, 100, ErrorMessage = "Số trang phải từ 1 đến 100")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Kích thước trang phải từ 1 đến 50")]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "CreatedAt";

    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// DTO để thống kê rạp chiếu phim
/// </summary>
public class CinemaStatsDto
{
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int InactiveRooms { get; set; }
    public int TotalSeats { get; set; }
}
