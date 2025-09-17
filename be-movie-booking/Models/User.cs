namespace be_movie_booking.Models;

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Banned = 3
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
