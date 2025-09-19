using System.ComponentModel.DataAnnotations;

namespace be_movie_booking.DTOs;

public class UserReadDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string FullName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class UserUpdateMeDto
{
    [Required]
    [StringLength(200)]
    public string FullName { get; set; } = null!;

    [Phone]
    public string? Phone { get; set; }
}

public class PagedResult<T>
{
    public required List<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
}


