namespace be_movie_booking.Models;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
