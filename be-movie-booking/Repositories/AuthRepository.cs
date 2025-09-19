using be_movie_booking.Data;
using be_movie_booking.Models;
using Microsoft.EntityFrameworkCore;

namespace be_movie_booking.Repositories;

public interface IAuthRepository
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
public class AuthRepository : IAuthRepository
{
    private readonly MovieBookingDbContext _db;

    public AuthRepository(MovieBookingDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
    {
        return _db.Users.AnyAsync(u => u.Email == email, ct);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken ct = default)
    {
        return _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _db.Users.AddAsync(user, ct);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }
}
