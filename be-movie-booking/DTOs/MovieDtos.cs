using System.ComponentModel.DataAnnotations;
using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để tạo phim mới
/// </summary>
public class CreateMovieDto
{
    [Required(ErrorMessage = "Tiêu đề phim là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề phim không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Tiêu đề gốc không được vượt quá 200 ký tự")]
    public string? OriginalTitle { get; set; }

    [Required(ErrorMessage = "Thời lượng phim là bắt buộc")]
    [Range(1, 600, ErrorMessage = "Thời lượng phim phải từ 1 đến 600 phút")]
    public int DurationMinutes { get; set; }

    [StringLength(10, ErrorMessage = "Độ tuổi không được vượt quá 10 ký tự")]
    public string? Rated { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
    public string? Description { get; set; }

    public DateTime? ReleaseDate { get; set; }

    [Url(ErrorMessage = "URL poster không hợp lệ")]
    public string? PosterUrl { get; set; }

    [Url(ErrorMessage = "URL backdrop không hợp lệ")]
    public string? BackdropUrl { get; set; }

    [Url(ErrorMessage = "URL trailer không hợp lệ")]
    public string? TrailerUrl { get; set; }

    [StringLength(200, ErrorMessage = "Tên đạo diễn không được vượt quá 200 ký tự")]
    public string? Director { get; set; }

    [StringLength(500, ErrorMessage = "Danh sách diễn viên không được vượt quá 500 ký tự")]
    public string? Actors { get; set; }

    [Required(ErrorMessage = "Danh sách thể loại là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phim phải có ít nhất 1 thể loại")]
    public List<Guid> GenreIds { get; set; } = new();
}

/// <summary>
/// DTO để cập nhật phim
/// </summary>
public class UpdateMovieDto
{
    [Required(ErrorMessage = "Tiêu đề phim là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tiêu đề phim không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [StringLength(200, ErrorMessage = "Tiêu đề gốc không được vượt quá 200 ký tự")]
    public string? OriginalTitle { get; set; }

    [Required(ErrorMessage = "Thời lượng phim là bắt buộc")]
    [Range(1, 600, ErrorMessage = "Thời lượng phim phải từ 1 đến 600 phút")]
    public int DurationMinutes { get; set; }

    [StringLength(10, ErrorMessage = "Độ tuổi không được vượt quá 10 ký tự")]
    public string? Rated { get; set; }

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự")]
    public string? Description { get; set; }

    public DateTime? ReleaseDate { get; set; }

    [Url(ErrorMessage = "URL poster không hợp lệ")]
    public string? PosterUrl { get; set; }

    [Url(ErrorMessage = "URL backdrop không hợp lệ")]
    public string? BackdropUrl { get; set; }

    [Url(ErrorMessage = "URL trailer không hợp lệ")]
    public string? TrailerUrl { get; set; }

    [StringLength(200, ErrorMessage = "Tên đạo diễn không được vượt quá 200 ký tự")]
    public string? Director { get; set; }

    [StringLength(500, ErrorMessage = "Danh sách diễn viên không được vượt quá 500 ký tự")]
    public string? Actors { get; set; }

    [Required(ErrorMessage = "Danh sách thể loại là bắt buộc")]
    [MinLength(1, ErrorMessage = "Phim phải có ít nhất 1 thể loại")]
    public List<Guid> GenreIds { get; set; } = new();
}

/// <summary>
/// DTO để thay đổi trạng thái phim
/// </summary>
public class ChangeMovieStatusDto
{
    [Required(ErrorMessage = "Trạng thái là bắt buộc")]
    public MovieStatus Status { get; set; } = MovieStatus.Draft;
}

/// <summary>
/// DTO để tìm kiếm và lọc phim
/// </summary>
public class MovieSearchDto
{
    [StringLength(100, ErrorMessage = "Từ khóa tìm kiếm không được vượt quá 100 ký tự")]
    public string? Search { get; set; }

    public List<Guid>? GenreIds { get; set; }

    public MovieStatus? Status { get; set; }

    public int? ReleaseYear { get; set; }

    public DateTime? ReleaseDateFrom { get; set; }

    public DateTime? ReleaseDateTo { get; set; }

    [Range(1, 100, ErrorMessage = "Số trang phải từ 1 đến 100")]
    public int Page { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Kích thước trang phải từ 1 đến 50")]
    public int PageSize { get; set; } = 10;

    public string SortBy { get; set; } = "CreatedAt";

    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// DTO để đọc thông tin phim
/// </summary>
public class MovieReadDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? OriginalTitle { get; set; }
    public int DurationMinutes { get; set; }
    public string? Rated { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? TrailerUrl { get; set; }
    public string? Director { get; set; }
    public string? Actors { get; set; }
    public MovieStatus Status { get; set; } = MovieStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<GenreReadDto> Genres { get; set; } = new();
}

/// <summary>
/// DTO để thống kê phim
/// </summary>
public class MovieStatsDto
{
    public int TotalMovies { get; set; }
    public int DraftMovies { get; set; }
    public int ComingSoonMovies { get; set; }
    public int NowShowingMovies { get; set; }
    public int ArchivedMovies { get; set; }
}
