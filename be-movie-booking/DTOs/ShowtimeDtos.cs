using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để tạo suất chiếu mới
/// </summary>
public class CreateShowtimeDto
{
    [Required(ErrorMessage = "ID phim là bắt buộc")]
    public Guid MovieId { get; set; }

    [Required(ErrorMessage = "ID phòng chiếu là bắt buộc")]
    public Guid RoomId { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
    public DateTime StartUtc { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
    public DateTime EndUtc { get; set; }

    [Required(ErrorMessage = "Ngôn ngữ là bắt buộc")]
    [StringLength(10, ErrorMessage = "Ngôn ngữ không được vượt quá 10 ký tự")]
    public string Language { get; set; } = "vi";

    public bool Subtitle { get; set; } = true;

    [Required(ErrorMessage = "Định dạng phim là bắt buộc")]
    public string Format { get; set; } = "TwoD";

    [Required(ErrorMessage = "Giá cơ bản là bắt buộc")]
    [Range(0, int.MaxValue, ErrorMessage = "Giá cơ bản phải lớn hơn hoặc bằng 0")]
    public int BasePriceMinor { get; set; }
}

/// <summary>
/// DTO để cập nhật suất chiếu
/// </summary>
public class UpdateShowtimeDto
{
    [Required(ErrorMessage = "ID phim là bắt buộc")]
    public Guid MovieId { get; set; }

    [Required(ErrorMessage = "ID phòng chiếu là bắt buộc")]
    public Guid RoomId { get; set; }

    [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
    public DateTime StartUtc { get; set; }

    [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
    public DateTime EndUtc { get; set; }

    [Required(ErrorMessage = "Ngôn ngữ là bắt buộc")]
    [StringLength(10, ErrorMessage = "Ngôn ngữ không được vượt quá 10 ký tự")]
    public string Language { get; set; } = "vi";

    public bool Subtitle { get; set; } = true;

    [Required(ErrorMessage = "Định dạng phim là bắt buộc")]
    public string Format { get; set; } = "TwoD";

    [Required(ErrorMessage = "Giá cơ bản là bắt buộc")]
    [Range(0, int.MaxValue, ErrorMessage = "Giá cơ bản phải lớn hơn hoặc bằng 0")]
    public int BasePriceMinor { get; set; }
}

/// <summary>
/// DTO để tìm kiếm và lọc suất chiếu
/// </summary>
public class ShowtimeSearchDto
{
    public Guid? MovieId { get; set; }
    public Guid? CinemaId { get; set; }
    public Guid? RoomId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Language { get; set; }
    public string? Format { get; set; }

    [Range(1, 100, ErrorMessage = "Số trang phải từ 1 đến 100")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Kích thước trang phải từ 1 đến 50")]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "StartUtc";

    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// DTO để đọc thông tin suất chiếu
/// </summary>
public class ShowtimeReadDto
{
    public Guid Id { get; set; }
    public Guid MovieId { get; set; }
    public string MovieTitle { get; set; } = null!;
    public int MovieDurationMinutes { get; set; }
    public string? MoviePosterUrl { get; set; }
    
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public string RoomCode { get; set; } = null!;
    
    public Guid CinemaId { get; set; }
    public string CinemaName { get; set; } = null!;
    public string CinemaAddress { get; set; } = null!;
    
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Language { get; set; } = null!;
    public bool Subtitle { get; set; }
    public string Format { get; set; } = null!;
    public int BasePriceMinor { get; set; }
    public DateTime CreatedAt { get; set; }
}
