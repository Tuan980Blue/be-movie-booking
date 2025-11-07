using System.ComponentModel.DataAnnotations;
using be_movie_booking.Models;

namespace be_movie_booking.DTOs;

/// <summary>
/// Thông tin khách hàng
/// </summary>
public class CustomerInfoDto
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
}

/// <summary>
/// Thông tin người dùng
/// </summary>
public class UserInfoDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
}

/// <summary>
/// Thông tin suất chiếu
/// </summary>
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

/// <summary>
/// Thông tin ghế
/// </summary>
public class SeatInfoDto
{
    public Guid Id { get; set; }
    public string RowLabel { get; set; } = null!;
    public int SeatNumber { get; set; }
    public SeatType SeatType { get; set; }
}

