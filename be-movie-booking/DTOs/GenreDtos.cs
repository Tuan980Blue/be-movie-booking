using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

/// <summary>
/// DTO để đọc thông tin thể loại
/// </summary>
public class GenreReadDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int MovieCount { get; set; } = 0;
}

/// <summary>
/// DTO để tạo thể loại mới
/// </summary>
public class CreateGenreDto
{
    [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
    [StringLength(50, ErrorMessage = "Tên thể loại không được vượt quá 50 ký tự")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// DTO để cập nhật thể loại
/// </summary>
public class UpdateGenreDto
{
    [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
    [StringLength(50, ErrorMessage = "Tên thể loại không được vượt quá 50 ký tự")]
    public string Name { get; set; } = null!;
}

/// <summary>
/// DTO để tạo nhiều thể loại cùng lúc
/// </summary>
public class CreateGenresDto
{
    [Required(ErrorMessage = "Danh sách thể loại là bắt buộc")]
    [MinLength(1, ErrorMessage = "Danh sách thể loại phải có ít nhất 1 phần tử")]
    public List<CreateGenreDto> Genres { get; set; } = new();
}