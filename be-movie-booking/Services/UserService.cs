using be_movie_booking.DTOs;
using be_movie_booking.Models;
using be_movie_booking.Repositories;

namespace be_movie_booking.Services;

public interface IUserService
{
    Task<UserReadDto?> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<UserReadDto?> UpdateMeAsync(Guid userId, UserUpdateMeDto dto, CancellationToken ct = default);
    Task<PagedResult<UserReadDto>> ListUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<UserReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> AssignAdminRoleAsync(Guid userId, CancellationToken ct = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly be_movie_booking.Data.MovieBookingDbContext _db;

    public UserService(IUserRepository users, be_movie_booking.Data.MovieBookingDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<UserReadDto?> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdWithRolesAsync(userId, ct);
        return user == null ? null : MapToReadDto(user);
    }

    public async Task<UserReadDto?> UpdateMeAsync(Guid userId, UserUpdateMeDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return null;

        user.FullName = dto.FullName;
        user.Phone = dto.Phone;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);
        await _db.SaveChangesAsync(ct);

        user = await _users.GetByIdWithRolesAsync(userId, ct);
        if (user == null) return null;
        return MapToReadDto(user);
    }

    public async Task<PagedResult<UserReadDto>> ListUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;
        var (users, total) = await _users.ListAsync(page, pageSize, search, ct);
        var items = users.Select(MapToReadDto).ToList();
        return new PagedResult<UserReadDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<UserReadDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdWithRolesAsync(id, ct);
        return user == null ? null : MapToReadDto(user);
    }

    public async Task<bool> AssignAdminRoleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return false;

        // Check if user already has admin role
        var hasAdminRole = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
        if (hasAdminRole) return true;

        // Add admin role
        var adminRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Admin role ID
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = adminRoleId
        };

        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static UserReadDto MapToReadDto(User user)
    {
        return new UserReadDto
        {
            Id = user.Id,
            Email = user.Email,
            Phone = user.Phone,
            FullName = user.FullName,
            Status = user.Status.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }
}


